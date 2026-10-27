using Unity.Entities;
using Unity.NetCode;

namespace TanksKombat2.Shared
{
    public enum MatchState : byte
    {
        WaitingForPlayers,
        RoundInProgress,
        RoundFinished,
        MatchFinished
    }

    public struct MatchTimingSettings
    {
        public float WaitingForPlayersTime;
        public float NewRoundTime;
        public float RoundFinishedTime;
        public float RoundFightMessageDelay;
        public float MatchResultTime;
    }

    // Ghost singleton carrying the server-authoritative match flow state (phase, countdown,
    // round number, winner).
    public struct MatchStateComponent : IComponentData
    {
        [GhostField] public MatchState State;
        [GhostField] public float TimeRemaining;
        [GhostField] public int Round;
        [GhostField] public int WinnerNetworkId;

        public MatchTimingSettings TimingSettings;
    }
}