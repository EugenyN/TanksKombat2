using Unity.Collections;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C broadcast RPC: server announces a newly registered player (id, name, color).
    public struct PlayerJoinedRpc : IRpcCommand
    {
        public int NetworkId;
        public FixedString32Bytes PlayerName;
        public int ColorIndex;
    }
}