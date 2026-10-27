using TMPro;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: connection status line and host migration debug text.
    public class ConnectionStateView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _connectionText;
        [SerializeField] private TMP_Text _lobbyNameText;
        [SerializeField] private TMP_Text _hostMigrationText;
        
        public void SetText(string value)
        {
            _connectionText.text = value;
        }
        
        public void SetLobbyName(string value)
        {
            _lobbyNameText.text = value;
        }
        
        public void SetHostMigrationMessage(string value)
        {
            _hostMigrationText.gameObject.SetActive(true);
            _hostMigrationText.text = value;
        }
        
        public void SetHostMigrationMessageVisible(bool value)
        {
            _hostMigrationText.gameObject.SetActive(value);
        }
    }
}