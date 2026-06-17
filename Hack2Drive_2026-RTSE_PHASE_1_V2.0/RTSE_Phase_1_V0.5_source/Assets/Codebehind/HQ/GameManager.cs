using UnityEngine;

namespace HQ
{
    public enum GameState { WaitingToStart, Playing, GameOver }

    /// <summary>
    /// Central game manager for Phase 1.
    /// Handles timer (60s countdown), scoring (distance), speed modifiers, and game state.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("References")]
        public ProjectedBody body;
        public TokenManager tokenManager;
        public EventManager eventManager;
        public YellowEffect yellowEffect;
        public GameUI gameUI;
        public CameraStreamer frontCamera;
        public CameraStreamer backCamera;

        [Header("Settings")]
        public float gameDuration = 60f; // seconds

        // ── Game state ─────────────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.WaitingToStart;
        public float TimeRemaining  { get; private set; }
        public float TotalDistance   => body != null ? body.distanceTraveled : 0f;
        public float SpeedMultiplier => body != null ? body.speedMultiplier : 1f;

        // ── Brightness / light event flags (set by EventManager) ───────────
        public bool allTokensYellow = false;  // all tokens forced to yellow
        public bool lightIsOn       = false;  // player pressed L to turn on light
        public bool darknessActive  = false;  // overlay should be visible

        // ── Police event state ─────────────────────────────────────────────
        public bool policeEventActive       = false;
        public bool policeWaitingForRedToken = false;

        // ── Faster-car event state ─────────────────────────────────────────
        public bool fasterCarEventActive = false;
        public int  fasterCarLane        = -1;

        // ── Control flow ───────────────────────────────────────────────────
        private void Update()
        {
            switch (State)
            {
                case GameState.WaitingToStart:
                    // Keep the car from rolling while waiting
                    if (body != null) body.speedMultiplier = 0f;

                    // Start only on manual action / input
                    if (Input.anyKeyDown)
                        StartGame();
                    break;

                case GameState.Playing:
                    TimeRemaining -= Time.deltaTime;
                    if (TimeRemaining <= 0f)
                    {
                        TimeRemaining = 0f;
                        EndGame();
                    }

                    // Sync camera corruption with yellow effect
                    if (frontCamera != null)
                        frontCamera.corruptedCamera = yellowEffect.IsCameraCorrupted;
                    if (backCamera != null)
                        backCamera.corruptedCamera = yellowEffect.IsCameraCorrupted;

                    break;

                case GameState.GameOver:
                    // Freeze the car
                    if (body != null) body.speedMultiplier = 0f;
                    break;
            }
        }

        public void StartGame()
        {
            State = GameState.Playing;
            TimeRemaining = gameDuration;
            if (body != null)
            {
                body.distanceTraveled = 0f;
                body.speedMultiplier  = 1f;
            }
            Debug.Log("[GameManager] Game started! 60 seconds on the clock.");
        }

        private void EndGame()
        {
            State = GameState.GameOver;
            Debug.Log($"[GameManager] GAME OVER — Distance: {TotalDistance:F0}");
        }

        public void RestartGame()
        {
            if (body != null)
            {
                body.tripFloat        = 0f;
                body.distanceTraveled = 0f;
                body.speedMultiplier  = 1f;
                body.currentLane      = ProjectedBody.DefaultLane;
                body.playerX          = 0f;
            }
            allTokensYellow = false;
            lightIsOn       = false;
            darknessActive  = false;
            policeEventActive = false;
            policeWaitingForRedToken = false;
            fasterCarEventActive = false;

            if (tokenManager != null) tokenManager.tokens.Clear();

            StartGame();
        }

        // ── Event callbacks ────────────────────────────────────────────────

        /// <summary>Called by TokenManager when player collects a red token.</summary>
        public void OnRedTokenCollected()
        {
            if (policeWaitingForRedToken)
            {
                policeWaitingForRedToken = false;
                policeEventActive = false;
                Debug.Log("[GameManager] Police event cleared — red token taken.");
            }
        }

        /// <summary>Called by EventManager when player fails to take red token during police event.</summary>
        public void OnPoliceIgnored()
        {
            if (body != null)
            {
                body.speedMultiplier *= 0.5f;
                Debug.Log($"[GameManager] Police ignored! Speed halved → {body.speedMultiplier:F2}x");
            }
            policeEventActive = false;
            policeWaitingForRedToken = false;
        }

        /// <summary>Called by EventManager when the faster car collides with player.</summary>
        public void OnFasterCarCollision()
        {
            if (body != null)
            {
                body.speedMultiplier *= 0.5f;
                Debug.Log($"[GameManager] Faster car collision! Speed halved → {body.speedMultiplier:F2}x");
            }
            fasterCarEventActive = false;
        }

        /// <summary>Called by EventManager to start the brightness/darkness event.</summary>
        public void OnDarknessEvent()
        {
            darknessActive  = true;
            allTokensYellow = true;
            lightIsOn       = false;
            Debug.Log("[GameManager] Darkness event! All tokens are yellow. Press L to turn on light.");
        }

        /// <summary>Called by PlayerController/EventManager when L key is pressed.</summary>
        public void OnLightToggle()
        {
            if (darknessActive)
            {
                lightIsOn       = true;
                darknessActive  = false;
                allTokensYellow = false;
                Debug.Log("[GameManager] Light turned ON! Green tokens now +5% instead of +10%.");
            }
        }
    }
}
