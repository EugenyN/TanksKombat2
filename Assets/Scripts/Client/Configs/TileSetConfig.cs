using System.Collections.Generic;
using TanksKombat2.Shared;
using Unity.Mathematics;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Themed tileset atlas layout: grid dimensions, tile pixel size and the atlas
    // texture itself. The client reads this asset directly via the UnityObjectRef in
    // ClientSettingsComponent; the map editor reads the asset as well.
    [CreateAssetMenu(fileName = "TileSetConfig", menuName = "TanksKombat2/Tile Set Config")]
    public class TileSetConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Configs/TileSetConfig.asset";

        public int Columns = 6;
        public int Rows = 8;
        public int TilePixelSize = 16;
        public Texture2D Atlas;

        [SerializeField]
        public Dictionary<MapSizePreset, int> MapTileSizes = new ()
        {
            { MapSizePreset.Small, 32 }, { MapSizePreset.Large, 32 }, { MapSizePreset.ExtraLarge, 16 }
        };
        
        public static int GetThemeRow(MapTheme theme) => math.max((int)theme - 1, 0);
    }
}
