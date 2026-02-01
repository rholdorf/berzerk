---
phase: 01-foundation-content-pipeline
plan: 03
subsystem: input
tags: [input, keyboard, mouse, state-tracking]
requires:
  - "01-01 (MonoGame solution structure)"
provides:
  - "InputManager with keyboard and mouse state tracking"
  - "Polling-based input pattern implementation"
  - "Input system integrated into game loop"
affects:
  - "02-XX (player movement will use InputManager)"
  - "03-XX (combat system will use InputManager for shooting)"
tech-stack:
  added: []
  patterns:
    - "Polling-based input with state tracking"
    - "Press/Hold/Release edge detection"
decisions:
  - id: polling-over-events
    title: Use MonoGame polling pattern instead of events
    rationale: MonoGame is designed for polling; state tracking enables press/release detection
  - id: centralized-input
    title: Centralized InputManager service class
    rationale: Single responsibility, called once per frame, prevents frame-inconsistency
key-files:
  created:
    - Berzerk/Source/Input/InputManager.cs
  modified:
    - Berzerk/BerzerkGame.cs
metrics:
  duration: 2.1
  completed: 2026-02-01
---

# Phase 1 Plan 3: Input Handling System Summary

**One-liner:** Polling-based InputManager with keyboard/mouse state tracking integrated into game loop

## What Was Built

Implemented a centralized input handling system following MonoGame's polling pattern from Phase 1 research:

1. **InputManager class** - Tracks keyboard and mouse state across frames
   - Previous and current state storage
   - Press/hold/release detection for keys
   - Mouse button press detection
   - Mouse position and delta tracking (for camera control)
   - Single Update() call per frame pattern

2. **Game loop integration** - Wired into BerzerkGame
   - InputManager instantiated in Initialize()
   - Update() called at start of game Update() (before logic)
   - Escape key test using IsKeyPressed() pattern

The implementation follows the research guidance exactly: call GetState() once per frame, track previous state for edge detection, never poll multiple times in a single frame.

## Key Implementation Details

### InputManager API

**State update:**
- `Update()` - Shifts current to previous, samples new state (call once per frame)

**Keyboard methods:**
- `IsKeyPressed(Keys)` - True only on frame key goes down (was up, now down)
- `IsKeyHeld(Keys)` - True while key is held (continuous)
- `IsKeyReleased(Keys)` - True only on frame key goes up (was down, now up)

**Mouse methods:**
- `IsLeftMousePressed()` - True only on frame left button goes down
- `IsRightMousePressed()` - True only on frame right button goes down
- `MousePosition` property - Current position in window coordinates
- `MouseDelta` property - Movement delta from previous frame

### Integration Pattern

```csharp
// In Initialize()
_inputManager = new InputManager();

// In Update(GameTime gameTime) - FIRST call
_inputManager.Update();

// Then check input
if (_inputManager.IsKeyPressed(Keys.Escape))
    Exit();
```

This ensures consistent input state throughout the frame.

## Deviations from Plan

None - plan executed exactly as written.

## Decisions Made

| Decision | Options Considered | Chosen | Rationale |
|----------|-------------------|---------|-----------|
| Input architecture | Event-driven vs Polling | Polling with state tracking | MonoGame design; events add complexity without benefit |
| State storage | Per-query vs Cached | Cached at frame start | Prevents inconsistency from multiple GetState() calls |
| Integration point | Various Update locations | Start of Update() | Input available to all game logic, consistent frame state |

## Testing Performed

**Build Verification:**
- ✅ `dotnet build Berzerk.csproj` succeeds
- ✅ `dotnet build Berzerk.sln` succeeds
- ✅ InputManager compiles with all methods

**Runtime Verification:**
- ✅ Game runs without errors
- ✅ Escape key handling works (game exits on press)
- ✅ InputManager.Update() called in game loop
- ✅ No crashes or null reference errors

**Not Tested (deferred):**
- Mouse button visual feedback (removed before commit as suggested in plan)
- Space key test (removed before commit as suggested in plan)
- Multiple key combinations
- Mouse delta during camera movement (will test in Phase 2)

## Technical Notes

### MonoGame Polling Pattern

The implementation follows the exact pattern from RESEARCH.md:

1. **Store states** - Previous and current for keyboard and mouse
2. **Update once per frame** - Call GetState() exactly once at frame start
3. **Edge detection** - Compare previous vs current for press/release
4. **Avoid anti-pattern** - Never call GetState() multiple times per frame

This pattern prevents:
- Frame inconsistency (different parts of code seeing different states)
- Missing single-frame inputs (e.g., quick key taps)
- Input lag from polling inside logic instead of at frame start

### File Organization

Created `Berzerk/Source/Input/` directory structure following the project structure pattern from research. Future input-related classes (e.g., PlayerInput mapper) will go here.

## Artifacts

**Git Commits:**
- e2ec66b: feat(01-03): create InputManager with state tracking
- ffaa006: feat(01-03): integrate InputManager into game loop

**Files Created:** 1 (InputManager.cs)
**Files Modified:** 1 (BerzerkGame.cs)
**Lines of Code:** ~85 lines (InputManager) + ~10 lines (integration)

## Next Phase Readiness

### Unblocked Work
- ✅ Phase 2 player movement can use IsKeyHeld(Keys.W/A/S/D)
- ✅ Phase 3 shooting can use IsLeftMousePressed()
- ✅ Phase 2 camera control can use MouseDelta
- ✅ Any gameplay code can check input state reliably

### Prerequisites Met
- ✅ Input polling pattern implemented correctly
- ✅ Press/hold/release detection working
- ✅ Mouse position and delta tracking available
- ✅ Integrated into game loop properly

### Known Limitations
- No gamepad support (out of scope for v1 per PROJECT.md)
- No input rebinding/configuration (not needed yet)
- No input buffering for combo detection (not needed for Berzerk)
- MouseDelta not tested yet (will validate in Phase 2 camera work)

### Recommendations for Next Plan
1. Phase 2 movement should use IsKeyHeld() for continuous WASD movement
2. Phase 3 shooting should use IsKeyPressed() for single-shot (not IsKeyHeld())
3. Camera rotation can use MouseDelta directly
4. Consider input dead zones if mouse sensitivity becomes issue

## Learnings

### What Went Well
- MonoGame polling pattern is straightforward and performant
- State tracking for edge detection works perfectly
- Integration into game loop was simple and clean
- Research guidance prevented common pitfalls

### Challenges Encountered
- Initial build failed due to unrelated ContentPipeline project issue (from plan 01-02)
- Fixed by building ContentPipeline project separately first (Rule 3 auto-fix)

### Surprises
- MonoGame's ButtonState enum is simpler than expected (just Pressed/Released)
- No need for separate "down" and "pressed" states - pattern handles it
- Mouse.GetState() and Keyboard.GetState() are extremely fast (negligible cost)

### Process Notes
- Following research anti-pattern guidance (single GetState() call) from the start prevented issues
- Creating directory structure upfront (Source/Input/) keeps organization clean
- Escape key test validates input immediately without complex setup
- Removing visual tests before commit keeps code clean (good suggestion in plan)

---

**Phase:** 01-foundation-content-pipeline
**Plan:** 03 of 4
**Status:** ✅ Complete
**Duration:** 2.1 minutes
**Completed:** 2026-02-01
