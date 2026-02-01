# Project Research Summary

**Project:** Berzerk 3D
**Domain:** 3D Action Shooter with Procedural Maze Generation (MonoGame)
**Researched:** 2026-01-31
**Confidence:** MEDIUM

## Executive Summary

Berzerk 3D is a modern 3D reimagining of the 1980 arcade classic Berzerk, built as a third-person shooter in MonoGame. The original game's brilliance was extreme simplicity creating intense decisions under pressure, driven by the iconic "Evil Otto" mechanic that prevents camping. Expert developers in this space recommend a hybrid approach: handcrafted room prefabs connected procedurally, not full procedural generation which creates bland experiences. The MonoGame stack is mature for this use case, but requires careful content pipeline setup for FBX/Mixamo animations.

The recommended approach prioritizes validating the core arcade loop first with a single handcrafted room before adding procedural complexity. Critical technical risks include FBX animation import failures, third-person camera clipping through geometry, and AI pathfinding performance collapse with multiple enemies. These are all preventable with proper architecture decisions early in development. The key differentiator is translating Evil Otto's time-pressure mechanic to 3D while maintaining the fast arcade pacing, not adding modern complexity (crafting, open world, realistic graphics) that would dilute the core experience.

Start with MonoGame 3.8.4.1 on .NET 8 using DesktopGL for cross-platform compatibility. Build the third-person camera and shooting mechanics first to validate the 3D translation feels good, then layer in Evil Otto, procedural generation, and polish. The biggest mistake would be building a large procedural generation system before proving the core loop is fun in a single room. Testing with 10+ animated enemies should be a gate before considering the game production-ready.

## Key Findings

### Recommended Stack

MonoGame 3.8.4.1 on .NET 8 provides a solid, proven foundation for 3D action games with extensive community support and commercial releases. The framework requires manual implementation of systems that Unity/Unreal provide out-of-box (cameras, animation, physics), but this gives fine control needed for arcade-style gameplay.

**Core technologies:**
- **MonoGame 3.8.4.1 + .NET 8**: Cross-platform game framework with active development and DirectX 12/Vulkan support in preview 3.8.5
- **MonoGame.Extended 5.3.1**: Official extensions for cameras, input helpers, sprite batching - fills common gaps in base framework
- **BEPUphysics v2 (2.5.0-beta.27)**: Pure C# 3D physics engine, BUT consider custom AABB collision instead for arcade simplicity
- **MonoSkelly or Aether.Animation**: Skeletal animation for Mixamo FBX models - custom content pipeline work required
- **Roy-T AStar**: Grid-based pathfinding for enemy AI navigation through procedural mazes

**Critical version notes:**
- FBX 2013 format only (newer versions not supported by content pipeline)
- .NET 8 required (MonoGame 3.8+ dropped .NET Framework support)
- Mixamo animations require custom content processors - no plug-and-play solution

### Expected Features

The original Berzerk's appeal was pure gameplay loop and personality (voice taunts), not complexity. Modern players expect third-person cameras and health bars, but avoid feature bloat.

**Must have (table stakes):**
- **Responsive shooting** - must feel good within first 5 minutes, includes visual/audio feedback
- **Third-person camera system** - most complex technical piece, requires smooth movement and collision detection
- **Player health/death** - modern players expect health bar over single-hit death from original
- **Basic enemy AI** - simple pursuit, readable and fair behavior
- **Level progression** - clear room to advance (doors open when enemies defeated)
- **Visual and audio feedback** - muzzle flash, hit sparks, damage reactions, score popups

**Should have (competitive differentiators):**
- **Evil Otto mechanic** - the defining feature that creates time pressure and prevents camping (non-negotiable)
- **Voice synthesis taunts** - personality and nostalgia ("Intruder alert!", "Chicken, fight like a robot!")
- **Procedural maze generation** - hybrid approach with handcrafted room prefabs connected randomly
- **Environmental hazards** - electrified walls from original, adds spatial awareness challenge
- **Risk-reward room clearing** - Evil Otto speeds up when all robots dead, creates strategic tension

**Defer (v2+):**
- **Run-based progression** - conflicts with arcade purity, test pure version first
- **Multiplayer co-op** - massive scope increase, netcode complexity
- **Complex weapon crafting** - diverts from core arcade loop
- **Narrative/story mode** - would slow pacing, breaks flow
- **Boss rooms** - adds complexity, may break pacing

### Architecture Approach

MonoGame requires manual system implementation but provides full control. Use simple object-oriented entity architecture initially, migrate to ECS only if performance demands it with 100+ entities.

**Major components:**
1. **Game Class** - MonoGame base with Initialize, LoadContent, Update, Draw lifecycle
2. **Scene/Screen Manager** - Stack-based state management for menu, gameplay, game over screens
3. **Entity System** - Simple OOP with Player, Enemy, Projectile classes; component composition for shared behaviors
4. **Camera System** - Third-person follow with collision detection, smooth interpolation, mouse rotation
5. **Animation System** - Custom content pipeline processor for Mixamo FBX, keyframe interpolation, bone transforms
6. **AI System** - State machines (patrol, chase, attack) with grid-based A* pathfinding
7. **Collision System** - AABB for entities/walls, raycasting for projectiles (avoid full physics engine complexity)
8. **Room Generator** - Hybrid approach: handcrafted prefab rooms assembled procedurally with validation

**Critical patterns:**
- Content Pipeline processes FBX at build time to .xnb format (not runtime)
- Async pathfinding with caching - never block Update() loop
- LOD system for animation (reduce bone count for distant enemies)
- Spatial partitioning for collision detection when 20+ entities active

### Critical Pitfalls

Research identified 6 critical pitfalls that will block development if not addressed architecturally from the start.

1. **FBX/Mixamo Animation Import Failures** - Mixamo exports need Blender intermediate re-export with correct bind pose settings. Test pipeline with 3+ animations on same character before building asset library. Custom content processor required.

2. **Cross-Platform Content Pipeline Incompatibility** - Assets must build on Windows, Linux, macOS. Standardize FBX 2013 format, test on all platforms early, document exact MonoGame and tool versions.

3. **Third-Person Camera Clipping Through Geometry** - Must implement collision detection from start with sphere-cast/raycast from player to camera position, smooth spring-based interpolation. Retrofitting after basic camera is significantly harder.

4. **AI Pathfinding Performance Collapse** - Naive A* synchronous pathfinding causes framerate drops with 5+ enemies. Must implement async pathfinding with caching and throttling from start. Test with 10+ enemies as acceptance criteria.

5. **SkinnedEffect Performance with Multiple Animated Enemies** - GPU skinning expensive without LOD system. Implement distance-based bone count reduction, frustum culling, animation update throttling. Benchmark 10+ animated enemies before moving forward.

6. **Procedural Generation Creates Unplayable Layouts** - Must validate connectivity, spawn point reachability, difficulty constraints post-generation. Run async with loading screen. Test with 100+ seeds to catch edge cases.

## Implications for Roadmap

Based on combined research, the roadmap should follow a risk-retirement progression: prove the core 3D arcade loop works first, then layer in complexity.

### Phase 1: Content Pipeline & Foundation
**Rationale:** FBX animation import is the highest technical risk and gates all visual development. Must establish working pipeline before building game systems that depend on it.

**Delivers:** MonoGame project structure, working FBX import with 3+ Mixamo animations on test character, basic rendering with SkinnedEffect.

**Addresses:**
- Critical Pitfall #1 (FBX import failures)
- Critical Pitfall #2 (cross-platform content)
- Stack validation (MonoGame 3.8.4.1 + .NET 8 working)

**Avoids:** Building large asset library before import pipeline proven stable.

**Needs research:** No - well-documented pattern, use MonoGame 3D Platformer Starter Kit as reference.

### Phase 2: Third-Person Camera & Movement
**Rationale:** Camera system is the most complex technical piece and gates shooting feel, movement, and overall "feel" of 3D translation. Dependencies from architecture research show responsive shooting requires camera first.

**Delivers:** Player entity with WASD movement, third-person camera with mouse rotation, collision detection preventing geometry clipping, smooth spring-based interpolation.

**Addresses:**
- Table stakes: Movement controls, Camera system
- Critical Pitfall #3 (camera clipping)

**Avoids:** Building shooting mechanics before camera feels good - wasted iteration if camera needs major refactoring.

**Needs research:** No - standard third-person camera patterns well-documented.

### Phase 3: Core Combat Loop
**Rationale:** Validate "shooting in 3D space feels satisfying" before adding complexity. This is the foundational gameplay loop - everything else is meaningless if this doesn't work.

**Delivers:** Laser weapon shooting, projectile spawning/collision, basic robot enemy with health, visual/audio feedback (muzzle flash, hit sparks), room clear detection.

**Addresses:**
- Table stakes: Responsive shooting, basic enemy AI, visual/audio feedback, room clear victory
- Core loop from FEATURES.md

**Avoids:** Adding procedural generation or Evil Otto before basic combat proven fun.

**Needs research:** No - standard shooter mechanics.

### Phase 4: Single Perfect Room
**Rationale:** Feature research emphasizes validating core loop with one perfect handcrafted room before procedural complexity. Architecture research shows this prevents "looks done but isn't" where generated rooms are bland.

**Delivers:** One handcrafted room with walls, electrified hazards, spawn points, door system that opens on room clear, scoring system, health bar HUD.

**Addresses:**
- Table stakes: Player health/death, level progression, scoring system
- Differentiator: Electrified walls environmental hazard
- MVP definition from FEATURES.md

**Avoids:** Building procedural generation system before knowing what makes a "good room."

**Needs research:** No - game design iteration, not technical research.

### Phase 5: Evil Otto Mechanic
**Rationale:** The defining Berzerk feature that creates time pressure. Must be added before procedural generation to validate room difficulty balancing with time pressure active.

**Delivers:** Indestructible Evil Otto entity, AI that matches player speed when room cleared, voice taunts system ("Intruder alert!"), risk-reward room clearing dynamics.

**Addresses:**
- Differentiator: Evil Otto mechanic (non-negotiable for Berzerk identity)
- Differentiator: Voice synthesis taunts
- Original Berzerk's core tension mechanic

**Avoids:** Building many rooms without understanding how Evil Otto affects pacing and difficulty.

**Needs research:** No - original game mechanic, well-documented.

### Phase 6: AI & Pathfinding
**Rationale:** After validating core combat and time pressure, enemies need intelligent navigation. Must implement with async architecture from start to avoid Pitfall #4.

**Delivers:** Enemy state machine (patrol, chase, attack), grid-based A* pathfinding with async execution, path caching and throttling, 10+ enemy performance benchmark.

**Addresses:**
- Table stakes: Basic enemy AI improvement to navigation
- Critical Pitfall #4 (pathfinding performance)
- Architecture requirement for async pathfinding

**Avoids:** Synchronous pathfinding that requires major refactor later.

**Needs research:** No - Roy-T AStar well-documented, standard patterns.

### Phase 7: Procedural Room Generation
**Rationale:** Now that core loop is validated and performance benchmarked, can build hybrid procedural system using learned room design principles.

**Delivers:** Room prefab system, procedural connection algorithm (DFS maze or BSP), room transition on door collision, validation for connectivity/playability, async generation with loading screen.

**Addresses:**
- Differentiator: Procedural maze generation
- Critical Pitfall #6 (unplayable layouts)
- Critical Pitfall #6 (synchronous generation freezes)
- v1.x feature: Procedural room connection

**Avoids:** Full procedural generation which creates bland content - use hybrid with prefabs.

**Needs research:** Possibly - procedural algorithms need experimentation, but basic patterns documented.

### Phase 8: Polish & Expansion
**Rationale:** Core game complete, now add variety and polish for replayability.

**Delivers:** 5-10 handcrafted room prefabs, multiple weapon types, difficulty progression (harder rooms), chain reaction explosions, visual/audio polish (particles, screen shake), high score table.

**Addresses:**
- v1.x features: Multiple weapons, difficulty curve, chain reactions, visual polish
- Differentiator: Chain reaction kills
- Replayability through variety

**Avoids:** Polish before core validated - premature optimization.

**Needs research:** No - polish iteration, standard patterns.

### Phase Ordering Rationale

- **Content pipeline first** because FBX import failures would block all later work on animation
- **Camera before shooting** because architecture research shows responsive shooting depends on camera system
- **Single room before procedural** because feature research warns against full procedural creating bland content
- **Evil Otto before AI complexity** because it defines difficulty balance and pacing
- **Async pathfinding from start** because retrofitting async to synchronous is major refactor
- **Polish last** because it's additive and meaningless if core loop doesn't work

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 7 (Procedural Generation):** Algorithm selection needs experimentation - DFS vs BSP vs cellular automata for Berzerk-style rooms. Validation heuristics for "playable" layouts may need iteration.
- **Phase 5 (Evil Otto):** Voice synthesis implementation needs research - text-to-speech libraries for C#, audio file management, dynamic taunt selection.

Phases with standard patterns (skip research-phase):
- **Phase 1 (Content Pipeline):** MonoGame 3D Platformer Starter Kit (July 2025) provides reference implementation
- **Phase 2 (Camera):** Third-person camera is well-documented, multiple community examples
- **Phase 3 (Combat):** Standard shooter mechanics, no unique challenges
- **Phase 4 (Single Room):** Game design iteration, not technical research
- **Phase 6 (AI/Pathfinding):** Roy-T AStar documented, standard state machine patterns

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | Versions verified from official releases (Jan 2025). MonoGame stable but FBX animation requires custom work. BEPUphysics may be overkill - custom AABB collision recommended. |
| Features | MEDIUM | Original Berzerk mechanics well-documented. Modern TPS expectations clear. Uncertainty around optimal procedural generation approach (hybrid vs full). |
| Architecture | MEDIUM | MonoGame patterns well-established but no official 3D action game template (3D Platformer Starter Kit is reference). Component architecture vs ECS decision needs profiling data. |
| Pitfalls | MEDIUM | FBX import issues extensively documented by community. Camera/pathfinding pitfalls verified across multiple sources. Cross-platform testing needed to validate solutions. |

**Overall confidence:** MEDIUM

Confidence is MEDIUM not LOW because:
- Core MonoGame framework is mature with official documentation
- Critical pitfalls have documented solutions in community
- Architecture patterns verified in multiple shipped games
- Original Berzerk mechanics are well-understood

Confidence is not HIGH because:
- No official MonoGame 3D action shooter template (3D Platformer is close but not exact match)
- FBX animation pipeline requires custom implementation
- Procedural generation quality requires experimentation
- Cross-platform content pipeline needs hands-on validation

### Gaps to Address

Areas where research was inconclusive or needs validation during implementation:

- **Voice synthesis implementation:** Research identified this as key differentiator but didn't find MonoGame-specific text-to-speech libraries. Need to research NAudio, Windows Speech Synthesis, or pre-generated audio files approach during Phase 5 planning.

- **Optimal procedural algorithm for Berzerk rooms:** Multiple maze algorithms documented (DFS, Prim's, BSP, cellular automata) but unclear which produces best gameplay for room-based maze navigation. May need Phase 7 spike to prototype 2-3 algorithms and compare playability.

- **BEPUphysics vs custom collision:** Stack research recommends BEPUphysics v2 but also notes custom AABB may suffice for arcade gameplay. Need to validate during Phase 3 if projectile raycasting and entity AABB collision are sufficient before adding physics engine complexity.

- **Animation LOD thresholds:** Pitfall research identifies skeletal animation performance issues but doesn't specify exact distance thresholds for bone count reduction. Need profiling during Phase 6 to determine when to drop from 4 bones -> 2 bones -> 1 bone based on camera distance.

- **Cross-platform testing infrastructure:** Research emphasizes need for testing on Windows/Linux/macOS but doesn't specify CI/CD setup for MonoGame. Address during Phase 1 - document exact tool versions, automate content builds, validate on all platforms.

- **Evil Otto speed balancing:** Original Berzerk's Evil Otto "matched player speed" when room cleared but 3D movement has more degrees of freedom. Need gameplay testing during Phase 5 to tune speed multipliers and acceleration curves for 3D space.

## Sources

### Primary (HIGH confidence)

**Official MonoGame Documentation:**
- MonoGame 3.8.4.1 and 3.8.5-preview.3 releases (GitHub)
- MonoGame 3D Platformer Starter Kit (July 2025)
- Content Pipeline Documentation
- Standard Content Importers (FBX support)
- Game class lifecycle and architecture patterns
- Collision detection primitives

**MonoGame.Extended:**
- Version 5.3.1 release (January 2025)
- Camera system documentation
- Screen management patterns

**BEPUphysics:**
- v2.5.0-beta.27 GitHub releases
- "MonoGame Physics by Example" (2024 book)

### Secondary (MEDIUM confidence)

**Community Consensus (MonoGame Forums, Multiple Sources):**
- FBX animation import issues and solutions (5+ community threads)
- Third-person camera implementations (verified examples)
- Skeletal animation with Mixamo (community processors)
- Project structure recommendations
- Component vs ECS architecture discussions
- Pathfinding libraries (Roy-T AStar, RogueSharp)

**Original Berzerk Research:**
- Wikipedia, Hardcore Gaming 101, TV Tropes
- Evil Otto mechanics and design intent
- "Greatest video game villain of all time" retrospectives
- 64,000 procedural mazes, voice synthesis innovation

**Procedural Generation Best Practices:**
- Hybrid approaches (handcrafted + procedural)
- When procedural generation works vs creates bland content
- Algorithm comparisons (DFS, Prim's, BSP, cellular automata)

### Tertiary (LOW confidence)

**Single-Source References (Need Validation):**
- XnaMixamoImporter (GitHub) - custom processor, not officially maintained
- MonoECS (GitHub) - ECS implementation, smaller community
- MonoSkelly - animation library, actively maintained but newer
- Specific shader optimization techniques
- Exact performance thresholds for LOD systems

**Competitor Analysis (Inferred, Not Domain-Specific):**
- Enter the Gungeon and Nuclear Throne feature analysis
- Third-person shooter mechanics (general game dev, not MonoGame-specific)
- Procedural shooter comparisons (not MonoGame implementations)

---
*Research completed: 2026-01-31*
*Ready for roadmap: yes*
