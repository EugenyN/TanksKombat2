using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Keeps the on-screen touch controls at a constant physical size on any device by driving
    // ONLY the canvas CanvasScaler.scaleFactor. Individual controls are never touched.
    // Physical size ends up = authoredUnits * 2.54 / ReferenceDpi (cm), independent of the
    // device DPI; raise ReferenceDpi to shrink the controls, lower it to enlarge them.
    public class OnScreenControls : MonoBehaviour
    {
        [SerializeField] private float _referenceDpi = 144f;
        
        private void Awake()
        {
            ApplyScale();
        }

        private void OnRectTransformDimensionsChange() => ApplyScale();

        private void ApplyScale()
        {
            float dpi = Screen.dpi > 10f ? Screen.dpi : (Application.isMobilePlatform ? 160f : 96f);
            GetComponent<CanvasScaler>().scaleFactor = dpi / _referenceDpi;
        }
    }
}
