using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI window: credits screen with a back button.
    public class CreditsMenuView : MonoBehaviour
    {
        [SerializeField] private WindowManager _windowManager;
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _versionText;

        private void Awake()
        {
            _backButton.onClick.AddListener(OnBack);

            _versionText.text = $"V:{Application.version}";
        }

        private void OnBack()
        {
            _windowManager.Back();
        }
    }
}
