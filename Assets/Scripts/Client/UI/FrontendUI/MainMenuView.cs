using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: main menu navigation (host/join/options/credits/exit) plus
    // title music that plays while the window is active.
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;
        
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private Button _optionsButton;
        [SerializeField] private Button _creditsButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private AudioSource _titleMusic;

        private void Awake()
        {
            _hostButton.onClick.AddListener(OnHostButtonClick);
            _joinButton.onClick.AddListener(OnJoinButtonClick);
            _optionsButton.onClick.AddListener(OnOptionsButtonClick);
            _creditsButton.onClick.AddListener(OnCreditsButtonClick);
            _exitButton.onClick.AddListener(OnExitButtonClick);
            
            _hostButton.gameObject.SetActive(ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer);
            _joinButton.gameObject.SetActive(ClientServerBootstrap.RequestedPlayType != ClientServerBootstrap.PlayType.Server);
        }

        private void OnEnable()
        {
            if (_titleMusic != null)
                _titleMusic.Play();
        }

        private void OnDisable()
        {
            if (_titleMusic != null)
                _titleMusic.Pause();
        }

        private void OnHostButtonClick()
        {
            _windowManager.Open<GameRulesMenuView>();
        }

        private void OnJoinButtonClick()
        {
            _windowManager.Open<JoinGameMenuView>();
        }

        private void OnOptionsButtonClick()
        {
            _windowManager.Open<OptionsMenuView>();
        }

        private void OnCreditsButtonClick()
        {
            _windowManager.Open<CreditsMenuView>();
        }

        private void OnExitButtonClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
