using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client effects: plays the bonus-pickup sound. Consumes both local event
    // entities created by the predicted TankBonusPickupSystem (no ReceiveRpcCommandRequest)
    // and server echoes for other players; skips own echo (sound already played locally).
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct TakeBonusEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
            state.RequireForUpdate<TakeBonusRpc>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<TakeBonusRpc>>()
                         .WithNone<SendRpcCommandRequest>()
                         .WithEntityAccess())
            {
                // skip echo of own bonus take returned by server (already played locally)
                if (RpcUtils.IsEchoRpc(ref state, entity, rpc.ValueRO.OwnerNetworkId))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                AudioSystem.PlayAudio(ecb, AudioClipKeys.TakeBonus);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
