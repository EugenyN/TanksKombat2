using Unity.Collections;
using Unity.Entities;

namespace TanksKombat2.Server
{
    // Server-only spawn/navigation data: cell indices of the largest reachable
    // region of the level. Split from LevelDataComponent so the per-frame-copied
    // level struct stays lean; clients never receive it.
    public struct LevelReachableCellsComponent : IComponentData
    {
        public FixedList4096Bytes<ushort> Cells;
    }
}