using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // Palette slot of the tank's owner, copied by the server from the owner's
    // PlayerInfoComponent entry at spawn time. Lets the client tint the tank
    // instantly from its own ghost data without looking up the player-info buffer.
    public struct TankColorComponent : IComponentData
    {
        [GhostField] public int ColorIndex;
    }
}