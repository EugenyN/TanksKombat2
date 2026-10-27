using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;

namespace TanksKombat2.Client
{
    // Client presentation: blinks temporarily invulnerable tanks by oscillating the base-color
    // alpha; restores full opacity once InvulnerabilityTimer reaches zero.
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct TankBlinkingSystem : ISystem
    {
        private const float TransparentAlpha = 0.2f;
        private const float BlinkPeriod = 0.2f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TankStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (baseColor, tankState) in SystemAPI
                         .Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<TankStateComponent>>())
            {
                ref var color = ref baseColor.ValueRW.Value;
                    
                float alpha;
                if (tankState.ValueRO.InvulnerabilityTimer > 0)
                    alpha = SystemAPI.Time.ElapsedTime % BlinkPeriod < BlinkPeriod / 2 ? 1.0f : TransparentAlpha;
                else
                    alpha = 1;

                if (color.w != alpha)
                    color.w = alpha;
            }
        }
    }
}