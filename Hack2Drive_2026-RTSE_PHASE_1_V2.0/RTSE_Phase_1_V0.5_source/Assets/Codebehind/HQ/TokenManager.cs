using System.Collections.Generic;
using UnityEngine;

namespace HQ
{
    /// <summary>
    /// Spawns tokens along the track, renders them by setting sprites on Line segments,
    /// and handles collision/collection logic.
    /// </summary>
    public class TokenManager : MonoBehaviour
    {
        [Header("References")]
        public TrackObject track;
        public ProjectedBody body;
        public GameManager gameManager;
        public YellowEffect yellowEffect;

        [Header("Token Sprites (assign in Inspector)")]
        public Sprite greenSprite;
        public Sprite redSprite;
        public Sprite yellowSprite;
        public Sprite unknownSprite; // for "hidden type" effect

        [Header("Audio")]
        public AudioSource audioSource;  // attach an AudioSource component on this GameObject
        public AudioClip   greenSound;   // played on green token collection
        public AudioClip   redSound;     // played on red token collection
        public AudioClip   yellowSound;  // played on yellow token collection

        [Header("Settings")]
        public float tokensPerMinute = 60f; // Target number of tokens per minute
        public float collectRadius = 0.8f; // how close in X to collect

        // ── Internal state ─────────────────────────────────────────────────
        public List<Token> tokens = new List<Token>();
        private float _spawnTimer = 0f;

        [Header("Collection Stats")]
        public int greenTokensCollected = 0;
        public int redTokensCollected = 0;
        public int yellowTokensCollected = 0;

        // ── Spawn ──────────────────────────────────────────────────────────
        public void SpawnTokensAhead()
        {
            if (body.speedMultiplier <= 0f) return; // pause spawning if car is stopped

            _spawnTimer -= Time.fixedDeltaTime;

            while (_spawnTimer <= 0f)
            {
                // Replenish timer with small random jitter for natural distribution
                float baseInterval = 60f / Mathf.Max(1f, tokensPerMinute);
                _spawnTimer += baseInterval * Random.Range(0.8f, 1.2f); 

                int playerSeg = body.trip / track.segmentLength;
                
                // Spawn tokens perfectly near the horizon automatically (280 segments ahead)
                int wrappedSeg = (playerSeg + 280) % track.Length;
                if (wrappedSeg < 0) wrappedSeg += track.Length;

                // Don't double-spawn on same segment
                bool alreadyExists = false;
                foreach (var t in tokens)
                {
                    if (t.segmentIndex == wrappedSeg) { alreadyExists = true; break; }
                }
                if (alreadyExists) continue;

                // Decide how many tokens to spawn in this row (1 to 3)
                int tokensThisRow = Random.Range(1, 4); 
                List<int> availableLanes = new List<int> { 0, 1, 2, 3, 4 };

                for (int i = 0; i < tokensThisRow; i++)
                {
                    // Pick random unique lane
                    int randIdx = Random.Range(0, availableLanes.Count);
                    int lane = availableLanes[randIdx];
                    availableLanes.RemoveAt(randIdx);

                    // Check if brightness event forces all yellow
                TokenType type;
                if (gameManager != null && gameManager.allTokensYellow)
                {
                    type = TokenType.Yellow;
                }
                else
                {
                    // Random distribution: 40% green, 30% red, 30% yellow
                    float r = Random.value;
                    if (r < 0.4f)       type = TokenType.Green;
                    else if (r < 0.7f)  type = TokenType.Red;
                    else                type = TokenType.Yellow;
                    }

                    Token token = new Token(type, wrappedSeg, lane);

                    // If "invisible tokens" effect is active
                    if (yellowEffect.AreTokensInvisible)
                    {
                        token.isVisible = false;
                    }

                tokens.Add(token);
                }
            }
        }

        // ── Render tokens dynamically directly to HqRenderer or HqRearRenderer
        public void DrawTokensForSegment(int segment, ref Line line, HqRenderer renderer = null, HqRearRenderer rearRenderer = null)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                // Only render active, visible tokens belonging to this segment
                if (t.segmentIndex == segment && !t.collected)
                {
                    // Evaluate visibility override
                    t.isVisible = !yellowEffect.AreTokensInvisible;
                    if (!t.isVisible) continue;

                    Sprite spr;
                    // If the gray effect is rolling, override visual so player can't see the type
                    if (yellowEffect != null && yellowEffect.AreTokensGray)
                    {
                        spr = unknownSprite;
                    }
                    else
                    {
                        switch (t.type)
                        {
                            case TokenType.Green:  spr = greenSprite;  break;
                            case TokenType.Red:    spr = redSprite;    break;
                            case TokenType.Yellow: spr = yellowSprite; break;
                            default: spr = unknownSprite; break;
                        }
                    }

                    // Ask the active renderer to draw this specifically as a token 
                    // which correctly scales its width to match exactly 1 lane.
                    if (renderer != null) renderer.drawToken(ref line, spr, ProjectedBody.LanePositions[t.lane]);
                    if (rearRenderer != null) rearRenderer.drawToken(ref line, spr, ProjectedBody.LanePositions[t.lane]);
                }
            }
        }

        // ── Collision detection ────────────────────────────────────────────
        public void CheckCollection()
        {
            int playerSeg = body.trip / track.segmentLength;

            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                Token t = tokens[i];
                if (t.collected) continue;

                // Check segment proximity (within 3 segments)
                int segDist = Mathf.Abs(t.segmentIndex - playerSeg);
                // Handle track wrap-around
                if (segDist > track.Length / 2) segDist = track.Length - segDist;

                if (segDist > 3) continue;

                // Check lane proximity
                float tokenX = ProjectedBody.LanePositions[t.lane];
                float dist   = Mathf.Abs(body.playerX - tokenX);

                if (dist < collectRadius)
                {
                    CollectToken(t);
                }
            }
        }

        private void CollectToken(Token t)
        {
            t.collected = true;

            // Collecting ANY token immediately clears the Gray/Unknown effect
            if (yellowEffect != null && yellowEffect.AreTokensGray)
            {
                yellowEffect.ClearGrayEffect();
            }

            switch (t.type)
            {
                case TokenType.Green:
                    greenTokensCollected++;
                    // +10% speed boost
                    body.speedMultiplier += 0.10f;
                    Debug.Log($"[Token] Green collected: speed +10% → {body.speedMultiplier:F2}x");
                    PlayCollectionSound(greenSound);
                    break;

                case TokenType.Red:
                    redTokensCollected++;
                    // −20% speed penalty
                    body.speedMultiplier = Mathf.Max(0.1f, body.speedMultiplier - 0.20f);
                    Debug.Log($"[Token] Red collected: speed -20% → {body.speedMultiplier:F2}x");
                    PlayCollectionSound(redSound);

                    // If police event is active, the player obeyed → clear it
                    if (gameManager != null)
                        gameManager.OnRedTokenCollected();
                    break;

                case TokenType.Yellow:
                    yellowTokensCollected++;
                    // Random yellow effect (see YellowEffect.cs)
                    yellowEffect.ActivateRandom();
                    PlayCollectionSound(yellowSound);
                    break;
            }
            // We no longer poke track.lines[...].tokenIndex since we don't rely on it!
        }

        // ── Audio ──────────────────────────────────────────────────────────
        private void PlayCollectionSound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }

        // ── Cleanup old tokens far behind ──────────────────────────────────
        public void CleanupOldTokens()
        {
            int playerSeg = body.trip / track.segmentLength;
            tokens.RemoveAll(t =>
            {
                if (t.collected) return true;
                int dist = playerSeg - t.segmentIndex;
                if (dist < 0) dist += track.Length;
                
                // If dist > track.Length / 2, the token is actually far AHEAD of the car, 
                // so we do NOT delete it. We only delete if it's strictly a short distance BEHIND.
                return dist > 50 && dist < (track.Length / 2); // remove tokens passed us by 50 segments
            });
        }

        // ── Update loop ────────────────────────────────────────────────────
        private void FixedUpdate()
        {
            if (gameManager != null && gameManager.State != GameState.Playing) return;

            SpawnTokensAhead();
            CheckCollection();
            CleanupOldTokens();
        }
    }
}
