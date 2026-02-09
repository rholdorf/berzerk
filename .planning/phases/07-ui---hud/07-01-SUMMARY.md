---
phase: 07-ui-hud
plan: 01
subsystem: ui
tags: [monogame, spritefont, hud, score, ammo, notifications]

# Dependency graph
requires:
  - phase: 03-core-combat
    provides: AmmoSystem for ammo display integration
  - phase: 04-player-health-survival
    provides: HealthSystem event pattern reference
provides:
  - ScoreSystem with event-based score tracking
  - AmmoCounter with low-ammo flash visual feedback
  - ScoreCounter for top-center score display
  - PickupNotification system for timed popup messages
affects: [07-ui-hud (Plan 03 will integrate these into BerzerkGame)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Event-driven UI updates via System.Action events"
    - "Viewport-relative positioning for resolution independence"
    - "Sinusoidal flash animation for low-ammo warning"
    - "Linear fade-out for notification queue"

key-files:
  created:
    - Berzerk/Source/Combat/ScoreSystem.cs
    - Berzerk/Source/UI/AmmoCounter.cs
    - Berzerk/Source/UI/ScoreCounter.cs
    - Berzerk/Source/UI/PickupNotification.cs
  modified: []

key-decisions:
  - "ScoreSystem fires OnScoreChanged event with current score value for UI updates"
  - "AmmoCounter uses sinusoidal pulse (8 Hz) to flash between red and white when magazine < 10 rounds"
  - "PickupNotification uses vertical stacking (35px offset) for multiple simultaneous notifications"

patterns-established:
  - "HUD text elements follow pattern: LoadContent(ContentManager), Draw(SpriteBatch, data, Viewport)"
  - "Flash effects use sinusoidal interpolation for smooth visual feedback"
  - "Notification queue uses linear fade in last 0.5s of 2.0s duration"

# Metrics
duration: 2min
completed: 2026-02-09
---

# Phase 7 Plan 01: UI & HUD Core Components Summary

**ScoreSystem with event-driven updates, AmmoCounter with low-ammo flash (<10 rounds), ScoreCounter, and PickupNotification queue with fade-out**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-09T15:29:33Z
- **Completed:** 2026-02-09T15:31:31Z
- **Tasks:** 1
- **Files modified:** 4 (all new)

## Accomplishments
- ScoreSystem tracks player score with event-based change notifications
- AmmoCounter displays magazine/reserve at top-right with sinusoidal flash when low (<10 rounds)
- ScoreCounter displays score centered at top of screen
- PickupNotification manages vertical stacking popup queue with linear fade-out

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ScoreSystem and HUD display elements** - `7e6437d` (feat)

## Files Created/Modified
- `Berzerk/Source/Combat/ScoreSystem.cs` - Point tracking with OnScoreChanged event, 50 points per enemy
- `Berzerk/Source/UI/AmmoCounter.cs` - Top-right ammo display with low-ammo flash (sinusoidal pulse at 8 Hz)
- `Berzerk/Source/UI/ScoreCounter.cs` - Top-center score display
- `Berzerk/Source/UI/PickupNotification.cs` - Timed notification queue with 35px vertical stacking and 0.5s fade-out

## Decisions Made

**ScoreSystem event pattern:**
- Chose `event Action<int>` passing current score value (vs parameterless event)
- Enables subscribers to receive score without additional property access
- Consistent with project's event-driven architecture

**AmmoCounter flash implementation:**
- Sinusoidal pulse at 8 Hz between red and white for low-ammo warning
- Accumulates total game time via Update(deltaTime) for smooth animation
- Threshold set at <10 rounds per CONTEXT.md specification

**PickupNotification stacking:**
- Vertical offset of 35px between stacked notifications
- Base Y position at 100px from top
- Linear fade over final 0.5s of 2.0s duration

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for integration:**
- All four UI components compile successfully
- APIs match expected interfaces specified in plan frontmatter:
  - AmmoCounter.Draw(SpriteBatch, int currentMag, int reserveAmmo, Viewport)
  - ScoreCounter.Draw(SpriteBatch, int score, Viewport)
  - PickupNotification.Show(string text, Viewport) + Update/Draw
  - ScoreSystem.OnScoreChanged event subscription

**Blockers:** None

**Concerns:** None - components are independent and ready for BerzerkGame integration in Plan 03

---
*Phase: 07-ui-hud*
*Completed: 2026-02-09*
