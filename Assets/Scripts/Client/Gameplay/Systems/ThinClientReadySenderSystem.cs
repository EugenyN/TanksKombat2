using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Thin clients cannot read hardware input, so they auto-send PlayerReadyRpc once per LoadLevelRpc received.
    // The [UpdateAfter] ordering guarantees NetworkStreamInGame (added by ClientConnectionApprovalSystem) is present
    // in the same frame the first LoadLevelRpc becomes consumable, so the first pulse is never missed.
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    [UpdateAfter(typeof(ClientConnectionApprovalSystem))]
    public partial struct ThinClientReadySenderSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<LocalConnection>();
            state.RequireForUpdate<LoadLevelRpc>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rpc = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(rpc, default(PlayerReadyRpc));
            state.EntityManager.AddComponentData(rpc, new SendRpcCommandRequest { TargetConnection =
                SystemAPI.GetSingletonEntity<LocalConnection>() });
        }
    }
}