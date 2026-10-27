#if !UNITY_SERVER
using Unity.Entities;
using Unity.NetCode;
using Unity.NetCode.HostMigration;

namespace TanksKombat2.Client.UI
{
    // Debug-only server system: shows host migration progress and stats in the HUD. Disabled.
    [DisableAutoCreation]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class ServerHostMigrationViewSystem : SystemBase
    {
        private EntityQuery _migrationInfoQuery;

        protected override void OnCreate()
        {
            _migrationInfoQuery = GetEntityQuery(ComponentType.ReadOnly<HostMigrationInfoComponent>());

            RequireForUpdate<HostMigrationStats>();
            RequireForUpdate<EnableHostMigration>();
        }

        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var connectionStateView = InGameUIViews.Instance.ConnectionState;

            // show only for hosts
            if (ClientServerBootstrap.ServerWorld == null)
                return;

            if (_migrationInfoQuery.IsEmptyIgnoreFilter)
            {
                connectionStateView.SetHostMigrationMessageVisible(false);
                return;
            }

            var prefix = "<color=#ff0000ff>[Server]</color>";

            var stats = SystemAPI.GetSingleton<HostMigrationStats>();
            var text = $"{prefix} Ghost Count: {stats.GhostCount} Prefab Count: {stats.PrefabCount} Update Size: {stats.UpdateSize} Total Update Size: {stats.TotalUpdateSize}\n";

            var migrationInfo = _migrationInfoQuery.GetSingleton<HostMigrationInfoComponent>();
            if (migrationInfo.IsHostMigration)
                text += $"{prefix} Host migration in progress: ";
            else
                text += $"{prefix} Starting: ";

            var connectionTime = UnityEngine.Time.realtimeSinceStartup - migrationInfo.StartTime;
            var migrationInfoEntity = _migrationInfoQuery.GetSingletonEntity();
            ClientHostMigrationViewSystem.CheckRelayStatus(ref text, World, migrationInfoEntity, prefix, connectionTime);
            
            connectionStateView.SetHostMigrationMessage(text);
            connectionStateView.SetHostMigrationMessageVisible(true);
        }
    }
}
#endif