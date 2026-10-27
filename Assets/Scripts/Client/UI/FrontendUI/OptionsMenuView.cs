using TMPro;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: display/gameplay options (vsync, health bars, mobile on-screen
    // controls); persists changes to GameSettings when the window closes.
    public class OptionsMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;
        [SerializeField] private OptionSelector _vsyncSelector;
        [SerializeField] private TMP_Text _vsyncText;
        [SerializeField] private OptionSelector _healthBarsSelector;
        [SerializeField] private OptionSelector _onScreenControlsSelector;
        [SerializeField] private TMP_Text _onScreenControlsText;
        [SerializeField] private Button _backButton;

        private GameSettings _settings;

        private void Awake()
        {
            _settings = GameSettings.Instance;

#if UNITY_ANDROID || UNITY_IOS
            _vsyncSelector.gameObject.SetActive(false);
            _vsyncText.gameObject.SetActive(false);
#else
            _vsyncSelector.SetOptions(new[] { "Off", "On" });
            _vsyncSelector.SetIndex(_settings.Vsync ? 1 : 0);
            _vsyncSelector.OnIndexChanged.AddListener(OnVsyncChanged);
#endif
            
            _healthBarsSelector.SetOptions(new[] { "Off", "On" });
            _healthBarsSelector.SetIndex(_settings.HealthBars ? 1 : 0);
            _healthBarsSelector.OnIndexChanged.AddListener(OnHealthBarsChanged);
            
#if UNITY_ANDROID || UNITY_IOS
            _onScreenControlsSelector.SetOptions(new[] { "Off", "On" });
            _onScreenControlsSelector.SetIndex(_settings.OnScreenControls ? 1 : 0);
            _onScreenControlsSelector.OnIndexChanged.AddListener(i => _settings.OnScreenControls = i != 0);
#else
            _onScreenControlsSelector.gameObject.SetActive(false);
            _onScreenControlsText.gameObject.SetActive(false);
#endif

            _backButton.onClick.AddListener(OnBack);
        }

        private void OnVsyncChanged(int index)
        {
            _settings.Vsync = index != 0;
            QualitySettingsInitializer.ApplyQualitySettings();
        }

        private void OnHealthBarsChanged(int index)
        {
            _settings.HealthBars = index != 0;
            
            if (ClientServerBootstrap.ClientWorld != null)
                ClientServerBootstrap.ClientWorld.GetExistingSystemManaged<ClientSettingsInitSystem>().ApplyHealthBars();
        }

        private void OnDisable()
        {
            _settings.Save();
        }

        private void OnBack()
        {
            _windowManager.Back();
        }
    }
}
