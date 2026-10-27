using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace TanksKombat2.Server
{
    // Unmanaged singleton with baked logical map dimensions (in tiles) per MapSize,
    // filled by ServerSettingsAuthoring.Baker from the MapSizeConfig asset at bake time.
    public struct ServerMapSizesComponent : IComponentData
    {
        public FixedList64Bytes<int2> MapSizes;
        public int2 Get(MapSizePreset size) => MapSizes[(int)size];
    }
}