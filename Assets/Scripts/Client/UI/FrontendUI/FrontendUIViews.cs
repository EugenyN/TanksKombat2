using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Scene-bound registry of frontend UI views (Frontend scene): single serialized wiring
    // point for the menu view systems. Systems read views from the static Instance instead
    // of scanning the scene; the instance dies together with its scene.
    [AutoStaticsCleanup]
    public partial class FrontendUIViews : MonoBehaviour
    {
        public static FrontendUIViews Instance { get; private set; }

        [SerializeField] private MainMenuView _mainMenu;

        public MainMenuView MainMenu => _mainMenu;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(FrontendUIViews)}] Duplicate instance detected, destroying {name}.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
