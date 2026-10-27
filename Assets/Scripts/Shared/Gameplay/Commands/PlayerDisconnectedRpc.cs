using Unity.Collections;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // S2C: the server notifies clients that a player disconnected.
    public struct PlayerDisconnectedRpc : IRpcCommand
    {
        public int NetworkId;
        public FixedString32Bytes PlayerName;
        public int ColorIndex;
    }
}