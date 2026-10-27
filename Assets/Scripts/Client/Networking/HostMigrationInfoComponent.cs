#if !UNITY_SERVER
using Unity.Collections;
using Unity.Entities;

namespace TanksKombat2.Client
{
    // Client-only marker/state component created during host migration to track relay  reconnection progress;
    public struct HostMigrationInfoComponent : IComponentData
    {
        public bool WaitForJoinCode;
        public bool WaitForHostSetup;
        public bool IsHostMigration;
        public float StartTime;
        public FixedString32Bytes OldJoinCode;
    }
}
#endif