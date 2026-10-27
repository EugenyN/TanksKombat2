using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C event RPC: a bonus spawned on the map; triggers the client-side spawn effect.
    public struct BonusSpawnedRpc : IRpcCommand
    {
    }
}