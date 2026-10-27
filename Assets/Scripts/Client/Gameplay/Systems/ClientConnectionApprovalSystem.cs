using TanksKombat2.Shared;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace TanksKombat2.Client
{
    // Client-side connection approval: sends JoinApprovalRequestRpc for client connections during
    // Handshake/Approval, then — once the server approves and the sub-scene has streamed in —
    // marks the local connection as joined and adds NetworkStreamInGame to start sending commands.
    // Single-world host: there is no client-to-server approval round trip. NetCode's fake local
    // connection (LocalConnection + NetworkId, no NetworkStreamConnection) is handled directly in
    // Phase 2, including SpectatorTag and HostPlayerTag, because the host never passes through
    // ServerConnectionApprovalSystem. NetworkStreamInGame is not required by NetCode for that fake
    // connection; the project uses it as its own "in game" gate for client systems.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct ClientConnectionApprovalSystem : ISystem
    {
        private FixedString32Bytes _playerName;
   
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RpcCollection>();
            
            if (state.WorldUnmanaged.IsThinClient())
            {
                // Seed by the unique world sequence number: thin-client worlds are created in the
                // same frame, so ElapsedTime would give them all the same name.
                var random = Random.CreateFromIndex((uint)state.WorldUnmanaged.SequenceNumber);
                _playerName = $"Thin{random.NextInt(1, 9999)}";
            }
            else
            {
                _playerName.CopyFromTruncated(GameSettings.Instance.PlayerName);
            }
        }
        
        public void OnUpdate(ref SystemState state)
        {
            // Phase 1: For client connections that haven't received a NetworkId yet (still in
            // Handshake/Approval), send the JoinApprovalRequestRpc exactly once. IApprovalRpcCommand
            // can only be sent while the connection is in Handshake or Approval state — not
            // Connecting — so we check the state before sending. A single-world host has no
            // approval RPC round trip and is handled by Phase 2 instead; skipping the loop there
            // also prevents matching the server-side connections of remote/thin clients.
            if (!state.WorldUnmanaged.IsHost())
            {
                // "I am the host" must be computed here, not in OnCreate: in the Relay flow the
                // client world is created before the server world, so the check would fail there.
                // Thin clients run inside the host's process but must not claim host status.
                bool isHost = ClientServerBootstrap.ServerWorld != null && !state.WorldUnmanaged.IsThinClient();
                bool spectatorMode = GameSettings.Instance.SpectatorMode && !state.WorldUnmanaged.IsThinClient();

                var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
                foreach (var (connection, entity) in SystemAPI.Query<RefRO<NetworkStreamConnection>>()
                             .WithNone<NetworkId, ApprovalStartedTag>().WithEntityAccess())
                {
                    if (connection.ValueRO.CurrentState != ConnectionState.State.Handshake &&
                        connection.ValueRO.CurrentState != ConnectionState.State.Approval)
                        continue;

                    var sendApprovalMsg = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(sendApprovalMsg, new JoinApprovalRequestRpc {
                        PlayerName = _playerName, Spectator = spectatorMode, IsHost = isHost
                    });
                    commandBuffer.AddComponent<SendRpcCommandRequest>(sendApprovalMsg);
                    commandBuffer.AddComponent<ApprovalStartedTag>(entity);

                    Debug.Log($"Sent JoinApprovalRequestRpc for '{_playerName}'.");
                }
                commandBuffer.Playback(state.EntityManager);
                commandBuffer.Dispose();
            }

            // Phase 2: Enter the game once the sub-scene is loaded (GamePrefabsComponent present).
            // LocalConnection identifies this client's own connection: it is the fake host
            // connection in a host world, or the approved client connection otherwise (NetworkId
            // is not a singleton in a host world). Waiting for the sub-scene ensures ghost prefabs
            // are registered before snapshots start arriving.
            if (!SystemAPI.HasSingleton<GamePrefabsComponent>())
                return;
            if (!SystemAPI.TryGetSingletonEntity<LocalConnection>(out var playerConnection))
                return;
            if (SystemAPI.HasComponent<NetworkStreamInGame>(playerConnection))
                return;

            // A single-world host never passes through ServerConnectionApprovalSystem, so apply the
            // spectator and host markers here.
            if (state.WorldUnmanaged.IsHost())
            {
                bool spectatorMode = GameSettings.Instance.SpectatorMode && !state.WorldUnmanaged.IsThinClient();
                if (spectatorMode)
                    state.EntityManager.AddComponent<SpectatorTag>(playerConnection);
                state.EntityManager.AddComponent<HostPlayerTag>(playerConnection);
            }

            state.EntityManager.AddComponentData(playerConnection, new JoinedPlayerComponent {
                PlayerName = _playerName
            });
            state.EntityManager.AddComponentData(playerConnection, default(NetworkStreamInGame));
        }
    }
}
