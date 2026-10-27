using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // Networked tank runtime state (lives, ammo, invulnerability) simulated by server, synced to clients.
    public struct TankStateComponent : IComponentData
    {
        [GhostField] public byte Lives;
        [GhostField] public byte Bullets;
        [GhostField] public float InvulnerabilityTimer;
        [GhostField] public bool IsShooting;
    }
}