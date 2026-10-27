using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client-side level setup: consumes the server's LoadLevelRpc RPC, writes the
    // match config and level data into the Level singleton, and destroys the command
    // entity via the BeginSimulation ECB.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientLevelLoadSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<LoadLevelRpc>(),
                ComponentType.ReadOnly<ReceiveRpcCommandRequest>()));
            
            state.RequireForUpdate<GamePrefabsComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Resolve the Level singleton fresh per update: in single-world host mode the entity
            // is server-owned and destroyed between rounds/matches, so a cached reference would
            // go stale. Creation (a structural change) must happen outside the query iteration.
            if (!SystemAPI.TryGetSingletonEntity<MatchConfigComponent>(out var levelSingleton))
            {
                if (state.WorldUnmanaged.IsHost())
                    return; // server owns the Level entity and rebuilds it — wait for it

                // Pure client: create and own the Level entity for the whole session.
                levelSingleton = state.EntityManager.CreateEntity();
                state.EntityManager.SetName(levelSingleton, "Level");
                state.EntityManager.AddComponentData(levelSingleton, new MatchConfigComponent());
                state.EntityManager.AddComponentData(levelSingleton, new LevelDataComponent());
            }

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (loadLevelRpc, entity) in SystemAPI.Query<RefRO<LoadLevelRpc>>().WithEntityAccess())
            {
                state.EntityManager.SetComponentData(levelSingleton, loadLevelRpc.ValueRO.MatchConfig);
                state.EntityManager.SetComponentData(levelSingleton, new LevelDataComponent
                {
                    LevelData = loadLevelRpc.ValueRO.LevelData,
                    Size      = loadLevelRpc.ValueRO.LevelSize
                });

                // Destroyed via the BeginSimulation ECB (next frame)
                commandBuffer.DestroyEntity(entity);

                Debug.Log("Received LoadLevelRpc from server. Init Level.");
            }
        }
    }
}