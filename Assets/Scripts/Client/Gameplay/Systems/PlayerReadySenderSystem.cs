using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Sends a PlayerReadyRpc when the local human player presses Fire during WaitingForPlayers in the first round.
    // The system is gated by the local player's current Ready flag in PlayerInfoComponent to avoid duplicate RPCs.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerReadySenderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<LocalConnection>();
            state.RequireForUpdate<MatchStateComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();
            if (matchState.State != MatchState.WaitingForPlayers || matchState.Round != 1)
                return;

            // Ignore UI clicks (menus/windows) so they never trigger the ready RPC.
            if (!GameInput.IsPlayerInputAllowed)
                return;

            if (!GameInput.Actions.Player.Shoot.WasPressedThisFrame())
                return;

            if (!SystemAPI.TryGetSingletonEntity<LocalConnection>(out var playerConnection))
                return;
            int localNetworkId = state.EntityManager.GetComponentData<NetworkId>(playerConnection).Value;

            bool alreadyReady = false;
            foreach (var playerInfoComponent in SystemAPI.GetSingletonBuffer<PlayerInfoComponent>(true))
            {
                if (playerInfoComponent.NetworkId == localNetworkId && playerInfoComponent.Ready)
                {
                    alreadyReady = true;
                    break;
                }
            }

            if (alreadyReady)
                return;

            var rpc = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(rpc, default(PlayerReadyRpc));
            state.EntityManager.AddComponentData(rpc,
                new SendRpcCommandRequest { TargetConnection = playerConnection });
        }
    }
}