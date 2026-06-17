using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Manages random game events:
    ///   1) Faster car behind → switch lanes or -50% speed
    ///   2) Police car behind → take next red token or -50% speed
    ///   3) Low brightness    → all tokens yellow until light ON
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        [Header("References")]
        public GameManager gameManager;
        public ProjectedBody body;
        public TrackObject track;
        public TokenManager tokenManager;

        [Header("Event Sprites (assign in Inspector)")]
        public Sprite fasterCarSprite;
        public Sprite policeCarSprite;

        [Header("Timing")]
        public float minEventInterval = 12f;
        public float maxEventInterval = 20f;

        // ── Internal state ─────────────────────────────────────────────────
        private float _nextEventTime;
        private float _eventTimer;

        // Faster car
        private float _fasterCarTimer;
        private int   _fasterCarSegment = -1; // where the NPC car sprite is

        // Police car
        private float _policeTimer;
        private float _policeGracePeriod = 10f; // seconds to take a red token
        private int   _policeCarSegment = -1;

        // Brightness
        private float _brightnessTimer;
        private float _brightnessDuration = 15f;

        private void Start()
        {
            ScheduleNextEvent();
        }

        private void ScheduleNextEvent()
        {
            _nextEventTime = Time.time + Random.Range(minEventInterval, maxEventInterval);
        }

        private void Update()
        {
            if (gameManager == null || gameManager.State != GameState.Playing) return;

            // Trigger new event
            if (Time.time >= _nextEventTime && 
                !gameManager.fasterCarEventActive && 
                !gameManager.policeEventActive && 
                !gameManager.darknessActive)
            {
                TriggerRandomEvent();
                ScheduleNextEvent();
            }

            // Update active events
            UpdateFasterCarEvent();
            UpdatePoliceEvent();
            UpdateBrightnessEvent();
        }

        // ── Event triggers ─────────────────────────────────────────────────

        private void TriggerRandomEvent()
        {
            int roll = Random.Range(0, 3);
            switch (roll)
            {
                case 0: TriggerFasterCar(); break;
                case 1: TriggerPoliceCar(); break;
                case 2: TriggerDarkness();  break;
            }
        }

        private void TriggerFasterCar()
        {
            // Pick the player's current lane as the obstacle lane
            gameManager.fasterCarEventActive = true;
            gameManager.fasterCarLane        = body.currentLane;

            // Place car sprite behind player
            int playerSeg   = body.trip / track.segmentLength;
            _fasterCarSegment = (playerSeg - 10 + track.Length) % track.Length;
            _fasterCarTimer   = 5f; // 5 seconds to dodge

            Debug.Log($"[Event] FASTER CAR in lane {body.currentLane}! Switch lanes!");
        }

        private void TriggerPoliceCar()
        {
            gameManager.policeEventActive       = true;
            gameManager.policeWaitingForRedToken = true;

            int playerSeg     = body.trip / track.segmentLength;
            _policeCarSegment  = (playerSeg - 8 + track.Length) % track.Length;
            _policeTimer       = _policeGracePeriod;

            Debug.Log("[Event] POLICE CAR! Take the next red token!");
        }

        private void TriggerDarkness()
        {
            gameManager.OnDarknessEvent();
            _brightnessTimer = _brightnessDuration;
            Debug.Log("[Event] LOW BRIGHTNESS! All tokens yellow. Press L to turn on light.");
        }

        // ── Event updates ──────────────────────────────────────────────────

        private void UpdateFasterCarEvent()
        {
            if (!gameManager.fasterCarEventActive) return;

            _fasterCarTimer -= Time.deltaTime;

            // Move the car closer to player
            int playerSeg = body.trip / track.segmentLength;
            float progress = 1f - (_fasterCarTimer / 5f);
            int newSegment = (playerSeg - 10 + (int)(10 * progress) + track.Length) % track.Length;

            if (_fasterCarSegment != newSegment)
            {
                // Restore scenery on the old segment
                if (_fasterCarSegment >= 0 && _fasterCarSegment < track.Length)
                {
                    track.lines[_fasterCarSegment].npcBlockedLane = -99;
                    track.lines[_fasterCarSegment].sprite = track.lines[_fasterCarSegment].originalSprite;
                    track.lines[_fasterCarSegment].spriteX = track.lines[_fasterCarSegment].originalSpriteX;
                }
                _fasterCarSegment = newSegment;
            }

            // Render car sprite on the new segment
            if (_fasterCarSegment >= 0 && _fasterCarSegment < track.Length && fasterCarSprite != null)
            {
                ref Line line = ref track.lines[_fasterCarSegment];
                line.sprite  = fasterCarSprite;
                line.spriteX = ProjectedBody.LanePositions[gameManager.fasterCarLane];
                line.npcBlockedLane = gameManager.fasterCarLane;
            }

            // Check: has player dodged?
            if (body.currentLane != gameManager.fasterCarLane)
            {
                // Player dodged successfully
                Debug.Log("[Event] Faster car dodged!");
                ClearFasterCar();
                return;
            }

            // Timer expired and still in same lane → collision
            if (_fasterCarTimer <= 0f)
            {
                gameManager.OnFasterCarCollision();
                ClearFasterCar();
            }
        }

        private void ClearFasterCar()
        {
            gameManager.fasterCarEventActive = false;
            if (_fasterCarSegment >= 0 && _fasterCarSegment < track.Length)
            {
                track.lines[_fasterCarSegment].npcBlockedLane = -99;
                track.lines[_fasterCarSegment].sprite = track.lines[_fasterCarSegment].originalSprite;
                track.lines[_fasterCarSegment].spriteX = track.lines[_fasterCarSegment].originalSpriteX;
            }
            _fasterCarSegment = -1;
        }

        private void UpdatePoliceEvent()
        {
            if (!gameManager.policeEventActive) return;

            _policeTimer -= Time.deltaTime;

            // Move police car behind player
            int playerSeg = body.trip / track.segmentLength;
            int newSegment = (playerSeg - 6 + track.Length) % track.Length;

            if (_policeCarSegment != newSegment)
            {
                // Restore scenery on the old segment
                if (_policeCarSegment >= 0 && _policeCarSegment < track.Length)
                {
                    track.lines[_policeCarSegment].sprite = track.lines[_policeCarSegment].originalSprite;
                    track.lines[_policeCarSegment].spriteX = track.lines[_policeCarSegment].originalSpriteX;
                }
                _policeCarSegment = newSegment;
            }

            if (_policeCarSegment >= 0 && _policeCarSegment < track.Length && policeCarSprite != null)
            {
                ref Line line = ref track.lines[_policeCarSegment];
                line.sprite  = policeCarSprite;
                line.spriteX = ProjectedBody.LanePositions[body.currentLane];
            }

            // If red token was collected, event is cleared in GameManager.OnRedTokenCollected()

            // Timer expired without taking red token
            if (_policeTimer <= 0f && gameManager.policeWaitingForRedToken)
            {
                gameManager.OnPoliceIgnored();
                ClearPoliceCar();
            }
        }

        public void ClearPoliceCar()
        {
            gameManager.policeEventActive = false;
            gameManager.policeWaitingForRedToken = false;
            if (_policeCarSegment >= 0 && _policeCarSegment < track.Length)
            {
                track.lines[_policeCarSegment].sprite = track.lines[_policeCarSegment].originalSprite;
                track.lines[_policeCarSegment].spriteX = track.lines[_policeCarSegment].originalSpriteX;
            }
            _policeCarSegment = -1;
        }

        private void UpdateBrightnessEvent()
        {
            if (!gameManager.darknessActive) return;

            _brightnessTimer -= Time.deltaTime;

            // Auto-clear after duration (even if player didn't press L)
            if (_brightnessTimer <= 0f)
            {
                gameManager.darknessActive  = false;
                gameManager.allTokensYellow = false;
                gameManager.lightIsOn       = false;
                Debug.Log("[Event] Darkness event expired.");
            }
        }
    }
}
