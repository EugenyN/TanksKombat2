using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: in-game pause menu (resume, options, disconnect/close server).
    public class InGameMenuView : MonoBehaviour
    {
        [SerializeField] private GameObject _optionsMenu;
        
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _disconnectButton;
        [SerializeField] private Button _closeServerButton;
        
        private void Awake()
        {
            _resumeButton.onClick.AddListener(OnResumeButtonClick);
            _optionsButton.onClick.AddListener(OnOptionsButtonClick);
            _disconnectButton.onClick.AddListener(OnDisconnectButtonClick);
            _closeServerButton.onClick.AddListener(OnDisconnectButtonClick);
            
            _disconnectButton.gameObject.SetActive(ClientServerBootstrap.ServerWorld == null);
            _closeServerButton.gameObject.SetActive(ClientServerBootstrap.ServerWorld != null);
        }
        
        private void OnDisconnectButtonClick()
        {
            WindowManager.Instance.Back();
            GameManager.Instance.QuitGame();
        }

        private void OnOptionsButtonClick()
        {
            WindowManager.Instance.Open(_optionsMenu);
        }

        private void OnResumeButtonClick()
        {
            WindowManager.Instance.Back();
        }
    }
}