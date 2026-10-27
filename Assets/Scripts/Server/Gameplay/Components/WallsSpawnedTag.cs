using Unity.Entities;

namespace TanksKombat2.Server
{
    // Server-only marker on the level entity: set once WallsSpawnSystem has spawned the
    // wall entities, so later structural changes to the level don't re-spawn them.
    public struct WallsSpawnedTag : IComponentData
    {
    }
}
