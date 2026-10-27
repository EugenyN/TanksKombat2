using System;
using System.Collections.Generic;
using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Custom map configs per map id; baked into a single blob asset consumed by the server level loader.
    [DisallowMultipleComponent]
    public class CustomMapsAuthoring : MonoBehaviour
    {
        [Tooltip("Map cell layouts and sizes per custom map id.")]
        [SerializeField] public Dictionary<CustomMap, CustomMapConfig> CustomMaps = new();

        private class Baker : Baker<CustomMapsAuthoring>
        {
            public override void Bake(CustomMapsAuthoring authoring)
            {
                var mapsBlob = CreateMapsDataBlob(authoring);

                AddBlobAsset(ref mapsBlob, out var hash);

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CustomMapsComponent { MapsDataBlob = mapsBlob });
            }

            private BlobAssetReference<CustomMapsData> CreateMapsDataBlob(CustomMapsAuthoring authoring)
            {
                var builder = new BlobBuilder(Allocator.Temp);

                ref var mapsData = ref builder.ConstructRoot<CustomMapsData>();

                var mapsCount = Enum.GetValues(typeof(CustomMap)).Length;

                var mapsCellsDataArrayBuilder = builder.Allocate(ref mapsData.MapsCellsData, mapsCount);
                var mapSizesBuilder = builder.Allocate(ref mapsData.MapsSizes, mapsCount);
                for (int i = 0; i < mapsCellsDataArrayBuilder.Length; i++)
                {
                    var config = authoring.CustomMaps.GetValueOrDefault((CustomMap)i);

                    var mapCellsArrayBuilder = builder.Allocate(ref mapsCellsDataArrayBuilder[i], config.Cells.Length);
                    for (int j = 0; j < config.Cells.Length; j++)
                        mapCellsArrayBuilder[j] = config.Cells[j];

                    mapSizesBuilder[i] = config.Size;
                }

                var result = builder.CreateBlobAssetReference<CustomMapsData>(Allocator.Persistent);

                builder.Dispose();

                return result;
            }
        }
    }
}