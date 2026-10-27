using Unity.Collections;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // C2S approval RPC: client sends this during the connection Approval phase to request joining the match.
    public struct JoinApprovalRequestRpc : IApprovalRpcCommand
    {
        public FixedString32Bytes PlayerName;
        public bool Spectator;
        public bool IsHost;
    }
}