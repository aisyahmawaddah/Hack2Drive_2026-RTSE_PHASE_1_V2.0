using System.Collections.Generic;
using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Reads control inputs from ControlReceiver (TCP from Python) and drives the ProjectedBody.
    /// Handles 5-lane snapping, input delay, output delay, and the light toggle command.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public HqRenderer hQcamera;
        public ProjectedBody body;
        public ControlReceiver controlReceiver;
        public GameManager gameManager;
        public YellowEffect yellowEffect;

        [Header("Settings")]
        public float baseSpeed        = 200f;
        public float boostMultiplier  = 3f;
        public float steerCooldown    = 0.15f; // seconds between lane changes

        // ── Input/output delay ─────────────────────────────────────────────
        private struct TimedInput
        {
            public float time;
            public float steering;
            public float acceleration;
        }
        private Queue<TimedInput> _inputBuffer  = new Queue<TimedInput>();
        private Queue<TimedInput> _outputBuffer = new Queue<TimedInput>();
        private const float DelaySeconds = 0.3f; // 300ms delay

        // ── Steer cooldown ─────────────────────────────────────────────────
        private float _lastSteerTime = -999f;
        private float _prevSteering  = 0f;

        // ── Keyboard fallback (for testing in editor) ──────────────────────
        private bool _useKeyboardFallback = false;

        private void Start()
        {
            if (body != null) body.baseSpeed = baseSpeed;
        }

        private void FixedUpdate()
        {
            if (gameManager != null && gameManager.State != GameState.Playing) return;

            float steering;
            float acceleration;

            // Get raw input from ControlReceiver or keyboard fallback
            if (controlReceiver != null && controlReceiver.IsConnected)
            {
                steering     = controlReceiver.Steering;
                acceleration = controlReceiver.Acceleration;
                _useKeyboardFallback = false;
            }
            else
            {
                // Fallback to keyboard for testing
                _useKeyboardFallback = true;
                steering     = 0f;
                acceleration = 0f;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) steering = 1f;
                if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) steering = -1f;
                if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) acceleration = 1f;
                if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) acceleration = -1f;
            }

            // ── Light toggle (L key or could be mapped from Python) ────────
            if (Input.GetKeyDown(KeyCode.L) && gameManager != null)
            {
                gameManager.OnLightToggle();
            }

            // ── Input delay ────────────────────────────────────────────────
            if (yellowEffect != null && yellowEffect.IsInputDelayed)
            {
                // Buffer the input, use delayed version
                _inputBuffer.Enqueue(new TimedInput 
                { 
                    time = Time.time, 
                    steering = steering, 
                    acceleration = acceleration 
                });

                // Pop inputs that are old enough
                steering = 0f;
                acceleration = 0f;
                while (_inputBuffer.Count > 0 && Time.time - _inputBuffer.Peek().time >= DelaySeconds)
                {
                    var old = _inputBuffer.Dequeue();
                    steering     = old.steering;
                    acceleration = old.acceleration;
                }
            }
            else
            {
                _inputBuffer.Clear(); // flush when effect ends
            }

            // ── Apply steering (lane change) ───────────────────────────────
            ApplySteering(steering);

            // ── Compute desired speed ──────────────────────────────────────
            float spd = baseSpeed;
            if (acceleration > 0.5f) spd *= boostMultiplier;
            else if (acceleration < -0.5f) spd *= -0.5f;

            // ── Output delay ───────────────────────────────────────────────
            if (yellowEffect != null && yellowEffect.IsOutputDelayed)
            {
                _outputBuffer.Enqueue(new TimedInput 
                { 
                    time = Time.time, 
                    steering = 0f, 
                    acceleration = spd 
                });

                spd = baseSpeed; // use neutral speed until delayed value arrives
                while (_outputBuffer.Count > 0 && Time.time - _outputBuffer.Peek().time >= DelaySeconds)
                {
                    var old = _outputBuffer.Dequeue();
                    spd = old.acceleration; // we stored speed in acceleration field
                }
            }
            else
            {
                _outputBuffer.Clear();
            }

            body.baseSpeed = spd;
        }

        private void ApplySteering(float steering)
        {
            // Detect new lane-change request (edge transitions)
            if (Mathf.Abs(steering) > 0.5f && Mathf.Abs(_prevSteering) < 0.5f)
            {
                if (Time.time - _lastSteerTime >= steerCooldown)
                {
                    int delta = steering > 0 ? 1 : -1;
                    body.ShiftLane(delta);
                    _lastSteerTime = Time.time;
                }
            }
            _prevSteering = steering;
        }
    }
}
