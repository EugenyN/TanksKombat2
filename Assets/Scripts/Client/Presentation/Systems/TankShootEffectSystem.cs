using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Scripting.LifecycleManagement;
using Unity.Transforms;

namespace TanksKombat2.Client
{
    // Client effects: reacts to TankShotRpc events (locally predicted and echoed by the
    // server) — plays the shoot/empty-ammo animation and sound, spawns the muzzle-flash VFX
    // attached to the shooting tank; the flash is destroyed early if the tank moves or rotates.
    [NoAutoStaticsCleanup]
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct TankShootEffectSystem : ISystem
    {
        private EntityQuery _tankQuery;

        private static readonly float3[] _shotFxOffsets = {
            new (0, 9.5f, 0), new (-1, 9.5f, 0), new (0, 5.5f, 0), new (1, 9.5f, 0)
        };
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _tankQuery = SystemAPI.QueryBuilder()
                .WithAll<TankStateComponent, GhostOwner>()
                .Build();
            
            state.RequireForUpdate<GamePrefabsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            
            var ghostOwnerLookup = SystemAPI.GetComponentLookup<GhostOwner>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            foreach (var (rpc, entity) in SystemAPI.Query<RefRO<TankShotRpc>>().WithEntityAccess())
            {
                // skip echo of own shot returned by server (already played locally)
                if (RpcUtils.IsEchoRpc(ref state, entity, rpc.ValueRO.OwnerNetworkId))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var tank = Entity.Null;
                var tanks = _tankQuery.ToEntityArray(Allocator.Temp);
                for (int i = 0; i < tanks.Length; i++)
                {
                    if (ghostOwnerLookup[tanks[i]].NetworkId == rpc.ValueRO.OwnerNetworkId)
                    {
                        tank = tanks[i];
                        break;
                    }
                }

                if (tank == Entity.Null)
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                SpriteAnimationSystem.Play(ecb, tank, TankAnimations.Shoot);

                if (rpc.ValueRO.EmptyAmmo)
                {
                    AudioSystem.PlayAudio(ecb, AudioClipKeys.OutOfAmmo);
                }
                else
                {
                    AudioSystem.PlayAudio(ecb, AudioClipKeys.TankShot);
                    CreateShotVfx(ecb, tank, transformLookup);
                }

                ecb.DestroyEntity(entity);
            }

            // destroy the muzzle flash if its tank moved, rotated or died since the flash spawned
            foreach (var (shotVfx, entity) in SystemAPI.Query<RefRO<TankShotVfxComponent>>().WithEntityAccess())
            {
                var tank = shotVfx.ValueRO.Tank;
                if (!transformLookup.HasComponent(tank))
                    return;
                
                bool tankChanged = !math.all(transformLookup[tank].Position == shotVfx.ValueRO.TankPosition) || 
                                   LevelHelper.GetDirection(transformLookup[tank].Rotation) != shotVfx.ValueRO.TankDirection;
                if (tankChanged)
                    ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
        }

        private void CreateShotVfx(EntityCommandBuffer ecb, Entity tank, 
            in ComponentLookup<LocalTransform> transformLookup)
        {
            var shotVfxPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().ShotVfx;
            var shotVfx = ecb.Instantiate(shotVfxPrefab);
            
            var direction = LevelHelper.GetDirection(transformLookup[tank].Rotation);
            var shotFxOffset = _shotFxOffsets[direction] * (1f / 16);

            ecb.AddComponent(shotVfx, new Parent { Value = tank });
            ecb.AddComponent(shotVfx, new TankShotVfxComponent
            {
                Tank = tank,
                TankPosition = transformLookup[tank].Position,
                TankDirection = direction
            });
            ecb.SetComponent(shotVfx,
                LocalTransform.FromPositionRotationScale(shotFxOffset, quaternion.identity, 0.5f));
        }
    }
}
