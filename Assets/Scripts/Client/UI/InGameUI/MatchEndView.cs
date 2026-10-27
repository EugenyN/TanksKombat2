using System;
using System.Collections.Generic;
using System.Linq;
using TanksKombat2.Shared;
using TMPro;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: end-of-match screen (winner, results table, start new match/disconnect).
    public class MatchEndView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _winMessageText;
        [SerializeField] private TMP_Text _nextActionText;
        [SerializeField] private Button _startNewMatchButton;
        [SerializeField] private Button _disconnectButton;
        [SerializeField] private TMP_Text _disconnectButtonText;
        [SerializeField] private GameObject _resultsTable;
        
        public event Action StartNewMatchClicked;
        public event Action DisconnectClicked;
        
        private void Awake()
        {
            _startNewMatchButton.gameObject.SetActive(ClientServerBootstrap.ServerWorld != null);

            _disconnectButtonText.text = ClientServerBootstrap.ServerWorld != null ? 
                "CLOSE SERVER" : "DISCONNECT";
            
            _disconnectButton.onClick.AddListener(() => DisconnectClicked?.Invoke());
            _startNewMatchButton.onClick.AddListener(() => StartNewMatchClicked?.Invoke());
        }

        public void SetWinner(string playerName, Color color)
        {
            _winMessageText.text = $"{playerName.FormatWithColor(color)} wins game !";
        }
        
        public void SetDraw()
        {
            _winMessageText.text = "Nobody wins !";
        }

        public void SetNextActionTime(float timer)
        {
            var time = Math.Ceiling(timer);
            
            if (ClientServerBootstrap.ServerWorld != null)
                _nextActionText.text = $"Choosing next action {time}...";
            else
                _nextActionText.text = $"Host is choosing next action {time}...";
        }

        public void SetResultTable(List<(string playerName, Color color, int winCount)> resultTable, int roundsToWin)
        {
            resultTable.Sort((a, b) => b.winCount.CompareTo(a.winCount));

            int place = 1;
            foreach (Transform child in _resultsTable.transform)
            {
                if (resultTable.Count >= place)
                {
                    var (playerName, color, winCount) = resultTable[place - 1];
                    var winsString = string.Concat(Enumerable.Repeat("<sprite name=\"win\">", winCount))
                        + new string(' ', roundsToWin - winCount);
                    var textElement = child.GetComponent<TMP_Text>();
                    textElement.text = $"{place}. {winsString} {playerName.FormatWithColor(color)}";
                    child.gameObject.SetActive(true);
                }
                else
                    child.gameObject.SetActive(false);

                place++;
            }
        }
    }
}