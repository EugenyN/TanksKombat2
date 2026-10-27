using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Adds a disabled PlayAnimationRequest flag, enabled at runtime to trigger sprite animation.
    public class PlayAnimationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayAnimationAuthoring>
        {
            public override void Bake(PlayAnimationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<PlayAnimationRequest>(entity);
                SetComponentEnabled<PlayAnimationRequest>(entity, false);
            }
        }
    }
}
