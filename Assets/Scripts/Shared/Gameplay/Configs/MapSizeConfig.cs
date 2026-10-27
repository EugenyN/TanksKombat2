using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Single editable source of truth for logical map dimensions (in tiles) per MapSize.
    // The server consumes baked copies via the unmanaged ServerMapSizesComponent (see ServerSettingsAuthoring.Baker)
    [CreateAssetMenu(fileName = "MapSizesConfig", menuName = "TanksKombat2/Map Sizes Config")]
    public class MapSizeConfig : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Configs/MapSizesConfig.asset";

        [SerializeField] public Dictionary<MapSizePreset, int2> MapSizes = new()
        {
            { MapSizePreset.Small, new int2(10, 15) },
            { MapSizePreset.Large, new int2(19, 13) },
            { MapSizePreset.ExtraLarge, new int2(38, 26) }
        };
    }
}