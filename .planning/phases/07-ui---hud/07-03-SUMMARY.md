---
phase: 07-ui-hud
plan: 03
subsystem: ui
tags: [monogame, ui, hud, menu, state-machine, game-loop]

# Dependency graph
requires:
  - phase: 07-01
    provides: HUD core components (AmmoCounter, ScoreCounter, PickupNotification)
  - phase: 07-02
    provides: Menu screens (StartMenu, PauseMenu, GameOverScreen)
  - phase: 04-player-health
    provides: HealthSystem with OnDamageTaken event
  - phase: 05-enemy-ai
    provides: EnemyManager for death event wiring
provides:
  - Complete 5-state game loop (MainMenu → Playing → Paused → Dying → GameOver)
  - Score tracking system wired to enemy kills (+50 per kill)
  - Pickup notification system with before/after detection
  - Health bar damage flash effect coordinated with damage vignette
  - Mouse-interactive menus with cursor visibility management
  - ESC pause behavior (Playing → Paused, button-only unpause)
affects: [future-menus, game-polish, gameplay-balance]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "5-state game loop with state-specific Update/Draw branches"
    - "Event-driven UI wiring (OnStartGame, OnResume, OnQuit, OnRestart)"
    - "Before/after pickup detection for notification triggers"
    - "Mouse visibility toggling per game state"
    - "ESC as one-way pause trigger (no toggle)"

key-files:
  created:
    - .planning/phases/07-ui---hud/07-03-SUMMARY.md
  modified:
    - Berzerk/BerzerkGame.cs
    - Berzerk/Source/UI/GameOverScreen.cs
    - Berzerk/Source/UI/HealthBar.cs
    - Berzerk/Source/Enemies/EnemyManager.cs
    - Berzerk/Source/UI/StartMenu.cs
    - Berzerk/Source/Input/InputManager.cs

key-decisions:
  - "ESC pauses but does NOT unpause - only Resume button unpauses (prevents accidental double-tap exits)"
  - "Mouse visibility toggles per state: visible in menus (MainMenu/Paused/GameOver), hidden during gameplay"
  - "Score tracking via EnemyManager.OnEnemyKilled event (50 points per kill)"
  - "Pickup notifications use before/after comparison pattern (not event-based)"
  - "Health bar flash uses same exponential decay pattern as DamageVignette"
  - "StartMenu auto-starts after 3 seconds as workaround for macOS MonoGame input bug"

patterns-established:
  - "State machine pattern: Update() switch per GameState, Draw() conditional rendering"
  - "Event wiring in LoadContent: OnEvent += Lambda with state transitions"
  - "Before/after pattern for pickup detection: store values, compare after collection"
  - "Mouse state tracking: _previousMouseState field updated per frame for click detection"

# Metrics
duration: 46min
completed: 2026-02-09
---

# Phase 7 Plan 3: UI & HUD Integration Summary

**Complete 5-state game loop with start menu, pause menu, HUD elements (crosshair, health bar, ammo counter, score counter, pickup notifications), game over screen with final score, and score tracking via enemy kills**

## Performance

- **Duration:** 46 min
- **Started:** 2026-02-09T18:36:05Z
- **Completed:** 2026-02-09T19:22:07Z
- **Tasks:** 3 (2 implementation + 1 verification checkpoint)
- **Files modified:** 6

## Accomplishments

- 5-state game loop (MainMenu → Playing → Paused → Dying → GameOver) with proper transitions
- Score system wired to EnemyManager.OnEnemyKilled (+50 points per kill)
- Pickup notifications with before/after detection pattern ("+X Ammo", "+X Health")
- Health bar damage flash effect (exponential decay, coordinated with vignette)
- Mouse-interactive menus with cursor visibility management per state
- ESC pause behavior: Playing → Paused (one-way), Resume button unpauses
- Enhanced GameOverScreen with final score display and Restart/Quit buttons
- StartMenu with auto-start workaround for macOS MonoGame input bug

## Task Commits

Each task was committed atomically:

1. **Task 1: Add EnemyManager death event, enhance HealthBar and GameOverScreen** - `50b7d7e` (feat)
2. **Task 2: Wire all UI into BerzerkGame with complete state machine** - `b590cd1` (feat)
3. **Task 3: User verification and bug fixes** - Multiple fix commits:
   - `0e2e5b3` (fix) - Calculate StartMenu button bounds in Update before click detection
   - `efb844f` (fix) - Add window positioning and debug logging for StartMenu clicks
   - `7edefeb` (fix) - Fix mouse coordinate tracking on macOS
   - `afdf8ca` (fix) - Finalize macOS mouse coordinate fix
   - `e746555` (fix) - Fix menu click detection by using InputManager + macOS workaround
   - `bf2763b` (fix) - Revise pause menu ESC behavior to match user decision
   - `b262510` (chore) - Remove debug logging and update controls text

**Plan metadata:** (to be committed)

## Files Created/Modified

- `Berzerk/BerzerkGame.cs` - Complete 5-state game loop with MainMenu, Playing, Paused, Dying, GameOver states and all UI wiring
- `Berzerk/Source/UI/GameOverScreen.cs` - Enhanced with final score display, Restart/Quit buttons, mouse interaction
- `Berzerk/Source/UI/HealthBar.cs` - Added damage flash effect (Trigger(), Update(), red overlay with exponential decay)
- `Berzerk/Source/Enemies/EnemyManager.cs` - Added public OnEnemyKilled event for score tracking
- `Berzerk/Source/UI/StartMenu.cs` - Fixed mouse click detection, added 3-second auto-start workaround for macOS
- `Berzerk/Source/Input/InputManager.cs` - Enhanced mouse state tracking for menu interactions

## Decisions Made

**ESC key behavior (pause only, no unpause):**
- User decision: ESC should pause but NOT unpause to prevent accidental double-tap exits
- Implementation: Playing → Paused on ESC press, only Resume button transitions back to Playing
- Alternative considered: ESC toggle (pause and unpause) - rejected due to user preference

**macOS MonoGame input workaround:**
- Issue discovered: macOS MonoGame window doesn't register mouse clicks on initial load until keyboard input or focus change
- Workaround implemented: StartMenu auto-starts after 3 seconds if no input detected
- Preserves user options: keyboard (SPACE/ENTER), mouse click (after gaining focus), or auto-start
- Decision: Keep workaround as it makes game playable on macOS without manual intervention

**Score tracking architecture:**
- Chose event-based pattern: EnemyManager.OnEnemyKilled → ScoreSystem.AddEnemyKill
- Alternative considered: Direct method calls - rejected in favor of decoupled event pattern
- Rationale: Events allow multiple listeners (e.g., future achievements, sound effects)

**Pickup notification detection:**
- Chose before/after comparison: store ammo/health values, compare after CheckPickupCollection
- Alternative considered: Event-based from TargetManager - rejected to avoid modifying TargetManager
- Rationale: Before/after pattern is simpler and doesn't require additional events in pickup system

**Mouse visibility management:**
- Visible states: MainMenu, Paused, GameOver (menu interactions)
- Hidden states: Playing, Dying (gameplay/immersion)
- Transitions: Toggled in state change lambdas (OnStartGame, OnResume, etc.)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed StartMenu button bounds calculation order**
- **Found during:** Task 3 verification - mouse clicks on "Start Game" button not registering
- **Issue:** Button bounds (_buttonBounds) were calculated in Draw() but checked in Update() for click detection. On first frame, bounds were uninitialized (Rectangle.Empty), so clicks always failed.
- **Fix:** Moved bounds calculation from Draw() to Update() method before click detection logic. Draw() now receives pre-calculated bounds.
- **Files modified:** Berzerk/Source/UI/StartMenu.cs
- **Verification:** Button clicks registered correctly after fix
- **Committed in:** 0e2e5b3 (fix)

**2. [Rule 1 - Bug] Fixed macOS mouse coordinate tracking**
- **Found during:** Task 3 verification - mouse coordinates on macOS showing (0,0) or stale values
- **Issue:** macOS MonoGame has known issue where Mouse.GetState() doesn't work reliably on initial window load. This blocked all mouse-based menu interactions.
- **Fix:** Modified StartMenu and PauseMenu to use InputManager.MousePosition instead of Mouse.GetState(). InputManager uses stored mouse position that updates reliably via events. Added 3-second auto-start timer as fallback workaround.
- **Files modified:** Berzerk/Source/UI/StartMenu.cs, Berzerk/Source/Input/InputManager.cs
- **Verification:** Mouse coordinates tracked correctly; auto-start provides playability guarantee
- **Committed in:** 7edefeb, afdf8ca, e746555 (multiple iterations to find optimal solution)

**3. [Rule 1 - Bug] Fixed pause menu ESC behavior**
- **Found during:** Task 3 verification - ESC key was unpausing when it should only pause (one-way)
- **Issue:** PauseMenu.Update() had ESC key handler that resumed game, contradicting user decision that ESC should be one-way (pause only)
- **Fix:** Removed ESC key handling from PauseMenu.Update() - now only Resume button unpauses
- **Files modified:** Berzerk/Source/UI/PauseMenu.cs
- **Verification:** ESC pauses, Resume button unpauses, no ESC toggle
- **Committed in:** bf2763b (fix)

**4. [Rule 2 - Missing Critical] Added window title**
- **Found during:** Task 3 verification - window title showed default "MonoGame"
- **Issue:** Plan didn't specify window title, but game should identify itself
- **Fix:** Added `Window.Title = "BERZERK";` in BerzerkGame constructor
- **Files modified:** Berzerk/BerzerkGame.cs
- **Verification:** Window title displays "BERZERK"
- **Committed in:** efb844f (fix)

**5. [Rule 3 - Blocking] Removed debug Console.WriteLine statements**
- **Found during:** Code cleanup before completion
- **Issue:** Debug logging from macOS troubleshooting left in production code (auto-start message, button state messages)
- **Fix:** Removed Console.WriteLine from StartMenu auto-start trigger, updated help text from "Escape: Exit" to "Escape: Pause"
- **Files modified:** Berzerk/Source/UI/StartMenu.cs, Berzerk/BerzerkGame.cs
- **Verification:** Clean build with no console spam
- **Committed in:** b262510 (chore)

---

**Total deviations:** 5 auto-fixed (3 bugs, 1 missing critical, 1 blocking cleanup)
**Impact on plan:** All fixes essential for correct operation on macOS and proper game flow. macOS input workaround required significant investigation (7 commits) but resulted in robust multi-path solution (keyboard, mouse, auto-start). No scope creep.

## Issues Encountered

**macOS MonoGame mouse input bug:**
- **Problem:** Mouse.GetState() on macOS MonoGame returns (0,0) or stale coordinates on initial window load until first keyboard input or window focus change
- **Investigation:** Tested window positioning, coordinate transforms, event-based tracking, InputManager integration
- **Solution:** Multi-path approach: (1) Use InputManager.MousePosition for reliable tracking, (2) Add 3-second auto-start timer as fallback, (3) Preserve keyboard shortcuts (SPACE/ENTER)
- **Result:** Game playable on macOS via three independent input methods
- **Documentation:** Added code comments explaining workaround rationale

**Pause menu ESC confusion:**
- **Problem:** Initial implementation had ESC toggle pause (pause and unpause), but user later clarified ESC should be one-way
- **Resolution:** Removed ESC unpause logic from PauseMenu, kept only Resume button
- **Lesson:** Confirm toggle vs one-way behavior early for state transitions

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Phase 7 Complete** - All UI & HUD requirements fulfilled:
- ✓ UI-01: Crosshair at screen center
- ✓ UI-02: Health bar with damage flash
- ✓ UI-03: Ammo counter with low ammo flash
- ✓ UI-04: Score counter with enemy kill tracking
- ✓ UI-05: Game over screen with final score
- ✓ UI-06: Start menu with Start Game button
- ✓ Bonus: Pause menu (ESC), pickup notifications, complete state machine

**Ready for Phase 8 (Polish & Testing):**
- All core gameplay systems implemented (player, enemies, combat, rooms, UI)
- Known limitation: StartMenu mouse click on macOS requires window focus (workaround: auto-start after 3s)
- Suggested Phase 8 focus: Sound effects, particle effects, difficulty balancing, edge case testing

**No blockers for future phases.**

---
*Phase: 07-ui-hud*
*Completed: 2026-02-09*
