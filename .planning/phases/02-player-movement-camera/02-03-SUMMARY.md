---
phase: 02-player-movement-camera
plan: 03
subsystem: camera
tags: [monogame, third-person-camera, collision-detection, ray-casting, interpolation]

# Dependency graph
requires:
  - phase: 02-player-movement-camera
    plan: 01
    provides: InputManager with ScrollWheelDelta, MouseDelta, IsRightMouseHeld; Transform component
provides:
  - ThirdPersonCamera with smooth following, scroll wheel zoom, right-click orbit
  - Frame-rate independent exponential decay smoothing
  - Camera collision detection using Ray.Intersects
  - Distance-based angle transitions (eye-level to high angle)
  - CreateTestWalls() helper for integration testing
affects: [02-04-integration, camera-integration, player-camera-system]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Exponential decay smoothing for frame-rate independent camera motion"
    - "Spherical coordinates for camera orbit (yaw/pitch to Cartesian conversion)"
    - "Ray casting collision detection with smooth zoom-in behavior"
    - "Distance-based angle transitions for dynamic camera perspective"

key-files:
  created:
    - Berzerk/Source/Graphics/ThirdPersonCamera.cs
  modified: []

key-decisions:
  - "Exponential decay smoothing (1 - Pow(damping, deltaTime)) for frame-rate independence"
  - "Scroll wheel: positive delta = zoom in (decrease distance), negative = zoom out"
  - "Right-click drag for camera orbit (common third-person pattern)"
  - "Auto pitch transition based on distance: eye-level when close, high angle when far"
  - "Collision detection with Ray.Intersects against BoundingBox list"
  - "Smooth zoom-in on collision, smooth zoom-out when collision clears"

patterns-established:
  - "Camera pattern: Separate ViewMatrix and ProjectionMatrix properties for rendering"
  - "Smoothing pattern: Use exponential decay formula for all frame-rate independent interpolation"
  - "Collision pattern: Ray casting from player to camera with closest hit detection"
  - "Test geometry pattern: Static CreateTestWalls() helper for integration validation"

# Metrics
duration: 2min
completed: 2026-02-02
---

# Phase 2 Plan 03: Third-Person Camera Summary

**ThirdPersonCamera with exponential decay smoothing, scroll wheel zoom, right-click orbit, Ray.Intersects collision detection, and distance-based angle transitions from eye-level to high angle**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-02T02:34:36Z
- **Completed:** 2026-02-02T02:36:25Z
- **Tasks:** 2 (combined into single implementation)
- **Files modified:** 1

## Accomplishments
- Complete third-person camera system with 5 core behaviors: follow, zoom, orbit, collision, angle transition
- Frame-rate independent smoothing using exponential decay formula (1 - MathF.Pow(damping, deltaTime))
- Collision detection using Ray.Intersects with smooth zoom-in behavior when hitting walls
- Distance-based automatic pitch: eye-level at close range, high angle at far range
- CreateTestWalls() helper provides test geometry for integration validation

## Task Commits

Both tasks implemented in single file:

1. **Tasks 1-2: Create ThirdPersonCamera with all features** - `df76a4e` (feat)

## Files Created/Modified
- `Berzerk/Source/Graphics/ThirdPersonCamera.cs` - Complete third-person camera system (216 lines):
  - Smooth following via exponential decay lerp
  - Scroll wheel zoom (MinDistance=2f, MaxDistance=15f, ZoomSpeed=0.01f)
  - Right-click orbit with yaw/pitch (PitchMin=-45°, PitchMax=60°)
  - Distance-based angle transitions (eye-level MinPitch=0° when close, MaxPitch=30° when far)
  - Collision detection with Ray.Intersects against List<BoundingBox>
  - CreateTestWalls() static helper for test arena (left/right/back/front walls + center pillar)

## Decisions Made
- **Exponential decay smoothing:** Used formula `smoothFactor = 1f - MathF.Pow(dampingCoeff, deltaTime)` for frame-rate independent motion (from RESEARCH.md Pattern 2)
- **Scroll wheel direction:** Positive delta (scroll up) = zoom in = decrease distance. Negative delta (scroll down) = zoom out = increase distance. Intuitive user expectation.
- **Right-click orbit:** Camera orbits when IsRightMouseHeld() is true. When released, camera auto-transitions pitch based on distance. Matches common third-person game pattern.
- **Spherical coordinates:** Camera offset calculated via yaw/pitch angles converted to Cartesian (horizontalDist, verticalDist). Enables natural orbit around player.
- **Collision offset:** Camera stops 0.3 units before wall surface (CollisionOffset constant) to prevent exact clipping. Smooth lerp for zoom-in and zoom-out.
- **Minimum distance enforcement:** Camera never closer than MinDistance (2f) to prevent clipping inside player model (RESEARCH.md Pitfall 5).
- **Auto pitch blending:** When not manually orbiting, camera pitch lerps toward distance-based target (close = eye-level, far = high angle). Provides cinematic perspective shift.

## Deviations from Plan

None - plan executed exactly as written. Both Task 1 and Task 2 combined into single comprehensive implementation.

## Issues Encountered

None - implementation completed cleanly with successful build. All verification criteria met:
- Build succeeds with no errors (4 unrelated nullable warnings from Phase 1 code)
- ThirdPersonCamera.cs contains Update, HandleZoom, HandleOrbit, CheckCollision methods
- Ray.Intersects pattern implemented for collision detection
- InputManager integration via ScrollWheelDelta, MouseDelta, IsRightMouseHeld
- 216 lines (exceeds min_lines: 120 requirement)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Ready for integration (Plan 02-04):
- ThirdPersonCamera complete and tested (build passes)
- SetCollisionGeometry() method ready to receive scene bounding boxes
- CreateTestWalls() provides test collision geometry for integration validation
- ViewMatrix and ProjectionMatrix properties ready for rendering pipeline
- Frame-rate independent smoothing ensures consistent behavior at any FPS

Camera is standalone component - not yet integrated into BerzerkGame. Integration plan will:
- Instantiate ThirdPersonCamera with InputManager and player Transform
- Call Initialize() with GraphicsDevice for projection matrix setup
- Call Update() each frame after InputManager.Update()
- Use ViewMatrix/ProjectionMatrix for rendering
- Call SetCollisionGeometry() with test walls from CreateTestWalls()

No blockers or concerns.

---
*Phase: 02-player-movement-camera*
*Completed: 2026-02-02*
