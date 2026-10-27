using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Server
{
    // Registers joined players into the ghosted PlayerInfoComponent buffer on the MatchState
    // singleton, and broadcasts PlayerJoinedRpc to all in-game clients. Runs after NetCode
    // assigns NetworkId (post-approval), so both JoinedPlayerComponent and NetworkId are
    // guaranteed present on the connection entity.
    // NOTE: reconnection is NOT supported: a rejoining player gets a fresh NetworkId and is
    // registered as a brand-new entry (WinsCount reset; the old entry stays Disconnected).
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateBefore(typeof(ServerLevelLoadSendSystem))]
    public partial struct PlayerRegistrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            var matchStateEntity = SystemAPI.GetSingletonEntity<MatchStateComponent>();
            var playerInfoBuffer = SystemAPI.GetBuffer<PlayerInfoComponent>(matchStateEntity);

            foreach (var (joinedPlayer, networkId, connectionEntity)
                     in SystemAPI.Query<RefRO<JoinedPlayerComponent>, RefRO<NetworkId>>().WithEntityAccess())
            {
                int nid = networkId.ValueRO.Value;

                bool alreadyRegistered = false;
                for (int i = 0; i < playerInfoBuffer.Length; i++)
                {
                    if (playerInfoBuffer[i].NetworkId == nid)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (!alreadyRegistered)
                {
                    bool isSpectator = SystemAPI.HasComponent<SpectatorTag>(connectionEntity);
                    bool isHost = SystemAPI.HasComponent<HostPlayerTag>(connectionEntity);
                    int colorIndex = PickFreeColorIndex(playerInfoBuffer);

                    playerInfoBuffer.Add(new PlayerInfoComponent
                    {
                        NetworkId = nid,
                        PlayerName = joinedPlayer.ValueRO.PlayerName,
                        WinsCount = 0,
                        Ready = ConnectionUtils.IsBotNetworkId(nid) || (isSpectator && !isHost),
                        Spectator = isSpectator,
                        IsHost = isHost,
                        ColorIndex = colorIndex
                    });

                    // Broadcast join notification to all in-game clients.
                    var joinRpc = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(joinRpc, new PlayerJoinedRpc
                    {
                        NetworkId = nid,
                        PlayerName = joinedPlayer.ValueRO.PlayerName,
                        ColorIndex = colorIndex
                    });
                    commandBuffer.AddComponent(joinRpc, new SendRpcCommandRequest());
                }
            }
        }

        // Picks the smallest palette slot not held by an active (non-disconnected) participant.
        private static int PickFreeColorIndex(DynamicBuffer<PlayerInfoComponent> playerInfoBuffer)
        {
            int colorIndex = 0;
            while (true)
            {
                bool taken = false;
                for (int i = 0; i < playerInfoBuffer.Length; i++)
                {
                    if (!playerInfoBuffer[i].Disconnected && playerInfoBuffer[i].ColorIndex == colorIndex)
                    {
                        taken = true;
                        break;
                    }
                }

                if (!taken)
                    break;
                colorIndex++;
            }

            return colorIndex;
        }
    }
}
