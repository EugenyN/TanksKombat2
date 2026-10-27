using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI control: cyclic option selector implemented as a Selectable so it can receive
    // keyboard/gamepad focus; Left/Right navigation changes the index. The child arrows are plain
    // Images (no Button) so pointer events bubble up to this root: press selects the whole control,
    // click on an arrow (detected via the raycast hit) changes the value.
    [RequireComponent(typeof(RectTransform))]
    public class OptionSelector : Selectable, IMoveHandler, IPointerClickHandler
    {
        [SerializeField] private Image _leftArrow;
        [SerializeField] private Image _rightArrow;
        [SerializeField] private TMP_Text _label;

        [SerializeField] private List<string> _options = new();
        [SerializeField] private int _currentIndex;

        public UnityEvent<string> OnValueChanged;
        public UnityEvent<int> OnIndexChanged;

        protected override void Awake()
        {
            base.Awake();

            UpdateLabel();
        }

        // Mouse click: the arrows are plain Images, so the click bubbles up here;
        // the raycast hit tells which arrow was pressed.
        public void OnPointerClick(PointerEventData eventData)
        {
            var hit = eventData.pointerPressRaycast.gameObject;
            if (hit == null)
                return;
            if (hit.gameObject == _leftArrow.gameObject)
                Previous();
            else if (hit.gameObject == _rightArrow.gameObject)
                Next();
        }

        // Keyboard/gamepad navigation: Left/Right cycles the option, other directions
        // fall back to default navigation so focus can move to neighboring UI elements.
        public override void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    Previous();
                    eventData.Use();
                    break;
                case MoveDirection.Right:
                    Next();
                    eventData.Use();
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }
        public string Value
        {
            get => _options.Count > 0 ? _options[_currentIndex] : string.Empty;
            set
            {
                var index = _options.IndexOf(value);
                if (index >= 0)
                    SetIndexInternal(index, raiseEvent: true);
            }
        }

        public int CurrentIndex => _currentIndex;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            if (_options.Count > 0)
                _currentIndex = Mathf.Clamp(_currentIndex, 0, _options.Count - 1);
            UpdateLabel();
        }
#endif

        public void SetOptions(IEnumerable<string> options, bool raiseEvent = false)
        {
            _options = options.ToList();
            _currentIndex = 0;
            UpdateLabel();

            if (raiseEvent)
                RaiseChanged();
        }

        public void SetOptions<TEnum>(IEnumerable<TEnum> options) where TEnum : Enum
        {
            SetOptions(options.Select(e => e.ToDisplayString()));
        }

        public void SetOptions<TEnum>() where TEnum : Enum
        {
            SetOptions(Enum.GetValues(typeof(TEnum)).Cast<TEnum>());
        }

        public void Next()
        {
            if (_options.Count == 0) return;
            SetIndexInternal((_currentIndex + 1) % _options.Count, raiseEvent: true);
        }

        public void Previous()
        {
            if (_options.Count == 0) return;
            SetIndexInternal((_currentIndex - 1 + _options.Count) % _options.Count, raiseEvent: true);
        }

        public void SetIndex(int index)
        {
            SetIndexInternal(index, raiseEvent: true);
        }
        
        private void SetIndexInternal(int index, bool raiseEvent)
        {
            if (_options.Count == 0) return;
            index = Mathf.Clamp(index, 0, _options.Count - 1);
            if (index == _currentIndex) return;

            _currentIndex = index;
            UpdateLabel();

            if (raiseEvent)
                RaiseChanged();
        }

        private void RaiseChanged()
        {
            OnValueChanged?.Invoke(Value);
            OnIndexChanged?.Invoke(_currentIndex);
        }

        private void UpdateLabel()
        {
            if (_label == null || _options.Count == 0)
                return;
            _label.text = _options[Mathf.Clamp(_currentIndex, 0, _options.Count - 1)];
        }
    }
}
