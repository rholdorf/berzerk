---
phase: 03-core-combat-system
plan: 03
subsystem: combat
tags: [monogame, test-targets, ammo-pickups, collision-detection, object-pooling, bounding-sphere]

# Dependency graph
requires:
  - phase: 03-core-combat-system
    plan: 01
    provides: Projectile class with BoundingSphere collision detection
affects: [03-04-integration]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Test target with visual hit feedback (color change on hit)
    - Object pooling for AmmoPickup (Queue-based reuse)
    - Manager pattern for target and pickup lifecycle
    - BoundingSphere collision detection for projectile-target hits
    - Auto-collect pickups with distance-based radius check

key-files:
  created:
    - Berzerk/Source/Combat/TestTarget.cs
    - Berzerk/Source/Combat/AmmoPickup.cs
    - Berzerk/Source/Combat/TargetManager.cs
  modified: []

key-decisions:
  - "Test targets at fixed positions (-5,0.5,-5), (5,0.5,-5), (0,0.5,-8) for Phase 3 validation"
  - "Target size 1f with collision radius 0.7f (slightly smaller than cube diagonal)"
  - "One hit to destroy targets (arcade feel per CONTEXT.md)"
  - "Hit flash duration 0.1s, color changes Green→Red→Transparent"
  - "AmmoPickup amount 40 (within 30-50 range from CONTEXT.md)"
  - "Auto-collect radius 2f (generous for arcade feel)"
  - "Bobbing animation using sin(time * speed) * height for visual appeal"
  - "Pickup pool size 10 (handles multiple target destructions)"
  - "Console.WriteLine for collection feedback during testing"

patterns-established:
  - "TestTarget.OnHit() returns bool (true=alive, false=destroyed) for caller convenience"
  - "AmmoPickup.Activate() pattern for pooling reuse"
  - "TargetManager owns all targets and pickups, provides read-only access"
  - "Projectile-target collision uses sphere.Intersects(sphere)"
  - "Pickup collection notifies AmmoSystem.AddAmmo() directly"

# Metrics
duration: 2min
completed: 2026-02-02
---

# Phase 03 Plan 03: Test Targets and Ammo Pickups Summary

**Destructible colored cube targets with hit feedback that spawn floating ammo pickups when destroyed, using BoundingSphere collision**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-02T23:06:42Z
- **Completed:** 2026-02-02T23:08:50Z
- **Tasks:** 3
- **Files created:** 3

## Accomplishments

- TestTarget class with BoundingSphere/BoundingBox collision, hit flash (0.1s), color feedback (Green/Red/Transparent), and one-hit destruction
- AmmoPickup class with auto-collect radius (2f), floating bobbing animation, and 40 ammo amount
- TargetManager creates 3 test targets at fixed positions, handles projectile-target collision using sphere.Intersects(), spawns pickups on target destruction, checks pickup collection, and pools pickups (size 10)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create TestTarget with hit feedback and destruction** - `eed4228` (feat)
2. **Task 2: Create AmmoPickup with auto-collect and floating animation** - `e9b4ac9` (feat)
3. **Task 3: Create TargetManager for targets, pickups, and collisions** - `0ae8983` (feat)

## Files Created/Modified

- `Berzerk/Source/Combat/TestTarget.cs` - Destructible 1-unit cube with 0.7f collision radius, hit flash, color feedback
- `Berzerk/Source/Combat/AmmoPickup.cs` - Floating pickup with bobbing animation, 2f auto-collect radius, 40 ammo
- `Berzerk/Source/Combat/TargetManager.cs` - Manages 3 test targets, spawns/pools pickups, handles projectile collision and collection

## Decisions Made

- **Target hit points 1:** Arcade feel per CONTEXT.md - targets destroyed in one shot for responsive combat
- **Collision radius 0.7f:** Slightly smaller than cube diagonal (√3/2 ≈ 0.866) for balanced hit detection
- **Hit flash 0.1s:** Quick visual feedback without lingering effect
- **Ammo amount 40:** Middle of 30-50 range from CONTEXT.md
- **Collect radius 2f:** Generous auto-collect per research recommendation for arcade forgiveness
- **Fixed target positions:** Three targets at symmetric positions for Phase 3 validation testing
- **Console logging:** Collection feedback for debugging during integration phase

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all classes compiled successfully on first build. MonoGame BoundingSphere collision APIs worked as expected.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Plan 04 (Integration):**
- TestTarget provides GetBoundingSphere() for collision detection
- TestTarget provides GetBoundingBox() for rendering cubes via DebugRenderer
- TestTarget provides GetColor() for visual state (green/red/transparent)
- AmmoPickup provides GetDisplayPosition() for rendering at bobbing height
- AmmoPickup provides GetColor() returning Yellow (distinct from green targets)
- TargetManager.CheckProjectileCollisions() ready to integrate with ProjectileManager
- TargetManager.CheckPickupCollection() ready to integrate with PlayerController and AmmoSystem
- RespawnTargets() allows testing target respawning without restarting game

**Foundation established:**
- Test targets at fixed positions validate projectile-target collision
- Ammo pickups validate collection radius and AmmoSystem integration
- Object pooling prevents GC spikes from frequent pickup spawning
- All collision detection uses BoundingSphere for consistency with Projectile class

**No blockers** - all must-have artifacts created and verified.

---
*Phase: 03-core-combat-system*
*Completed: 2026-02-02*
