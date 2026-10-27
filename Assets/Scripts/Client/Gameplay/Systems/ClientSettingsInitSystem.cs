using Unity.Entities;

namespace TanksKombat2.Client
{
    // One-shot bridge: as soon as the SubScene-streamed ClientSettingsComponent singleton appears,
    // applies client player preferences (GameSettings) into it, then disables itself.
    // Runs before TankHealthBarSystem so health bars spawn with the correct preference.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TankHealthBarSystem))]
    public partial class ClientSettingsInitSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<ClientSettingsComponent>();
        }

        protected override void OnUpdate()
        {
            ApplyHealthBars();
            Enabled = false;
        }
        
        public void ApplyHealthBars()
        {
            var settings = SystemAPI.GetSingletonRW<ClientSettingsComponent>();
            settings.ValueRW.HealthBars = GameSettings.Instance.HealthBars;
        }
    }
}
