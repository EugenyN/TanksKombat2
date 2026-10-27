using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI control: highlights the first active Selectable of the window. With _selectFirstOnMove
    // enabled (default) the selection happens only on Move (keyboard/gamepad) press; otherwise the first
    // Selectable is selected automatically as soon as nothing is selected. Clears stale selection
    // when the window closes or a selected element is hidden.
    public class SelectOnMove : MonoBehaviour
    {
        [SerializeField] private bool _selectFirstOnMove;

        [SerializeField] private Selectable _firstSelectable;

        private void Update()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;

            var selected = eventSystem.currentSelectedGameObject;
            if (selected != null)
            {
                if (selected.activeInHierarchy)
                    return; // valid selection - do not interfere

                eventSystem.SetSelectedGameObject(null); // stale selection from a hidden element
            }

            if (_selectFirstOnMove)
            {
                if (eventSystem.currentInputModule is not InputSystemUIInputModule module)
                    return;

                var moveAction = module.move.action;
                if (moveAction == null || !moveAction.enabled || !moveAction.WasPressedThisFrame())
                    return;
            }

            var selectable = GetSelectable();
            if (selectable != null)
                eventSystem.SetSelectedGameObject(selectable.gameObject);
        }

        private void OnDisable()
        {
            // Clear the selection when the window closes so the next window starts unselected.
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return;
            eventSystem.SetSelectedGameObject(null);
        }

        private Selectable GetSelectable()
        {
            if (_firstSelectable != null && _firstSelectable.isActiveAndEnabled && _firstSelectable.IsInteractable())
                return _firstSelectable;

            var selectables = GetComponentsInChildren<Selectable>(false);
            foreach (var selectable in selectables)
            {
                // Skip controls that consume Left/Right navigation themselves (e.g. OptionSelector
                // child arrows) and are not meant to be focused via automatic navigation.
                if (selectable.navigation.mode == Navigation.Mode.None)
                    continue;

                if (selectable.IsActive() && selectable.IsInteractable())
                    return selectable;
            }

            return null;
        }
    }
}
