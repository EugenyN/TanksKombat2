using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Burst;
using Unity.Collections;

namespace TanksKombat2.Shared
{
    // Shared predicted bonus pickup: resolves TankContactEvent against bonus entities,
    // applies the bonus effect (ammo/lives) on both client and server, destroys the
    // bonus on the server and soft-hides it (Scale=0) on the client.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation |
                       WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(TankMovementSystem))]
    public partial struct TankBonusPickupSystem : ISystem
    {
        // Caps for bonus-granted resources; shared by client prediction and server sim.
        private const int MaxBullets = 99;
        private const int MaxLives = 16;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<TankContactEvent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) && 
                matchState.State != MatchState.RoundInProgress)
                return;

            var matchConfig = SystemAPI.GetSingleton<MatchConfigComponent>();
            var hitEvents = SystemAPI.GetSingletonBuffer<TankContactEvent>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var resolutionJob = new BonusPickupResolutionJob
            {
                HitEvents = hitEvents,
                CommandBuffer = commandBuffer,
                AmmoInBonus = matchConfig.GameModeConfig.AmmoInBonus,
                IsServer = state.WorldUnmanaged.IsServer(),
                IsFirstFullTick = (byte)(networkTime.IsFirstTimeFullyPredictingTick ? 1 : 0),
                TankStateLookup = SystemAPI.GetComponentLookup<TankStateComponent>(),
                BonusLookup = SystemAPI.GetComponentLookup<BonusComponent>(true),
                TransformsLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            };

            state.Dependency = resolutionJob.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(MatchConfigComponent))]
        private partial struct BonusPickupResolutionJob : IJobEntity
        {
            public DynamicBuffer<TankContactEvent> HitEvents;
            public EntityCommandBuffer CommandBuffer;
            public byte AmmoInBonus;

            public ComponentLookup<LocalTransform> TransformsLookup;
            public ComponentLookup<TankStateComponent> TankStateLookup;
            [ReadOnly] public ComponentLookup<BonusComponent> BonusLookup;
            
            public bool IsServer;
            public byte IsFirstFullTick;

            [BurstCompile]
            private void Execute()
            {
                for (var i = 0; i < HitEvents.Length; i++)
                    HitResolution(HitEvents[i]);
            }

            [BurstCompile]
            private void HitResolution(TankContactEvent hitData)
            {
                if (hitData.Target == Entity.Null || !BonusLookup.HasComponent(hitData.Target)) 
                    return;
                
                if (IsFirstFullTick == 1)
                    SendTakeBonusRpc(hitData.OwnerNetworkId);

                var bonusType = BonusLookup[hitData.Target].Type;
                var tankState = TankStateLookup[hitData.Tank];
                if (bonusType == BonusType.Ammo)
                {
                    if (tankState.Bullets < MaxBullets)
                        tankState.Bullets += AmmoInBonus;
                }
                if (bonusType == BonusType.Tank)
                {
                    if (tankState.Lives < MaxLives)
                        tankState.Lives += 1;
                }
                TankStateLookup[hitData.Tank] = tankState;

                if (IsServer)
                {
                    CommandBuffer.DestroyEntity(hitData.Target);
                }
                else
                {
                    var bonusTransform = TransformsLookup[hitData.Target];
                    bonusTransform.Scale = 0f;
                    TransformsLookup[hitData.Target] = bonusTransform;
                }
            }

            [BurstCompile]
            private void SendTakeBonusRpc(int ownerNetworkId)
            {
                // On the client this RPC entity is never sent (no SendRpcCommandRequest):
                // it acts as a local event consumed by TakeBonusEffectSystem for the
                // pickup sound. On the server it is a real RPC echoed to clients.
                var rpc = CommandBuffer.CreateEntity();
                CommandBuffer.AddComponent(rpc, new TakeBonusRpc { OwnerNetworkId = ownerNetworkId });
                if (IsServer)
                    CommandBuffer.AddComponent(rpc, new SendRpcCommandRequest());
            }
        }
    }
}
