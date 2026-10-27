using Unity.Collections;
using Unity.Mathematics;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C RPC: server sends the resolved match config and full level tile data so the client can build the map.
    public struct LoadLevelRpc : IRpcCommand
    {
        public MatchConfigComponent MatchConfig;

        [GhostFixedListCapacity(Capacity = 988)]
        public FixedList4096Bytes<MapTile> LevelData;

        public int2 LevelSize;
    }
}