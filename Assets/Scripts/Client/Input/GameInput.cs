using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client
{
    // Client input service: owns the InputSystem_Actions instance and the UI-driven
    // input-block flag. Statics are reset on play-mode transitions (domain reload is disabled).
    [AutoStaticsCleanup]
    public static partial class GameInput
    {
        // This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RuntimeInitializeOnLoad()
        {
            Actions = new InputSystem_Actions();
            Actions.Enable();
            _blocked = false;
            _unblockedFrame = -1;
        }

        public static InputSystem_Actions Actions { get; private set; } = null!;

        private static bool _blocked;
        private static int _unblockedFrame = -1;
        
        public static bool IsPlayerInputAllowed => !_blocked && Time.frameCount != _unblockedFrame;
        
        public static void SetPlayerInputBlocked(bool blocked)
        {
            if (_blocked == blocked)
                return;

            _blocked = blocked;
            if (blocked)
            {
                Actions.Player.Disable();
            }
            else
            {
                Actions.Player.Enable();
                _unblockedFrame = Time.frameCount;
            }
        }
    }
}