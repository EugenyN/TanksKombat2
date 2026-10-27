using TanksKombat2.Shared;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Server
{
    // Server-side connection approval: processes JoinApprovalRequestRpc from connecting clients.
    // Approves the connection (adds ConnectionApproved + JoinedPlayerComponent) only if
    // MatchState is WaitingForPlayers and the server isn't full. Otherwise disconnects with
    // ApprovalFailure so the client sees a transport-level rejection before the game scene loads.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ServerConnectionApprovalSystem : ISystem
    {
        private EntityQuery _inGameConnectionsQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _inGameConnectionsQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<NetworkId, NetworkStreamInGame>()
                .Build(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);
            int currentPlayers = _inGameConnectionsQuery.CalculateEntityCount();

            // If MatchStateComponent hasn't loaded yet (sub-scene still streaming), treat
            // the match as not started — allow the connection through.
            SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState);

            foreach (var (approvalRpc, requestSource, entity)
                     in SystemAPI.Query<RefRO<JoinApprovalRequestRpc>, RefRO<ReceiveRpcCommandRequest>>()
                         .WithEntityAccess())
            {
                commandBuffer.DestroyEntity(entity);

                var playerConnection = requestSource.ValueRO.SourceConnection;
                var playerName = approvalRpc.ValueRO.PlayerName;

                // Reject if match is not in WaitingForPlayers (or MatchStateComponent loaded
                // and indicates the match is already in progress).
                if (matchState.State != MatchState.WaitingForPlayers)
                {
                    Debug.Log($"Player '{playerName}' rejected: match in progress ({matchState.State}).");
                    commandBuffer.AddComponent(playerConnection, new NetworkStreamRequestDisconnect {
                        Reason = NetworkStreamDisconnectReason.ApprovalFailure
                    });
                    continue;
                }

                if (currentPlayers >= ConnectionUtils.MaxSessionPlayers)
                {
                    Debug.Log($"Player '{playerName}' rejected: match full ({ConnectionUtils.MaxSessionPlayers} players max).");
                    commandBuffer.AddComponent(playerConnection, new NetworkStreamRequestDisconnect {
                        Reason = NetworkStreamDisconnectReason.ApprovalFailure
                    });
                    continue;
                }

                currentPlayers++;

                // Approve the connection so NetCode assigns a NetworkId and starts replicating.
                // NetworkStreamInGame is NOT added here (see second pass below).
                commandBuffer.AddComponent<ConnectionApproved>(playerConnection);
                commandBuffer.AddComponent(playerConnection, new JoinedPlayerComponent { PlayerName = playerName });

                if (approvalRpc.ValueRO.Spectator)
                    commandBuffer.AddComponent(playerConnection, default(SpectatorTag));

                if (approvalRpc.ValueRO.IsHost)
                    commandBuffer.AddComponent(playerConnection, default(HostPlayerTag));

                // PlayerJoinedRpc broadcast is handled by PlayerRegistrationSystem,
                // which runs after NetCode assigns NetworkId (post-approval).
                Debug.Log($"Approved JoinApprovalRequestRpc for '{playerName}'.");
            }

            // Second pass: enter the game only once the approved connection actually has a
            // NetworkId (NetCode assigns it via its own ECB in NetworkReceiveSystemGroup, which
            // plays back after CommandReceiveSystemGroup). Because this ECB is only played back
            // on the NEXT frame, NetworkStreamInGame can never appear before NetworkId — closing
            // the one-frame window where the connection would enter the CommandReceiveSystem
            // query without a NetworkId and crash the command receive job.
            foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>()
                         .WithAll<ConnectionApproved, JoinedPlayerComponent>()
                         .WithNone<NetworkStreamInGame, NetworkStreamRequestDisconnect>()
                         .WithEntityAccess())
            {
                commandBuffer.AddComponent<NetworkStreamInGame>(entity);
                Debug.Log($"Connection {entity.ToFixedString()} entered game (NetworkId {networkId.ValueRO.Value}).");
            }
        }
    }
}
