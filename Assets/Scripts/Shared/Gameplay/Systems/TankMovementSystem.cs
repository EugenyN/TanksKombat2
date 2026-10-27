using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Unity.Transforms;

namespace TanksKombat2.Shared
{
    // Shared predicted tank movement: grid step with cell claiming so two tanks
    // never enter the same cell in one tick; emits TankContactEvent on collision.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct TankMovementSystem : ISystem
    {
        private EntityQuery _collidersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ColliderComponent, LocalTransform>();
            _collidersQuery = state.GetEntityQuery(builder);

            var eventEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(eventEntity, "TankContactEventBuffer");
            state.EntityManager.AddBuffer<TankContactEvent>(eventEntity).EnsureCapacity(32);

            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<LevelDataComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) &&
                matchState.State != MatchState.RoundInProgress)
                return;
            
            var hitEvents = SystemAPI.GetSingletonBuffer<TankContactEvent>();
            hitEvents.Clear();

            var (collisionWorld, buildJob) = CollisionWorld.Create(
                ref state, _collidersQuery, SystemAPI.GetSingleton<LevelDataComponent>(),
                SystemAPI.GetComponentLookup<GhostOwner>(true));

            state.Dependency = buildJob.Schedule(_collidersQuery, state.Dependency);
            
            var reserved = new NativeHashSet<int>(16, state.WorldUpdateAllocator);

            var tankMovementAndHitDetectionJob = new TankMovementJob
            {
                HitEvents = hitEvents,
                CollisionWorld = collisionWorld,
                Reserved = reserved
            };

            state.Dependency = tankMovementAndHitDetectionJob.Schedule(state.Dependency);
        }
        
        [WithAll(typeof(Simulate), typeof(TankStateComponent), typeof(TankPlayerInput))]
        [BurstCompile]
        partial struct TankMovementJob : IJobEntity
        {
            public CollisionWorld CollisionWorld;
            public NativeHashSet<int> Reserved;
            public DynamicBuffer<TankContactEvent> HitEvents;

            [BurstCompile]
            private void Execute(Entity entity, in TankPlayerInput playerInput, ref LocalTransform transform,
                in GhostOwner ghostOwner)
            {
                var input = playerInput;
                var levelSize = CollisionWorld.Level.Size;

                if (!input.IsMove)
                {
                    Reserved.Add(LevelHelper.Index(levelSize, transform.Position));
                    return;
                }

                int direction = input.GetDirection();

                if (!input.Block.IsSet)
                {
                    var currentCell = LevelHelper.WorldToGrid(transform.Position);
                    var nextCell = LevelHelper.WrapGridPos(levelSize, currentCell + LevelHelper.Direction(direction));
                    var nextIdx = LevelHelper.Index(levelSize, nextCell);

                    bool collide = CollisionWorld.TryHitCell(
                        entity, nextCell, CollisionFlags.Tank, out var target, out var isTrigger);
                    
                    if (collide && target != Entity.Null && !Reserved.Contains(nextIdx))
                    {
                        HitEvents.Add(new TankContactEvent {
                            Tank = entity, Target = target, OwnerNetworkId = ghostOwner.NetworkId
                        });
                    }

                    bool canMove = (!collide || isTrigger) && Reserved.Add(nextIdx);
                    if (canMove)
                        transform.Position = LevelHelper.GridToWorld(nextCell);
                    else
                        Reserved.Add(LevelHelper.Index(levelSize, transform.Position));
                }

                transform.Rotation = quaternion.RotateZ(math.radians(-direction * 90f));
            }
        }
    }
}