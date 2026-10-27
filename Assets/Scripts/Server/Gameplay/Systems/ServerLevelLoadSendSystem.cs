using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Server
{
    // On first update per real client connection (before any LevelSentTag is set), sends the
    // level snapshot (LoadLevelRpc RPC) and marks the connection with LevelSentTag.
    // Bots are excluded from the query via BotConnectionTag — they have no network stream.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(ServerLevelLoadSystem))]
    public partial struct ServerLevelLoadSendSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchConfigComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var matchConfig = SystemAPI.GetSingleton<MatchConfigComponent>();
            var levelDataComponent = SystemAPI.GetSingleton<LevelDataComponent>();

            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (networkId, playerConnection) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithNone<LevelSentTag, BotConnectionTag>()
                         .WithEntityAccess())
            {
                var req = commandBuffer.CreateEntity();
                commandBuffer.AddComponent(req, new LoadLevelRpc
                {
                    MatchConfig = matchConfig,
                    LevelData = levelDataComponent.LevelData,
                    LevelSize = levelDataComponent.Size
                });
                commandBuffer.AddComponent(req, new SendRpcCommandRequest { TargetConnection = playerConnection });
                commandBuffer.AddComponent<LevelSentTag>(playerConnection);

                Debug.Log($"Sending LoadLevelRpc to client with ID:{networkId.ValueRO.Value}");
            }
        }
    }
}