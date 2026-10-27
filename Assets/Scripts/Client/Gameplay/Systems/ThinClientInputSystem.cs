using System;
using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Thin client input: synthesizes TankPlayerInput to simulate a player for load
    // testing. Every 500-1000 ms one random InputEvent is set for a single tick
    // (0-3 move directions, 4 shoot); all other ticks the input stays default.
    // Without [BurstCompile] this ISystem runs on Mono (managed-safe).
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct ThinClientInputSystem : ISystem
    {
        private Unity.Mathematics.Random _random;
        private double _nextActionTime;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<MatchConfigComponent>();

            _random = Unity.Mathematics.Random.CreateFromIndex((uint)Environment.TickCount);
        }

        private void CreateThinClientInputEntity(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingletonEntity<LocalConnection>(out var connectionEntity))
                return;
            var connectionId = SystemAPI.GetComponent<NetworkId>(connectionEntity).Value;

            var entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<TankPlayerInput>(entity);
            state.EntityManager.AddComponent<InputBufferData<TankPlayerInput>>(entity);
            state.EntityManager.AddComponentData(entity, new GhostOwner { NetworkId = connectionId });

            SystemAPI.SetSingleton(new CommandTarget { targetEntity = entity });
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<CommandTarget>(out var commandTarget) &&
                commandTarget.targetEntity == Entity.Null)
                CreateThinClientInputEntity(ref state);

            var input = default(TankPlayerInput);
            var time = SystemAPI.Time.ElapsedTime;
            if (time >= _nextActionTime)
            {
                _nextActionTime = time + _random.NextInt(500, 1001) / 1000.0;

                switch (_random.NextInt(5)) // 0-3: move, 4: shoot
                {
                    case 0: input.MoveLeft.Set(); break;
                    case 1: input.MoveRight.Set(); break;
                    case 2: input.MoveUp.Set(); break;
                    case 3: input.MoveDown.Set(); break;
                    case 4: input.Shoot.Set(); break;
                }
            }

            foreach (var playerInput in SystemAPI.Query<RefRW<TankPlayerInput>>())
                playerInput.ValueRW = input;
        }
    }
}
