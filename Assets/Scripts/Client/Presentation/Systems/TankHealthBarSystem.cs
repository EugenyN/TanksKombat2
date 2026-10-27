using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client presentation: spawns and updates floating health bars above tanks, following each tank's
    // transform. Writes LocalTransform (resolved to LocalToWorld by TransformSystemGroup) plus a
    // PostTransformMatrix carrying the non-uniform bar width.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    public partial struct TankHealthBarSystem : ISystem
    {
        private const float BarHeight = 1f / 16f;
        private const float OneLiveWidth = 1 / 16f;
        private const float YOffset = BarHeight * 8 + BarHeight / 2;

        private Entity _healthBarPrefab;
        private EntityQuery _healthBarQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
            state.RequireForUpdate<ClientSettingsComponent>();
            _healthBarQuery = state.GetEntityQuery(ComponentType.ReadOnly<HealthBarComponent>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<ClientSettingsComponent>();
            if (!settings.HealthBars)
            {
                if (!_healthBarQuery.IsEmptyIgnoreFilter)
                    state.EntityManager.DestroyEntity(_healthBarQuery);
                return;
            }

            if (_healthBarPrefab == Entity.Null)
                _healthBarPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().HealthBar;

            var tanksWithBar = new NativeHashSet<Entity>(16, Allocator.Temp);
            foreach (var bar in SystemAPI.Query<RefRO<HealthBarComponent>>())
                tanksWithBar.Add(bar.ValueRO.Tank);

            var tankTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var tankStateLookup = SystemAPI.GetComponentLookup<TankStateComponent>(true);

            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (tankTransform, tankState, tank) in
                     SystemAPI.Query<RefRO<LocalTransform>, RefRO<TankStateComponent>>().WithEntityAccess())
            {
                if (tanksWithBar.Contains(tank))
                    continue;

                var bar = ecb.Instantiate(_healthBarPrefab);
                ecb.AddComponent(bar, new HealthBarComponent { Tank = tank });

                var (pos, scale) = ComputeBarTransform(tankTransform.ValueRO.Position, tankState.ValueRO.Lives);
                ecb.SetComponent(bar, LocalTransform.FromPosition(pos));
                ecb.AddComponent(bar, new PostTransformMatrix { Value = float4x4.Scale(scale) });
            }

            foreach (var (barData, barTransform, barScale, healthBar) in
                     SystemAPI.Query<RefRW<HealthBarComponent>, RefRW<LocalTransform>, RefRW<PostTransformMatrix>>()
                         .WithEntityAccess())
            {
                var tank = barData.ValueRO.Tank;
                if (!state.EntityManager.Exists(tank) || !tankTransformLookup.HasComponent(tank) ||
                    !tankStateLookup.HasComponent(tank))
                {
                    ecb.DestroyEntity(healthBar);
                    continue;
                }

                var (pos, scale) = ComputeBarTransform(tankTransformLookup[tank].Position, tankStateLookup[tank].Lives);
                barTransform.ValueRW.Position = pos;
                barScale.ValueRW.Value = float4x4.Scale(scale);
            }

            ecb.Playback(state.EntityManager);
            tanksWithBar.Dispose();
        }

        private static (float3 pos, float3 scale) ComputeBarTransform(float3 tankPos, byte lives)
        {
            float width = lives * OneLiveWidth;
            var pos = new float3(tankPos.x - 0.5f + width / 2, tankPos.y + YOffset, 0f);
            var scale = new float3(width, BarHeight, 1f);
            return (pos, scale);
        }
    }
}
