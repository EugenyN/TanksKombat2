using Unity.Mathematics;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    public enum HitKind
    {
        TankDamage,
        TankDestroy,
        BonusDestroy
    }

    // S2C event RPC: a bullet impacted (tank damage/destroy or bonus destroyed); drives client hit VFX.
    public struct BulletHitRpc : IRpcCommand
    {
        public float3 Position;
        public HitKind Kind;
    }
}