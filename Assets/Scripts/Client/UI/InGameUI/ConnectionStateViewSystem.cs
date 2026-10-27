using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: HUD connection status line. Self-hosts show "Host: <session id or local IP>",
    // clients show "<session id or server endpoint> | <ping / Timeout / Connecting / Connected>".
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ConnectionStateViewSystem : SystemBase
    {
        private string _hostId;
        
        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            
            var connectionStateView = InGameUIViews.Instance.ConnectionState;
            var connectionManager = GameManager.Instance.ConnectionManager;
            
            // throttle the status line to every 30th frame: string interpolation allocate,
            // and the HUD does not need per-frame precision.
            if (UnityEngine.Time.frameCount % 30 != 0) return;
            
            if (connectionManager.IsHost)
            {
                if (string.IsNullOrEmpty(_hostId))
                {
                    _hostId = connectionManager.Session != null ? 
                        connectionManager.Session.Id : connectionManager.GetLocalIPAddress();
                    connectionStateView.SetText("Host: " + _hostId);
                }
            }
            else
            {
                if (!TryGetConnectionInfo(out bool connected, out var networkSnapshotAck, out var endpoint))
                {
                    connectionStateView.SetText("Disconnected!");
                }
                else
                {
                    if (string.IsNullOrEmpty(_hostId))
                    {
                        _hostId = connectionManager.Session != null ? 
                            connectionManager.Session.Id : endpoint.Address;
                    }
                    
                    if (connected)
                    {
                        var pingText = networkSnapshotAck.EstimatedRTT > 0
                            ? $"{(int)networkSnapshotAck.EstimatedRTT}ms" : "Connected";
                        var msSinceLastPacket = NetworkTimeSystem.TimestampMS - networkSnapshotAck.LastReceiveTimestamp;
                        if (networkSnapshotAck.LastReceiveTimestamp != 0 && msSinceLastPacket > 1000)
                            connectionStateView.SetText($"{_hostId} | Timeout: {msSinceLastPacket / 1000.0:F1} sec");
                        else
                            connectionStateView.SetText($"{_hostId} | {pingText}");
                    }
                    else
                        connectionStateView.SetText($"{_hostId} | Connecting");
                }
            }
        }

        private bool TryGetConnectionInfo(out bool connected, out NetworkSnapshotAck networkSnapshotAck, out NetworkEndpoint endpoint)
        {
            connected = false;
            networkSnapshotAck = default;
            endpoint = default;

            if (!SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(out var connectionEntity))
                return false;

            var connection = EntityManager.GetComponentData<NetworkStreamConnection>(connectionEntity);
            endpoint = SystemAPI.GetSingleton<NetworkStreamDriver>().GetRemoteEndPoint(connection);
            connected = EntityManager.HasComponent<NetworkId>(connectionEntity);
            if (connected)
                networkSnapshotAck = EntityManager.GetComponentData<NetworkSnapshotAck>(connectionEntity);
            return true;
        }
    }
}