using System.Linq;
using TanksKombat2.Shared;
using TMPro;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: a single player HUD entry (name, lives/ammo, wins, status text).
    public class PlayerStateView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _livesAndAmmoText;
        [SerializeField] private TMP_Text _winsText;
        [SerializeField] private TMP_Text _statusText;
        
        private PlayerInfoComponent _info;
        private int _lives;
        private int _ammo;
        private MatchState _matchState;
        
        public void SetPlayerInfo(in PlayerInfoComponent info, Color color, int lives, int ammo, MatchState matchState)
        {
            if (_info.PlayerName.Equals(info.PlayerName)
                && _info.ColorIndex == info.ColorIndex
                && _info.Spectator == info.Spectator
                && _info.Disconnected == info.Disconnected
                && _info.WinsCount == info.WinsCount
                && _info.Ready == info.Ready
                && _lives == lives && _ammo == ammo
                && _matchState == matchState)
                return;

            _info = info;
            _lives = lives;
            _ammo = ammo;
            _matchState = matchState;
            
            if (info.Disconnected)
                SetColor(Color.gray);
            
            var nameColor = info.Disconnected ? Color.gray : color;
            _nameText.text = info.PlayerName.FormatWithColor(nameColor);
            
            if (info.Spectator)
            {
                _livesAndAmmoText.gameObject.SetActive(false);
                _winsText.gameObject.SetActive(false);
            }
            else
            {
                _livesAndAmmoText.gameObject.SetActive(true);
                SetLivesAmmoWins(lives, ammo, info.WinsCount);
            }
            
            string status;
            if (info.Disconnected)
                status = "OFFLINE";
            else if (matchState == MatchState.WaitingForPlayers)
                status = "READY!";
            else
                status = info.Spectator ?  "SPECTATE" : "";
            
            _statusText.text = status;
        }
        
        private void Update()
        {
            var blinking = _matchState == MatchState.WaitingForPlayers && !_info.Ready && !_info.Disconnected;
            
            if (blinking)
                _statusText.enabled = (int)(Time.time * 2) % 2 == 0;
            else
                _statusText.enabled = true;
        }

        private void SetColor(Color color)
        {
            _nameText.color = color;
            _livesAndAmmoText.color = color;
            _winsText.color = color;
            _statusText.color = color;
        }
        
        private void SetLivesAmmoWins(int lives, int ammo, int wins)
        {
            _livesAndAmmoText.text = $"<sprite name=\"life\">{lives,-2}<sprite name=\"ammo\">{ammo,-2}";
            _winsText.gameObject.SetActive(wins > 0);
            _winsText.text = string.Concat(Enumerable.Repeat("<sprite name=\"win\">", wins));
        }
    }
}