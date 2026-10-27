using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Server authority: watches NetCode connection events and marks the matching PlayerInfoComponent
    // entry as Disconnected in the ghosted player-info buffer, then broadcasts PlayerDisconnectedRpc
    // so clients can update the scoreboard/event log.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlayerDisconnectSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var connectionEventsForTick = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
            if (connectionEventsForTick.Length == 0)
                return;
            
            if (!SystemAPI.TryGetSingletonBuffer(out DynamicBuffer<PlayerInfoComponent> playerInfoBuffer))
                return;

            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            
            foreach (var evt in connectionEventsForTick)
            {
                if (evt.State != ConnectionState.State.Disconnected)
                    continue;

                Debug.Log($"[Server] Player {evt.Id.Value} disconnected with reason {evt.DisconnectReason}!");

                if (playerInfoBuffer.TryGetPlayerInfoIndex(evt.Id.Value, out var playerInfoIndex))
                {
                    ref var playerInfo = ref playerInfoBuffer.ElementAt(playerInfoIndex);

                    if (playerInfo.Disconnected) continue;
                    
                    playerInfo.Disconnected = true;

                    var rpc = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(rpc, new PlayerDisconnectedRpc
                    {
                        NetworkId = evt.Id.Value, PlayerName = playerInfo.PlayerName,
                        ColorIndex = playerInfo.ColorIndex
                    });
                    commandBuffer.AddComponent(rpc, new SendRpcCommandRequest());
                }
            }
        }
    }
}