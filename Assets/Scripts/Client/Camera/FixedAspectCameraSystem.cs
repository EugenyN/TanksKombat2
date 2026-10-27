using TanksKombat2.Shared;
using Unity.Entities;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client presentation: fits the main camera to the loaded level. Reacts to
    // LevelDataComponent changes by repositioning the camera over the play area and
    // re-applying the per-preset sprite size (from TileSetConfig) so the orthographic
    // size matches the map.
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class FixedAspectCameraSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingleton<ClientSettingsComponent>(out var clientSettings))
                return;

            foreach (var (levelData, matchConfig) in SystemAPI
                         .Query<RefRO<LevelDataComponent>, RefRO<MatchConfigComponent>>()
                         .WithChangeFilter<LevelDataComponent>())
            {
                var gameAreaSize = levelData.ValueRO.Size;
                var camera = Camera.main.GetComponent<FixedAspectCamera>();
                camera.SetSpriteSize(
                    clientSettings.TileSetConfig.Value.MapTileSizes[matchConfig.ValueRO.MapSizePreset]);
                camera.SetPosition(gameAreaSize.x, gameAreaSize.y);
                camera.UpdateCamera();
            }
        }
    }
}
