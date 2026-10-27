using System;
using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TanksKombat2.Server
{
    // Spawns a tank once per round for every connection that joined as a player
    // (JoinedPlayerComponent, not SpectatorTag) and has not been assigned a tank this round
    // (WithNone<TankSpawnedTag>). Requires the level to exist (RequireForUpdate<LevelDataComponent>);
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(PlayerRegistrationSystem))]
    public partial struct TankSpawnSystem : ISystem
    {
        private EntityQuery _tankQuery;
        private EntityQuery _spawnableConnectionQuery;
        private Entity _tankPrefab;
        private Random _random;
        
        public void OnCreate(ref SystemState state)
        {
            _tankQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, TankStateComponent>()
                .Build(ref state);

            _spawnableConnectionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, JoinedPlayerComponent>()
                .WithNone<SpectatorTag, TankSpawnedTag>()
                .Build(ref state);

            state.RequireForUpdate<LevelDataComponent>();
            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<MatchStateComponent>();
            state.RequireForUpdate<GamePrefabsComponent>();

            _random = Random.CreateFromIndex((uint)Environment.TickCount);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Snapshot connections first: spawning performs structural changes
            // (Instantiate, AddBuffer), which must not run inside a query iterator.
            using var connections = _spawnableConnectionQuery.ToEntityArray(Allocator.Temp);

            for (int i = 0; i < connections.Length; i++)
                SpawnTank(ref state, connections[i]);
        }

        [BurstCompile]
        private void SpawnTank(ref SystemState state, Entity playerConnection)
        {
            var entityManager = state.EntityManager;
            
            if (!SystemAPI.TryGetComponent<CommandTarget>(playerConnection, out var commandTarget))
            {
                Debug.LogError($"Connection {playerConnection} has no CommandTarget.");
                return;
            }

            if (commandTarget.targetEntity != Entity.Null)
            {
                Debug.LogError($"Connection {playerConnection} has a tank ({commandTarget.targetEntity}) but no TankSpawnedTag.");
                return;
            }

            if (!SystemAPI.TryGetComponent<NetworkId>(playerConnection, out var networkId))
            {
                Debug.LogError($"Connection {playerConnection} has no NetworkId.");
                return;
            }

            var levelData = SystemAPI.GetSingleton<LevelDataComponent>();
            var reachableCellsComponent = SystemAPI.GetSingleton<LevelReachableCellsComponent>();

            using var avoidTransforms = _tankQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            if (!reachableCellsComponent.TryFindSpawnPosition(levelData.Size, ref _random, avoidTransforms, out var validTankPos))
            {
                Debug.LogWarning($"Could not find valid spawn position for connection {playerConnection}.");
                return;
            }

            var matchConfig = SystemAPI.GetSingleton<MatchConfigComponent>();

            if (_tankPrefab == Entity.Null)
                _tankPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().Tank;
            var tank = entityManager.Instantiate(_tankPrefab);

            entityManager.SetComponentData(tank, LocalTransform.FromPositionRotation(
                LevelHelper.GridToWorld(validTankPos), quaternion.RotateZ(math.radians(90f) * _random.NextInt(0, 4))));

            entityManager.SetComponentData(tank, new GhostOwner { NetworkId = networkId.Value });
            entityManager.SetComponentData(tank, new PlayerIdComponent { PlayerEntity = playerConnection });

            var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
            var playerInfo = playerInfoBuffer.GetPlayerInfo(networkId.Value);
            entityManager.AddComponentData(tank, new TankColorComponent { ColorIndex = playerInfo.ColorIndex });
            entityManager.SetComponentData(tank, new TankStateComponent
            {
                Lives = matchConfig.GameModeConfig.Lives, Bullets = matchConfig.GameModeConfig.Ammo
            });

            entityManager.GetBuffer<LinkedEntityGroup>(playerConnection).Add(new LinkedEntityGroup { Value = tank });
            entityManager.SetComponentData(playerConnection, new CommandTarget { targetEntity = tank });

            // Tag bot tanks server-side so BotAiSystem drives their behavior. Real players have
            // no BotConnectionTag on their connection and are skipped here.
            if (SystemAPI.HasComponent<BotConnectionTag>(playerConnection))
            {
                var botAiConfig = SystemAPI.GetSingleton<ServerGameplayConfigComponent>().BotAiConfig;
                // Bot tanks get a path buffer so BotMovementSystem can stream
                // path steps into their TankPlayerInput.
                entityManager.AddBuffer<PathStep>(tank);

                entityManager.AddComponentData(tank, new BotComponent
                {
                    AiSpeed = botAiConfig.AiSpeed,
                    AiTimer = botAiConfig.AiSpeed,
                    MoveStepDelay = botAiConfig.MoveStepDelay,
                    MoveStepDelayRandom = botAiConfig.MoveStepDelayRandom,
                    Rand = new Random((uint)networkId.Value),
                    TargetEntity = Entity.Null,
                    TargetCell = new int2(-1, -1),
                    State = BotAiState.Undefined
                });

                // Movement requests start disabled; BotAiSystem enables one when it
                // wants BotMovementSystem to (re)build a path or stop the bot.
                entityManager.AddComponentData(tank, new BotMoveRequest());
                entityManager.SetComponentEnabled<BotMoveRequest>(tank, false);
            }

            // Mark the connection as having its tank for this round. Added only on success —
            // a failed spawn-position search leaves the connection untagged so the system
            // retries next frame.
            entityManager.AddComponent<TankSpawnedTag>(playerConnection);
        }
    }
}
