using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    public enum MapTile : byte
    {
        Background1,
        Background2,
        Background3,
        Wall1,
        Wall2,
        Wall3,
    }

    // Static level grid shared by server and clients: map size + tile data. Kept lean (tiles only) because it is
    // copied by value every frame into collision worlds and jobs
    public struct LevelDataComponent : IComponentData
    {
        public int2 Size;
        public FixedList4096Bytes<MapTile> LevelData;
    }
}