using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Authoring for the CollisionWorld collider: layer membership, collision mask and trigger mode.
    public class ColliderAuthoring : MonoBehaviour
    {
        [Tooltip("Collision layers this entity belongs to.")]
        public CollisionFlags BelongsTo;

        [Tooltip("Collision layers this entity can collide with.")]
        public CollisionFlags CollidesWith;

        [Tooltip("If enabled, collisions are detected without physical response (pickups, sensors).")]
        public bool IsTrigger;

        private class Baker : Baker<ColliderAuthoring>
        {
            public override void Bake(ColliderAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ColliderComponent
                {
                    BelongsTo = authoring.BelongsTo,
                    CollidesWith = authoring.CollidesWith,
                    IsTrigger = authoring.IsTrigger
                });
            }
        }
    }
}