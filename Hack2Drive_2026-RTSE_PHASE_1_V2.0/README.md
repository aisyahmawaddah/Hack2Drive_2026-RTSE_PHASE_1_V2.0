# Hack2Drive 2026

## Introduction
This lab introduces core principles of Real-Time Systems Engineering through a hands-on self-driving simulation. Participants will build a Python-based control system that:
- Receives real-time camera input from a Unity environment
- Processes perception using computer vision / ML
- Sends control commands under strict timing constraints

## Project Structure
- **[RTSE_Phase_1_V0.5_source](./RTSE_Phase_1_V0.5_source)**: 2D racing environment with tokens and events.
- **[RTSE_Phase_2_V0.5_source](./RTSE_Phase_2_V0.5_source)**: Navigation challenge focused on self-driving capabilities and Behavioural Cloning.

---

## Phase 1
![Phase 1 Demo](./Phase_1_game.gif)

### Technical Setup
- How to control the Unity environment using the Python communication script
- How to train / use a small YOLO model
- How to use simple computer vision algos (OpenCV)

### Game Rules
**Objective:** 60 seconds to travel the furthest distance.

#### Tokens & Effects
- **Green Token:** Increase speed by +10%
- **Red Token:** Decrease speed by −20%
- **Yellow Token:**
  - 20% = Next token type / color hidden
  - 20% = Next 5 seconds, tokens become invisible
  - 20% = Next 5 seconds, camera input delay
  - 20% = Next 5 seconds, action output delay
  - 20% = Next 5 seconds, corrupted camera input

#### Events
- A faster car appears behind, and the player must switch lanes. On collision: −50% speed.
- A police car appears behind, and the player must take the next red token. If you ignore: −50% speed.
- The brightness decreases under 50%. All tokens become Yellow until the light is turned ON. While light ON, green tokens increase speed by +5% instead.

---
