using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Manages the 5 possible yellow-token timed effects.
    /// Each effect starts on collection and expires after its duration.
    /// Query methods allow other systems to check active effects.
    /// </summary>
    public class YellowEffect : MonoBehaviour
    {
        // ── Effect durations ───────────────────────────────────────────────
        private const float EffectDuration = 5f; // seconds for timed effects

        // ── Active effect timers (-1 = inactive) ───────────────────────────
        private float _hideNextTokenTimer    = -1f; // 20%: next token type/color hidden
        private float _invisibleTokensTimer  = -1f; // 20%: all tokens invisible for 5s
        private float _inputDelayTimer       = -1f; // 20%: camera input delay for 5s
        private float _outputDelayTimer      = -1f; // 20%: action output delay for 5s
        private float _corruptedCameraTimer  = -1f; // 20%: corrupted camera for 5s

        // ── "All tokens gray" effect lasts until ANY token is picked up ────────
        public bool AreTokensGray { get; private set; } = false;

        // ── Public queries ─────────────────────────────────────────────────
        public bool AreTokensInvisible  => _invisibleTokensTimer > 0f;
        public bool IsInputDelayed      => _inputDelayTimer      > 0f;
        public bool IsOutputDelayed     => _outputDelayTimer     > 0f;
        public bool IsCameraCorrupted   => _corruptedCameraTimer > 0f;

        public void ClearGrayEffect()
        {
            AreTokensGray = false;
        }

        // ── Roll a random yellow effect (each 20%) ─────────────────────────
        public void ActivateRandom()
        {
            int roll = Random.Range(0, 5);
            switch (roll)
            {
                case 0:
                    AreTokensGray = true;
                    Debug.Log("[YellowEffect] All tokens turned gray until next pickup!");
                    break;
                case 1:
                    _invisibleTokensTimer = EffectDuration;
                    Debug.Log("[YellowEffect] Tokens invisible for 5s!");
                    break;
                case 2:
                    _inputDelayTimer = EffectDuration;
                    Debug.Log("[YellowEffect] Camera input delay for 5s!");
                    break;
                case 3:
                    _outputDelayTimer = EffectDuration;
                    Debug.Log("[YellowEffect] Action output delay for 5s!");
                    break;
                case 4:
                    _corruptedCameraTimer = EffectDuration;
                    Debug.Log("[YellowEffect] Corrupted camera for 5s!");
                    break;
            }
        }

        // ── Tick timers ────────────────────────────────────────────────────
        private void Update()
        {
            float dt = Time.deltaTime;
            if (_invisibleTokensTimer > 0f)  _invisibleTokensTimer  -= dt;
            if (_inputDelayTimer      > 0f)  _inputDelayTimer       -= dt;
            if (_outputDelayTimer     > 0f)  _outputDelayTimer      -= dt;
            if (_corruptedCameraTimer > 0f)  _corruptedCameraTimer  -= dt;
        }

        /// <summary>Returns a status string for the HUD.</summary>
        public string GetActiveEffectsText()
        {
            string s = "";
            if (AreTokensGray)              s += "TOKENS HIDDEN (GRAY) | ";
            if (AreTokensInvisible)         s += $"TOKENS INVISIBLE {_invisibleTokensTimer:F1}s | ";
            if (IsInputDelayed)             s += $"INPUT DELAY {_inputDelayTimer:F1}s | ";
            if (IsOutputDelayed)            s += $"OUTPUT DELAY {_outputDelayTimer:F1}s | ";
            if (IsCameraCorrupted)          s += $"CAMERA CORRUPTED {_corruptedCameraTimer:F1}s | ";
            return s;
        }
    }
}
