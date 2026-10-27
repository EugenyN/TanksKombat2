using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C broadcast RPC: a tank was destroyed; used for the event log and kill attribution.
    public struct TankDestroyedRpc : IRpcCommand
    {
        public int KillerNetworkId;
        public int VictimNetworkId;
    }
}