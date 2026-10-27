using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client presentation settings: tileset atlas and player color palette; baked into ClientSettingsComponent.
    public class ClientSettingsAuthoring : MonoBehaviour
    {
        [Tooltip("Tileset config used to build the level background.")]
        [SerializeField] public TileSetConfig TileSetConfig;

        [Tooltip("Color palette assigned to players by index.")]
        [SerializeField] public Color[] PlayerColors;

        private class Baker : Baker<ClientSettingsAuthoring>
        {
            public override void Bake(ClientSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var settings = new ClientSettingsComponent { TileSetConfig = authoring.TileSetConfig, HealthBars = true };
                foreach (var color in authoring.PlayerColors)
                    settings.PlayerColors.Add(new float4(color.r, color.g, color.b, color.a));
                AddComponent(entity, settings);
            }
        }
    }
}