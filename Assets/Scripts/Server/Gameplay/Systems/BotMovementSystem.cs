using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace TanksKombat2.Server
{
    // Server-only path-following for bot tanks.
    // Runs after BotAiSystem (so requests written by the AI are handled the same tick) and before TankMovementSystem
    // (so the input is consumed the same tick it is produced).
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(BotAiSystem))]
    [UpdateBefore(typeof(TankMovementSystem))]
    public partial struct BotMovementSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BotComponent>();
            state.RequireForUpdate<LevelDataComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var requestCount = SystemAPI.QueryBuilder()
                .WithAll<BotComponent, BotMoveRequest, PathStep>().Build().CalculateEntityCount();

            // Only build the occupancy snapshot when at least one BotMoveRequest is enabled this tick.
            if (requestCount > 0)
            {
                var level = SystemAPI.GetSingleton<LevelDataComponent>();

                // Snapshot of cells occupied by tanks, so BFS routes around stationary tanks.
                var occupiedCells = new NativeHashSet<int>(requestCount, state.WorldUpdateAllocator);
                foreach (var transformRef in SystemAPI.Query<RefRO<LocalTransform>>().WithAll<TankStateComponent>())
                    occupiedCells.Add(level.Index(transformRef.ValueRO.Position));

                // Parallel per-entity path building; the job query filters out disabled requests.
                state.Dependency = new RunMoveRequestJob
                {
                    Level = level, OccupiedCells = occupiedCells
                }.Schedule(state.Dependency);
            }

            state.Dependency = new PathFollowJob { DeltaTime = SystemAPI.Time.DeltaTime }.Schedule(state.Dependency);
        }
        
        [BurstCompile]
        private partial struct RunMoveRequestJob : IJobEntity
        {
            public LevelDataComponent Level;
            [ReadOnly] public NativeHashSet<int> OccupiedCells;

            private void Execute(ref BotComponent botComponent, DynamicBuffer<PathStep> path,
                in LocalTransform transform, ref BotMoveRequest request, EnabledRefRW<BotMoveRequest> requestEnabledRef)
            {
                path.Clear();
                
                if (request.Stop)
                    StopMove(ref botComponent);
                else
                    BuildPathAndStartMove(ref botComponent, path, in transform, request.TargetCell, request.GetUpClose);

                requestEnabledRef.ValueRW = false;
            }
            
            private void BuildPathAndStartMove(ref BotComponent botComponent, DynamicBuffer<PathStep> path, 
                in LocalTransform transform, int2 target, bool getUpClose)
            {
                var origin = LevelHelper.WorldToGrid(transform.Position);
                
                // Job-scoped Temp scratch: each worker thread owns a Temp allocator that is
                // reclaimed in full when the job completes, so no manual Dispose is required.
                var resultPath = new NativeList<int2>(Allocator.Temp);

                if (PathfindingHelper.TryFindPath(Level, origin, target, ref resultPath, getUpClose, OccupiedCells)
                    && resultPath.Length > 0)
                {
                    foreach (var cell in resultPath)
                        path.Add(new PathStep { Cell = cell });

                    botComponent.IsMovingToward = true;
                }
                else
                {
                    botComponent.IsMovingToward = false;
                }
                botComponent.PathIndex = 0;
                botComponent.MoveStepTimer = 0f;
                botComponent.TargetCell = target;
                botComponent.IsTargetReached = false;
            }

            private void StopMove(ref BotComponent botComponent)
            {
                botComponent.IsMovingToward = false;
                botComponent.PathIndex = 0;
                botComponent.MoveStepTimer = 0f;
                botComponent.TargetCell = new int2(-1, -1);
                botComponent.IsTargetReached = false;
            }
        }

        [BurstCompile]
        private partial struct PathFollowJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref BotComponent botComponent, ref TankPlayerInput input, in LocalTransform transform,
                DynamicBuffer<PathStep> path)
            {
                if (!botComponent.IsMovingToward)
                    return;

                botComponent.MoveStepTimer -= DeltaTime;
                if (botComponent.MoveStepTimer > 0f)
                    return;

                botComponent.MoveStepTimer = botComponent.MoveStepDelay +
                                             botComponent.Rand.NextFloat(0, botComponent.MoveStepDelayRandom);

                var currentCell = LevelHelper.WorldToGrid(transform.Position);

                // Drop steps we've already stepped onto.
                while (botComponent.PathIndex < path.Length && math.all(path[botComponent.PathIndex].Cell == currentCell))
                    botComponent.PathIndex++;

                if (botComponent.PathIndex >= path.Length)
                {
                    // Reached destination
                    botComponent.IsMovingToward = false;
                    botComponent.PathIndex = 0;
                    botComponent.MoveStepTimer = 0f;
                    botComponent.IsTargetReached = true;
                    return;
                }

                var nextCell = path[botComponent.PathIndex].Cell;
                var delta = nextCell - currentCell;

                // Direction indices match LevelHelper.Directions (0=Up,1=Right,2=Down,3=Left)
                // and TankPlayerInput.GetDirection().
                if (delta.y > 0) input.MoveUp.Set();
                else if (delta.x > 0) input.MoveRight.Set();
                else if (delta.y < 0) input.MoveDown.Set();
                else if (delta.x < 0) input.MoveLeft.Set();

                botComponent.PathIndex++;
            }
        }
    }
}
