using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: drives the match status banner text and its audio cues by match state.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MatchStatusViewSystem : SystemBase
    {
        private int _waitingSecond;
        
        private const float WaitingTickSoundDelay = 3f;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            RequireForUpdate<MatchStateComponent>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            var matchStatusView = InGameUIViews.Instance.MatchStatus;
            matchStatusView.SetMatchStatusSize(GetMatchStatusRectSize());
        }

        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var matchStatusView = InGameUIViews.Instance.MatchStatus;
            
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();

            string matchStatus = "";
                
            if (matchState.State == MatchState.WaitingForPlayers)
            {
                var matchTiming = matchState.TimingSettings;
                
                if (matchState.TimeRemaining > matchTiming.RoundFightMessageDelay)
                {
                    matchStatus = $"Round {matchState.Round}";
                    
                    if (matchState.Round == 1)
                    {
                        GetPlayersReadyStatus(out int totalPlayers, out int readyPlayers, out bool localReady);
                        var intTime = Mathf.CeilToInt(matchState.TimeRemaining - matchTiming.RoundFightMessageDelay);
                        matchStatus += localReady
                            ? $"\nWaiting for players ({readyPlayers}/{totalPlayers}) {intTime}..."
                            : $"\nPress <Fire> to start {intTime}...";

                        AudioSystem.PlayAudioOnce(EntityManager, AudioClipKeys.MatchStart);
                        
                        if (_waitingSecond != intTime && 
                            matchState.TimeRemaining < matchTiming.WaitingForPlayersTime - WaitingTickSoundDelay)
                        {
                            AudioSystem.PlayAudio(EntityManager, AudioClipKeys.WaitingTick);
                            _waitingSecond = intTime;
                        }
                    }
                }
                else
                {
                    AudioSystem.PlayAudioOnce(EntityManager, AudioClipKeys.RoundFight, true);
                    
                    matchStatus = "Fight !".FormatWithColor(Color.red);
                }
            }
            
            if (matchState.State == MatchState.RoundFinished || matchState.State == MatchState.MatchFinished)
            {
                if (matchState.WinnerNetworkId == 0)
                {
                    matchStatus = "Nobody wins !";
                }
                else
                {
                    var clientSettings = SystemAPI.GetSingleton<ClientSettingsComponent>();
                    var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
                    var winnerInfo = playerInfoBuffer.GetPlayerInfo(matchState.WinnerNetworkId);
                    var color = clientSettings.GetColor(winnerInfo.ColorIndex);
                    
                    if (matchState.State == MatchState.RoundFinished)
                        matchStatus = $"Player {winnerInfo.PlayerName.FormatWithColor(color)} wins !";
                    if (matchState.State == MatchState.MatchFinished)
                        matchStatus = $"Player {winnerInfo.PlayerName.FormatWithColor(color)} wins match !";
                }
                
                var localNetworkId = NetworkId.Invalid;
                if (SystemAPI.TryGetSingletonEntity<LocalConnection>(out var localConnection))
                    localNetworkId = SystemAPI.GetComponent<NetworkId>(localConnection);
                
                bool isLocalWin = localNetworkId.Value == matchState.WinnerNetworkId && matchState.WinnerNetworkId != 0;
                AudioSystem.PlayAudioOnce(EntityManager, isLocalWin ? AudioClipKeys.RoundWin : AudioClipKeys.RoundFail);
            }
            
            if (matchStatus != "")
                matchStatusView.ShowStatus(matchStatus);
            else
                matchStatusView.HideStatus();
        }

        private Vector2 GetMatchStatusRectSize()
        {
            SystemAPI.TryGetSingleton<ClientSettingsComponent>(out var clientSettingsComponent);
            SystemAPI.TryGetSingleton<MatchConfigComponent>(out var matchConfigComponent);
            SystemAPI.TryGetSingleton<LevelDataComponent>(out var levelDataComponent);

            var tileSize = clientSettingsComponent.TileSetConfig.Value.MapTileSizes[matchConfigComponent.MapSizePreset];
            return new Vector2(levelDataComponent.Size.x * tileSize, 48);
        }
        
        private void GetPlayersReadyStatus(out int totalPlayers, out int readyPlayers, out bool localReady)
        {
            localReady = false;
            
            var localNetworkId = NetworkId.Invalid;
            if (SystemAPI.TryGetSingletonEntity<LocalConnection>(out var localConnection))
                localNetworkId = SystemAPI.GetComponent<NetworkId>(localConnection);

            totalPlayers = 0;
            readyPlayers = 0;
            foreach (var playerInfo in SystemAPI.GetSingletonBuffer<PlayerInfoComponent>())
            {
                if (playerInfo.NetworkId == localNetworkId.Value && playerInfo.Ready)
                    localReady = true;
                if (playerInfo.Disconnected) continue;
                totalPlayers++;
                if (playerInfo.Ready) readyPlayers++;
            }
        }
    }
}