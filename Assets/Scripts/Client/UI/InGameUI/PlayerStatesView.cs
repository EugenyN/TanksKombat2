using TanksKombat2.Shared;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: row of per-player HUD entries (name, lives/ammo, wins, ready status).
    public class PlayerStatesView : MonoBehaviour
    {
        [SerializeField]
        private PlayerStateView[] _playerStatuses;
        
        private void Awake()
        {
            foreach (var playerStatus in _playerStatuses)
                playerStatus.gameObject.SetActive(false);
        }
        
        public void SetPlayerInfo(int statusIndex, PlayerInfoComponent playerInfo, Color color, int lives, int ammo,
            MatchState matchState)
        {
            if (statusIndex < 0 || statusIndex >= _playerStatuses.Length)
            {
                Debug.LogError($"[{nameof(PlayerStatesView)}] Player status index {statusIndex} is out of range " +
                               $"(0..{_playerStatuses.Length - 1}).");
                return;
            }

            var statusView = _playerStatuses[statusIndex];
            statusView.SetPlayerInfo(in playerInfo, color, lives, ammo, matchState);
            statusView.gameObject.SetActive(true);
        }
    }
}