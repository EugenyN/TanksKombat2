using System;
using System.Threading;
using TanksKombat2.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: host-game menu. Binds starts hosting via Relay (cancellable handshake) or Direct IP (synchronous local start).
    public class HostGameMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;

        [SerializeField] private TMP_InputField _playerNameInputField;
        
        [SerializeField] private OptionSelector _connectionModeSelector;
        [SerializeField] private TMP_InputField _sessionIDInputField;
        [SerializeField] private TMP_Text _sessionIDText;
        [SerializeField] private TMP_InputField _portInputField;
        
        [SerializeField] private Button _startHostButton;
        [SerializeField] private Button _backButton;
        
        [SerializeField] private OptionSelector _spectatorModeSelector;
        
        [SerializeField] private MenuMessageView _connectionMessageView;
        
        private GameSettings _settings;
        
        private CancellationTokenSource _hostCts;

        private void Awake()
        {
            _settings = GameSettings.Instance;
            
            _connectionModeSelector.OnIndexChanged.AddListener(OnConnectionModeChanged);
            _startHostButton.onClick.AddListener(OnStartHostButtonClick);
            _backButton.onClick.AddListener(OnBackButtonClick);

            _playerNameInputField.onValueChanged.AddListener(OnPlayerNameInputChanged);
            _sessionIDInputField.onValueChanged.AddListener(value => _settings.SessionName = value);
            _portInputField.onValueChanged.AddListener(value => _settings.Port = value);
            
            _playerNameInputField.text = _settings.PlayerName;
            
            _connectionModeSelector.SetOptions<ConnectionMode>();
            _connectionModeSelector.SetIndex((int)_settings.ConnectionMode);
            OnConnectionModeChanged((int)_settings.ConnectionMode);

            _sessionIDInputField.text = _settings.SessionName;
            _portInputField.text = _settings.Port;
            
            _spectatorModeSelector.OnIndexChanged.AddListener(i => _settings.SpectatorMode = i != 0);
            _spectatorModeSelector.SetOptions(new[] { "Off", "On" });
            _spectatorModeSelector.SetIndex(_settings.SpectatorMode ? 1 : 0);
            
            _connectionMessageView.onCancelButtonClick.AddListener(() =>
            {
                _hostCts?.Cancel(); 
                _windowManager.Back();
            });
            
            RefreshStartButtonInteractable();
        }

        private void OnPlayerNameInputChanged(string value)
        {
            _settings.PlayerName = value;
            RefreshStartButtonInteractable();
        }

        private void RefreshStartButtonInteractable()
        {
            _startHostButton.interactable = !string.IsNullOrWhiteSpace(_playerNameInputField.text);
        }

        private void OnConnectionModeChanged(int index)
        {
            var mode = (ConnectionMode)index;
            
            _settings.ConnectionMode = mode;
            
            if (mode == ConnectionMode.DirectIP)
            {
                _sessionIDInputField.gameObject.SetActive(false);
                _sessionIDText.gameObject.SetActive(false);
            }
            else
            {
                _sessionIDInputField.gameObject.SetActive(true);
                _sessionIDText.gameObject.SetActive(true);
            }
        }

        private async void OnStartHostButtonClick()
        {
            _windowManager.Back();
            _windowManager.Open(_connectionMessageView.gameObject);
            
            _connectionMessageView.ShowCancelButton(10);

            await Awaitable.NextFrameAsync();

            var progress = new InlineProgress<string>(_connectionMessageView.SetMessage);

            _hostCts?.Dispose();
            _hostCts = new CancellationTokenSource();
            _hostCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                if ((ConnectionMode)_connectionModeSelector.CurrentIndex == ConnectionMode.DirectIP)
                    GameManager.Instance.StartGameDirectIP(_settings.Port, progress);
                else
                    await GameManager.Instance.StartGameRelay(_settings.Port, _settings.SessionName, progress, _hostCts.Token);
            }
            catch (OperationCanceledException)
            {
                // User pressed Cancel (or the 30s timeout fired)
                _hostCts.Dispose();
                _hostCts = null;
                _connectionMessageView.SetMessage("Failed to start game: Timeout");
                _connectionMessageView.ShowCancelButton();
            }
            catch (Exception e)
            {
                Debug.Log($"[HostGame] Failed to start game: {e.Message}");
                _hostCts.Dispose();
                _hostCts = null;
                _connectionMessageView.SetMessage("Failed to start game: " + e.Message);
                _connectionMessageView.ShowCancelButton();
            }
        }

        private void OnBackButtonClick()
        {
            _windowManager.Back();
        }

        private void OnDestroy()
        {
            _hostCts?.Cancel();
            _hostCts?.Dispose();
            _hostCts = null;
        }
    }
}