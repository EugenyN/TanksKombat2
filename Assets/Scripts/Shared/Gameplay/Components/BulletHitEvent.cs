using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    // One-shot event buffered on bullet entities when a bullet hits something (server simulation).
    public struct BulletHitEvent : IBufferElementData
    {
        public int OwnerNetworkId;
        public Entity Target;
        public float3 Position;
    }
}