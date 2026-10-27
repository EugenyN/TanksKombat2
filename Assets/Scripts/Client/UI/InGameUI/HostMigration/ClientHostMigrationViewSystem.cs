#if !UNITY_SERVER
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Debug-only client system: shows relay/host migration progress in the HUD. Disabled.
    [DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientHostMigrationViewSystem : SystemBase
    {
        private EntityQuery _migrationInfoQuery;

        protected override void OnCreate()
        {
            _migrationInfoQuery = GetEntityQuery(ComponentType.ReadOnly<HostMigrationInfoComponent>());
        }
        
        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var connectionStateView = InGameUIViews.Instance.ConnectionState;

            // show only for clients
            if (ClientServerBootstrap.ServerWorld != null || ClientServerBootstrap.ClientWorld == null)
                return;

            if (_migrationInfoQuery.IsEmptyIgnoreFilter)
            {
                connectionStateView.SetHostMigrationMessageVisible(false);
                return;
            }

            string text;

            var prefix = "<color=#008000ff>[Client]</color>";
            
            var migrationInfo = _migrationInfoQuery.GetSingleton<HostMigrationInfoComponent>();
            if (migrationInfo.IsHostMigration)
                text = $"{prefix} Host migration in progress: ";
            else
                text = $"{prefix} ";
            var connectionTime = UnityEngine.Time.realtimeSinceStartup - migrationInfo.StartTime;

            // If we're waiting for relay but don't have a join code yet, we're still waiting for the lobby to send it to us
            if (migrationInfo.WaitForJoinCode)
            {
                text += $"Waiting for new join code ({connectionTime:F2} s)";

                var session = GameManager.Instance.ConnectionManager.Session;
                if (migrationInfo.OldJoinCode == session.Code)
                    return;
                migrationInfo.WaitForJoinCode = false;
                migrationInfo.StartTime = UnityEngine.Time.realtimeSinceStartup;
                World.EntityManager.SetComponentData(_migrationInfoQuery.GetSingletonEntity(), migrationInfo);
                Debug.Log($"{prefix}[HostMigration] New join code received ({connectionTime:F2} s)");
                return;
            }

            if (migrationInfo.WaitForHostSetup)
            {
                text += $"Waiting for host migration data ({connectionTime:F2} s)";
                return;
            }

            // TODO: Seems this catches the old relay connection when a host migration happens,
            // so immediately sees Established
            var migrationInfoEntity = _migrationInfoQuery.GetSingletonEntity();
            CheckRelayStatus(ref text, World, migrationInfoEntity, prefix, connectionTime);
            
            connectionStateView.SetHostMigrationMessage(text);
            connectionStateView.SetHostMigrationMessageVisible(true);
        }

        internal static void CheckRelayStatus(ref string statusText, World world, Entity migrationInfoEntity, 
            string prefix, double connectionTime)
        {
            var relayConnectionStatus = GetRelayConnectionStatus(world);
            switch (relayConnectionStatus)
            {
                case RelayConnectionStatus.Established:
                    statusText += "Relay connection established";
                    Debug.Log($"{prefix}[HostMigration] Relay connection established ({connectionTime:F2} s)");
                    world.EntityManager.DestroyEntity(migrationInfoEntity);
                    break;
                case RelayConnectionStatus.NotEstablished:
                    statusText += $"Connecting to relay server ({connectionTime:F2} s)";
                    break;
                case RelayConnectionStatus.AllocationInvalid:
                    statusText += "Relay connection failed; allocation is invalid";
                    break;
                // During client migration from old host to the new there will be some time where it's not connected to anything and waiting for the new join code
                case RelayConnectionStatus.NotUsingRelay:
                    break;
                default:
                    statusText += "Unexpected Relay connection status";
                    break;
            }
        }

        private static RelayConnectionStatus GetRelayConnectionStatus(World world)
        {
            using var drvQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            var networkStreamDriver = drvQuery.GetSingleton<NetworkStreamDriver>();

            // Get the server driver with the UDPNetworkInterface and with relay enabled
            var status = RelayConnectionStatus.NotUsingRelay;
            for (var i = networkStreamDriver.DriverStore.FirstDriver;
                 status == RelayConnectionStatus.NotUsingRelay && i < networkStreamDriver.DriverStore.LastDriver;
                 ++i)
            {
                var networkDriver = networkStreamDriver.DriverStore.GetDriverRO(i);
                world.EntityManager.CompleteAllTrackedJobs();
                status = networkDriver.GetRelayConnectionStatus();
            }

            return status;
        }
    }
}
#endif