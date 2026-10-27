using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client presentation: reacts to server BulletHitRpc events — spawns the explosion VFX
    // at the hit position and plays the matching impact sound depending on HitKind.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct BulletHitEffectSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
            state.RequireForUpdate<BulletHitRpc>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<BulletHitRpc>>().WithAll<ReceiveRpcCommandRequest>()
                         .WithEntityAccess())
            {
                var explosionPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().Explosion;
                var explosion = ecb.Instantiate(explosionPrefab);
                var position = rpc.ValueRO.Position;
                ecb.SetComponent(explosion, LocalTransform.FromPosition(position));
                ecb.DestroyEntity(entity);
                
                switch (rpc.ValueRO.Kind)
                {
                    case HitKind.TankDamage:
                        AudioSystem.PlayAudio(ecb, AudioClipKeys.TankDamage);
                        break;
                    case HitKind.TankDestroy:
                        AudioSystem.PlayAudio(ecb, AudioClipKeys.TankDestroy);
                        break;
                    case HitKind.BonusDestroy:
                        AudioSystem.PlayAudio(ecb, AudioClipKeys.BonusDestroy);
                        break;
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
