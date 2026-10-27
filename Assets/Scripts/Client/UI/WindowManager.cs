using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation service: keeps a stack of active UI windows,
    // toggles their GameObjects and blocks player input while any window is open.
    [AutoStaticsCleanup]
    public partial class WindowManager : MonoBehaviour
    {
        public static WindowManager Instance { get; private set; }
        
        private readonly Stack<GameObject> _stack = new();

        public GameObject Current =>
            _stack.Count > 0 ? _stack.Peek() : null;


        public bool IsCurrentBlocksEscape =>
            Current != null && Current.TryGetComponent<WindowBlocksEscape>(out _);

        private void Awake()
        {
            if (Instance != null && Instance != this) {
                Debug.LogError($"[{nameof(WindowManager)}] Duplicate instance detected, destroying {name}.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null;
            GameInput.SetPlayerInputBlocked(false);
        }
        
        public void Open(GameObject window, bool replaceCurrent = false)
        {
            if (window == null) {
                Debug.LogError($"[{nameof(WindowManager)}] Cannot open a null window.");
                return;
            }

            if (window == Current)
                return;

            if (_stack.Count > 0)
            {
                var top = _stack.Pop();
                top.SetActive(false);
                if (!replaceCurrent)
                    _stack.Push(top);
            }

            _stack.Push(window);

            window.SetActive(true);

            GameInput.SetPlayerInputBlocked(true);
        }

        public T Open<T>() where T : MonoBehaviour
        {
            var window = FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (window == null) {
                Debug.LogError($"[{nameof(WindowManager)}] No object of type {typeof(T).Name} found in the scene.");
                return null;
            }

            Open(window.gameObject);
            return window;
        }

        public void Back()
        {
            if (_stack.Count == 0)
                return;

            _stack.Pop().SetActive(false);

            if (_stack.Count > 0)
                _stack.Peek().SetActive(true);

            GameInput.SetPlayerInputBlocked(_stack.Count > 0);
        }
    }
}