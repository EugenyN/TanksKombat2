using UnityEngine;

namespace TanksKombat2.Client
{
    // Applies frame-pacing settings at startup. Mobile: forces a 60 FPS render target
    // (Unity's mobile default is 30; vSyncCount is ignored on mobile).
    // Desktop: restores the player's persisted Vsync preference from GameSettings QualitySettings.vSyncCount
    public static class QualitySettingsInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void ApplyQualitySettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
#else
            QualitySettings.vSyncCount = GameSettings.Instance.Vsync ? 1 : 0;
#endif
        }
    }
}
