using UnityEngine;

namespace TanksKombat2.Client
{
    // Client MonoBehaviour: letterbox/pixel-perfect fits the camera to the fixed 864x480 (16:9) logical resolution.
    [RequireComponent(typeof(Camera))]
    public class FixedAspectCamera : MonoBehaviour
    {
        [SerializeField] private float _targetWidth = 864;
        [SerializeField] private float _targetHeight = 480;
        [SerializeField] private float _depthZ = 0f;
        [SerializeField] private float _spriteSize = 32;
        private readonly bool _pixelPerfect = false; // not implemented

        private Camera _camera;
        private int _screenWidth;
        private int _screenHeight;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _screenWidth = Screen.width;
            _screenHeight = Screen.height;
        }

        private void OnEnable()
        {
            UpdateCamera();
        }

        private void Update()
        {
            if (_screenWidth == Screen.width && _screenHeight == Screen.height)
                return;

            _screenWidth = Screen.width;
            _screenHeight = Screen.height;

            UpdateCamera();
        }

        public void UpdateCamera()
        {
            if (_pixelPerfect)
            {
                // largest integer scale that fits the window; never larger than the
                // fractional maximum the regular letterbox mode would allow.
                int maxScale = Mathf.FloorToInt(Mathf.Min(Screen.width / _targetWidth, Screen.height / _targetHeight));
                int scale = Mathf.Max(1, maxScale);

                int pixelWidth = Mathf.RoundToInt(_targetWidth * scale);
                int pixelHeight = Mathf.RoundToInt(_targetHeight * scale);

                // integer pixel offsets keep sprite pixels aligned (no half-pixel blur).
                int offsetX = (Screen.width - pixelWidth) / 2;
                int offsetY = (Screen.height - pixelHeight) / 2;
                _camera.rect = new Rect(offsetX / (float)Screen.width, offsetY / (float)Screen.height,
                    pixelWidth / (float)Screen.width, pixelHeight / (float)Screen.height);
            }
            else
            {
                float targetAspect = _targetWidth / _targetHeight;
                float currentAspect = (float)Screen.width / Screen.height;

                if (currentAspect > targetAspect)
                {
                    float scale = targetAspect / currentAspect;
                    _camera.rect = new Rect((1f - scale) * 0.5f, 0f, scale, 1f);
                }
                else
                {
                    float scale = currentAspect / targetAspect;
                    _camera.rect = new Rect(0f, (1f - scale) * 0.5f, 1f, scale);
                }
            }

            _camera.orthographicSize = _targetHeight / Mathf.Max(1, _spriteSize) * 0.5f;
        }

        public void SetSpriteSize(float spriteSize)
        {
            _spriteSize = spriteSize;
        }

        public void SetPosition(int gameAreaWidth, int gameAreaHeight)
        {
            transform.position = new Vector3(gameAreaWidth * 0.5f, gameAreaHeight * 0.5f, _depthZ);
        }
    }
}
