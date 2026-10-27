using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    // Shared predicted bullet movement: grid raycast per tick; server destroys the
    // bullet and emits BulletHitEvent, client soft-hides it via Scale=0 until the
    // next snapshot either confirms the despawn or resurrects the bullet.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(TankMovementSystem))]
    public partial struct BulletMovementSystem : ISystem
    {
        private EntityQuery _collidersQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ColliderComponent, LocalTransform>();
            _collidersQuery = state.GetEntityQuery(builder);

            var eventEntity = state.EntityManager.CreateEntity();
            state.EntityManager.SetName(eventEntity, "BulletHitEventBuffer");
            state.EntityManager.AddBuffer<BulletHitEvent>(eventEntity).EnsureCapacity(32);

            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<LevelDataComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var hitEvents = SystemAPI.GetSingletonBuffer<BulletHitEvent>();
            hitEvents.Clear();

            var (collisionWorld, buildJob) = CollisionWorld.Create(
                ref state, _collidersQuery, SystemAPI.GetSingleton<LevelDataComponent>(),
                SystemAPI.GetComponentLookup<GhostOwner>(true));

            state.Dependency = buildJob.Schedule(_collidersQuery, state.Dependency);

            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var bulletJob = new BulletMovementAndHitDetectionJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                IsServer = state.WorldUnmanaged.IsServer(),
                MatchConfig = SystemAPI.GetSingleton<MatchConfigComponent>(),
                CollisionWorld = collisionWorld,
                CommandBuffer = commandBuffer,
                HitEvents = hitEvents
            };

            state.Dependency = bulletJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate), typeof(BulletTagComponent))]
        partial struct BulletMovementAndHitDetectionJob : IJobEntity
        {
            public float DeltaTime;
            public bool IsServer;
            public EntityCommandBuffer CommandBuffer;
            public DynamicBuffer<BulletHitEvent> HitEvents;
            
            public MatchConfigComponent MatchConfig;
            public CollisionWorld CollisionWorld;
            
            [BurstCompile]
            private void Execute(Entity bullet, ref LocalTransform transform, in GhostOwner bulletOwner)
            {
                if (!IsServer && transform.Scale == 0f)
                    return;

                var velocity = LevelHelper.Forward(transform.Rotation) * MatchConfig.BulletVelocity;
                var currentPos = transform.Position;
                
                var nextPosition = currentPos;
                nextPosition.xy += velocity * DeltaTime;

                var currentCell = LevelHelper.WorldToGrid(currentPos);
                var nextCell = LevelHelper.WorldToGrid(nextPosition);
                
                if (CollisionWorld.GridCast(
                        bullet, currentCell, nextCell, CollisionFlags.Bullet, bulletOwner.NetworkId,
                        out var target, out _, out var hitCell))
                {
                    HandleHit(bullet, ref transform, target, hitCell, bulletOwner.NetworkId);
                }
                else
                {
                    transform.Position = nextPosition;
                }
            }

            [BurstCompile]
            private void HandleHit(Entity bullet, ref LocalTransform transform, Entity target, 
                int2 cell, int ownerNetworkId)
            {
                if (IsServer)
                {
                    HitEvents.Add(new BulletHitEvent {
                        Target = target, Position = LevelHelper.GridToWorld(cell), OwnerNetworkId = ownerNetworkId
                    });

                    CommandBuffer.DestroyEntity(bullet);
                }
                else
                {
                    // Scale=0 hides the bullet on the client;
                    // the next snapshot restores it if the server keeps the bullet alive
                    transform.Scale = 0f;
                }
            }
        }
    }
}
