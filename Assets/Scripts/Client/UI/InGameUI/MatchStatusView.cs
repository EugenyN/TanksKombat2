using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TanksKombat2.Client.UI
{
    // Client presentation view: center-screen match status banner (round, fight, win text) with its size.
    public class MatchStatusView : MonoBehaviour
    {
        [SerializeField]
        private Image _statusImage;
        [SerializeField]
        private TMP_Text _statusText;
        [SerializeField]
        private TMP_Text _statusOutlineText;
        
        public void ShowStatus(string value)
        {
            gameObject.SetActive(true);
            _statusText.text = value;
            _statusOutlineText.text = value;
        }

        public void HideStatus()
        {
            gameObject.SetActive(false);
        }

        public void SetMatchStatusSize(Vector2 size)
        {
            _statusImage.rectTransform.sizeDelta = size;
        }
    }
}