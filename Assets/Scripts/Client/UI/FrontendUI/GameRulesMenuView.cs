using System.Linq;
using TanksKombat2.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: match setup (map type/custom map, mode, theme, bots, rounds);
    // writes the choices into GameSettings.
    public class GameRulesMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;
        [SerializeField] private OptionSelector _mapTypeSelector;
        [SerializeField] private OptionSelector _customMapSelector;
        [SerializeField] private TMP_Text _customMapText;
        [SerializeField] private OptionSelector _gameModeSelector;
        [SerializeField] private OptionSelector _mapThemeSelector;
        [SerializeField] private OptionSelector _botCountSelector;
        [SerializeField] private OptionSelector _botSkillSelector;
        [SerializeField] private OptionSelector _roundsToWinSelector;
        
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _backButton;
        
        private GameSettings _settings;
        
        private void Awake()
        {
            _settings = GameSettings.Instance;
            
            _nextButton.onClick.AddListener(OnNextButtonClick);
            _backButton.onClick.AddListener(OnBackButtonClick);
            
            _mapTypeSelector.SetOptions<MapType>();
            _mapTypeSelector.SetIndex((int)_settings.MapType);
            _mapTypeSelector.OnValueChanged.AddListener(mapType => _settings.MapType = mapType.FromDisplayString<MapType>());
            _mapTypeSelector.OnValueChanged.AddListener(_ => UpdateCustomMapRowVisibility());

            _customMapSelector.SetOptions<CustomMap>();
            _customMapSelector.SetIndex((int)_settings.CustomMap);
            _customMapSelector.OnValueChanged.AddListener(customMap => _settings.CustomMap = customMap.FromDisplayString<CustomMap>());

            UpdateCustomMapRowVisibility();
            
            _gameModeSelector.SetOptions<GameMode>();
            _gameModeSelector.SetIndex((int)_settings.GameMode);
            _gameModeSelector.OnValueChanged.AddListener(gameMode => _settings.GameMode = gameMode.FromDisplayString<GameMode>());
            
            _mapThemeSelector.SetOptions<MapTheme>();
            _mapThemeSelector.SetIndex((int)_settings.MapTheme);
            _mapThemeSelector.OnValueChanged.AddListener(mapTheme => _settings.MapTheme = mapTheme.FromDisplayString<MapTheme>());
            
            _botCountSelector.SetOptions(Enumerable.Range(0, ConnectionUtils.MaxSessionPlayers).Select(i => i.ToString()));
            _botCountSelector.SetIndex(_settings.BotCount);
            _botCountSelector.OnIndexChanged.AddListener(index => _settings.BotCount = index);
            
            _botSkillSelector.SetOptions<BotSkill>();
            _botSkillSelector.SetIndex((int)_settings.BotSkill);
            _botSkillSelector.OnIndexChanged.AddListener(index => _settings.BotSkill = (BotSkill)index);
            
            _roundsToWinSelector.SetOptions(Enumerable.Range(1, 5).Select(i => i.ToString()));
            _roundsToWinSelector.Value = _settings.RoundsToWin.ToString();
            _roundsToWinSelector.OnValueChanged.AddListener(value => _settings.RoundsToWin = int.Parse(value));
        }
        
        private void UpdateCustomMapRowVisibility()
        {
            _customMapSelector.gameObject.SetActive(_settings.MapType == MapType.Custom);
            _customMapText.gameObject.SetActive(_settings.MapType == MapType.Custom);
        }

        private void OnNextButtonClick()
        {
            _windowManager.Open<HostGameMenuView>();
        }

        private void OnBackButtonClick()
        {
            _windowManager.Back();
        }
        
    }
}