using Unity.Entities;
using Unity.Rendering;

namespace TanksKombat2.Client
{
    [MaterialProperty("_Frame")]
    public struct MaterialPropertyFrame : IComponentData
    {
        public float Value;
    }
}