using Unity.NetCode;

namespace TanksKombat2.Shared
{
    // C2S request RPC: player asks the server to start a new match from the match-end screen.
    public struct StartNewMatchRequestRpc : IRpcCommand
    {
    }
}