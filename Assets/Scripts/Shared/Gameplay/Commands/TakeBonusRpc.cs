using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C event RPC: a bonus was picked up by a tank; drives the client-side pickup effect.
    public struct TakeBonusRpc : IRpcCommand
    {
        public int OwnerNetworkId;
    }
}