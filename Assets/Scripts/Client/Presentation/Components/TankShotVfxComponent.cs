using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Client
{
    // Client VFX bookkeeping: muzzle-flash anchor — the shooting tank entity plus its
    // transform snapshot taken at spawn time; TankShootEffectSystem destroys the flash
    // when the tank moves, rotates or dies while the flash is playing.
    public struct TankShotVfxComponent : IComponentData
    {
        public Entity Tank;
        public float3 TankPosition;
        public int TankDirection;
    }
}
