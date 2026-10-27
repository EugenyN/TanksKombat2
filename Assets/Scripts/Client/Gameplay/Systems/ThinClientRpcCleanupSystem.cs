using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Thin-client worlds have no presentation systems, so broadcast gameplay RPCs
    // (TankShotRpc, BulletHitRpc, PlayerJoinedRpc, ...) would accumulate forever.
    // This system destroys everything the thin client cannot consume.
    // LoadLevelRpc is skipped because ClientLevelLoadSystem consumes it (and destroys
    // it via the begin-simulation ECB one frame later).
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ThinClientRpcCleanupSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Received server -> thin client RPCs that no thin client system consumes.
            foreach (var entity in SystemAPI.QueryBuilder()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithNone<LoadLevelRpc>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                ecb.DestroyEntity(entity);
            }

            // Local (non-networked) event entities created by the predicted sim in thin
            // clients (they have no SendRpcCommandRequest and no consumer here).
            foreach (var entity in SystemAPI.QueryBuilder()
                         .WithAll<TankShotRpc>()
                         .WithNone<ReceiveRpcCommandRequest>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                ecb.DestroyEntity(entity);
            }

            foreach (var entity in SystemAPI.QueryBuilder()
                         .WithAll<TakeBonusRpc>()
                         .WithNone<ReceiveRpcCommandRequest>()
                         .Build()
                         .ToEntityArray(Allocator.Temp))
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
