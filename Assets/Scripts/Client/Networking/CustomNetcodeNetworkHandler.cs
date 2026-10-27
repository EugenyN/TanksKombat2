using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.NetCode;
using Unity.Services.Multiplayer;

namespace TanksKombat2.Client
{
    // INetworkHandler that wires the NetCode client/server worlds and drivers into the
    // Multiplayer SDK sessions (Unity.Services.Multiplayer). Mirrors the SDK's default
    // EntitiesNetcodeNetworkHandler with fixes/improvements.
    internal class CustomNetcodeNetworkHandler : INetworkHandler
    {
        private const int ConnectionTimeoutSeconds = 30;

        private NetworkConfiguration _configuration;
        private World _clientWorld;
        private World _serverWorld;

        private NetworkStreamDriver _clientDriver;
        private NetworkStreamDriver _serverDriver;
        private Entity _connectionEntity;

        private INetworkStreamDriverConstructor _oldDriverConstructor;
        private readonly CustomDriverConstructor _driverConstructor = new CustomDriverConstructor();

        // Cancellation token for the client connection poll (ValidateConnectionAsync). Set from
        // the token the SDK passes through StartAsync(NetworkConfiguration, CancellationToken) —
        // a linked token (caller token + Application.exitCancellationToken). Defaults to
        // CancellationToken.None (never cancels) when only the tokenless overload is used.
        internal CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        // SDK entry point with caller-driven cancellation (INetworkHandler overload): aborts the
        // connection poll when the session creation is cancelled or the application exits.
        public Task StartAsync(NetworkConfiguration configuration, CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
            return StartAsync(configuration);
        }

        public async Task StartAsync(NetworkConfiguration configuration)
        {
            _configuration = configuration;

            SetupWorlds();
            ValidateWorlds();
            SetupDriverConstructor();

            try
            {
                switch (_configuration.Role)
                {
                    case NetworkRole.Client:
                        await ConnectAsync();
                        break;
                    case NetworkRole.Host:
                        Listen();
                        // Single-world host has no IPC self-connection: the world already contains
                        // the fake LocalConnection that represents the host player.
                        if (!IsSingleWorldHostMode)
                            await SelfConnectAsync();
                        break;
                    case NetworkRole.Server:
                        Listen();
                        break;
                }
            }
            catch
            {
                // The SDK does not call StopAsync when StartAsync fails (only on a later
                // LeaveAsync), so without this our constructor would leak with a stale
                // NetworkConfiguration and be used to build drivers for the next session/world.
                RestoreDriverConstructor();
                throw;
            }
        }

        // Restores the previous driver constructor (same guard as StopAsync: skip if a newer
        // session has already replaced it) and resets our per-session state.
        private void RestoreDriverConstructor()
        {
            if (NetworkStreamReceiveSystem.DriverConstructor == _driverConstructor)
                NetworkStreamReceiveSystem.DriverConstructor = _oldDriverConstructor;
            _oldDriverConstructor = null;
        }

        public async Task StopAsync()
        {
            CleanupServer();
            await CleanupClientAsync();
            RestoreDriverConstructor();
        }

        // True when the project is configured for single-world hosting and this session is the host.
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
        private bool IsSingleWorldHostMode =>
            _configuration.Role == NetworkRole.Host &&
            NetCodeConfig.Global.HostWorldModeSelection == NetCodeConfig.HostWorldMode.SingleWorld;
#else
        private bool IsSingleWorldHostMode => false;
#endif
        
        private void SetupWorlds()
        {
            // Single-world host: client and server systems share one world. Reuse an existing host
            // world if the bootstrap already created one, otherwise create it here.
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
            if (IsSingleWorldHostMode)
            {
                var hostWorld = ClientServerBootstrap.ServerWorld;
                if (hostWorld == null || !hostWorld.IsHost())
                    hostWorld = ClientServerBootstrap.CreateSingleWorldHost("HostWorld");

                _serverWorld = hostWorld;
                _clientWorld = hostWorld;
                return;
            }
#endif

            _clientWorld = ClientServerBootstrap.ClientWorld;
            _serverWorld = ClientServerBootstrap.ServerWorld;

            if ((_configuration.Role is NetworkRole.Client or NetworkRole.Host) && _clientWorld == null)
            {
                _clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
            }

            if ((_configuration.Role is NetworkRole.Host or NetworkRole.Server) && _serverWorld == null)
            {
                _serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            }
        }

        private void SetupDriverConstructor()
        {
            _driverConstructor.Configuration = _configuration;
            _oldDriverConstructor = NetworkStreamReceiveSystem.DriverConstructor;
            NetworkStreamReceiveSystem.DriverConstructor = _driverConstructor;
        }

        private void SetupClientDriver()
        {
            using var drvQuery =
                _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            _clientDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            _clientDriver.RequireConnectionApproval = true;

            using (var debugQuery = _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                NetworkStreamReceiveSystem.DriverConstructor.CreateClientDriver(_clientWorld, ref driverStore,
                    netDebug);
                _clientDriver.ResetDriverStore(_clientWorld.Unmanaged, ref driverStore);
            }
        }

        private void SetupServerDriver()
        {
            using var drvQuery =
                _serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            _serverDriver = drvQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW;
            _serverDriver.RequireConnectionApproval = true;

            using (var debugQuery = _serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetDebug>()))
            {
                var netDebug = debugQuery.GetSingleton<NetDebug>();
                var driverStore = new NetworkDriverStore();
                NetworkStreamReceiveSystem.DriverConstructor.CreateServerDriver(_serverWorld, ref driverStore,
                    netDebug);
                _serverDriver.ResetDriverStore(_serverWorld.Unmanaged, ref driverStore);
            }
        }

        private void Listen()
        {
            ValidateWorld(_serverWorld);
            SetupServerDriver();

            Unity.Networking.Transport.NetworkEndpoint listenEndpoint = default;

            switch (_configuration.Type)
            {
                case NetworkType.Direct:
                    listenEndpoint = _configuration.DirectNetworkListenAddress;
                    break;
                case NetworkType.Relay:
                    listenEndpoint = Unity.Networking.Transport.NetworkEndpoint.AnyIpv4;
                    break;
            }

            if (!listenEndpoint.IsValid)
            {
                throw new SessionException("Invalid endpoint to listen to", SessionError.NetworkSetupFailed, null);
            }

            if (_serverDriver.Listen(listenEndpoint))
            {
                var serverUdpPort = _serverDriver.GetLocalEndPoint(_driverConstructor.ServerUdpDriverId).Port;

                if (_configuration.Type == NetworkType.Direct)
                {
                    _configuration.UpdatePublishPort(serverUdpPort);
                }
            }
            else
            {
                throw new SessionException("We assume the first driver created is IPC Network Interface!",
                    SessionError.NetworkSetupFailed, null);
            }
        }

        private async Task SelfConnectAsync()
        {
            ValidateWorld(_clientWorld);
            SetupClientDriver();

            var ipcPort = _serverDriver.GetLocalEndPoint(_driverConstructor.ServerIpcDriverId).Port;
            var selfEndpoint = Unity.Networking.Transport.NetworkEndpoint.LoopbackIpv4.WithPort(ipcPort);
            _connectionEntity = _clientDriver.Connect(_clientWorld.EntityManager, selfEndpoint);

            await ValidateConnectionAsync();
        }

        private async Task ConnectAsync()
        {
            ValidateWorld(_clientWorld);
            SetupClientDriver();

            Unity.Networking.Transport.NetworkEndpoint connectEndpoint = default;

            switch (_configuration.Type)
            {
                case NetworkType.Direct:
                {
                    connectEndpoint = _configuration.DirectNetworkPublishAddress;
                    break;
                }
                case NetworkType.Relay:
                {
                    connectEndpoint = _configuration.RelayClientData.Endpoint;
                    break;
                }
            }

            if (!connectEndpoint.IsValid)
            {
                throw new SessionException("Invalid endpoint to connect to", SessionError.NetworkSetupFailed, null);
            }

            _connectionEntity = _clientDriver.Connect(_clientWorld.EntityManager, connectEndpoint);
            await ValidateConnectionAsync();
        }

        private async Task ValidateConnectionAsync()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var connection = _clientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(_connectionEntity);

            while (connection.CurrentState != ConnectionState.State.Connected &&
                   connection.CurrentState != ConnectionState.State.Disconnected)
            {
                CancellationToken.ThrowIfCancellationRequested();

                if (stopwatch.Elapsed.TotalSeconds >= ConnectionTimeoutSeconds)
                {
                    await CleanupClientAsync();
                    throw new SessionException("Connection timeout. Failed connection", SessionError.NetworkSetupFailed,
                        null);
                }

                await Task.Delay(100, CancellationToken);

                // The world can be destroyed while we wait (quit, scene unload, session
                // teardown) — bail out instead of touching a disposed EntityManager.
                if (_clientWorld == null || !_clientWorld.IsCreated)
                    throw new SessionException("Client world was destroyed while waiting for the connection.",
                        SessionError.NetworkSetupFailed, null);

                if (!_clientWorld.EntityManager.Exists(_connectionEntity))
                {
                    throw new SessionException(ConnectionErrors.GetMessage(_clientWorld.EntityManager, _connectionEntity),
                        SessionError.NetworkSetupFailed, null);
                }

                connection = _clientWorld.EntityManager.GetComponentData<NetworkStreamConnection>(_connectionEntity);

                if (connection.CurrentState == ConnectionState.State.Disconnected)
                    throw new SessionException(ConnectionErrors.GetMessage(_clientWorld.EntityManager, _connectionEntity),
                        SessionError.NetworkSetupFailed, null);
            }

            if (connection.CurrentState == ConnectionState.State.Disconnected)
                throw new SessionException(ConnectionErrors.GetMessage(_clientWorld.EntityManager, _connectionEntity),
                    SessionError.NetworkSetupFailed, null);
        }

        private async Task CleanupClientAsync()
        {
            if (_clientWorld != null &&
                _clientWorld.IsCreated &&
                _clientWorld.EntityManager.Exists(_connectionEntity))
            {
                _clientWorld.EntityManager.AddComponent<NetworkStreamRequestDisconnect>(_connectionEntity);
                _connectionEntity = default;

                var connectionQuery =
                    _clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
                while (!connectionQuery.IsEmpty)
                {
                    await Task.Delay(100);
                    if (_clientWorld == null || !_clientWorld.IsCreated)
                    {
                        return; // if world is gone after the yield immediately return, the query will already be disposed
                    }
                }

                connectionQuery.Dispose();
            }
        }

        private void CleanupServer()
        {
            if (_serverWorld != null && _serverWorld.IsCreated)
            {
                _serverWorld.Dispose();
                _serverWorld = null;
            }
        }

        private void ValidateWorlds()
        {
            if ((_configuration.Role is NetworkRole.Client or NetworkRole.Host) &&
                _clientWorld is not { IsCreated : true })
            {
                throw new SessionException(
                    "Invalid client world. Please make sure it has been created before attempting to setup network.",
                    SessionError.NetworkSetupFailed, null);
            }

            if ((_configuration.Role is NetworkRole.Server or NetworkRole.Host) &&
                _serverWorld is not { IsCreated : true })
            {
                throw new SessionException(
                    "Invalid server world. Please make sure it has been created before attempting to setup network.",
                    SessionError.NetworkSetupFailed, null);
            }
        }

        private void ValidateWorld(World world)
        {
            if (world == null || !world.IsCreated)
            {
                throw new SessionException("Invalid world to setup network", SessionError.NetworkSetupFailed, null);
            }
        }
    }
}