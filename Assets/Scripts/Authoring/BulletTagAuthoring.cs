using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Marks the entity as a bullet projectile; bakes a BulletTagComponent.
    [DisallowMultipleComponent]
    public class BulletTagAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BulletTagAuthoring>
        {
            public override void Bake(BulletTagAuthoring authoring)
            {
                var component = default(BulletTagComponent);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}