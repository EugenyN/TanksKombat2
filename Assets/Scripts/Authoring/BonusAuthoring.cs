using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Marks the entity as a bonus pickup; bakes a BonusComponent.
    [DisallowMultipleComponent]
    public class BonusAuthoring : MonoBehaviour
    {
        private class Baker : Baker<BonusAuthoring>
        {
            public override void Bake(BonusAuthoring authoring)
            {
                var component = default(BonusComponent);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}