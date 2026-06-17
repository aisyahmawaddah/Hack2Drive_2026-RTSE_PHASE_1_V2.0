# SPEEDTRIALS2D 2026 - Phase 1

## Technical Setup
- How to control the Unity environment using the Python communication script
- How to train / use a small YOLO model
- How to use simple computer vision algos (OpenCV)

## Game Rules
**Objective:** 60 seconds to travel the furthest distance.

### Tokens & Effects
- **Green Token:** Increase speed by +10%
- **Red Token:** Decrease speed by −20%
- **Yellow Token:**
  - 20% = Next token type / color hidden
  - 20% = Next 5 seconds, tokens become invisible
  - 20% = Next 5 seconds, camera input delay
  - 20% = Next 5 seconds, action output delay
  - 20% = Next 5 seconds, corrupted camera input

### Events
- A faster car appears behind, and the player must switch lanes. On collision: −50% speed.
- A police car appears behind, and the player must take the next red token. If you ignore: −50% speed.
- The brightness decreases under 50%. All tokens become Yellow until the light is turned ON. While light ON, green tokens increase speed by +5% instead.

![Phase 1 Demo](../Phase_1_game.gif)
