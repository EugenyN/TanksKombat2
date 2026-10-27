using TMPro;
using UnityEngine;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: single-line in-game event log that auto-clears after a delay.
    public class EventLogView : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _eventLogText;

        [SerializeField] 
        private float _messageDuration = 6f;

        private float _eventMessageTimer;
        
        private void Update()
        {
            _eventMessageTimer -= Time.deltaTime;
            if (_eventMessageTimer < 0f)
                _eventLogText.text = "";
        }

        public void SetEventLogMessage(string value)
        {
            _eventLogText.text = value;
            _eventMessageTimer = _messageDuration;
        }
    }
}