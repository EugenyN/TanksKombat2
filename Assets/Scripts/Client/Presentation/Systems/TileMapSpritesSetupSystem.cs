using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client presentation: assigns the correct atlas frame (theme row * columns + tile) to each
    // wall tile via MaterialPropertyFrame, so the tiled-atlas shader shows the wall sprites of
    // the loaded level theme.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientLevelLoadSystem))]
    public partial struct TileMapSpritesSetupSystem : ISystem
    {
        private EntityQuery _allWallsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<LevelDataComponent>();
            state.RequireForUpdate<ClientSettingsComponent>();

            _allWallsQuery = SystemAPI.QueryBuilder()
                .WithAll<WallTagComponent>()
                .Build();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var levelData = SystemAPI.GetSingleton<LevelDataComponent>();

            // Never bake against the default empty grid (before the first LoadLevelRpc).
            if (levelData.LevelData.IsEmpty)
                return;

            // Level changed -> invalidate every baked frame (structural change, outside the
            // iterator); the one-shot cycle below re-bakes them against the fresh grid.
            if (SystemAPI.HasSingleton<LoadLevelRpc>() && !_allWallsQuery.IsEmptyIgnoreFilter)
                state.EntityManager.RemoveComponent<MaterialPropertyFrame>(_allWallsQuery);

            var levelCfg = SystemAPI.GetSingleton<MatchConfigComponent>();
            var clientSettings = SystemAPI.GetSingleton<ClientSettingsComponent>();
            int columns = clientSettings.TileSetConfig.Value.Columns;
            int themeRow = TileSetConfig.GetThemeRow(levelCfg.MapTheme);
            
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<WallTagComponent>()
                         .WithNone<MaterialPropertyFrame>()
                         .WithEntityAccess())
            {
                var tile = levelData.GetTile(LevelHelper.WorldToGrid(transform.ValueRO.Position));
                ecb.AddComponent(entity, new MaterialPropertyFrame { Value = themeRow * columns + (int)tile });
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}