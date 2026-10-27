using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace TanksKombat2.Server
{
    // During an active round, spawns a bonus on a random reachable cell free of tanks and existing bonuses,
    // at randomized intervals up to the mode limit.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(ServerLevelLoadSystem))]
    public partial struct BonusSpawnSystem : ISystem
    {
        private EntityQuery _avoidTransformsQuery;
        private EntityQuery _bonusesQuery;

        private Entity _bonusesPrefab;

        private NativeReference<Random> _randomReference;
        private float _nextSpawnTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, BonusComponent>();
            _bonusesQuery = state.GetEntityQuery(builder);

            builder.Reset();
            builder.WithAll<LocalTransform>().WithAny<TankStateComponent, BonusComponent>();
            _avoidTransformsQuery = state.GetEntityQuery(builder);
            
            state.RequireForUpdate<GamePrefabsComponent>();
            state.RequireForUpdate<LevelReachableCellsComponent>();
            state.RequireForUpdate<MatchStateComponent>();
            
            _randomReference = new NativeReference<Random>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _randomReference.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState);
            if (matchState.State != MatchState.RoundInProgress)
                return;

            var matchConfigComponent = SystemAPI.GetSingleton<MatchConfigComponent>();
            
            if (_randomReference.Value.state == 0)
            {
                _randomReference.Value = Random.CreateFromIndex((uint)(SystemAPI.Time.ElapsedTime * 1000.0));
                _nextSpawnTime = GetNextSpawnTime(matchConfigComponent.GameModeConfig);
            }
            
            if (_bonusesPrefab == Entity.Null)
            {
                var spawner = SystemAPI.GetSingleton<GamePrefabsComponent>();
                _bonusesPrefab = spawner.Bonus;
            }

            var gameModeConfig = matchConfigComponent.GameModeConfig;
            int currentBonusesCount = _bonusesQuery.CalculateEntityCountWithoutFiltering();
            if (currentBonusesCount >= gameModeConfig.MaxBonusesOnScene)
                return;
            
            _nextSpawnTime -= SystemAPI.Time.DeltaTime;
            if (_nextSpawnTime > 0f)
                return;
            _nextSpawnTime = GetNextSpawnTime(gameModeConfig);

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var avoidTransforms = _avoidTransformsQuery.ToComponentDataListAsync<LocalTransform>(
                state.WorldUpdateAllocator, out var avoidTransformsHandle);
            
            var reachableCellsComponent = SystemAPI.GetSingleton<LevelReachableCellsComponent>();
            var levelDataComponent = SystemAPI.GetSingleton<LevelDataComponent>();
            
            state.Dependency = JobHandle.CombineDependencies(avoidTransformsHandle, state.Dependency);

            var spawnBonus = new SpawnBonusJob
            {
                CommandBuffer = commandBuffer,
                AvoidTransforms = avoidTransforms.AsDeferredJobArray(),
                LevelSize = levelDataComponent.Size,
                ReachableCells = reachableCellsComponent,
                Random = _randomReference,
                BonusPrefab = _bonusesPrefab,
                AmmoChance = gameModeConfig.AmmoChance
            };
            state.Dependency = spawnBonus.Schedule(state.Dependency);
        }

        private float GetNextSpawnTime(GameModeConfig gameModeConfig)
        {
            var delay = _randomReference.Value.NextFloat(gameModeConfig.BonusSpawnDelay, 
                gameModeConfig.BonusSpawnDelay + gameModeConfig.BonusSpawnDelayRandom);
            return math.max(0.1f, delay);
        }

        [BurstCompile]
        private struct SpawnBonusJob : IJob
        {
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly] public NativeArray<LocalTransform> AvoidTransforms;
            [ReadOnly] public LevelReachableCellsComponent ReachableCells;
            public int2 LevelSize;

            public NativeReference<Random> Random;
            public Entity BonusPrefab;
            public float AmmoChance;

            public void Execute()
            {
                var rand = Random.Value;
                
                if (!ReachableCells.TryFindSpawnPosition(LevelSize, ref rand, AvoidTransforms, out var validPos))
                    return;
                
                var bonus = CommandBuffer.Instantiate(BonusPrefab);
                CommandBuffer.SetComponent(bonus, new BonusComponent { Type = 
                    rand.NextFloat(1f) < AmmoChance ? BonusType.Ammo : BonusType.Tank });
                CommandBuffer.SetComponent(bonus, LocalTransform.FromPosition(LevelHelper.GridToWorld(validPos)));

                var rpc = CommandBuffer.CreateEntity();
                CommandBuffer.AddComponent(rpc, new BonusSpawnedRpc());
                CommandBuffer.AddComponent(rpc, new SendRpcCommandRequest());

                Random.Value = rand;
            }
        }
    }
}
