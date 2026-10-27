using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Burst;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    // Shared predicted shooting: on the first fully predicted tick of a shoot press spawns a
    // bullet (owner = shooter) and fires a TankShotRpc RPC; server is authoritative.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(TankMovementSystem))]
    [UpdateAfter(typeof(TankBonusPickupSystem))]
    public partial struct TankShootingSystem : ISystem
    {
        private const float HorizontalBulletYOffset = 1 * (1f / 16); // 1 pixel offset
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GamePrefabsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) &&
                matchState.State != MatchState.RoundInProgress)
                return;

            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var bulletPrefab = SystemAPI.GetSingleton<GamePrefabsComponent>().Bullet;
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var shootingJob = new TankShootingJob
            {
                CommandBuffer = commandBuffer,
                BulletPrefab = bulletPrefab,
                IsFirstFullTick = (byte)(networkTime.IsFirstTimeFullyPredictingTick ? 1 : 0),
                IsServer = state.WorldUnmanaged.IsServer()
            };

            state.Dependency = shootingJob.Schedule(state.Dependency);
        }
        
        [WithAll(typeof(Simulate), typeof(TankStateComponent), typeof(TankPlayerInput))]
        [BurstCompile]
        partial struct TankShootingJob : IJobEntity
        {
            public EntityCommandBuffer CommandBuffer;
            public Entity BulletPrefab;

            public byte IsFirstFullTick;
            public bool IsServer;

            [BurstCompile]
            private void Execute(in TankPlayerInput playerInput, in LocalTransform transform, 
                ref TankStateComponent state, in GhostOwner ghostOwner)
            {
                if (!state.IsShooting && playerInput.Shoot.IsSet)
                    Shoot(ref state, in transform, ghostOwner.NetworkId);

                if (!playerInput.Shoot.IsSet)
                    state.IsShooting = false;
            }

            [BurstCompile]
            private void Shoot(ref TankStateComponent state, in LocalTransform transform, int networkId)
            {
                state.IsShooting = true;

                if (state.Bullets <= 0)
                {
                    if (IsFirstFullTick == 1)
                        SendShootRpc(networkId, IsServer, true);
                    return;
                }
                if (state.InvulnerabilityTimer > 0) return;

                state.Bullets--;

                if (IsFirstFullTick == 1)
                {
                    var position = transform.Position;
                    var isHorizontal = LevelHelper.GetDirection(transform.Rotation) is 1 or 3;
                    position.y += math.select(0f, HorizontalBulletYOffset, isHorizontal);
                    
                    var bullet = CommandBuffer.Instantiate(BulletPrefab);
                    CommandBuffer.SetComponent(bullet, LocalTransform.FromPositionRotation(position, transform.Rotation));
                    CommandBuffer.SetComponent(bullet, new GhostOwner { NetworkId = networkId });

                    SendShootRpc(networkId, IsServer, false);
                }
            }

            [BurstCompile]
            private void SendShootRpc(int networkId, bool isServer, bool emptyAmmo)
            {
                var rpc = CommandBuffer.CreateEntity();
                CommandBuffer.AddComponent(rpc, new TankShotRpc { OwnerNetworkId = networkId, EmptyAmmo = emptyAmmo });
                if (isServer)
                    CommandBuffer.AddComponent(rpc, new SendRpcCommandRequest());
            }
        }
    }
}
