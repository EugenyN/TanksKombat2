using Unity.Entities;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: shows the OnScreenControls canvas on mobile builds
    // when GameSettings.OnScreenControls is enabled. Re-evaluates every frame so the
    // toggle can be flipped at runtime. Non-mobile builds always hide the controls.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class OnScreenControlsSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;
            var controls = InGameUIViews.Instance.OnScreenControls;

#if UNITY_ANDROID || UNITY_IOS
            bool active = GameSettings.Instance.OnScreenControls;
#else
            bool active = false;
#endif
            if (controls.gameObject.activeSelf != active)
                controls.gameObject.SetActive(active);
        }
    }
}
