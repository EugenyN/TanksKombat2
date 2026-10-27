using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode.HostMigration;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Server-side defaults: match/gameplay configs and map sizes; baked into ServerSettingsComponent.
    [DisallowMultipleComponent]
    public class ServerSettingsAuthoring : MonoBehaviour
    {
        [Tooltip("Default match configuration (rounds, timings, scores).")]
        [RegisterBinding(typeof(ServerSettingsComponent), "levelData")]
        public MatchConfigComponent MatchConfig = MatchConfigComponent.Default;

        [Tooltip("Server gameplay tuning (spawn rates, speeds, combat values).")]
        public ServerGameplayConfigComponent GameplayConfig = ServerGameplayConfigComponent.Default;

        [Tooltip("Config asset with the available map sizes.")]
        [SerializeField] public MapSizeConfig MapSizesConfig;

        private class Baker : Baker<ServerSettingsAuthoring>
        {
            public override void Bake(ServerSettingsAuthoring authoring)
            {
                var component = default(ServerSettingsComponent);
                component.DefaultMatchConfig = authoring.MatchConfig;
                component.DefaultGameplayConfig = authoring.GameplayConfig;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);

                var mapSizes = new ServerMapSizesComponent();
                foreach (var size in authoring.MapSizesConfig.MapSizes.Values)
                    mapSizes.MapSizes.Add(size);

                AddComponent(entity, mapSizes);
                AddComponent<IncludeInMigration>(entity);
            }
        }
    }
}