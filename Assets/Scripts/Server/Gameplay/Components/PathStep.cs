using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Server
{
    // Server-only buffer holding the grid cells of a bot's current path, ordered from start (index 0) to goal.
    public struct PathStep : IBufferElementData
    {
        public int2 Cell;
    }
}
