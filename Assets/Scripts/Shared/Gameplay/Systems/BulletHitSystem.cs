using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;

namespace TanksKombat2.Shared
{
    // Shared predicted hit resolution: consumes BulletHitEvent (produced by
    // BulletMovementSystem on the server), applies damage/destroy and emits hit RPCs.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(BulletMovementSystem))]
    public partial struct BulletHitSystem : ISystem
    {
        private NativeQueue<Entity> _playerClearQueue;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _playerClearQueue = new NativeQueue<Entity>(Allocator.Persistent);

            state.RequireForUpdate<MatchStateComponent>();
            state.RequireForUpdate<BulletHitEvent>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            _playerClearQueue.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();
            if (matchState.State != MatchState.RoundInProgress)
                return;

            var hitEvents = SystemAPI.GetSingletonBuffer<BulletHitEvent>(true);
            if (hitEvents.Length == 0)
                return;
            
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var hitResolutionJob = new BulletHitResolutionJob
            {
                HitEvents = hitEvents,
                PlayerClearQueue = _playerClearQueue,
                CommandBuffer = commandBuffer,
                TankStateLookup = SystemAPI.GetComponentLookup<TankStateComponent>(),
                PlayerIdLookup = SystemAPI.GetComponentLookup<PlayerIdComponent>(true),
                NetworkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true),
            };
            var hitResolutionHandle = hitResolutionJob.Schedule(state.Dependency);

            var detachTankFromConnectionJob = new DetachTankFromConnectionJob
            {
                PlayerClearQueue = _playerClearQueue,
                CommandTargetLookup = SystemAPI.GetComponentLookup<CommandTarget>(),
                LinkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
            };

            state.Dependency = detachTankFromConnectionJob.Schedule(hitResolutionHandle);
        }
        
        [BurstCompile]
        private partial struct BulletHitResolutionJob : IJobEntity
        {
            [ReadOnly] public DynamicBuffer<BulletHitEvent> HitEvents;
            public NativeQueue<Entity> PlayerClearQueue;
            public EntityCommandBuffer CommandBuffer;

            public ComponentLookup<TankStateComponent> TankStateLookup;
            [ReadOnly] public ComponentLookup<PlayerIdComponent> PlayerIdLookup;
            [ReadOnly] public ComponentLookup<NetworkId> NetworkIdLookup;

            [BurstCompile]
            private void Execute(in MatchConfigComponent matchConfig)
            {
                for (var i = 0; i < HitEvents.Length; i++)
                {
                    // Skip if an earlier event this tick already hit the same target.
                    var target = HitEvents[i].Target;
                    var duplicate = false;
                    for (var j = 0; j < i; j++)
                        if (HitEvents[j].Target == target) { duplicate = true; break; }

                    if (!duplicate)
                        HitResolution(HitEvents[i], matchConfig);
                }
            }

            [BurstCompile]
            private void HitResolution(BulletHitEvent hitData, in MatchConfigComponent matchConfig)
            {
                if (hitData.Target != Entity.Null)
                {
                    if (TankStateLookup.HasComponent(hitData.Target))
                    {
                        var tankState = TankStateLookup[hitData.Target];

                        if (tankState.InvulnerabilityTimer > 0) return;
                        if (tankState.Lives <= 0) return;
                        
                        tankState.Lives--;
                        tankState.InvulnerabilityTimer = matchConfig.GameModeConfig.InvulnerabilityTime;
                            
                        TankStateLookup[hitData.Target] = tankState;

                        if (tankState.Lives == 0)
                        {
                            var playerConnection = PlayerIdLookup[hitData.Target].PlayerEntity;
                            PlayerClearQueue.Enqueue(playerConnection);
                            CommandBuffer.DestroyEntity(hitData.Target);

                            SendHitRpc(hitData.Position, HitKind.TankDestroy);
                            SendTankDestroyedRpc(hitData.OwnerNetworkId, NetworkIdLookup[playerConnection].Value);
                        }
                        else
                        {
                            SendHitRpc(hitData.Position, HitKind.TankDamage);
                        }
                    }
                    else // bonus
                    {
                        CommandBuffer.DestroyEntity(hitData.Target);
                        SendHitRpc(hitData.Position, HitKind.BonusDestroy);
                    }
                }
            }

            [BurstCompile]
            private void SendHitRpc(float3 position, HitKind kind)
            {
                var rpc = CommandBuffer.CreateEntity();
                CommandBuffer.AddComponent(rpc, new BulletHitRpc { Position = position, Kind = kind });
                CommandBuffer.AddComponent(rpc, new SendRpcCommandRequest());
            }

            [BurstCompile]
            private void SendTankDestroyedRpc(int killerNetworkId, int victimNetworkId)
            {
                var tankDestroyRpc = CommandBuffer.CreateEntity();
                CommandBuffer.AddComponent(tankDestroyRpc, new TankDestroyedRpc
                {
                    KillerNetworkId = killerNetworkId, VictimNetworkId = victimNetworkId
                });
                CommandBuffer.AddComponent(tankDestroyRpc, new SendRpcCommandRequest());
            }
        }

        // Detaches the destroyed tank from its player connection (CommandTarget + child link).
        [BurstCompile]
        private struct DetachTankFromConnectionJob : IJob
        {
            public NativeQueue<Entity> PlayerClearQueue;
            public ComponentLookup<CommandTarget> CommandTargetLookup;
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;

            [BurstCompile]
            public void Execute()
            {
                while (PlayerClearQueue.TryDequeue(out var playerConnection))
                {
                    ConnectionUtils.DetachTankFromConnection(
                        playerConnection, CommandTargetLookup, LinkedEntityGroupLookup);
                }
            }
        }
    }
}
