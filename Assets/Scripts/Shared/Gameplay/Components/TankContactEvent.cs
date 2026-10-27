using Unity.Entities;

namespace TanksKombat2.Shared
{
    // One-shot event buffered when a tank physically touches another entity (tank-vs-tank/obstacle contact).
    public struct TankContactEvent : IBufferElementData
    {
        public Entity Tank;
        public Entity Target;
        public int OwnerNetworkId;
    }
}