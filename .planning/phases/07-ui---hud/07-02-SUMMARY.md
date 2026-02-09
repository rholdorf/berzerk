---
phase: 07-ui-hud
plan: 02
subsystem: ui
tags: [monogame, ui, menu, mouse-input, event-driven]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: MonoGame foundation, content pipeline, Font asset
  - phase: 04-player-health-survival
    provides: UI pattern with programmatic textures (GameOverScreen, HealthBar)
provides:
  - StartMenu with OnStartGame event for game entry point
  - PauseMenu with OnResume and OnQuit events for in-game overlay
  - Event-driven menu architecture ready for BerzerkGame integration
affects: [07-03-integration, game-state-management]

# Tech tracking
tech-stack:
  added: []
  patterns: [event-driven menus, mouse state tracking, button hover feedback, shared DrawButton helper]

key-files:
  created:
    - Berzerk/Source/UI/StartMenu.cs
    - Berzerk/Source/UI/PauseMenu.cs
  modified: []

key-decisions:
  - "Mouse interaction via current/previous MouseState parameters (no InputManager coupling)"
  - "Shared DrawButton helper method between menus for consistency"
  - "Semi-transparent overlay (Color.Black * 0.7f) for pause menu background"

patterns-established:
  - "Button pattern: measure text, add padding, center, draw background with hover state, draw text"
  - "Event-driven menu architecture: expose Action events, let BerzerkGame wire them up"

# Metrics
duration: 2min
completed: 2026-02-09
---

# Phase 7 Plan 02: Menu Screens Summary

**StartMenu and PauseMenu with mouse-driven buttons, hover feedback, and event-driven architecture using programmatic textures**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-09T15:30:17Z
- **Completed:** 2026-02-09T15:32:38Z
- **Tasks:** 1
- **Files modified:** 2

## Accomplishments
- StartMenu with centered "BERZERK" title and "Start Game" button firing OnStartGame event
- PauseMenu with darkened overlay, "PAUSED" title, Resume/Quit buttons firing respective events
- Consistent minimalist button styling with hover feedback (DarkGray on hover, Gray * 0.5f normally)
- Shared DrawButton helper method ensuring visual consistency between menus

## Task Commits

Each task was committed atomically:

1. **Task 1: Create StartMenu and PauseMenu screen components** - `5ad196a` (feat)

## Files Created/Modified
- `Berzerk/Source/UI/StartMenu.cs` - Game entry screen with title and start button, OnStartGame event
- `Berzerk/Source/UI/PauseMenu.cs` - In-game pause overlay with semi-transparent background, Resume/Quit buttons, OnResume/OnQuit events

## Decisions Made

**1. Mouse state parameter pattern**
- Rationale: Menus take MouseState current/previous as parameters rather than using InputManager, keeping them decoupled and testable. BerzerkGame will pass Mouse.GetState() and track previous state.

**2. Shared DrawButton helper method**
- Rationale: Both menus use identical button styling. Extracted private helper method to avoid code duplication and ensure consistent visual appearance.

**3. Semi-transparent overlay intensity**
- Rationale: Used Color.Black * 0.7f for pause menu overlay to darken gameplay while keeping it partially visible, maintaining visual context during pause.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - straightforward implementation following established UI patterns from GameOverScreen and HealthBar.

## Next Phase Readiness

Ready for Plan 03 (BerzerkGame Integration):
- Both menus expose events ready to wire into BerzerkGame state machine
- Mouse interaction pattern established and tested via build
- Consistent button styling matches phase CONTEXT minimalist white/gray aesthetic

No blockers.

---
*Phase: 07-ui-hud*
*Completed: 2026-02-09*
