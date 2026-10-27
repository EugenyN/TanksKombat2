using UnityEngine;
using Unity.Entities;

namespace TanksKombat2.Shared
{
    // Authoring for match/round timings; bakes the authoritative MatchStateComponent singleton.
    public class MatchStateAuthoring : MonoBehaviour
    {
        [Tooltip("Waiting-for-players countdown of the first round (seconds).")]
        public float WaitingForPlayersTime = 15.5f;

        [Tooltip("Countdown between rounds (seconds).")]
        public float NewRoundTime = 3.0f;

        [Tooltip("RoundFinished result display time (seconds).")]
        public float RoundFinishedTime = 5.0f;

        [Tooltip("Time the \"Fight !\" message shows before a round starts (seconds).")]
        public float RoundFightMessageDelay = 1.5f;

        [Tooltip("MatchFinished result display time (seconds).")]
        public float MatchResultTime = 20.0f;

        private class Baker : Baker<MatchStateAuthoring>
        {
            public override void Bake(MatchStateAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.ManualOverride);
                AddComponent(entity, new MatchStateComponent
                {
                    State = MatchState.WaitingForPlayers,
                    TimeRemaining = authoring.WaitingForPlayersTime,
                    Round = 1,
                    TimingSettings = new MatchTimingSettings
                    {
                        WaitingForPlayersTime = authoring.WaitingForPlayersTime,
                        NewRoundTime = authoring.NewRoundTime,
                        RoundFinishedTime = authoring.RoundFinishedTime,
                        RoundFightMessageDelay = authoring.RoundFightMessageDelay,
                        MatchResultTime = authoring.MatchResultTime
                    }
                });
                AddBuffer<PlayerInfoComponent>(entity);
            }
        }
    }
}