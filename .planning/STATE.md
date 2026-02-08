# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-01-31)

**Core value:** Combate arcade intenso em salas geradas proceduralmente - a essência do Berzerk original transportada para 3D moderno com modelos animados.
**Current focus:** Phase 6 - Room System & Progression

## Current Position

Phase: 6 of 8 (Room System & Progression)
Plan: 2 of 5 complete in Phase 6
Status: In progress - room data structures and manager complete, BerzerkGame integration next
Last activity: 2026-02-08 — Completed 06-02-PLAN.md (RoomManager lifecycle management)

Progress: [███████████░] 79% (5 phases complete + 2 plans, 22 of ~28 total plans)

## Performance Metrics

**Velocity:**
- Total plans completed: 22
- Average duration: 13.9 min
- Total execution time: 5.55 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01 - Foundation & Content Pipeline | 4 | 161.1 min | 40.3 min |
| 02 - Player Movement & Camera | 4 | 137.0 min | 34.3 min |
| 03 - Core Combat System | 4 | 15.0 min | 3.8 min |
| 04 - Player Health & Survival | 3 | 7.0 min | 2.3 min |
| 05 - Enemy AI & Combat | 5 | 16.0 min | 3.2 min |
| 06 - Room System & Progression | 2 | 3.0 min | 1.5 min |
| **Phase 6 breakdown** | | | |
| 06-01 (Room Infrastructure) | 1 | 2.0 min | 2.0 min |
| 06-02 (RoomManager Lifecycle) | 1 | 1.0 min | 1.0 min |

**Recent Trend:**
- Last 5 plans: 05-03 (4 min), 05-04 (4 min), 05-05 (2 min), 06-01 (2 min), 06-02 (1 min)
- Trend: Pure data structure plans extremely fast (1-2 min), code generation plans fast (3-4 min)

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- Phase 1: MonoGame 3.8.4.1 + .NET 8 chosen as foundation (cross-platform support)
- Phase 1: Mixamo assets for 3D models/animations (quality free assets)
- Phase 1: FBX 2013 format required (MonoGame content pipeline compatibility)
- 01-01: Target .NET 8 instead of .NET 9/10 (MonoGame minimum, stable baseline)
- 01-01: Enable MonoGameContentBuilderExitOnError (fail-fast on FBX issues)
- 01-01: Content pipeline targets AnyCPU (MonoGame requirement)
- 01-02: Store animation data in Model.Tag (standard MonoGame pattern)
- 01-02: Verbose logging for FBX processor (maximize import visibility)
- 01-03: Polling-based input over events (MonoGame design pattern)
- 01-03: Centralized InputManager service (single responsibility, frame consistency)
- 01-04: BasicEffect rendering for Phase 1 validation (defer skinned animation)
- 01-04: Separate FBX files per animation (Mixamo export limitation)
- 01-04: Camera at (0,1,3) for model viewing (standard 3D preview position)
- 02-01: ScrollWheelValue is cumulative - calculate per-frame delta (MonoGame behavior)
- 02-01: Separate held vs pressed detection for mouse buttons (continuous vs edge)
- 02-01: Transform uses Quaternion for rotation (prevent gimbal lock)
- 02-01: Transform provides derived direction vectors as properties (Forward/Right/Up)
- 02-02: MoveSpeed 5 units/sec, Acceleration 20f, Deceleration 15f (snappy arcade feel)
- 02-02: Player rotates to face movement direction, not cursor (arcade third-person action)
- 02-02: Smooth rotation using quaternion slerp with exponential smoothing
- 02-02: Temporary camera offset (0, 100, 200) follows player from behind/above
- 02-03: Exponential decay smoothing (1 - Pow(damping, deltaTime)) for frame-rate independence
- 02-03: Scroll wheel: positive delta = zoom in (decrease distance)
- 02-03: Right-click drag for camera orbit (common third-person pattern)
- 02-03: Auto pitch transition based on distance: eye-level close, high angle far
- 02-03: Collision detection with Ray.Intersects against BoundingBox list
- 02-03: Smooth zoom-in on collision, smooth zoom-out when collision clears
- 02-04: Tank controls (W/S forward/back, A/D rotate, Q/E strafe) for arcade shooter feel
- 02-04: Camera auto-follow only when character moves (idle = free orbit)
- 02-04: Skeletal animation implemented in Phase 2 (not deferred to Phase 8)
- 02-04: Mixamo models require 180-degree Y-axis rotation (+Z forward → -Z forward)
- 02-04: Model scale factor 0.01x for Mixamo assets (centimeters → game units)
- 02-04: Keyframe extraction from FBX, per-frame interpolation, bone hierarchy composition
- 02-04: Crosshair UI with programmatic texture generation (no external assets)
- 03-01: Object pooling with Queue<Projectile> prevents GC spikes (pre-allocate 50)
- 03-01: Distance-based projectile lifetime (75 units) instead of time-based
- 03-01: Fire rate 6.5 shots/sec, projectile speed 50 units/sec (middle of CONTEXT ranges)
- 03-01: Magazine 25 rounds + reserve 125 rounds with auto-reload
- 03-01: Frame-rate independent cooldown using GameTime.ElapsedGameTime.TotalSeconds
- 03-02: 8-segment UV sphere mesh for low-poly arcade aesthetic
- 03-02: Cyan emissive (0.3, 0.9, 1.0) for projectiles, orange (1.0, 0.8, 0.3) for impacts
- 03-02: Impact effects last 0.2s with fade and shrink animation
- 03-02: Wall collision via BoundingSphere.Intersects(BoundingBox) after projectile movement
- 03-02: Pre-allocate 20 impact effects in pool to prevent GC spikes
- 03-03: Test targets at fixed positions with 1-hit destruction for arcade feel
- 03-03: Target collision radius 0.7f (smaller than cube diagonal for balanced hit detection)
- 03-03: Hit flash 0.1s, color changes Green→Red→Transparent
- 03-03: AmmoPickup amount 40, auto-collect radius 2f (generous arcade forgiveness)
- 03-03: Bobbing animation for pickups using sin(time * speed) * height
- 03-03: Pickup pooling (size 10) handles multiple target destructions without GC
- 03-04: Camera forward vector = normalized(lookAt - cameraPosition) for aim direction
- 03-04: Projectile spawn at player position + Vector3.Up * 1.5f for shoulder height
- 03-04: R key respawns all targets for iterative testing without game restart
- 03-04: Combat update order: weapon → projectiles → targets → pickups for state propagation
- 03-04: Console output for ammo feedback during Phase 3 validation (defer HUD to Phase 8)
- 04-01: Health range 0-200 HP starting at 100 HP (allows overheal from future pickups)
- 04-01: OnDamageTaken event for immediate feedback, OnDeath event for state transitions
- 04-01: DamageVignette red gradient with quadratic falloff for stronger edge emphasis
- 04-01: Exponential decay fade (0.4s) using Math.Pow(0.01, deltaTime / duration) pattern
- 04-01: Programmatic UI textures (256x256 for vignette) - no external assets
- 04-02: ScreenFade linear interpolation (1.5s) for predictable death timing (not exponential)
- 04-02: HealthBar top-left (20, 20) with 200x20px, color transitions Green→Yellow→Red
- 04-02: Arial Bold 32pt SpriteFont for GameOverScreen (ASCII 32-126 character range)
- 04-02: 1x1 pixel texture pattern for all UI primitives (ScreenFade, HealthBar backgrounds)
- 04-03: GameState enum (Playing/Dying/GameOver) for game state machine flow control
- 04-03: PlayerController.IsEnabled pattern for external input gating without internal modification
- 04-03: H key test damage (10 HP) for iterative development validation
- 04-03: R key dual-purpose: respawn targets in Playing, restart in GameOver
- 04-03: Death sequence flow: OnDeath event → Dying state → 1.5s fade → GameOver state
- 04-03: RestartGame() centralizes health, fade, position, ammo, targets, weapon reset
- 05-01: Enemy speed 3.5 units/sec (70% of player speed) for chaseable but escapable combat
- 05-01: Attack range hysteresis (2.5 enter, 3.5 exit) prevents rapid state switching
- 05-01: Enemy health 30 HP requires 2-3 laser hits for satisfying combat feel
- 05-01: Direct movement toward player (no complex pathfinding for single-room arcade)
- 05-01: Health pickup green color (universal health indicator), heals 25 HP
- 05-01: Enemy FSM with Idle/Chase/Attack/Dying states, OnStateEnter/OnStateExit hooks
- 05-02: Min spawn distance 10u from player (safe reaction time per CONTEXT)
- 05-02: Min 3u between enemies to prevent spawn clustering
- 05-02: Random spawn with 20 attempts, fallback to corner safe zones
- 05-02: 35% drop chance (middle of 30-40% range), 50/50 ammo/health split
- 05-02: Progressive difficulty: 2 + wave number, caps at 10 enemies max
- 05-02: Health pickup pooling (size 10) mirrors ammo pickup pattern
- 05-03: Explosion duration 0.3s with 2.0f max radius (satisfying destruction feedback)
- 05-03: Expand over first half, shrink over second half for visual impact
- 05-03: Orange (1.0, 0.8, 0.3) explosion color matches impact effects from 03-02
- 05-03: Health pickup radius 0.3f (vs 0.2f ammo) for visual distinction
- 05-03: Placeholder cube rendering for Phase 5, Mixamo models in Plan 04
- 05-03: 8-segment sphere mesh matches ProjectileRenderer for consistency
- 05-04: Knockback force 8 units/sec provides satisfying pushback without excessive disruption
- 05-04: Horizontal-only knockback (Y=0) keeps arcade-style ground combat feel
- 05-04: Attack callback wiring after spawn allows dynamic enemy addition
- 05-04: Explosion pooling in EnemyManager prevents GC spikes on mass death
- 05-04: G key spawns test waves for combat tuning and verification
- 05-05: Player and enemies share same character model (test-character.fbx from Mixamo)
- 05-05: Animation files are skeleton-only (no skin): idle.fbx, walk.fbx, bash.fbx
- 05-05: Shared animation model pattern: load once, reuse across all enemy instances
- 05-05: FSM-driven animation switching: SetCurrentModel() in OnStateEnter()
- 05-05: Model assignment flow: Renderer loads → Manager receives → spawn wires to controllers
- 06-01: Enum state machine for doors (vs class-based State pattern) - simpler for states without per-state data
- 06-01: Trigger volumes extend INTO room (not outside) to detect player approaching from inside
- 06-01: Door.GetActiveCollision() returns null when Open for dynamic collision list building
- 06-01: 30x30 room with 4 cardinal doors matches existing game area dimensions
- 06-01: Room as passive data container - no Update/Draw logic (RoomManager handles)
- 06-02: 0.5s delay before doors open after room clear (visual clarity per RESEARCH.md)
- 06-02: 3 units spawn offset from door position (balances safety and proximity)
- 06-02: Event subscription pattern for EnemyManager.OnAllEnemiesDefeated wiring
- 06-02: Single transition per frame (break after first trigger detection)
- 06-02: TransitionToNewRoom() resets room/doors only, preserves player health/ammo

### Pending Todos

None yet.

### Blockers/Concerns

**From Research:**
- Phase 1: FBX animation import is highest technical risk - custom content processor now implemented
- Phase 1: Cross-platform content pipeline must be validated on all targets early
- Future: Third-person camera clipping requires collision detection from start (Phase 2)
- Phase 5: Direct movement sufficient for single-room arcade - complex pathfinding not needed (RESEARCH confirmed)

**From 01-02:**
- BoneContent.Index doesn't exist at build time - manual tracking required
- Keyframe extraction deferred to Plan 04 (animation playback)

**From 01-04 (Phase 1 complete):**
- No skeletal animation yet - using BasicEffect static rendering (sufficient for validation)
- Mixamo animations in separate FBX files - future optimization: merge in content pipeline
- Animation blending not implemented - instant switching only (defer to Phase 2+)

## Session Continuity

Last session: 2026-02-08
Stopped at: Completed 06-02-PLAN.md (RoomManager lifecycle management)
Resume file: None

**Phase 6 Progress:** Room data structures and manager complete. Plan 06-01 created Room data container with walls, doors, spawn points, and collision geometry. Plan 06-02 created RoomManager with Initialize/Update/Reset pattern, event-driven room clear detection (HandleAllEnemiesDefeated for EnemyManager wiring), 0.5s door opening delay, trigger-based transition detection, and spawn position calculation (3u offset from entry door). Next: BerzerkGame integration to wire RoomManager events, replace hardcoded walls with Room.GetCollisionGeometry(), and handle room transitions.

---
*State initialized: 2026-01-31*
*Last updated: 2026-02-08 (Phase 6 plan 02 complete - RoomManager lifecycle)*
