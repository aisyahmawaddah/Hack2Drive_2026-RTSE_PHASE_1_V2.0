using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Full-screen dark overlay for the low-brightness event.
    /// Controls a Canvas UI panel that dims the game view.
    /// Created at runtime — no prefab needed.
    /// </summary>
    public class DarkOverlay : MonoBehaviour
    {
        [Header("References")]
        public GameManager gameManager;

        [Header("Settings")]
        public Color darkColor = new Color(0f, 0f, 0f, 0.65f); // 65% black
        public float fadeSpeed = 2f;

        private GameObject _overlayPanel;
        private UnityEngine.UI.Image _overlayImage;
        private Canvas _canvas;
        private float _currentAlpha = 0f;

        private void Start()
        {
            CreateOverlay();
        }

        private void CreateOverlay()
        {
            // Create a Canvas
            GameObject canvasGO = new GameObject("DarkOverlay_Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100; // on top of everything

            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();

            // Create the dark panel
            _overlayPanel = new GameObject("DarkPanel");
            _overlayPanel.transform.SetParent(canvasGO.transform, false);

            _overlayImage = _overlayPanel.AddComponent<UnityEngine.UI.Image>();
            _overlayImage.color = new Color(0f, 0f, 0f, 0f);
            _overlayImage.raycastTarget = false;

            // Stretch to fill entire screen
            RectTransform rt = _overlayPanel.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _overlayPanel.SetActive(true);
        }

        private void Update()
        {
            if (gameManager == null || _overlayImage == null) return;

            float targetAlpha = gameManager.darknessActive ? darkColor.a : 0f;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);
            _overlayImage.color = new Color(darkColor.r, darkColor.g, darkColor.b, _currentAlpha);
        }
    }
}
