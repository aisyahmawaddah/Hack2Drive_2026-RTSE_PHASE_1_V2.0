using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HQ
{
    /// <summary>
    /// HUD overlay for Phase 1 — displays timer, distance, speed, active effects, and event warnings.
    /// Entirely runtime-created (no prefab/scene setup required beyond adding this MonoBehaviour).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("References")]
        public GameManager gameManager;
        public ProjectedBody body;
        public YellowEffect yellowEffect;
        public TokenManager tokenManager;

        [Header("Custom Scene UI (TextMeshPro)")]
        public TextMeshProUGUI speedMeter;
        public TextMeshProUGUI distanceTextTMP; // Link "Text (TMP) (1)" here
        public TextMeshProUGUI timerTextTMP;    
        
        [Header("Token Counters (Replaces FPS)")]
        public TextMeshProUGUI greenTokenText;
        public TextMeshProUGUI redTokenText;
        public TextMeshProUGUI yellowTokenText;

        // ── Runtime UI elements ────────────────────────────────────────────
        private Canvas    _canvas;
        private Text      _timerText;
        private Text      _distanceText;
        private Text      _speedText;
        private Text      _effectsText;
        private Text      _warningText;
        private Text      _gameOverText;
        private Text      _restartText;
        private GameObject _gameOverPanel;
        
        private GameObject _waitingOverlayPanel;
        private Text       _waitingText;

        // ── Setup ─────────────────────────────────────────────────────
        private void Start()
        {
            BuildUI();
        }

        private void BuildUI()
        {
            // Canvas
            GameObject canvasGO = new GameObject("GameUI_Canvas");
            canvasGO.transform.SetParent(transform);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);

            // Timer — top center
            _timerText = CreateText(canvasGO, "Timer", "60.0",
                TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -60), 36, Color.white);

            // Distance — top right
            _distanceText = CreateText(canvasGO, "Distance", "0 m",
                TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-15, -40), 28, Color.cyan);

            // Speed — bottom left
            _speedText = CreateText(canvasGO, "Speed", "1.00x",
                TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(15, 10), 24, Color.green);

            // Active effects — bottom right
            _effectsText = CreateText(canvasGO, "Effects", "",
                TextAnchor.LowerRight, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-15, 10), 18, Color.yellow);

            // Warning — center
            _warningText = CreateText(canvasGO, "Warning", "",
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f),
                Vector2.zero, 30, Color.red);

            // Game Over panel
            _gameOverPanel = new GameObject("GameOverPanel");
            _gameOverPanel.transform.SetParent(canvasGO.transform, false);
            Image bg = _gameOverPanel.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            RectTransform bgrt = _gameOverPanel.GetComponent<RectTransform>();
            bgrt.anchorMin = Vector2.zero;
            bgrt.anchorMax = Vector2.one;
            bgrt.offsetMin = Vector2.zero;
            bgrt.offsetMax = Vector2.zero;
            _gameOverPanel.SetActive(false);

            _gameOverText = CreateText(_gameOverPanel, "GameOverText", "GAME OVER",
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                Vector2.zero, 48, Color.white);

            _restartText = CreateText(_gameOverPanel, "RestartText", "Press R to restart",
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.4f), new Vector2(0.5f, 0.4f),
                Vector2.zero, 24, new Color(0.7f, 0.7f, 0.7f));

            // Waiting To Start panel
            _waitingOverlayPanel = new GameObject("WaitingOverlayPanel");
            _waitingOverlayPanel.transform.SetParent(canvasGO.transform, false);
            Image waitBg = _waitingOverlayPanel.AddComponent<Image>();
            waitBg.color = new Color(0, 0, 0, 0.6f); // Low opacity black overlay
            RectTransform waitRt = _waitingOverlayPanel.GetComponent<RectTransform>();
            waitRt.anchorMin = Vector2.zero;
            waitRt.anchorMax = Vector2.one;
            waitRt.offsetMin = Vector2.zero;
            waitRt.offsetMax = Vector2.zero;
            _waitingOverlayPanel.SetActive(false);

            _waitingText = CreateText(_waitingOverlayPanel, "WaitingText", "WAITING FOR COMMAND",
                TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 36, new Color(1f, 1.0f, 1.0f, 0.8f));
        }

        // ── Update HUD ────────────────────────────────────────────────────
        private void Update()
        {
            if (gameManager == null) return;

            switch (gameManager.State)
            {
                case GameState.WaitingToStart:
                    _timerText.text = "Waiting...";
                    _gameOverPanel.SetActive(false);
                    _waitingOverlayPanel.SetActive(true);
                    
                    if (timerTextTMP != null) timerTextTMP.text = "READY";
                    break;

                case GameState.Playing:
                    _gameOverPanel.SetActive(false);
                    _waitingOverlayPanel.SetActive(false);
                    _timerText.text    = gameManager.TimeRemaining.ToString("F1") + "s";
                    _distanceText.text = (gameManager.TotalDistance / 100f).ToString("F0") + " m";

                    // Speed indicator with color
                    float sm = body != null ? body.speedMultiplier : 1f;
                    _speedText.text  = sm.ToString("F2") + "x";
                    _speedText.color = sm >= 1f ? Color.green : Color.red;

                    // Active effects
                    if (yellowEffect != null)
                        _effectsText.text = yellowEffect.GetActiveEffectsText();

                    // --- Custom TextMeshPro Override ---
                    if (timerTextTMP != null) timerTextTMP.text = gameManager.TimeRemaining.ToString("F1") + "s";
                    if (distanceTextTMP != null) distanceTextTMP.text = (gameManager.TotalDistance / 100f).ToString("F0") + " m";
                    if (speedMeter != null) 
                    {
                        speedMeter.text  = sm.ToString("F2") + "x";
                        speedMeter.color = sm >= 1f ? Color.green : Color.red;
                    }
                    if (tokenManager != null)
                    {
                        if (greenTokenText != null)  greenTokenText.text  = tokenManager.greenTokensCollected.ToString();
                        if (redTokenText != null)    redTokenText.text    = tokenManager.redTokensCollected.ToString();
                        if (yellowTokenText != null) yellowTokenText.text = tokenManager.yellowTokensCollected.ToString();
                    }

                    // Event warnings
                    UpdateWarning();
                    break;

                case GameState.GameOver:
                    _gameOverPanel.SetActive(true);
                    _waitingOverlayPanel.SetActive(false);
                    _gameOverText.text = $"GAME OVER\nDistance: {(gameManager.TotalDistance / 100f):F0} m";
                    _warningText.text = "";

                    if (Input.GetKeyDown(KeyCode.R))
                        gameManager.RestartGame();
                    break;
            }
        }

        private void UpdateWarning()
        {
            if (gameManager.fasterCarEventActive)
            {
                _warningText.text  = "⚠ CAR BEHIND — SWITCH LANES!";
                _warningText.color = new Color(1f, 0.6f, 0f); // orange
            }
            else if (gameManager.policeEventActive)
            {
                _warningText.text  = "🚔 POLICE — TAKE RED TOKEN!";
                _warningText.color = new Color(1f, 0.2f, 0.2f); // red
            }
            else if (gameManager.darknessActive)
            {
                _warningText.text  = "💡 LOW BRIGHTNESS — PRESS L!";
                _warningText.color = Color.yellow;
            }
            else
            {
                _warningText.text = "";
            }
        }

        // ── Helper: create a UI Text element ──────────────────────────────
        private Text CreateText(GameObject parent, string name, string defaultText,
            TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offset, int fontSize, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            Text txt = go.AddComponent<Text>();
            txt.text      = defaultText;
            txt.font      = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize  = fontSize;
            txt.color     = color;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;

            // Add shadow for readability
            Shadow shadow = go.AddComponent<Shadow>();
            shadow.effectColor   = new Color(0, 0, 0, 0.8f);
            shadow.effectDistance = new Vector2(2, -2);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin     = anchorMin;
            rt.anchorMax     = anchorMax;
            rt.anchoredPosition = offset;
            rt.sizeDelta     = new Vector2(500, 60);

            return txt;
        }
    }
}
