using Unity.Mathematics;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Custom map asset: fixed logical size (MapSize) + a grid of MapTile cells.
    // Created/edited by the map editor (editor-time), consumed by CustomMapsAuthoring
    // which bakes cells into a blob for the server. Size is immutable after creation.
    public class CustomMapConfig : ScriptableObject
    {
        [SerializeField] private MapSizePreset _size;
        [SerializeField] private MapTile[] _cells;

        public MapSizePreset Size => _size;
        public MapTile[] Cells => _cells;

        public static CustomMapConfig Create(MapSizePreset size, int2 dimensions)
        {
            var config = CreateInstance<CustomMapConfig>();

            config._size = size;
            config._cells = new MapTile[dimensions.x * dimensions.y];

            return config;
        }
    }
}