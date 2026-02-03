---
phase: 05-enemy-ai-combat
plan: 01
subsystem: ai
tags: [monogame, fsm, enemy-ai, state-machine, combat, health-system]

# Dependency graph
requires:
  - phase: 04-player-health-survival
    provides: HealthSystem event pattern
  - phase: 03-core-combat
    provides: AmmoPickup pooling pattern
  - phase: 02-player-movement-camera
    provides: Transform class, BoundingSphere collision

provides:
  - EnemyState FSM enum (Idle/Chase/Attack/Dying)
  - EnemyHealth event-driven health system
  - EnemyController with state-based AI and movement
  - HealthPickup collectable with green visual

affects: [05-02-enemy-manager, 05-03-combat-integration, 05-04-verification]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Enemy FSM with hysteresis to prevent state thrashing"
    - "Event-driven health system mirroring player HealthSystem"
    - "Direct movement toward player (no complex pathfinding)"

key-files:
  created:
    - Berzerk/Source/Enemies/EnemyState.cs
    - Berzerk/Source/Enemies/EnemyHealth.cs
    - Berzerk/Source/Enemies/EnemyController.cs
    - Berzerk/Source/Combat/HealthPickup.cs
  modified: []

key-decisions:
  - "Enemy speed 3.5 units/sec (70% of player speed) for chaseable but escapable combat"
  - "Attack range hysteresis (2.5 enter, 3.5 exit) prevents rapid state switching"
  - "Health 30 HP requires 2-3 laser hits (15 HP each) for satisfying combat"
  - "Health pickup green color (universal health indicator)"
  - "Direct movement pattern over complex pathfinding (appropriate for single-room arcade)"

patterns-established:
  - "Enemy FSM: switch statement on enum with OnStateEnter/OnStateExit hooks"
  - "Health events: OnDamageTaken fires immediately, OnDeath fires at zero health"
  - "Pooling support: Activate(position) and Deactivate() for manager integration"
  - "OnAttackExecuted event passes damage amount to manager for player damage"

# Metrics
duration: 3min
completed: 2026-02-03
---

# Phase 05 Plan 01: Core Enemy Infrastructure Summary

**Enemy FSM with Idle/Chase/Attack/Dying states, event-driven 30 HP health system, direct movement AI at 3.5 units/sec, and green health pickups with bobbing animation**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-03T16:40:11Z
- **Completed:** 2026-02-03T16:42:44Z
- **Tasks:** 3
- **Files created:** 4

## Accomplishments
- Enemy state machine with four behavioral states and smooth transitions
- Health system firing OnDamageTaken and OnDeath events for reactive integration
- Movement controller with detection (15 units), chase, and melee attack (2.5 units)
- Health pickup mirroring AmmoPickup pattern with green color and pooling support

## Task Commits

Each task was committed atomically:

1. **Task 1: Create enemy state machine and health system** - `1345a60` (feat)
2. **Task 2: Create enemy controller with FSM movement** - `2391801` (feat)
3. **Task 3: Create health pickup** - `4bfb918` (feat)

## Files Created/Modified
- `Berzerk/Source/Enemies/EnemyState.cs` - FSM states (Idle, Chase, Attack, Dying)
- `Berzerk/Source/Enemies/EnemyHealth.cs` - Event-driven health with 30 HP max
- `Berzerk/Source/Enemies/EnemyController.cs` - Individual enemy behavior with FSM, movement, and combat
- `Berzerk/Source/Combat/HealthPickup.cs` - Green health pickup with bobbing animation

## Decisions Made
- **Enemy movement speed:** 3.5 units/sec (70% of player's 5.0) - allows player to escape if needed
- **Attack range hysteresis:** Enter at 2.5 units, exit at 3.5 units - prevents rapid Chase/Attack flickering
- **Enemy health:** 30 HP requires 2-3 laser hits at 15 HP per shot - more satisfying than 1-hit kills
- **Health pickup color:** Green (universal health indicator from RESEARCH)
- **Movement pattern:** Direct movement toward player - appropriate for single-room arcade gameplay (RESEARCH confirmed no complex pathfinding needed)
- **Attack cooldown:** 1 second between attacks - balances threat without overwhelming player
- **Give-up range:** 25 units - enemies stop chasing if player escapes far enough

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None - all code compiled successfully, patterns followed established conventions (HealthSystem, AmmoPickup, PlayerController).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

Core enemy infrastructure complete and ready for manager integration in Plan 02:
- EnemyState enum defines all FSM states
- EnemyHealth fires events on damage and death
- EnemyController Update() accepts playerPos for AI updates
- HealthPickup ready for drop mechanics
- All classes support pooling (Activate/Deactivate)
- BoundingSphere collision detection ready for projectile hits
- OnAttackExecuted event ready for player damage integration

No blockers. Manager can spawn enemies, update AI, handle combat events, and manage pickups.

---
*Phase: 05-enemy-ai-combat*
*Completed: 2026-02-03*
