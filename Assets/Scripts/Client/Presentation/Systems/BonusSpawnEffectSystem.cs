using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client presentation: plays the bonus-spawn sound when the server broadcasts BonusSpawnedRpc.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct BonusSpawnEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (_, entity) in SystemAPI.Query<RefRO<BonusSpawnedRpc>>()
                         .WithAll<ReceiveRpcCommandRequest>()
                         .WithNone<SendRpcCommandRequest>()
                         .WithEntityAccess())
            {
                AudioSystem.PlayAudio(ecb, AudioClipKeys.BonusSpawn);
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
