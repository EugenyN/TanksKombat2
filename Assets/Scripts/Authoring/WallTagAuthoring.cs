using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Marks the entity as a wall blocking tank movement and bullets; bakes a WallTagComponent.
    public class WallTagAuthoring : MonoBehaviour
    {
        private class Baker : Baker<WallTagAuthoring>
        {
            public override void Bake(WallTagAuthoring authoring)
            {
                var component = default(WallTagComponent);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}