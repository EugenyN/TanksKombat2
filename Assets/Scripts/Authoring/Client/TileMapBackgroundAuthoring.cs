using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Marks the entity as the tile-map background rendered from the tileset config.
    public class TileMapBackgroundAuthoring : MonoBehaviour
    {
        private class Baker : Baker<TileMapBackgroundAuthoring>
        {
            public override void Bake(TileMapBackgroundAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TileMapBackground());
                AddComponent<DisableRendering>(entity);
            }
        }
    }
}
