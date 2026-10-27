using System;
using System.Net;
using TanksKombat2.Bootstrap;
using TanksKombat2.Shared;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client-side persisted player preferences (PlayerPrefs). Match-affecting choices are
    // snapshotted into the server world via ServerConfigOverride when a game starts.
    [AutoStaticsCleanup]
    public partial class GameSettings
    {
        public static GameSettings Instance { get; private set; } = null!;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RuntimeInitializeOnLoad() => Instance = new GameSettings();

        private const string PlayerNameKey = "PlayerName";
        private const string MapTypeKey = "MapType";
        private const string CustomMapKey = "CustomMap";
        private const string MapThemeKey = "MapTheme";
        private const string GameModeKey = "GameMode";
        private const string BotSkillKey = "BotSkill";

        private const string ConnectionModeKey = "ConnectionMode";
        private const string SessionNameKey = "SessionName";
        private const string IPAddressKey = "IPAddress";
        private const string PortKey = "Port";

        private const string VsyncKey = "Vsync";
        private const string HealthBarsKey = "HealthBars";
        private const string OnScreenControlsKey = "OnScreenControls";
        private const string SpectatorModeKey = "SpectatorMode";
        private const string BotCountKey = "BotCount";
        private const string RoundsToWinKey = "RoundsToWin";

        private GameSettings()
        {
            _mapType = (MapType)PlayerPrefs.GetInt(MapTypeKey, (int)MapType.SmallCave);
            _customMap = (CustomMap)PlayerPrefs.GetInt(CustomMapKey, (int)CustomMap.Alien);
            _mapTheme = (MapTheme)PlayerPrefs.GetInt(MapThemeKey, (int)MapTheme.Random);
            _gameMode = (GameMode)PlayerPrefs.GetInt(GameModeKey, (int)GameMode.Standard);
            _botCount = PlayerPrefs.GetInt(BotCountKey, 0);
            _botSkill = (BotSkill)PlayerPrefs.GetInt(BotSkillKey, (int)BotSkill.Normal);
            _roundsToWin = PlayerPrefs.GetInt(RoundsToWinKey, 3);

            _playerName = PlayerPrefs.GetString(PlayerNameKey, Environment.UserName);
            _connectionMode = (ConnectionMode)PlayerPrefs.GetInt(ConnectionModeKey, (int)ConnectionMode.UnityRelay);
            _sessionName = PlayerPrefs.GetString(SessionNameKey, Dns.GetHostName());
            _ipAddress = PlayerPrefs.GetString(IPAddressKey, GameBootstrap.DefaultIPAddress);
            _port = PlayerPrefs.GetString(PortKey, GameBootstrap.DefaultServerPort.ToString());

            _vsync = PlayerPrefs.GetInt(VsyncKey, 1) != 0;
            _healthBars = PlayerPrefs.GetInt(HealthBarsKey, 1) != 0;
            _onScreenControls = PlayerPrefs.GetInt(OnScreenControlsKey, 1) != 0;
            _spectatorMode = PlayerPrefs.GetInt(SpectatorModeKey, 0) != 0;

        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        private string _playerName;

        public string PlayerName
        {
            get => _playerName;
            set
            {
                if (_playerName == value)
                    return;

                _playerName = value;
                PlayerPrefs.SetString(PlayerNameKey, value);
            }
        }

        private MapType _mapType;

        public MapType MapType
        {
            get => _mapType;
            set
            {
                if (_mapType == value)
                    return;

                _mapType = value;
                PlayerPrefs.SetInt(MapTypeKey, (int)value);
            }
        }

        private CustomMap _customMap;

        public CustomMap CustomMap
        {
            get => _customMap;
            set
            {
                if (_customMap == value)
                    return;

                _customMap = value;
                PlayerPrefs.SetInt(CustomMapKey, (int)value);
            }
        }

        private MapTheme _mapTheme;

        public MapTheme MapTheme
        {
            get => _mapTheme;
            set
            {
                if (_mapTheme == value)
                    return;

                _mapTheme = value;
                PlayerPrefs.SetInt(MapThemeKey, (int)value);
            }
        }

        private GameMode _gameMode;

        public GameMode GameMode
        {
            get => _gameMode;
            set
            {
                if (_gameMode == value)
                    return;

                _gameMode = value;
                PlayerPrefs.SetInt(GameModeKey, (int)value);
            }
        }

        private BotSkill _botSkill;

        public BotSkill BotSkill
        {
            get => _botSkill;
            set
            {
                if (_botSkill == value)
                    return;

                _botSkill = value;
                PlayerPrefs.SetInt(BotSkillKey, (int)value);
            }
        }

        private ConnectionMode _connectionMode;

        public ConnectionMode ConnectionMode
        {
            get => _connectionMode;
            set
            {
                if (_connectionMode == value)
                    return;

                _connectionMode = value;
                PlayerPrefs.SetInt(ConnectionModeKey, (int)value);
            }
        }

        private string _sessionName;

        public string SessionName
        {
            get => _sessionName;
            set
            {
                if (_sessionName == value)
                    return;

                _sessionName = value;
                PlayerPrefs.SetString(SessionNameKey, value);
            }
        }

        private string _ipAddress;

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress == value)
                    return;

                _ipAddress = value;
                PlayerPrefs.SetString(IPAddressKey, value);
            }
        }

        private string _port;

        public string Port
        {
            get => _port;
            set
            {
                if (_port == value)
                    return;

                _port = value;
                PlayerPrefs.SetString(PortKey, value);
            }
        }

        private bool _vsync;

        public bool Vsync
        {
            get => _vsync;
            set
            {
                if (_vsync == value)
                    return;

                _vsync = value;
                PlayerPrefs.SetInt(VsyncKey, value ? 1 : 0);
            }
        }

        private bool _healthBars;

        public bool HealthBars
        {
            get => _healthBars;
            set
            {
                if (_healthBars == value)
                    return;

                _healthBars = value;
                PlayerPrefs.SetInt(HealthBarsKey, value ? 1 : 0);
            }
        }

        private bool _onScreenControls;

        // Enables the on-screen touch controls (mobile).
        public bool OnScreenControls
        {
            get => _onScreenControls;
            set
            {
                if (_onScreenControls == value)
                    return;

                _onScreenControls = value;
                PlayerPrefs.SetInt(OnScreenControlsKey, value ? 1 : 0);
            }
        }

        private bool _spectatorMode;

        public bool SpectatorMode
        {
            get => _spectatorMode;
            set
            {
                if (_spectatorMode == value)
                    return;

                _spectatorMode = value;
                PlayerPrefs.SetInt(SpectatorModeKey, value ? 1 : 0);
            }
        }

        private int _botCount;

        public int BotCount
        {
            get => _botCount;
            set
            {
                var clamped = Mathf.Clamp(value, 0, ConnectionUtils.MaxSessionPlayers - 1);
                if (_botCount == clamped)
                    return;

                _botCount = clamped;
                PlayerPrefs.SetInt(BotCountKey, clamped);
            }
        }

        private int _roundsToWin;
        
        public int RoundsToWin
        {
            get => _roundsToWin;
            set
            {
                if (value < 1)
                    value = 1;
                if (_roundsToWin == value)
                    return;

                _roundsToWin = value;
                PlayerPrefs.SetInt(RoundsToWinKey, value);
            }
        }
    }
}