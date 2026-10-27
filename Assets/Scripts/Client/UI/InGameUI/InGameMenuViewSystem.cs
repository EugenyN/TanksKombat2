using Unity.Entities;

namespace TanksKombat2.Client.UI
{
    // Client presentation system: opens/closes the in-game menu on the Menu input action.
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class InGameMenuViewSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (InGameUIViews.Instance == null) return;

            if (GameInput.Actions.UI.Menu.WasPressedThisFrame())
            {
                if (WindowManager.Instance.Current == null)
                    WindowManager.Instance.Open(InGameUIViews.Instance.InGameMenu.gameObject);
                else if (!WindowManager.Instance.IsCurrentBlocksEscape)
                    WindowManager.Instance.Back();
            }
        }
    }
}
