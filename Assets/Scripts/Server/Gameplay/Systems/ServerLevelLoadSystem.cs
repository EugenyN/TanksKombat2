using System;
using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace TanksKombat2.Server
{
    // Server-authoritative one-shot level builder: resolves MapTheme/GameMode/bot-skill config
    // from ServerSettingsComponent, generates (cellular automata caves) or copies the tile grid
    // into LevelDataComponent, then emits the "Level" entity consumed by spawn/AI/state systems.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerLevelLoadSystem : ISystem
    {
        private EntityQuery _levelQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _levelQuery = state.GetEntityQuery(ComponentType.ReadWrite<LevelDataComponent>());

            state.RequireForUpdate<ServerSettingsComponent>();
            state.RequireForUpdate<ServerMapSizesComponent>();
            state.RequireForUpdate<MatchStateComponent>();
            state.RequireForUpdate<CustomMapsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_levelQuery.IsEmptyIgnoreFilter)
                return;

            var settings = SystemAPI.GetSingleton<ServerSettingsComponent>();

            ref var matchConfig = ref settings.DefaultMatchConfig;
            ref var gameplayConfig = ref settings.DefaultGameplayConfig;

            var mapSizes = SystemAPI.GetSingleton<ServerMapSizesComponent>().MapSizes;

            var random = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000.0));

            if (matchConfig.MapTheme == MapTheme.Random)
                matchConfig.MapTheme = (MapTheme)random.NextInt(1, (int)MapTheme.Dungeon + 1);

            matchConfig.GameModeConfig = matchConfig.GameMode switch
            {
                GameMode.Standard => gameplayConfig.GameModeConfigs.Standard,
                GameMode.Pro => gameplayConfig.GameModeConfigs.Pro,
                GameMode.FireAndIron => gameplayConfig.GameModeConfigs.FireAndIron,
                GameMode.QuickDeath => gameplayConfig.GameModeConfigs.QuickDeath,
                _ => throw new ArgumentOutOfRangeException()
            };

            float skillFactor = matchConfig.BotSkill switch
            {
                BotSkill.Easy => -1, BotSkill.Normal => 0, BotSkill.Hard => 1,
                _ => throw new ArgumentOutOfRangeException()
            };

            ref var aiConfig = ref gameplayConfig.BotAiConfig;
            aiConfig.AiSpeed += skillFactor * aiConfig.AiSpeedSkillFactor;
            aiConfig.EscapeChance += skillFactor * aiConfig.EscapeChanceSkillFactor;
            aiConfig.StrafeChance += skillFactor * aiConfig.StrafeChanceSkillFactor;
            aiConfig.AttackChance += skillFactor * aiConfig.AttackChanceSkillFactor;
            aiConfig.MoveStepDelay += skillFactor * aiConfig.MoveStepDelaySkillFactor;

            var levelData = new FixedList4096Bytes<MapTile>();
            
            if (matchConfig.MapType == MapType.ExtraLargeCave ||
                matchConfig.MapType == MapType.LargeCave ||
                matchConfig.MapType == MapType.SmallCave)
            {
                matchConfig.MapSizePreset = matchConfig.MapType switch
                {
                    MapType.SmallCave => MapSizePreset.Small, 
                    MapType.LargeCave => MapSizePreset.Large,
                    MapType.ExtraLargeCave => MapSizePreset.ExtraLarge,
                    _ => throw new ArgumentOutOfRangeException()
                };
                var mapSize = mapSizes[(int)matchConfig.MapSizePreset];
                GetRandomCaveMapData(ref state, gameplayConfig.MapGenerationConfig, mapSize, ref levelData);
            }
            else
            {
                ref var mapsData = ref SystemAPI.GetSingleton<CustomMapsComponent>().MapsDataBlob.Value;
                
                if (matchConfig.MapType == MapType.CustomRandom && mapsData.MapsCellsData.Length > 0)
                    matchConfig.CustomMap = (CustomMap)random.NextInt(0, mapsData.MapsCellsData.Length);
                
                matchConfig.MapSizePreset = mapsData.MapsSizes[(int)matchConfig.CustomMap];
                GetCustomMapData(ref mapsData, (int)matchConfig.CustomMap, ref levelData);
            }

            var levelDataComponent = new LevelDataComponent { 
                LevelData = levelData, 
                Size = mapSizes[(int)matchConfig.MapSizePreset] 
            };
            var reachableCellsComponent = new LevelReachableCellsComponent {
                Cells = ReachableCellsHelper.ComputeReachableCells(levelDataComponent)
            };

            var level = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(level, "Level");
            state.EntityManager.AddComponentData(level, matchConfig);
            state.EntityManager.AddComponentData(level, gameplayConfig);
            state.EntityManager.AddComponentData(level, levelDataComponent);
            state.EntityManager.AddComponentData(level, reachableCellsComponent);
        }
        
        private void GetRandomCaveMapData(ref SystemState state, MapGenerationConfig config, int2 mapSize, ref FixedList4096Bytes<MapTile> levelData)
        {
            int totalCells = mapSize.x * mapSize.y;
            var cells = new NativeArray<int>(totalCells, Allocator.TempJob);

            if (config.Seed == 0)
                config.Seed = (uint)(SystemAPI.Time.ElapsedTime * 1000.0);

            var generateJob = new GenerateMapJob {
                Width = mapSize.x, Height = mapSize.y, Config = config, ResultCells = cells
            };

            generateJob.Run();

            for (int i = 0; i < totalCells; i++)
            {
                var tile = cells[i] switch
                {
                    -2 => MapTile.Background3, -1 => MapTile.Background2, 0 => MapTile.Background1,
                    1 => MapTile.Wall1, 2 => MapTile.Wall2, 3 => MapTile.Wall3,
                    _ => MapTile.Background1
                };

                levelData.Add(tile);
            }

            cells.Dispose();
        }
        
        private void GetCustomMapData(ref CustomMapsData mapsData, int mapIndex, ref FixedList4096Bytes<MapTile> levelData)
        {
            ref var levelCellsData = ref mapsData.MapsCellsData[mapIndex];
            for (int i = 0; i < levelCellsData.Length; i++)
                levelData.Add(levelCellsData[i]);
        }

        [BurstCompile]
        private struct GenerateMapJob : IJob
        {
            public int Width;
            public int Height;
            public MapGenerationConfig Config;

            public NativeArray<int> ResultCells;

            public void Execute()
            {
                int totalCells = Width * Height;
                var random = Random.CreateFromIndex(Config.Seed);

                for (int i = 0; i < totalCells; i++)
                    ResultCells[i] = random.NextFloat() < Config.ChanceToStartAlive ? 1 : 0;

                var nextMap = new NativeArray<int>(totalCells, Allocator.Temp);

                for (int step = 0; step < Config.NumberOfSteps; step++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        for (int y = 0; y < Height; y++)
                        {
                            int nbs = CountAliveNeighbours(x, y);
                            int idx = Flatten(x, y);

                            if (ResultCells[idx] > 0)
                                nextMap[idx] = nbs >= Config.DeathLimit ? 1: 0;
                            else
                                nextMap[idx] = nbs > Config.BirthLimit ? 2 : -(Config.BirthLimit - nbs);
                        }
                    }

                    for (int i = 0; i < totalCells; i++)
                        ResultCells[i] = nextMap[i];
                }
            }

            private int CountAliveNeighbours(int x, int y)
            {
                int count = 0;
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        int nx = x + i;
                        int ny = y + j;

                        if (i == 0 && j == 0)
                            continue;

                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height)
                            count++;
                        else if (ResultCells[Flatten(nx, ny)] > 0)
                            count++;
                    }
                }

                return count;
            }

            private int Flatten(int x, int y) => y * Width + x;
        }
    }
}
