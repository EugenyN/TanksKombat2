using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // One-shot visual effects: entity is destroyed when its lifetime expires.
    public class VisualEffectLifetimeAuthoring : MonoBehaviour
    {
        [Tooltip("Seconds before the effect entity is destroyed.")]
        public float Lifetime;

        private class Baker : Baker<VisualEffectLifetimeAuthoring>
        {
            public override void Bake(VisualEffectLifetimeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new VisualEffectLifetime { Value = authoring.Lifetime });
            }
        }
    }
}