using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TanksKombat2.Bootstrap;
using TanksKombat2.Shared;
using Unity.Collections;
using UnityEngine;
using Unity.NetCode;
using Unity.Entities;
using Unity.NetCode.HostMigration;
using Unity.Networking.Transport;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;

namespace TanksKombat2.Client
{
    // Client-side facade over Unity Services sessions and the NetCode transport: initializes
    // services, hosts/joins Relay sessions or Direct-IP servers, tracks the active session and
    // owns disconnect/quit cleanup.
    public class ConnectionManager
    {
        public ISession Session => _session;
        
        private readonly bool EnableHostMigration = false;
        private readonly bool EnableRelay = true;
        
        private ISession _session;

        public bool IsHost
        {
            get
            {
                var hostWorld = ClientServerBootstrap.ServerWorld;
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
                if (NetCodeConfig.Global.HostWorldModeSelection == NetCodeConfig.HostWorldMode.SingleWorld)
                    return hostWorld != null && hostWorld.IsHost();
                else
                    return hostWorld != null;
#else
                return hostWorld != null;
#endif
            }
        }

        private async Task InitializeServices(CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    ct.ThrowIfCancellationRequested();
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
                Debug.Log("Initialized services");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception while initializing services: {e.Message}");
                if (e.InnerException != null)
                    Debug.LogError(e.InnerException.Message);
                throw new Exception($"Failed to initialize Unity Services: {e.Message}", e);
            }
        }

        public async Task StartSessionsClientServer(string portText, string sessionIdText,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            progress?.Report("Initializing Unity Services...");
            await InitializeServices(ct);
            if (!AuthenticationService.Instance.IsSignedIn)
                throw new Exception("Failed to sign in to Unity Services.");

            ct.ThrowIfCancellationRequested();
            var port = GameBootstrap.ParsePortOrDefault(portText);

            var options = new SessionOptions { MaxPlayers = ConnectionUtils.MaxSessionPlayers };

            if (EnableRelay)
                options = options.WithRelayNetwork();
            else
                options = options.WithDirectNetwork(port: port);

            if (EnableHostMigration)
                options = options.WithHostMigration();

            options = options.WithNetworkHandler(new CustomNetcodeNetworkHandler());

            progress?.Report("Creating game session...");
            _session = await MultiplayerService.Instance.CreateOrJoinSessionAsync(sessionIdText, options, ct);

            _session.SessionMigrated += OnSessionMigrated;
            _session.SessionHostChanged += OnHostChanged;

            if (EnableHostMigration)
                ClientServerBootstrap.ServerWorld.EntityManager.CreateSingleton<EnableHostMigration>();
        }

        public void StartIpPortClientServer(string portText, World server, World client, IProgress<string> progress = null)
        {
            progress?.Report("Starting local server...");
            var port = GameBootstrap.ParsePortOrDefault(portText);
            GameBootstrap.StartIpPortClientServer(port, server, client);
        }
        
        private void OnHostChanged(string newHostId)
        {
            if (!EnableHostMigration) return; // service-side lobby host promotion is not a game-session migration
#if !UNITY_SERVER
            SetHostMigrationInfo(new HostMigrationInfoComponent
            {
                WaitForJoinCode = true, OldJoinCode = _session.Code, IsHostMigration = true, 
                StartTime = Time.realtimeSinceStartup
            });
#endif
        }
        
#if !UNITY_SERVER
        // Set up the component for tracking the relay connection status during the host migration,
        // the HUD UI then prints the information
        private Entity SetHostMigrationInfo(HostMigrationInfoComponent infoComponent)
        {
            var em = ClientServerBootstrap.ClientWorld.EntityManager;
            
            using var relayEntityQuery = em.CreateEntityQuery(ComponentType.ReadOnly<HostMigrationInfoComponent>());
            Entity relayEntity;
            // There could already be a HostMigrationInfoComponent if we got a host migration event but the host
            // failed and another one was picked
            if (!relayEntityQuery.IsEmptyIgnoreFilter)
                relayEntity = relayEntityQuery.ToEntityArray(Allocator.Temp)[0];
            else
                relayEntity = em.CreateEntity(ComponentType.ReadOnly<HostMigrationInfoComponent>());
            em.AddComponentData(relayEntity, infoComponent);
            return relayEntity;
        }
#endif

        // When host migration is completed cleanup and reset the migration info elements.
        private void OnSessionMigrated()
        {
#if !UNITY_SERVER
            if (_session.IsHost)
            {
            }
            else
            {
                var em = ClientServerBootstrap.ClientWorld.EntityManager;
                using var waitForRelayQuery = em.CreateEntityQuery(ComponentType.ReadOnly<HostMigrationInfoComponent>());
                em.DestroyEntity(waitForRelayQuery);
            }
#endif
        }

        public async Task ConnectToIpPortServer(string addressText, string portText, EntityManager em,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            progress?.Report("Connecting to server...");
            Debug.Log($"[ConnectToServer] Called on '{addressText}:{portText}'.");
            
            var ep = NetworkEndpoint.Parse(addressText, GameBootstrap.ParsePortOrDefault(portText));
            if (!ep.IsValid)
                throw new Exception($"Invalid IP address: '{addressText}'.");
            
            using var drvQuery = em.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var clientDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            clientDriver.RequireConnectionApproval = true;
            var connectionEntity = clientDriver.Connect(em, ep);

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                if (!em.Exists(connectionEntity))
                    throw new Exception(ConnectionErrors.GetMessage(em, connectionEntity));

                var connection = em.GetComponentData<NetworkStreamConnection>(connectionEntity);
                if (connection.CurrentState == ConnectionState.State.Connected)
                    return;
                if (connection.CurrentState == ConnectionState.State.Disconnected)
                    throw new Exception(ConnectionErrors.GetMessage(em, connectionEntity));

                await Awaitable.NextFrameAsync(ct);
            }
        }

        public async Task ConnectToSessionsServer(string sessionIdText,
            IProgress<string> progress = null, CancellationToken ct = default)
        {
            progress?.Report("Initializing Unity Services...");
            await InitializeServices(ct);

            ct.ThrowIfCancellationRequested();
            Debug.Log($"[ConnectToServer] Called on session ID '{sessionIdText}'.");

            var options = new JoinSessionOptions();
            // When joining the session network type will automatically be used (relay/direct)
            if (EnableHostMigration)
                options = options.WithHostMigration();

            options = options.WithNetworkHandler(new CustomNetcodeNetworkHandler());

            progress?.Report("Joining session...");
            try
            {
                _session = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionIdText, options, ct);
            }
            catch (OperationCanceledException)
            {
                // Surface cancellation unchanged so callers can distinguish cancel/timeout
                // from a join failure (the SDK rethrows the raw token's OCE).
                throw;
            }
            catch (SessionException)
            {
                // SessionException (including the NetworkSetupFailed ones thrown by
                // CustomNetcodeNetworkHandler) propagates unchanged through the SDK with a
                // clean message — rethrow it as-is so the SessionError stays machine-readable.
                throw;
            }
            catch (Exception e)
            {
                // The Sessions SDK wraps other internal failures as
                // `new SessionException($"Joining network failed: {innerException}", ...)` — it
                // embeds the inner's ToString() into the message WITHOUT setting InnerException.
                // Extract the first line so the UI shows a readable message.
                var index = e.Message.IndexOf("\r\n", StringComparison.Ordinal);
                var message = index >= 0 ? e.Message[..index] : e.Message;
                throw new Exception(message, e);
            }
            _session.SessionMigrated += OnSessionMigrated;
            _session.SessionHostChanged += OnHostChanged;
        }

        public async Task Disconnect()
        {
            Debug.Log("[Disconnect] Called.");

            if (MultiplayerService.Instance == null) return;

            if (_session != null)
            {
                _session.SessionMigrated -= OnSessionMigrated;
                _session.SessionHostChanged -= OnHostChanged;
            }
            _session = null;
            
            var sessions = MultiplayerService.Instance.Sessions.ToArray();
            foreach (var session in sessions)
            {
                try
                {
                    Debug.Log($"[Disconnect] Leaving {session.Value.Id}");
                    await session.Value.LeaveAsync();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Disconnect] Failed to leave session '{session.Value.Id}': {e.Message}");
                }
            }
        }
        
        // Returns the host's first non-loopback IPv4 address for display in the HUD; falls back
        // to 0.0.0.0 when DNS resolution fails or no suitable address is found.
        public string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !ip.Equals(IPAddress.Loopback))
                        return ip.ToString();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to resolve local IP address: {e.Message}");
            }

            return IPAddress.Any.ToString();
        }
    }
}
