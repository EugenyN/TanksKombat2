using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Client-side prediction of the server's PlayerDisconnectSystem: when the local client's own
    // connection drops, no further ghost updates arrive, so mark the local player's entry in the
    // ghosted player-info buffer as Disconnected locally to keep the scoreboard up to date.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ClientLocalDisconnectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var evt in SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick)
            {
                // Id 0 means the connection never finished the handshake, so the server never
                // assigned it a NetworkId; real network ids start at 1. Skip such events.
                if (evt.State == ConnectionState.State.Disconnected && evt.Id.Value != 0)
                {
                    if (SystemAPI.TryGetSingletonBuffer<PlayerInfoComponent>(out var playerInfoBuffer))
                    {
                        for (int i = 0; i < playerInfoBuffer.Length; i++)
                        {
                            ref var playerInfo = ref playerInfoBuffer.ElementAt(i);
                            if (playerInfo.NetworkId == evt.Id.Value)
                            {
                                playerInfo.Disconnected = true;
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
