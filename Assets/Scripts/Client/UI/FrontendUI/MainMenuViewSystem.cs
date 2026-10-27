using Unity.Entities;

namespace TanksKombat2.Client.UI
{
    // Client Local-world system: opens the main menu window on start; the UI "Menu"
    // action (Esc) navigates back to it when a submenu is open.
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MainMenuViewSystem : SystemBase
    {
        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            WindowManager.Instance.Open(FrontendUIViews.Instance.MainMenu.gameObject);
        }

        protected override void OnUpdate()
        {
            if (GameInput.Actions.UI.Menu.WasPressedThisFrame())
            {
                if (!WindowManager.Instance.IsCurrentBlocksEscape)
                    WindowManager.Instance.Back();
            }
        }
    }
}