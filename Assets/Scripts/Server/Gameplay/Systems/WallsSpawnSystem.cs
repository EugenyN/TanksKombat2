using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TanksKombat2.Server
{
    // Spawns one wall entity per wall tile right after ServerLevelLoadSystem creates the
    // level entity. Requires a LevelDataComponent without WallsSpawnedTag to run; the tag
    // added at the end makes this idempotent and immune to later structural changes.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(ServerLevelLoadSystem))]
    public partial struct WallsSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<LevelDataComponent>()
                .WithNone<WallsSpawnedTag>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var levelEntity = SystemAPI.GetSingletonEntity<LevelDataComponent>();
            var levelData = SystemAPI.GetSingleton<LevelDataComponent>();

            var positions = new NativeList<float3>(64, Allocator.Temp);
            for (int i = 0; i < levelData.LevelData.Length; i++)
            {
                if (levelData.LevelData[i].IsWall())
                    positions.Add(LevelHelper.GridToWorld(levelData.GridPos(i)));
            }

            var wallPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().Wall;
            var walls = state.EntityManager.Instantiate(wallPrefab, positions.Length, Allocator.Temp);
            for (int i = 0; i < walls.Length; i++)
                state.EntityManager.SetComponentData(walls[i], LocalTransform.FromPosition(positions[i]));

            walls.Dispose();
            positions.Dispose();

            state.EntityManager.AddComponent<WallsSpawnedTag>(levelEntity);
        }
    }
}
