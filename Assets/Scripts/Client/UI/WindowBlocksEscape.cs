using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client UI marker: put on a window that must not be closed by the Esc ("Menu")
    // action. Explicit Back/Cancel buttons still close it via WindowManager.Back().
    public class WindowBlocksEscape : MonoBehaviour
    {
    }
}
