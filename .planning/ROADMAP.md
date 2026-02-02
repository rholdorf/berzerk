# Roadmap: Berzerk 3D

## Overview

This roadmap delivers a 3D reimagining of the classic arcade Berzerk, transforming room-based robot combat into modern third-person shooter format. We start by establishing the MonoGame foundation and FBX animation pipeline, then build upward through player controls, camera systems, combat mechanics, enemy AI, room progression, and polish. Each phase validates a complete capability before adding complexity, ensuring the core arcade loop feels right before procedural generation or advanced features.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation & Content Pipeline** - MonoGame project setup with working FBX import
- [x] **Phase 2: Player Movement & Camera** - Third-person controls and camera system
- [ ] **Phase 3: Core Combat System** - Laser weapon shooting and projectile mechanics
- [ ] **Phase 4: Player Health & Survival** - Damage system and death state
- [ ] **Phase 5: Enemy AI & Combat** - Robot enemies with combat behavior
- [ ] **Phase 6: Room System & Progression** - Room structure with door progression
- [ ] **Phase 7: UI & HUD** - Player feedback interface
- [ ] **Phase 8: Animation & Visual Polish** - Character animations and effects

## Phase Details

### Phase 1: Foundation & Content Pipeline
**Goal**: MonoGame project runs cross-platform with working Mixamo FBX import
**Depends on**: Nothing (first phase)
**Requirements**: FOUND-01, FOUND-02, FOUND-03, FOUND-04
**Success Criteria** (what must be TRUE):
  1. MonoGame project builds and runs on Windows, Linux, and macOS (macOS validated; Windows/Linux deferred to later phase)
  2. Custom FBX Content Pipeline processor imports Mixamo models (FBX 2013 format)
  3. Test character model loads and renders with at least 3 animations
  4. Keyboard and mouse input is detected and responds
**Plans**: 4 plans

Plans:
- [x] 01-01-PLAN.md — Create MonoGame solution with game and content pipeline projects
- [x] 01-02-PLAN.md — Implement custom FBX content processor with verbose logging
- [x] 01-03-PLAN.md — Implement input handling system
- [x] 01-04-PLAN.md — Load and render test Mixamo character with animations

### Phase 2: Player Movement & Camera
**Goal**: Player character moves responsively with smooth third-person camera
**Depends on**: Phase 1
**Requirements**: MOVE-01, MOVE-02, MOVE-03, MOVE-04, MOVE-05
**Success Criteria** (what must be TRUE):
  1. Player character moves in 3D space using WASD controls
  2. Player character rotates toward movement direction
  3. Third-person camera follows player smoothly with spring interpolation
  4. Camera does not clip through walls (collision detection active)
  5. Camera distance and angle can be adjusted
**Plans**: 4 plans

Plans:
- [x] 02-01-PLAN.md — Input extension and Transform foundation
- [x] 02-02-PLAN.md — Player movement controller with WASD
- [x] 02-03-PLAN.md — Third-person camera with collision and zoom
- [x] 02-04-PLAN.md — Integration and verification checkpoint

### Phase 3: Core Combat System
**Goal**: Player can shoot laser weapon with visible projectiles that hit targets
**Depends on**: Phase 2
**Requirements**: COMBAT-01, COMBAT-02, COMBAT-03, COMBAT-04, COMBAT-05, COMBAT-06, COMBAT-07
**Success Criteria** (what must be TRUE):
  1. Player aims with mouse cursor and fires on mouse click
  2. Laser projectiles spawn and travel through 3D space visibly
  3. Projectiles collide with walls and stop/disappear
  4. Projectiles collide with test targets and register hits
  5. Ammunition counter decreases when firing
  6. Ammo pickups spawn and can be collected to restore ammunition
**Plans**: 4 plans

Plans:
- [ ] 03-01-PLAN.md — Core combat infrastructure (Projectile, ProjectileManager, AmmoSystem, WeaponSystem)
- [ ] 03-02-PLAN.md — Projectile visuals and wall collision with impact effects
- [ ] 03-03-PLAN.md — Test targets and ammo pickups
- [ ] 03-04-PLAN.md — Integration and verification checkpoint

### Phase 4: Player Health & Survival
**Goal**: Player has health system and can die
**Depends on**: Phase 3
**Requirements**: HEALTH-01, HEALTH-02, HEALTH-03, HEALTH-04
**Success Criteria** (what must be TRUE):
  1. Player has health points that display visually
  2. Player health decreases when hit by test damage source
  3. Player dies (character stops responding) when health reaches zero
  4. Game over state triggers when player dies
**Plans**: TBD

Plans:
- [ ] TBD (to be planned in /gsd:plan-phase 4)

### Phase 5: Enemy AI & Combat
**Goal**: Robot enemies spawn, chase player, attack, and can be destroyed
**Depends on**: Phase 4
**Requirements**: AI-01, AI-02, AI-03, AI-04, AI-05, AI-06, ANIM-05, ANIM-06, ANIM-07, ANIM-08
**Success Criteria** (what must be TRUE):
  1. Robot enemies spawn in room using Mixamo models
  2. Robots detect player within proximity range and pursue
  3. Robots navigate toward player using pathfinding (not straight-line)
  4. Robots attack player on melee contact, dealing damage
  5. Robots are destroyed when hit by laser projectiles
  6. Destroyed robots play death animation, disappear, and award score points
  7. Robot animations play correctly (walk when moving, attack during melee, death when destroyed)
**Plans**: TBD

Plans:
- [ ] TBD (to be planned in /gsd:plan-phase 5)

### Phase 6: Room System & Progression
**Goal**: Player navigates through connected rooms with door progression
**Depends on**: Phase 5
**Requirements**: ROOM-01, ROOM-02, ROOM-03, ROOM-04, ROOM-05, ROOM-06
**Success Criteria** (what must be TRUE):
  1. Single handcrafted room with 3D maze layout loads and renders
  2. Room walls have collision detection (block player and projectiles)
  3. Room has doors at cardinal positions that are initially blocked
  4. Doors open automatically when all robots in current room are destroyed
  5. Player can walk through open doors to trigger room transition
  6. New room loads with fresh robot spawns when player transitions
**Plans**: TBD

Plans:
- [ ] TBD (to be planned in /gsd:plan-phase 6)

### Phase 7: UI & HUD
**Goal**: Player has complete HUD showing all gameplay information
**Depends on**: Phase 6
**Requirements**: UI-01, UI-02, UI-03, UI-04, UI-05, UI-06
**Success Criteria** (what must be TRUE):
  1. Crosshair displays at screen center for aiming
  2. Health bar displays current HP and updates when damaged
  3. Ammo counter displays current ammunition and updates when firing/collecting
  4. Score counter displays current points and updates when destroying robots
  5. Game over screen displays final score when player dies
  6. Simple start menu allows player to begin game
**Plans**: TBD

Plans:
- [ ] TBD (to be planned in /gsd:plan-phase 7)

### Phase 8: Animation & Visual Polish
**Goal**: Complete character animations and visual effects for polished experience
**Depends on**: Phase 7
**Requirements**: ANIM-01, ANIM-02, ANIM-03, ANIM-04, POLISH-01, POLISH-02, POLISH-03, POLISH-04, POLISH-05
**Success Criteria** (what must be TRUE):
  1. Player character uses Mixamo model with all animations integrated
  2. Player animations transition correctly (idle when stationary, walk/run when moving, shoot when firing)
  3. Laser projectiles have visual effect (glowing trail or similar)
  4. Sound effects play for laser fire, robot destruction, and player damage
  5. Background music plays during gameplay
**Plans**: TBD

Plans:
- [ ] TBD (to be planned in /gsd:plan-phase 8)

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7 -> 8

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Content Pipeline | 4/4 | Complete | 2026-02-01 |
| 2. Player Movement & Camera | 4/4 | Complete | 2026-02-02 |
| 3. Core Combat System | 0/4 | Planned | - |
| 4. Player Health & Survival | 0/? | Not started | - |
| 5. Enemy AI & Combat | 0/? | Not started | - |
| 6. Room System & Progression | 0/? | Not started | - |
| 7. UI & HUD | 0/? | Not started | - |
| 8. Animation & Visual Polish | 0/? | Not started | - |

---
*Roadmap created: 2026-01-31*
*Last updated: 2026-02-02*
