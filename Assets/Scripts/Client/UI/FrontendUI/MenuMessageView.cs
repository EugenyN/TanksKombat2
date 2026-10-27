using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client UI overlay: displays a status message (e.g. connection progress) and an
    // optional cancel button that can appear after a configurable delay.
    public class MenuMessageView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Button _cancelButton;

        private CancellationTokenSource _cancelRevealCts;

        public Button.ButtonClickedEvent onCancelButtonClick => _cancelButton.onClick;

        private void OnEnable()
        {
            _messageText.text = string.Empty;
            _cancelButton.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            CancelPendingReveal();
        }

        public void SetMessage(string message)
        {
            _messageText.text = message;
        }

        public async void ShowCancelButton(float delaySeconds = 0)
        {
            CancelPendingReveal();

            if (delaySeconds > 0)
            {
                _cancelRevealCts = new CancellationTokenSource();
                try
                {
                    await Awaitable.WaitForSecondsAsync(delaySeconds, _cancelRevealCts.Token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            _cancelButton.gameObject.SetActive(true);
        }

        private void CancelPendingReveal()
        {
            _cancelRevealCts?.Cancel();
            _cancelRevealCts?.Dispose();
            _cancelRevealCts = null;
        }
    }
}