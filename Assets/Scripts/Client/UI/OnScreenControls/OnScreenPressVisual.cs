using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI control: pressed-state visual feedback for on-screen touch controls. Not a Selectable,
    // so tapping it never steals the EventSystem selection (e.g. from open in-game menu elements).
    public class OnScreenPressVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Graphic _targetGraphic;

        [SerializeField] private Color _pressedColor = new(0.7843f, 0.7843f, 0.7843f, 1f);

        private Color _normalColor;
        private bool _pressed;

        private void Awake()
        {
            if (_targetGraphic == null)
                _targetGraphic = GetComponent<Graphic>();

            if (_targetGraphic != null)
                _normalColor = _targetGraphic.color;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            SetColor(_pressedColor);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            SetColor(_normalColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_pressed)
                return;

            _pressed = false;
            SetColor(_normalColor);
        }

        private void SetColor(Color color)
        {
            if (_targetGraphic != null)
                _targetGraphic.color = color;
        }
    }
}
