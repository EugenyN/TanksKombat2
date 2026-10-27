using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Client
{
    // Tracks the connection state of the client's server connection: copies the last event of
    // NetCode's per-tick ConnectionEventsForTick into a ClientConnectionStateComponent singleton.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(NetworkReceiveSystemGroup))]
    public partial struct ClientConnectionStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamDriver>();
            
            state.EntityManager.CreateSingleton<ClientConnectionStateComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var connectionEvents = SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick;
            if (connectionEvents.Length == 0)
                return;
            
            var last = connectionEvents[^1];
            SystemAPI.SetSingleton(new ClientConnectionStateComponent {
                ConnectionEntity = last.ConnectionEntity, State = last.State, Reason = last.DisconnectReason
            });
        }
        
        public static bool TryGet(EntityManager entityManager, Entity connectionEntity, 
            out ClientConnectionStateComponent connectionState)
        {
            using var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ClientConnectionStateComponent>());
            return query.TryGetSingleton(out connectionState) && connectionState.ConnectionEntity == connectionEntity;
        }
    }
}
