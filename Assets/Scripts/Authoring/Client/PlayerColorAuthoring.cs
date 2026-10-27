using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Marks the renderer as colorable by player; bakes a URPMaterialPropertyBaseColor set at runtime.
    [DisallowMultipleComponent]
    public class PlayerColorAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerColorAuthoring>
        {
            public override void Bake(PlayerColorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new URPMaterialPropertyBaseColor {Value = 1});
            }
        }
    }
}
