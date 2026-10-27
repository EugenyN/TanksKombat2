using System;
using TanksKombat2.Shared;
using Unity.Entities;

namespace TanksKombat2.Server
{
    // Blob data for all baked custom maps: cells are already converted to MapTile at bake time.
    [Serializable]
    public struct CustomMapsData
    {
        [NonSerialized] public BlobArray<BlobArray<MapTile>> MapsCellsData;
        [NonSerialized] public BlobArray<MapSizePreset> MapsSizes;
    }

    // Server-side singleton pointing to the blob with all baked custom maps used for level loading.
    public struct CustomMapsComponent : IComponentData
    {
        public BlobAssetReference<CustomMapsData> MapsDataBlob;
    }
}