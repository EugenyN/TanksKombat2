using Unity.Entities;

namespace TanksKombat2.Server
{
    // Marks a connection whose tank has already been spawned for the current round by TankSpawnSystem.
    public struct TankSpawnedTag : IComponentData
    {
    }
}
