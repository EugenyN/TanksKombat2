using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Shared
{
    // Marks the entity as a player; bakes a PlayerIdComponent (id assigned at runtime).
    public class PlayerIdAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerIdAuthoring>
        {
            public override void Bake(PlayerIdAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PlayerIdComponent());
            }
        }
    }
}