using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.HostMigration;

namespace TanksKombat2.Client
{
    // Client-side part of the host-migration flow (from the official NetCode sample): after a
    // connection is re-established (NetCode tags it with NetworkStreamIsReconnected), re-add
    // NetworkStreamInGame so the driver treats the connection as fully in game again.
    // NOTE: currently NOT used — host migration is disabled (ConnectionManager.EnableHostMigration
    // == false), so NetCode never creates the HostMigrationConfig singleton and this system never
    // runs. Wire up EnableHostMigration to activate it.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct HostMigrationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamDriver>();
            state.RequireForUpdate<MatchConfigComponent>();
            state.RequireForUpdate<HostMigrationConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var evt in SystemAPI.GetSingleton<NetworkStreamDriver>().ConnectionEventsForTick)
            {
                var reconnected = SystemAPI.GetComponentLookup<NetworkStreamIsReconnected>();
                if (evt.State == ConnectionState.State.Connected && reconnected.HasComponent(evt.ConnectionEntity))
                {
                    state.EntityManager.AddComponent<NetworkStreamInGame>(evt.ConnectionEntity);
                    // Remove the reconnection tag as we're done with it
                    state.EntityManager.RemoveComponent<NetworkStreamIsReconnected>(evt.ConnectionEntity);
                }
            }
        }
    }
}