using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Scene-bound registry of in-game UI views (GameScene): single serialized wiring point
    // for the client view systems. Systems read views from the static Instance instead of
    // scanning the scene; the instance dies together with its scene.
    [AutoStaticsCleanup]
    public partial class InGameUIViews : MonoBehaviour
    {
        public static InGameUIViews Instance { get; private set; }

        [SerializeField] private InGameMenuView _inGameMenu;
        [SerializeField] private ConnectionStateView _connectionState;
        [SerializeField] private EventLogView _eventLog;
        [SerializeField] private PlayerStatesView _playerStates;
        [SerializeField] private MatchStatusView _matchStatus;
        [SerializeField] private MatchEndView _matchEnd;
        [SerializeField] private OnScreenControls _onScreenControls;

        public InGameMenuView InGameMenu => _inGameMenu;
        public ConnectionStateView ConnectionState => _connectionState;
        public EventLogView EventLog => _eventLog;
        public PlayerStatesView PlayerStates => _playerStates;
        public MatchStatusView MatchStatus => _matchStatus;
        public MatchEndView MatchEnd => _matchEnd;
        public OnScreenControls OnScreenControls => _onScreenControls;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"[{nameof(InGameUIViews)}] Duplicate instance detected, destroying {name}.");
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
