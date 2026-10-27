using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client effects: ages one-shot visual effects and destroys them when their
    // VisualEffectLifetime expires. Runs in the client SimulationSystemGroup right after
    // the effect spawner systems (see BulletHitEffectSystem comment).
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(TankShootEffectSystem))]
    [UpdateAfter(typeof(BulletHitEffectSystem))]
    public partial struct VisualEffectLifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<VisualEffectLifetime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<VisualEffectLifetime>>().WithEntityAccess())
            {
                lifetime.ValueRW.Value -= deltaTime;
                if (lifetime.ValueRO.Value <= 0f)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }
    }
}
