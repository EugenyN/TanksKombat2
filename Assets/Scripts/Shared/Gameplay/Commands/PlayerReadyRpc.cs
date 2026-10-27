using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // C2S RPC: player signals readiness (Fire in WaitingForPlayers during round 1).
    public struct PlayerReadyRpc : IRpcCommand
    {
    }
}