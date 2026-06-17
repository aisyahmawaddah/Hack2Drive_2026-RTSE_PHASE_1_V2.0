using UnityEngine;

namespace HQ
{
    // ── Token type ─────────────────────────────────────────────────────
    public enum TokenType { Green, Red, Yellow }

    // ── Single token on the track ──────────────────────────────────────
    [System.Serializable]
    public class Token
    {
        public TokenType type;
        public int  segmentIndex;   // which road segment the token sits on
        public int  lane;           // 0..4 (maps to ProjectedBody.LanePositions)
        public bool collected;      // already picked up
        public bool isVisible;      // false during "invisible tokens" yellow effect
        public bool typeHidden;     // true when "next token hidden" yellow effect applies

        // True type is still stored in `type`; renderer shows `?` sprite if typeHidden.

        public Token(TokenType t, int segment, int lane)
        {
            type         = t;
            segmentIndex = segment;
            this.lane    = lane;
            collected    = false;
            isVisible    = true;
            typeHidden   = false;
        }
    }
}
