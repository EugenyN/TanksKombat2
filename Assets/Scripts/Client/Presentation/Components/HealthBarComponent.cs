using Unity.Entities;

namespace TanksKombat2.Client
{
    // Client presentation: links a floating health bar to the tank whose lives it displays.
    public struct HealthBarComponent : IComponentData
    {
        public Entity Tank;
    }
}
