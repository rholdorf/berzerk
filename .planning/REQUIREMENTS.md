# Requirements: Berzerk 3D

**Defined:** 2026-01-31
**Core Value:** Combate arcade intenso em salas com labirintos - a essência do Berzerk original em 3D moderno

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Foundation

- [x] **FOUND-01**: MonoGame project setup with .NET 8 cross-platform (Windows, Linux, macOS)
- [x] **FOUND-02**: Custom FBX Content Pipeline processor for Mixamo models (FBX 2013 format)
- [x] **FOUND-03**: Asset loading system for 3D models and animations
- [x] **FOUND-04**: Input handling for keyboard and mouse

### Player Movement & Camera

- [x] **MOVE-01**: Player character moves with WASD controls
- [x] **MOVE-02**: Player rotation based on mouse movement
- [x] **MOVE-03**: Third-person camera follows player smoothly
- [x] **MOVE-04**: Camera collision detection (no clipping through walls)
- [x] **MOVE-05**: Camera distance and angle adjustable

### Combat System

- [x] **COMBAT-01**: Player can aim with mouse cursor
- [x] **COMBAT-02**: Player fires laser weapon on mouse click
- [x] **COMBAT-03**: Laser projectiles spawn and travel through 3D space
- [x] **COMBAT-04**: Projectile collision detection with enemies and walls
- [x] **COMBAT-05**: Ammunition system with limited ammo count
- [x] **COMBAT-06**: Ammo pickups spawn in room
- [x] **COMBAT-07**: Player can collect ammo pickups on collision

### Health & Damage

- [ ] **HEALTH-01**: Player has health points (HP bar)
- [ ] **HEALTH-02**: Player takes damage when hit by enemy
- [ ] **HEALTH-03**: Player dies when health reaches zero
- [ ] **HEALTH-04**: Game over state when player dies

### Enemy AI

- [ ] **AI-01**: Robot enemies spawn in room
- [ ] **AI-02**: Robots detect player within proximity range
- [ ] **AI-03**: Robots navigate toward player using pathfinding
- [ ] **AI-04**: Robots attack player on melee contact
- [ ] **AI-05**: Robots can be destroyed by laser projectiles
- [ ] **AI-06**: Destroyed robots disappear and award points

### Room & Level

- [ ] **ROOM-01**: Single handcrafted room with 3D maze layout loads
- [ ] **ROOM-02**: Room walls have collision (block player and projectiles)
- [ ] **ROOM-03**: Room doors exist at cardinal positions (blocked initially)
- [ ] **ROOM-04**: Doors open when all robots in room are destroyed
- [ ] **ROOM-05**: Player can traverse through open doors to next room
- [ ] **ROOM-06**: New room loads with fresh robot spawns

### UI & HUD

- [ ] **UI-01**: Crosshair/reticle displays at screen center
- [ ] **UI-02**: Health bar displays current HP
- [ ] **UI-03**: Ammo counter displays current ammunition
- [ ] **UI-04**: Score counter displays current points
- [ ] **UI-05**: Game over screen displays final score
- [ ] **UI-06**: Simple start menu to begin game

### Animation & Visuals

- [ ] **ANIM-01**: Player character uses Mixamo model with animations
- [ ] **ANIM-02**: Player idle animation plays when stationary
- [ ] **ANIM-03**: Player walk/run animation plays when moving
- [ ] **ANIM-04**: Player shoot animation plays when firing
- [ ] **ANIM-05**: Robot enemies use Mixamo models with animations
- [ ] **ANIM-06**: Robot walk animation plays when moving
- [ ] **ANIM-07**: Robot attack animation plays during melee
- [ ] **ANIM-08**: Robot death animation plays when destroyed

### Polish

- [ ] **POLISH-01**: Laser projectiles have visual effect (glowing trail)
- [ ] **POLISH-02**: Sound effect plays on laser fire
- [ ] **POLISH-03**: Sound effect plays on robot destruction
- [ ] **POLISH-04**: Sound effect plays on player damage
- [ ] **POLISH-05**: Basic background music during gameplay

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Evil Otto Mechanic

- **OTTO-01**: Evil Otto entity spawns after time threshold in room
- **OTTO-02**: Evil Otto is indestructible (cannot be damaged)
- **OTTO-03**: Evil Otto navigates directly toward player (no pathfinding, passes through walls)
- **OTTO-04**: Evil Otto speed increases as more robots are cleared
- **OTTO-05**: Evil Otto kills player instantly on contact
- **OTTO-06**: Evil Otto despawns when player exits room

### Voice Synthesis

- **VOICE-01**: Voice taunts play on specific events ("Intruder alert!" on spawn)
- **VOICE-02**: Voice mocks player ("Chicken, fight like a robot!" when fleeing)
- **VOICE-03**: Voice celebrates kills ("The humanoid must not escape!")

### Environmental Hazards

- **HAZARD-01**: Electrified walls damage player on contact
- **HAZARD-02**: Robots explode when colliding with walls
- **HAZARD-03**: Robot explosions damage nearby robots (chain reactions)
- **HAZARD-04**: Robot explosions damage player if in blast radius

### Procedural Generation

- **PROCGEN-01**: Maze layouts generate procedurally using algorithm (DFS, Prim's, or BSP)
- **PROCGEN-02**: Room difficulty scales with progression (more robots, tighter corridors)
- **PROCGEN-03**: Hybrid approach: handcrafted prefab sections connected procedurally
- **PROCGEN-04**: Validation ensures rooms are solvable and fair

### Progression & Meta

- **META-01**: Run-based progression with persistent unlocks
- **META-02**: Leaderboard for high scores
- **META-03**: Multiple character models to unlock
- **META-04**: Additional weapon types (spread shot, charge beam)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Multiplayer or co-op | Single-player focused; multiplayer adds massive complexity |
| Open world or hub area | Room-based arcade structure is core to Berzerk identity |
| Cover mechanics | Fast arcade combat, not tactical shooter |
| Weapon crafting/customization | Dilutes focused arcade experience |
| Complex narrative or cutscenes | Arcade game, not story-driven |
| Realistic graphics or physics simulation | Stylized arcade aesthetic, not simulation |
| Gamepad support in v1 | Keyboard+mouse only to start, expand controls later |
| VR support | Scope too large for initial release |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| FOUND-01 | Phase 1 | Complete |
| FOUND-02 | Phase 1 | Complete |
| FOUND-03 | Phase 1 | Complete |
| FOUND-04 | Phase 1 | Complete |
| MOVE-01 | Phase 2 | Complete |
| MOVE-02 | Phase 2 | Complete |
| MOVE-03 | Phase 2 | Complete |
| MOVE-04 | Phase 2 | Complete |
| MOVE-05 | Phase 2 | Complete |
| COMBAT-01 | Phase 3 | Complete |
| COMBAT-02 | Phase 3 | Complete |
| COMBAT-03 | Phase 3 | Complete |
| COMBAT-04 | Phase 3 | Complete |
| COMBAT-05 | Phase 3 | Complete |
| COMBAT-06 | Phase 3 | Complete |
| COMBAT-07 | Phase 3 | Complete |
| HEALTH-01 | Phase 4 | Pending |
| HEALTH-02 | Phase 4 | Pending |
| HEALTH-03 | Phase 4 | Pending |
| HEALTH-04 | Phase 4 | Pending |
| AI-01 | Phase 5 | Pending |
| AI-02 | Phase 5 | Pending |
| AI-03 | Phase 5 | Pending |
| AI-04 | Phase 5 | Pending |
| AI-05 | Phase 5 | Pending |
| AI-06 | Phase 5 | Pending |
| ANIM-05 | Phase 5 | Pending |
| ANIM-06 | Phase 5 | Pending |
| ANIM-07 | Phase 5 | Pending |
| ANIM-08 | Phase 5 | Pending |
| ROOM-01 | Phase 6 | Pending |
| ROOM-02 | Phase 6 | Pending |
| ROOM-03 | Phase 6 | Pending |
| ROOM-04 | Phase 6 | Pending |
| ROOM-05 | Phase 6 | Pending |
| ROOM-06 | Phase 6 | Pending |
| UI-01 | Phase 7 | Pending |
| UI-02 | Phase 7 | Pending |
| UI-03 | Phase 7 | Pending |
| UI-04 | Phase 7 | Pending |
| UI-05 | Phase 7 | Pending |
| UI-06 | Phase 7 | Pending |
| ANIM-01 | Phase 8 | Pending |
| ANIM-02 | Phase 8 | Pending |
| ANIM-03 | Phase 8 | Pending |
| ANIM-04 | Phase 8 | Pending |
| POLISH-01 | Phase 8 | Pending |
| POLISH-02 | Phase 8 | Pending |
| POLISH-03 | Phase 8 | Pending |
| POLISH-04 | Phase 8 | Pending |
| POLISH-05 | Phase 8 | Pending |

**Coverage:**
- v1 requirements: 51 total
- Mapped to phases: 51/51 (100%)
- Unmapped: 0

---
*Requirements defined: 2026-01-31*
*Last updated: 2026-01-31 after roadmap creation*
