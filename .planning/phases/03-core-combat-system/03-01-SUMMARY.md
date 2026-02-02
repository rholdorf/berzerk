---
phase: 03-core-combat-system
plan: 01
subsystem: combat
tags: [monogame, projectile, object-pooling, ammunition, fire-rate, bounding-sphere]

# Dependency graph
requires:
  - phase: 02-player-movement-camera
    provides: Transform class for position/rotation tracking
provides:
  - Projectile pooling system preventing GC spikes during rapid fire
  - Magazine + reserve ammunition system with auto-reload
  - Fire rate limiting with frame-rate independent timing
  - Foundation for projectile rendering and collision (Plan 02)
affects: [03-02-projectile-visuals, 03-03-test-targets, 03-04-integration]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Object pooling with Queue<T> for projectile reuse
    - Manager pattern for centralized projectile lifecycle
    - Two-tier ammo system (magazine + reserve)
    - Frame-rate independent cooldown timing

key-files:
  created:
    - Berzerk/Source/Combat/Projectile.cs
    - Berzerk/Source/Combat/ProjectileManager.cs
    - Berzerk/Source/Combat/AmmoSystem.cs
    - Berzerk/Source/Combat/WeaponSystem.cs
  modified: []

key-decisions:
  - "Pre-allocate 50 projectiles in pool to prevent GC spikes"
  - "Distance-based lifetime (75 units) instead of time-based"
  - "Fire rate 6.5 shots/sec (within 5-8 range from CONTEXT)"
  - "Projectile speed 50 units/sec (within 40-60 range from CONTEXT)"
  - "Magazine size 25 rounds, reserve 125 rounds (within 100-150 range)"
  - "Auto-reload from reserve when magazine empties"

patterns-established:
  - "Projectile activate/deactivate pattern for pool reuse"
  - "Backward iteration for safe removal during update"
  - "BoundingSphere collision detection support"
  - "Frame-rate independent timing using GameTime.ElapsedGameTime.TotalSeconds"

# Metrics
duration: 2min
completed: 2026-02-02
---

# Phase 03 Plan 01: Core Combat Infrastructure Summary

**Object-pooled projectile system with magazine+reserve ammo and 6.5 shots/sec fire rate using MonoGame primitives**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-02T22:59:41Z
- **Completed:** 2026-02-02T23:01:41Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Projectile class with Transform integration, distance-based lifetime tracking, and BoundingSphere collision support
- ProjectileManager with pre-allocated 50-projectile pool to prevent GC spikes during rapid fire
- AmmoSystem implementing magazine (25) + reserve (125) with auto-reload on magazine empty
- WeaponSystem with frame-rate independent fire rate limiting (6.5 shots/sec)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Projectile and ProjectileManager with object pooling** - `4b663ab` (feat)
2. **Task 2: Create AmmoSystem and WeaponSystem with fire rate control** - `bb04942` (feat)

## Files Created/Modified
- `Berzerk/Source/Combat/Projectile.cs` - Poolable projectile with position, velocity, distance tracking and collision sphere
- `Berzerk/Source/Combat/ProjectileManager.cs` - Manages active projectiles with Queue-based pooling (50 pre-allocated)
- `Berzerk/Source/Combat/AmmoSystem.cs` - Two-tier ammo (magazine/reserve) with auto-reload and pickup support
- `Berzerk/Source/Combat/WeaponSystem.cs` - Fire rate limiting (6.5/sec) with delta-time cooldown and projectile spawning

## Decisions Made
- **Fire rate 6.5 shots/sec:** Middle of 5-8 range from CONTEXT.md for balanced automatic fire
- **Projectile speed 50 units/sec:** Middle of 40-60 range for fast but visible arcade feel
- **Distance-based lifetime 75 units:** Middle of 50-100 range, deactivates after traveling set distance (not time-based)
- **Magazine 25, reserve 125:** Within CONTEXT.md ranges (100-150 total), balanced for resource management
- **Pool size 50:** Accommodates ~7.7 seconds of continuous fire at max rate, handles burst scenarios
- **Normalized direction in manager:** ProjectileManager normalizes direction parameter to prevent invalid velocity

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all classes compiled successfully on first build. MonoGame BoundingSphere and GameTime APIs worked as expected from research.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

**Ready for Plan 02 (Projectile visuals and wall collision):**
- Projectile class has GetBoundingSphere() for collision detection
- ProjectileManager provides GetActiveProjectiles() for rendering iteration
- OnHitWall() and OnHitTarget() hooks ready for impact effect spawning
- Transform integration enables world matrix calculation for rendering

**Foundation established:**
- Combat namespace created with four core classes
- Object pooling pattern prevents GC spikes (validated in research)
- Frame-rate independent timing pattern ready for integration
- AmmoSystem ready for HUD display integration (CurrentMagazine, ReserveAmmo properties)

**No blockers** - all must-have artifacts created and verified.

---
*Phase: 03-core-combat-system*
*Completed: 2026-02-02*
