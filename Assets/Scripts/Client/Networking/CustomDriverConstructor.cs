using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Multiplayer;

namespace TanksKombat2.Client
{
    // This driver constructor and network handler are the same as the default ones
    // from the Multiplayer SDK except the network handler contains fixes. One where
    // the default driver constructor is overwritten but not replaced back after use, so
    // if you use sessions once then it will the be session driver constructor used next time
    // unless the default driver constructor is again set. And another where a query
    // checking for network disconnects doesn't verify the world still exists after waiting
    // for a delay and re-checking (will result in crash if world was destroyed while waiting).
    internal class CustomDriverConstructor : INetworkStreamDriverConstructor
    {
        public NetworkConfiguration Configuration;
        public const int InvalidDriverId = 0;

        public int ClientIpcDriverId { get; private set; } = InvalidDriverId;
        public int ClientUdpDriverId { get; private set; } = InvalidDriverId;
        public int ClientWebSocketDriverId { get; private set; } = InvalidDriverId;

        public int ServerIpcDriverId { get; private set; } = InvalidDriverId;
        public int ServerUdpDriverId { get; private set; } = InvalidDriverId;
        public int ServerWebSocketDriverId { get; private set; } = InvalidDriverId;

        // Single-world hosts communicate through the world itself, so no IPC driver is needed.
#if NETCODE_EXPERIMENTAL_SINGLE_WORLD_HOST
        private bool IsSingleWorldHost =>
            Configuration.Role == NetworkRole.Host &&
            NetCodeConfig.Global.HostWorldModeSelection == NetCodeConfig.HostWorldMode.SingleWorld;
#else
        private bool IsSingleWorldHost => false;
#endif

        public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var ipcSettings = DefaultDriverBuilder.GetNetworkSettings();

            var driverId = NetworkDriverStore.FirstDriverId;

            if (Configuration.Role == NetworkRole.Host || Configuration.Role == NetworkRole.Server)
            {
                if (!IsSingleWorldHost)
                {
                    DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, ipcSettings);
                    ClientIpcDriverId = driverId;
                }
            }
            else if (Configuration.Role == NetworkRole.Client)
            {
                var udpSettings = DefaultDriverBuilder.GetNetworkSettings();

                if (Configuration.Type == NetworkType.Relay)
                {
                    var relayClientData = Configuration.RelayClientData;
                    udpSettings.WithRelayParameters(ref relayClientData);
                }

#if !UNITY_WEBGL
                DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, udpSettings);
                ClientUdpDriverId = driverId;
#else
                DefaultDriverBuilder.RegisterClientWebSocketDriver(world, ref driverStore, netDebug, udpSettings);
                ClientWebSocketDriverId = driverId;
#endif
            }
        }

        public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
        {
            var ipcSettings = DefaultDriverBuilder.GetNetworkSettings();

            var driverId = NetworkDriverStore.FirstDriverId;

            if (Configuration.Role == NetworkRole.Host)
            {
                DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, ipcSettings);
                ServerIpcDriverId = driverId;
                driverId++;
            }

            var udpSettings = DefaultDriverBuilder.GetNetworkSettings();

            if (Configuration.Type == NetworkType.Relay)
            {
                var relayServerData = Configuration.RelayServerData;
                udpSettings.WithRelayParameters(ref relayServerData);
            }

#if !UNITY_WEBGL
            DefaultDriverBuilder.RegisterServerUdpDriver(world, ref driverStore, netDebug, udpSettings);
            ServerUdpDriverId = driverId;
#else
            DefaultDriverBuilder.RegisterServerWebSocketDriver(world, ref driverStore, netDebug, udpSettings);
            ClientWebSocketDriverId = driverId;
#endif
        }
    }
}