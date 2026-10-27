using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace TanksKombat2.Client
{
    [MaterialProperty("_BaseMap_ST")]
    public struct MaterialPropertyBaseMap : IComponentData
    {
        public float4 Value;
    }
}