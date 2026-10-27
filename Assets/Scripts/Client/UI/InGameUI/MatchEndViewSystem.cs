using System.Collections.Generic;
using TanksKombat2.Shared;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: opens the match-end screen when the match finishes and handles its buttons.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MatchEndViewSystem : SystemBase
    {
        private const float MatchEndViewDelay = 4f;
        
        protected override void OnCreate()
        {
            base.OnCreate();

            RequireForUpdate<MatchStateComponent>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            var matchEndView = InGameUIViews.Instance.MatchEnd;
            matchEndView.StartNewMatchClicked += OnStartNewMatchClicked;
            matchEndView.DisconnectClicked += OnDisconnectClicked;
        }

        protected override void OnStopRunning()
        {
            if (InGameUIViews.Instance == null) return;
            
            var matchEndView = InGameUIViews.Instance.MatchEnd;
            matchEndView.StartNewMatchClicked -= OnStartNewMatchClicked;
            matchEndView.DisconnectClicked -= OnDisconnectClicked;

            base.OnStopRunning();
        }

        private void OnStartNewMatchClicked()
        {
            AudioSystem.StopAudio(EntityManager);

            if (!SystemAPI.TryGetSingletonEntity<LocalConnection>(out var playerConnection))
                return;

            var rpc = EntityManager.CreateEntity();
            EntityManager.AddComponentData(rpc, default(StartNewMatchRequestRpc));
            EntityManager.AddComponentData(rpc, new SendRpcCommandRequest
            {
                TargetConnection = playerConnection
            });
        }

        private void OnDisconnectClicked()
        {
            WindowManager.Instance.Back();
            GameManager.Instance.QuitGame();
        }

        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var matchEndView = InGameUIViews.Instance.MatchEnd;
            
            var matchState = SystemAPI.GetSingleton<MatchStateComponent>();
            var settings = SystemAPI.GetSingleton<ClientSettingsComponent>();

            if (matchState.State == MatchState.MatchFinished)
            {
                var matchTiming = matchState.TimingSettings;
                
                if (WindowManager.Instance.Current != matchEndView.gameObject && 
                    matchState.TimeRemaining < matchTiming.MatchResultTime - MatchEndViewDelay)
                {
                    var playerInfoBuffer = SystemAPI.GetSingletonBuffer<PlayerInfoComponent>();
                    if (matchState.WinnerNetworkId != 0)
                    {
                        var winnerInfo = playerInfoBuffer.GetPlayerInfo(matchState.WinnerNetworkId);
                        matchEndView.SetWinner(winnerInfo.PlayerName.ToString(), 
                            settings.GetColor(winnerInfo.ColorIndex));
                    }
                    else
                    {
                        matchEndView.SetDraw();
                    }

                    var resultTable = new List<(string playerName, Color color, int winCount)>();
                    foreach (var playerInfo in playerInfoBuffer)
                    {
                        if (playerInfo.Spectator) continue;
                        var color1 = settings.GetColor(playerInfo.ColorIndex);
                        resultTable.Add((playerInfo.PlayerName.ToString(), color1, playerInfo.WinsCount));
                    }

                    var config = SystemAPI.GetSingleton<MatchConfigComponent>();
                    matchEndView.SetResultTable(resultTable, config.RoundsToWin);
                    WindowManager.Instance.Open(matchEndView.gameObject, true);
                    
                    AudioSystem.PlayAudio(EntityManager, AudioClipKeys.MatchResult);
                }
                matchEndView.SetNextActionTime(matchState.TimeRemaining);
            }
            
            if (matchState.State == MatchState.WaitingForPlayers)
            {
                if (WindowManager.Instance.Current == matchEndView.gameObject)
                    WindowManager.Instance.Back();
            }
        }
    }
}