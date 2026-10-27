using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Creates N fake bot "connections" on the server, where N is MatchConfigComponent.BotCount
    // (clamped to MaxBots). Bot connections persist across rounds; the system restarts whenever
    // the Level singletons are (re)created, so OnStartRunning re-runs every round and recreates
    // any missing connections.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(ServerLevelLoadSystem))]
    [UpdateBefore(typeof(PlayerRegistrationSystem))]
    public partial struct BotConnectionsCreateSystem : ISystem, ISystemStartStop
    {
        private EntityQuery _botConnectionQuery;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchStateComponent>();
            state.RequireForUpdate<MatchConfigComponent>();

            _botConnectionQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<BotConnectionTag>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            var entityManager = state.EntityManager;

            var config = SystemAPI.GetSingleton<MatchConfigComponent>();
            int desired = math.clamp(config.BotCount, 0, ConnectionUtils.MaxSessionPlayers - 1);
            if (desired != config.BotCount)
                Debug.LogWarning($"MatchConfig.BotCount={config.BotCount} is out of range.");

            // Create missing bot connections (idempotent across rounds — existing bots persist).
            int existing = _botConnectionQuery.CalculateEntityCount();
            for (int i = existing; i < desired; i++)
            {
                var conn = entityManager.CreateEntity();
                entityManager.SetName(conn, $"BotConnection{ConnectionUtils.BotNetworkIdBase + i}");
                entityManager.AddComponentData(conn, new NetworkId { Value = ConnectionUtils.BotNetworkIdBase + i });
                entityManager.AddComponentData(conn, new JoinedPlayerComponent { PlayerName = $"Bot {i + 1}" });
                entityManager.AddComponentData(conn, new CommandTarget());
                entityManager.AddBuffer<LinkedEntityGroup>(conn);
                entityManager.AddComponentData(conn, new BotConnectionTag());

                Debug.Log($"Created bot connection 'Bot {i + 1}' (NetworkId={ConnectionUtils.BotNetworkIdBase + i}).");
            }
        }

        public void OnStopRunning(ref SystemState state)
        { }
    }
}
