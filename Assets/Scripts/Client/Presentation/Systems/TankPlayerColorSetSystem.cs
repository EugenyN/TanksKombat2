using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TanksKombat2.Client
{
    // Tints tanks with the palette color of their owner. The color slot travels on the
    // tank ghost itself (TankColorComponent, copied from the owner's score entry by the
    // server), so the color applies on the frame the ghost is created.
    [BurstCompile]
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct TankPlayerColorSetSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var settings = SystemAPI.GetSingleton<ClientSettingsComponent>();
            foreach (var (color, tankColor) in SystemAPI
                         .Query<RefRW<URPMaterialPropertyBaseColor>, RefRO<TankColorComponent>>()
                         .WithChangeFilter<TankColorComponent>())
            {
                var c = settings.GetColor(tankColor.ValueRO.ColorIndex).linear;
                color.ValueRW.Value = new float4(c.r, c.g, c.b, c.a);
            }
        }
    }
}
