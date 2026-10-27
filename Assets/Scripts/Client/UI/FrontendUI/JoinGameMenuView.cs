using System;
using System.Threading;
using TanksKombat2.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: join-game menu. Joins a game as a pure client via Relay or Direct IP.
    public class JoinGameMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;
        
        [SerializeField] private TMP_InputField _playerNameInputField;
        [SerializeField] private OptionSelector _connectionModeSelector;
        [SerializeField] private TMP_InputField _sessionIDInputField;
        [SerializeField] private TMP_Text _sessionIDText;
        [SerializeField] private TMP_InputField _addressInputField;
        [SerializeField] private TMP_Text _addressText;
        [SerializeField] private TMP_InputField _portInputField;
        [SerializeField] private Button _connectButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private OptionSelector _spectatorModeSelector;
        [SerializeField] private MenuMessageView _connectionMessageView;
        
        private GameSettings _settings;
        
        private CancellationTokenSource _connectCts;

        private void Awake()
        {
            _settings = GameSettings.Instance;
            
            _connectButton.onClick.AddListener(OnConnectButtonClick);
            _backButton.onClick.AddListener(OnBackButtonClick);
            
            _playerNameInputField.text = _settings.PlayerName;
            
            _sessionIDInputField.text = _settings.SessionName;
            _addressInputField.text = _settings.IPAddress;
            _portInputField.text = _settings.Port;
            
            _connectionModeSelector.OnIndexChanged.AddListener(OnConnectionModeChanged);
            _connectionModeSelector.SetOptions<ConnectionMode>();
            _connectionModeSelector.SetIndex((int)_settings.ConnectionMode);
            OnConnectionModeChanged((int)_settings.ConnectionMode);

            _playerNameInputField.onValueChanged.AddListener(OnPlayerNameInputChanged);
            _sessionIDInputField.onValueChanged.AddListener(value => _settings.SessionName = value);
            _addressInputField.onValueChanged.AddListener(value => _settings.IPAddress = value);
            _portInputField.onValueChanged.AddListener(value => _settings.Port = value);
            
            _spectatorModeSelector.OnIndexChanged.AddListener(i => _settings.SpectatorMode = i != 0);
            _spectatorModeSelector.SetOptions(new[] { "Off", "On" });
            _spectatorModeSelector.SetIndex(_settings.SpectatorMode ? 1 : 0);
            
            _connectionMessageView.onCancelButtonClick.AddListener(() =>
            {
                _connectCts?.Cancel(); 
                _windowManager.Back();
            });
            
            RefreshConnectButtonInteractable();
        }

        private void OnPlayerNameInputChanged(string value)
        {
            _settings.PlayerName = value;
            RefreshConnectButtonInteractable();
        }

        private void RefreshConnectButtonInteractable()
        {
            _connectButton.interactable = !string.IsNullOrWhiteSpace(_playerNameInputField.text);
        }
        
        private void OnConnectionModeChanged(int index)
        {
            var mode = (ConnectionMode)index;
            
            _settings.ConnectionMode = mode;

            if (mode == ConnectionMode.DirectIP)
            {
                _addressInputField.gameObject.SetActive(true);
                _addressText.gameObject.SetActive(true);
                _sessionIDInputField.gameObject.SetActive(false);
                _sessionIDText.gameObject.SetActive(false);
            }
            else
            {
                _addressInputField.gameObject.SetActive(false);
                _addressText.gameObject.SetActive(false);
                _sessionIDInputField.gameObject.SetActive(true);
                _sessionIDText.gameObject.SetActive(true);
            }
        }

        private async void OnConnectButtonClick()
        {
            _windowManager.Back();
            _windowManager.Open(_connectionMessageView.gameObject);
            
            _connectionMessageView.ShowCancelButton(10);
            
            await Awaitable.NextFrameAsync();

            var progress = new InlineProgress<string>(_connectionMessageView.SetMessage);

            _connectCts?.Dispose();
            _connectCts = new CancellationTokenSource();
            _connectCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                if ((ConnectionMode)_connectionModeSelector.CurrentIndex == ConnectionMode.DirectIP)
                    await GameManager.Instance.JoinGameDirectIp(_settings.IPAddress, _settings.Port, progress, _connectCts.Token);
                else
                    await GameManager.Instance.JoinGameRelay(_settings.SessionName, progress, _connectCts.Token);
            }
            catch (OperationCanceledException)
            {
                // User pressed Cancel (or the 30s timeout fired).
                _connectCts.Dispose();
                _connectCts = null;
                _connectionMessageView.SetMessage("Connection failed: Timeout");
                _connectionMessageView.ShowCancelButton();
            }
            catch (Exception e)
            {
                Debug.Log($"[JoinGame] Connection failed: {e.Message}");
                _connectCts.Dispose();
                _connectCts = null;
                _connectionMessageView.SetMessage($"Connection failed: {e.Message}");
                _connectionMessageView.ShowCancelButton();
            }
        }

        private void OnBackButtonClick()
        {
            _windowManager.Back();
        }

        private void OnDestroy()
        {
            _connectCts?.Cancel();
            _connectCts?.Dispose();
            _connectCts = null;
        }
    }
}
