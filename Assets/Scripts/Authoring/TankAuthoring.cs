using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Marks the entity as a tank; bakes tank state, input and color components.
    public class TankAuthoring : MonoBehaviour
    {
        private class Baker : Baker<TankAuthoring>
        {
            public override void Bake(TankAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, new TankStateComponent());
                AddComponent(entity, new TankPlayerInput());
                AddComponent(entity, new TankColorComponent());
            }
        }
    }
}