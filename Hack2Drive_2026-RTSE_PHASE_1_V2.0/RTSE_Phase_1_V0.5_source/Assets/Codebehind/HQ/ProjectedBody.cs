using System;
using UnityEngine;

namespace HQ
{
    public class ProjectedBody : MonoBehaviour
    {
        // ── Lanes ──────────────────────────────────────────────────────────
        // Lane positions in normalised road-space: ±1 = road edge, so keep within ±0.75
        // to ensure all lanes stay on the road surface and not in the grass.
        public static readonly float[] LanePositions = { -0.75f, -0.375f, 0f, 0.375f, 0.75f };
        public const int LaneCount = 5;
        public const int DefaultLane = 2; // centre lane index

        // ── Internal state ─────────────────────────────────────────────────
        internal float playerX = 0f;            // continuous X for rendering
        internal float baseSpeed = 200f;         // pixels/frame at neutral
        internal float speedMultiplier = 1f;     // cumulative token modifier
        internal float speed => baseSpeed * speedMultiplier; // effective speed
        public TrackObject track;

        [NonSerialized] private int playerPos;
        public float centrifugal = 0.1f;

        // ── Trip & distance ────────────────────────────────────────────────
        [NonSerialized] public float tripFloat = 0f;  // sub-pixel accumulator
        public int trip => (int)tripFloat;

        [NonSerialized] public float distanceTraveled = 0f; // total pixels traveled forward

        // ── Lane system ────────────────────────────────────────────────────
        [NonSerialized] public int currentLane = DefaultLane;  // 0..4
        [NonSerialized] private int targetLane  = DefaultLane;

        // Called by PlayerController to request a lane change
        public void ShiftLane(int delta)
        {
            targetLane = Mathf.Clamp(targetLane + delta, 0, LaneCount - 1);
        }

        public void SetLane(int lane)
        {
            targetLane = Mathf.Clamp(lane, 0, LaneCount - 1);
        }

        // ── FixedUpdate ────────────────────────────────────────────────────
        public void FixedUpdate()
        {
            float eff = baseSpeed * speedMultiplier;

            // Advance trip counter
            tripFloat += eff * Time.fixedDeltaTime;
            if (eff > 0f) distanceTraveled += eff * Time.fixedDeltaTime;

            float trackLen = track.Length * track.segmentLength;
            while (tripFloat >= trackLen) tripFloat -= trackLen;
            while (tripFloat < 0)         tripFloat += trackLen;

            // Snap to target lane (lerp for smoothness)
            currentLane = targetLane;
            float targetX = LanePositions[currentLane];
            playerX = Mathf.Lerp(playerX, targetX, 8f * Time.fixedDeltaTime);

            // Clamp to road boundaries (centrifugal perspective is handled by
            // the renderer's curve-accumulation loop — no need to mutate playerX here).
            playerX = Mathf.Clamp(playerX, LanePositions[0], LanePositions[LaneCount - 1]);
        }
    }
}
