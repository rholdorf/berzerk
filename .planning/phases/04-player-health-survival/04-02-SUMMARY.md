---
phase: 04-player-health-survival
plan: 02
subsystem: ui
tags: [MonoGame, SpriteBatch, SpriteFont, UI]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: MonoGame project structure and content pipeline
  - phase: 02-player-movement-camera
    provides: Game loop, SpriteBatch, UI patterns (Crosshair)
provides:
  - ScreenFade component for death sequence transitions
  - HealthBar component for HP visualization
  - GameOverScreen component for restart prompt
  - SpriteFont asset pipeline integration
affects: [05-enemies-ai, 06-procedural-generation, 08-polish-juice]

# Tech tracking
tech-stack:
  added: [FontDescriptionProcessor, Arial font asset]
  patterns: ["1x1 pixel texture for UI primitives", "Linear interpolation for predictable fade timing", "Centered text via MeasureString", "Color-coded health display (green/yellow/red)"]

key-files:
  created:
    - Berzerk/Source/UI/ScreenFade.cs
    - Berzerk/Source/UI/HealthBar.cs
    - Berzerk/Source/UI/GameOverScreen.cs
    - Berzerk/Content/Font.spritefont
  modified:
    - Berzerk/Content/Content.mgcb

key-decisions:
  - "ScreenFade uses linear interpolation (not exponential decay) for predictable 1.5s death timing"
  - "HealthBar displays at top-left (20,20) with 200x20px dimensions"
  - "HealthBar color transitions: Green (>50%) -> Yellow (>25%) -> Red (<=25%)"
  - "GameOverScreen uses Arial Bold 32pt for readability"
  - "SpriteFont covers ASCII range 32-126 for all needed characters"

patterns-established:
  - "Pattern: Full-screen fade using 1x1 pixel texture stretched to viewport"
  - "Pattern: UI color changes based on percentage thresholds for player feedback"
  - "Pattern: Text centering via font.MeasureString and viewport center calculation"
  - "Pattern: Content pipeline integration for font assets via .spritefont files"

# Metrics
duration: 2min
completed: 2026-02-03
---

# Phase 4 Plan 02: UI Visuals Summary

**ScreenFade with linear interpolation for death transitions, HealthBar with color-coded percentage display, and GameOverScreen with centered Arial Bold text**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-03T12:09:50Z
- **Completed:** 2026-02-03T12:11:45Z
- **Tasks:** 3
- **Files modified:** 5

## Accomplishments
- Created ScreenFade component with IsComplete property for state machine integration
- Created HealthBar component with color transitions (green/yellow/red) based on health percentage
- Created GameOverScreen with centered text and SpriteFont asset
- Integrated FontDescriptionProcessor into content pipeline for text rendering

## Task Commits

Each task was committed atomically:

1. **Task 1: Create ScreenFade overlay** - `86af587` (feat)
2. **Task 2: Create HealthBar UI** - `17e7c0e` (feat)
3. **Task 3: Create GameOverScreen and SpriteFont** - `2a6732f` (feat)

## Files Created/Modified
- `Berzerk/Source/UI/ScreenFade.cs` - Full-screen fade overlay with linear interpolation, IsComplete property, and Reset method
- `Berzerk/Source/UI/HealthBar.cs` - Top-left health bar with color-coded fill (green/yellow/red) based on health percentage
- `Berzerk/Source/UI/GameOverScreen.cs` - Centered "GAME OVER" text with restart prompt on black background
- `Berzerk/Content/Font.spritefont` - Arial Bold 32pt font definition for text rendering
- `Berzerk/Content/Content.mgcb` - Added FontDescriptionProcessor entry for font compilation

## Decisions Made

**ScreenFade timing:** Linear interpolation chosen over exponential decay for predictable 1.5-second fade duration matching CONTEXT.md specification (1-2 seconds). Enables precise state transition timing.

**HealthBar placement:** Top-left (20, 20) follows standard HUD conventions from arcade games. 200x20 pixel size provides visibility without screen obstruction.

**HealthBar colors:** Three-tier system (>50% green, >25% yellow, <=25% red) provides clear danger feedback. LimeGreen chosen over standard Green for better visibility.

**Font selection:** Arial Bold 32pt balances readability with arcade aesthetic. Bold weight ensures legibility on black background. ASCII range 32-126 covers all English text and punctuation.

**1x1 pixel pattern:** All components follow established Crosshair pattern - create single white pixel, tint via Color parameter. No external texture assets required.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all components compiled successfully on first build. Font asset processed through content pipeline without configuration issues.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Phase 4 Plan 03:** Death state machine integration
- ScreenFade provides IsComplete for state detection
- HealthBar accepts current/max health parameters (decoupled from HealthSystem)
- GameOverScreen ready for game-over state display
- All components follow SpriteBatch rendering pattern

**Component integration notes:**
- ScreenFade requires LoadContent(GraphicsDevice) and Update(deltaTime) calls
- HealthBar.Draw requires current and max health values
- GameOverScreen requires LoadContent(ContentManager, GraphicsDevice) for font loading
- All Draw methods require SpriteBatch Begin/End wrapper

**No blockers or concerns.**

---
*Phase: 04-player-health-survival*
*Completed: 2026-02-03*
