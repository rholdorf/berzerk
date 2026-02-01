# Pitfalls Research

**Domain:** MonoGame 3D Game Development (Berzerk Clone)
**Researched:** 2026-01-31
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: FBX/Mixamo Animation Import Failures

**What goes wrong:**
Mixamo FBX animations fail to import correctly, resulting in distorted meshes (elongated/bent limbs), incorrect bind poses, or animations that start in T-pose instead of playing. Models may appear shifted or corrupted when rendered with SkinnedEffect even though they look correct in 3D modeling software.

**Why it happens:**
Mixamo exports FBX 2013 format by default, which has limited support in MonoGame's content pipeline. XNA/MonoGame's SkinnedMesh importer is extremely version-sensitive and only works with specific FBX format versions and bone hierarchy structures. Simply converting FBX 2013 to FBX 2011 with Autodesk FBX Converter doesn't preserve bone transformations correctly, leading to mesh distortion. Additionally, Mixamo provides animation-only files (skeleton without mesh) which developers struggle to merge with character models.

**How to avoid:**
- Test FBX import pipeline early with a single Mixamo character before creating full asset library
- Use Blender as an intermediate tool to re-export FBX files with correct bind pose settings
- Ensure all FBX exports use "Use Time 0 as Reference Pose" to avoid bind pose errors
- Verify bone hierarchy consistency between model and all animation files (parent-child relationships must match exactly)
- Consider using custom content processors (community-developed XnaMixamoImporter or similar) specifically designed for Mixamo assets
- For multiple animations, load base model once and create a system to swap animation data rather than loading full model per animation

**Warning signs:**
- Initial FBX import shows "FBX-DOM unsupported, old format version" errors in content pipeline
- Character mesh renders but limbs are stretched, rotated incorrectly, or inverted
- Animations play but model stays in T-pose or bind position
- Different results between content pipeline build and runtime rendering
- "Failed to find bone hierarchy" or "Found multiple roots" import errors

**Phase to address:**
Phase 1 (Content Pipeline Setup) - Must establish working FBX import workflow before building game systems that depend on it. Include validation step with at least 3 different Mixamo animations on same character.

---

### Pitfall 2: Cross-Platform Content Pipeline Incompatibility

**What goes wrong:**
3D assets build successfully on one platform (e.g., Windows) but fail on others (Linux/macOS). FBX importer produces different results across platforms, or content pipeline tools have version mismatches. Shaders compile on one graphics backend (DirectX) but fail on others (OpenGL/Metal).

**Why it happens:**
MonoGame's content pipeline relies on platform-specific graphics APIs and shader compilers. The assimp library (used for FBX import) has platform-specific behavior and only .fbx format has been fully tested across all platforms. Older FBX versions from XNA samples don't work on modern MonoGame without manual conversion. Different shader models (DirectX HLSL vs OpenGL GLSL) require translation, and effects that work with DirectX may not translate correctly to OpenGL/Metal.

**How to avoid:**
- Standardize FBX format and version across all assets (use only FBX 2013 or later)
- Set up content pipeline testing on all target platforms early (Windows, Linux, macOS)
- Use MonoGame's MGCB (Content Builder) with identical tool versions across all development machines
- Validate shader compatibility by testing on all graphics backends (DirectX, OpenGL, Metal)
- Automate content builds via MGCB scripts to ensure consistency
- Document exact MonoGame version, content pipeline tools version, and FBX exporter settings
- Use only shader models that are universally supported (Shader Model 3.0 minimum)
- Normalize 3D coordinate systems (MonoGame uses Y-up; Blender defaults to Z-up)

**Warning signs:**
- Content builds on developer machine but fails in CI/CD or on teammates' machines
- Same .fbx file produces different visual results on different platforms
- Shaders work on Windows (DirectX) but cause runtime errors on macOS (Metal)
- "Model importer not found" or "Unknown importer" errors on non-Windows platforms
- Coordinate system mismatches (models appear rotated 90 degrees or upside-down)

**Phase to address:**
Phase 1 (Content Pipeline Setup) and Phase 2 (Cross-Platform Foundation) - Must validate cross-platform content pipeline before building substantial asset library. Each platform should have successful test builds with representative 3D assets.

---

### Pitfall 3: Third-Person Camera Clipping Through Geometry

**What goes wrong:**
Camera clips through walls and obstacles, exposing backfaces and destroying immersion. When the player backs into corners, the camera jumps erratically or shows the world from inside solid geometry. Camera stutters or snaps instead of moving smoothly when obstacles appear between camera and player.

**Why it happens:**
Developers implement basic camera following (position = playerPosition + offset) without collision detection. When obstacles block the line of sight between camera and player, the camera continues to its desired position regardless of geometry. Without proper smoothing/interpolation, camera position updates cause jarring snaps. Raycasting for occlusion is expensive, and naive implementations test every frame without optimization.

**How to avoid:**
- Implement sphere-cast or raycast from player to desired camera position, detect obstacles
- When obstacles detected, move camera to collision point (with small offset to prevent clipping)
- Use spring-based interpolation (critically-damped spring) for smooth camera movement, not linear interpolation
- Separate smooth times for position (0.3s) and rotation (0.2s) using SmoothDamp or similar
- Apply low-pass filter to smooth out small bumps in player movement
- Implement "slide along obstacles" behavior when occlusion occurs (don't just stop)
- Add near-plane clipping offset to prevent geometry from appearing when camera gets very close to walls
- Consider two-phase approach: ideal position calculation → occlusion check and adjustment → smooth interpolation to final position

**Warning signs:**
- Camera moves through walls during testing
- Rapid camera position changes when player moves near walls
- Players report motion sickness or disorientation during gameplay
- Camera view shows polygon backfaces (inside of geometry)
- Camera "teleports" instead of smoothly transitioning when obstacles appear/disappear
- Performance drops during camera updates (raycasting every obstacle every frame)

**Phase to address:**
Phase 3 (Character Controller & Camera) - Camera system must be implemented with collision detection from the start. Retrofitting collision detection after basic camera is built is significantly harder than building it correctly initially.

---

### Pitfall 4: AI Pathfinding Performance Collapse in Procedural Mazes

**What goes wrong:**
Game runs smoothly with 1-2 enemies but framerate drops dramatically with 5+ enemies pathfinding simultaneously. Pathfinding requests block the game loop causing stutters. Enemies get stuck in corners, fail to find paths, or oscillate back and forth instead of moving smoothly toward the player.

**Why it happens:**
A* pathfinding on large grid-based mazes is computationally expensive, especially when run synchronously every frame for multiple agents. Naive implementations recalculate full paths every frame even when player hasn't moved significantly. Without path caching or request throttling, multiple enemies simultaneously requesting paths creates CPU spikes. Procedurally generated mazes may have unexpected topology (dead ends, narrow corridors) that A* explores inefficiently.

**How to avoid:**
- Implement async pathfinding requests - never block Update() loop waiting for path calculations
- Use path caching: only recalculate when player moves beyond threshold distance (e.g., 3+ tiles)
- Throttle pathfinding requests: maximum N requests per frame, queue remaining for next frame
- Pre-calculate navigation mesh or waypoint graph during maze generation instead of using raw tile grid
- Use hierarchical pathfinding for large mazes (divide maze into regions, pathfind between regions first)
- Implement path smoothing after A* to prevent zigzag movement through open areas
- Add local steering behaviors (obstacle avoidance, separation) to handle dynamic obstacles without full pathfinding
- Consider simpler AI for distant enemies (direct movement toward player, only use pathfinding when close)
- Profile pathfinding separately from rendering to identify actual bottlenecks

**Warning signs:**
- Frame time spikes when new enemy spawns or player moves to new area
- CPU profiling shows significant time in pathfinding code (>10ms per frame)
- Enemies freeze briefly when calculating paths
- All enemies recalculate paths simultaneously when player moves
- Enemies fail to navigate around newly spawned obstacles or procedural maze variations
- Memory usage grows over time (path caching without limits)

**Phase to address:**
Phase 5 (AI Pathfinding) - Must implement with async/throttling architecture from the start. Retrofitting async behavior into synchronous pathfinding is a significant refactor. Include performance testing with 10+ simultaneous enemies as acceptance criteria.

---

### Pitfall 5: SkinnedEffect Performance with Multiple Animated Enemies

**What goes wrong:**
Game runs smoothly with static models but framerate drops significantly when 5+ animated characters are on screen. GPU performance degrades despite relatively simple models. Animations cause stuttering or frame pacing issues even with good average FPS.

**Why it happens:**
SkinnedEffect involves GPU bone matrix calculations and vertex skinning for every vertex every frame. Each animated character sends 72+ bone matrices to GPU (default bone count limit). Without instancing, every character requires separate draw call with full effect parameter setup. Developers often use highest quality skinning (4 bones per vertex) when simpler skinning (1-2 bones) would suffice for distant enemies. Constant buffer updates happen every frame even when animation state hasn't changed significantly.

**How to avoid:**
- Implement LOD (Level of Detail) system: reduce bone count for distant enemies (4 bones → 2 bones → 1 bone)
- Use hardware instancing for enemies with identical animation states (group by animation frame)
- Cache effect parameters and only update when values actually change (MonoGame does this for built-in effects, ensure custom effects follow pattern)
- Consider CPU-based animation for very simple models (BasicEffect) vs GPU (SkinnedEffect) based on profiling
- Limit WeightsPerVertex based on model complexity (not everything needs 4 bones per vertex)
- Implement animation culling: don't animate enemies outside camera frustum
- Use shader variants optimized for different bone counts (SkinnedEffect has 1-bone, 2-bone, 4-bone techniques)
- Profile GPU usage separately from CPU to identify actual bottleneck (may be draw calls, not skinning)
- Consider animation update throttling: update close enemies every frame, distant enemies every 2-3 frames

**Warning signs:**
- FPS drops proportionally to number of animated characters on screen
- GPU profiling shows high percentage time in vertex shader
- Draw call count is very high (one per animated enemy)
- Frame pacing becomes inconsistent (stuttering) even with acceptable average FPS
- Performance is fine on powerful GPU but poor on integrated graphics
- Animation updates don't align with frame updates (temporal artifacts)

**Phase to address:**
Phase 6 (Combat & Enemies) - Implement basic animation first, then add performance optimizations (LOD, instancing) before creating large numbers of enemies. Include performance benchmark with 10+ simultaneous animated enemies as gate before moving forward.

---

### Pitfall 6: Procedural Generation Creates Unplayable or Performance-Killing Layouts

**What goes wrong:**
Procedurally generated mazes are unsolvable (player spawn with no path to enemies), trivially easy (straight corridor), or create performance problems (too many entities in small area). Maze generation takes too long and causes loading stutters or freezes.

**Why it happens:**
Most maze generation algorithms (DFS, Kruskal's, Prim's) don't produce uniform spanning trees - they create predictable patterns that become boring quickly. Algorithms don't validate gameplay constraints (minimum path length, room distribution, difficulty progression). Generation runs synchronously during level load, blocking game initialization. No validation that player/enemy spawn points are reachable from each other. Procedural entity placement creates clusters (10+ enemies in one room) instead of distribution.

**How to avoid:**
- Validate maze after generation: run connectivity check ensuring player spawn connects to all critical areas
- Implement generation constraints: minimum/maximum path length between player and objectives
- Use Aldous-Broder or Wilson's algorithm if truly random mazes are required (though slower)
- Run generation asynchronously with loading screen - never block main thread
- Add post-generation validation: difficulty heuristics (path length, room count, junction density)
- Implement entity placement rules: maximum entities per room, minimum distance between enemies
- Consider hybrid approach: hand-crafted room templates assembled procedurally
- Cache generation parameters that produce good results; avoid pure randomness
- Profile generation time separately; set maximum time budget (e.g., 100ms for mobile)
- Test with seed values to reproduce problematic generations

**Warning signs:**
- Playtesting reveals unwinnable scenarios (spawn surrounded by walls)
- Mazes feel same despite procedural generation (pattern recognition)
- Level loading takes >1 second or causes visible freeze
- Some generated levels have 0 enemies or 30+ enemies in spawn room
- Memory allocation spikes during generation (GC pressure)
- Generation occasionally fails completely (infinite loops, stack overflow)

**Phase to address:**
Phase 7 (Procedural Generation) - Must include validation and constraints from initial implementation. Bolting validation onto existing generator requires significant refactoring. Include automated testing with 100+ generated seeds to catch edge cases.

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Loading each Mixamo animation as separate full model | Simple to implement - Content.Load<Model> per animation | Memory waste (duplicate mesh data), slow loading, harder to synchronize | Never - fix architecture early |
| Synchronous pathfinding in Update() loop | Easier to reason about - path ready immediately | Framerate drops with multiple enemies, blocks game logic | Prototyping only - must refactor for production |
| No camera collision detection | Quick to implement following camera | Immersion-breaking clipping, player complaints | Never for 3rd-person game |
| Using highest quality skinning (4 bones) for all models | Looks good, no LOD complexity | Poor performance on low-end GPUs, mobile unplayable | Only if targeting high-end PC exclusively |
| Procedural generation on main thread during load | Simple synchronous code | Loading freezes, poor UX, mobile ANR crashes | Only if generation is <100ms |
| Per-frame full path recalculation for AI | AI always has fresh path | Massive CPU waste, pathfinding dominates frame time | Never - implement path caching |
| BasicEffect instead of custom shaders | Built-in, easy to use | Limited visual quality, harder to optimize later | Early prototyping, non-critical objects |
| Global coordinate space without scene graph | Simple transform math | Hard to implement hierarchical animations, attachments | Very simple games with no animation |

## Integration Gotchas

Common mistakes when connecting to external services/tools.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| Mixamo FBX Export | Using default FBX 2013 format directly | Export from Mixamo → import to Blender → re-export with correct settings → MonoGame |
| Blender to MonoGame | Forgetting coordinate system difference (Z-up vs Y-up) | Apply rotation on export or in content pipeline; document coordinate convention |
| Content Pipeline MGCB | Using relative paths that break cross-platform | Use consistent absolute paths or project-relative paths; automate with scripts |
| SkinnedEffect bone limits | Exceeding 72 bone maximum | Verify bone count in modeling tool; split complex rigs or use bone reduction |
| FBX Version Compatibility | Assuming newer FBX = better | Test exact FBX version with MonoGame; document working version combination |
| Assimp Import Settings | Using default import settings for all formats | Configure importer per format (.fbx needs different settings than .obj) |
| Shader Cross-Compilation | Writing HLSL assuming DirectX only | Test shaders on OpenGL/Metal; use only cross-platform shader features |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Per-frame pathfinding for all enemies | Smooth at 1-2 enemies | Async pathfinding, path caching, throttling | 5+ enemies simultaneously |
| No animation LOD system | Good FPS with 3 enemies | Distance-based bone count reduction, culling | 10+ animated characters on screen |
| Synchronous procedural generation | Fast with small mazes | Async generation with loading screen | Mazes >50x50 or complex rules |
| Per-enemy collision checks (N²) | Works with few enemies | Spatial partitioning (grid, quadtree) | 20+ dynamic entities |
| Full path recalculation on every player movement | Responsive AI | Recalculate only when player moves >threshold | Any production scenario |
| Drawing each enemy separately without batching | Simple code | Hardware instancing, sprite batching equivalent | 50+ similar models |
| No shader parameter caching | Easy to write | Cache parameter lookups in constructor | Many draw calls per frame |
| Loading all animations at level start | Guaranteed availability | Lazy loading, streaming, pooling | 10+ character types with 5+ animations each |

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Trusted procedural generation seed from network | Malicious seeds crash game or exploit vulnerabilities | Validate seed ranges, sandbox generation, timeout limits |
| No validation on procedurally generated content | Generated levels with impossible geometry cause crashes | Post-generation validation, bounds checking, connectivity tests |
| Loading untrusted .fbx files | Buffer overflow in assimp library | Only load vetted assets, update assimp regularly, catch import exceptions |
| Client-side game state authority | Cheating in multiplayer (if added later) | Design for server authority from start even in single-player |

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Camera clipping through geometry | Motion sickness, immersion break | Collision detection, smooth occlusion handling |
| Stuttering during pathfinding calculations | Feels laggy, frustrating | Async pathfinding, frame-time budgeting |
| Procedural generation without player feedback | Staring at frozen screen | Loading screen, progress bar, background generation |
| No animation blending transitions | Robotic, jarring movement | Implement blend trees, transition smoothing |
| Camera that snaps instead of smooths | Disorienting, amateurish feel | Spring-based interpolation with separate position/rotation smooth times |
| Enemies stuck in corners visibly struggling | Breaks AI illusion | Better steering behaviors, path smoothing, failure recovery |
| Identical procedurally generated levels | "Procedural" claim feels false | Better algorithm variety, validation, player choice in generation |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **FBX Import:** Often missing validation that animations match model's bone hierarchy — verify every animation plays correctly on model before considering import "done"
- [ ] **Third-Person Camera:** Often missing collision detection and smooth interpolation — verify camera in tight corners and rapid player movement
- [ ] **AI Pathfinding:** Often missing path caching, async execution, and throttling — verify performance with 10+ enemies active simultaneously
- [ ] **Skeletal Animation:** Often missing LOD system and culling — verify performance with maximum expected enemy count on lowest-spec target hardware
- [ ] **Procedural Generation:** Often missing validation that generated levels are playable — verify connectivity, spawn point reachability, minimum path lengths
- [ ] **Cross-Platform Content:** Often missing testing on all platforms — verify content builds successfully on Windows, Linux, and macOS, not just development machine
- [ ] **Collision Detection:** Often missing edge cases (high velocity, thin walls) — verify projectiles don't tunnel through geometry at maximum speed
- [ ] **Animation Blending:** Often missing transition logic — verify no snapping when switching between idle/walk/attack states

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| FBX import broken after significant asset creation | MEDIUM | Create custom content processor, batch-process all assets through Blender re-export pipeline, document exact export settings |
| Camera clipping discovered late in development | LOW | Add collision detection as post-process step, ray-cast from player to camera position, interpolate to collision point |
| Pathfinding performance issues with many enemies | MEDIUM | Implement async pathfinding wrapper, add path caching layer, throttle requests, may require AI system refactor |
| Cross-platform content pipeline failures | HIGH | Standardize all content formats, create CI/CD pipeline for all platforms, may require asset re-export |
| Skeletal animation performance problems | MEDIUM | Add LOD system as post-process, implement frustum culling, reduce bone counts on existing models if necessary |
| Unplayable procedural generations | LOW-MEDIUM | Add post-generation validation pass, regenerate on failure, may need to adjust algorithm or add constraints |
| Procedural generation too slow | MEDIUM | Move to async/background thread, add loading screen, optimize algorithm, consider caching common patterns |
| Memory issues from duplicate animation data | MEDIUM | Refactor content loading to separate mesh and animation data, requires content pipeline changes |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| FBX/Mixamo import failures | Phase 1: Content Pipeline Setup | Successfully import 3+ different Mixamo animations on same character, render with SkinnedEffect |
| Cross-platform content incompatibility | Phase 1-2: Content Pipeline + Cross-Platform Foundation | Content builds successfully on all target platforms (Windows, Linux, macOS) |
| Camera clipping through geometry | Phase 3: Character Controller & Camera | Camera tested in tight corners, against walls, with rapid movement - no clipping visible |
| AI pathfinding performance collapse | Phase 5: AI Pathfinding | Performance profiling with 10+ enemies shows <5ms pathfinding time per frame |
| SkinnedEffect performance issues | Phase 6: Combat & Enemies | Maintain 60 FPS with 10+ animated enemies on minimum spec hardware |
| Unplayable procedural layouts | Phase 7: Procedural Generation | Automated tests validate 100+ generated seeds for connectivity, playability |
| Synchronous generation freezes | Phase 7: Procedural Generation | Generation happens with loading screen, no visible freeze, <2 second load time |
| Animation data duplication | Phase 4: Animation System | Memory profiling shows single mesh copy, separate animation data |

## Sources

**MonoGame FBX/Mixamo Animation:**
- [SkinnedMesh Import question - MonoGame Community](https://community.monogame.net/t/skinnedmesh-import-question/8902)
- [Animation and fbx - MonoGame Community](https://community.monogame.net/t/solved-animation-and-fbx/9342)
- [FBX models animation issue - GitHub Issue #3672](https://github.com/MonoGame/MonoGame/issues/3672)
- [Robust method to import skinned fbx models - MonoGame Community](https://community.monogame.net/t/robust-method-to-import-skinned-fbx-models/12936)
- [XnaMixamoImporter - GitHub](https://github.com/BaamStudios/XnaMixamoImporter)

**Cross-Platform Content Pipeline:**
- [Troubleshooting Content, Rendering, and Deployment Issues in MonoGame](https://www.mindfulchase.com/explore/troubleshooting-tips/game-development-tools/troubleshooting-content,-rendering,-and-deployment-issues-in-monogame.html)
- [Advanced Troubleshooting Guide for MonoGame](https://www.mindfulchase.com/explore/troubleshooting-tips/game-development-tools/advanced-troubleshooting-guide-for-monogame.html)
- [MonoGame Content Pipeline Documentation](https://docs.monogame.net/articles/getting_started/content_pipeline/why_content_pipeline.html)
- [Importing 3D Models - RB Whitaker's Wiki](http://rbwhitaker.wikidot.com/forum/t-1673938/importing-3d-models-into-the-monogame-content-pipeline)

**Third-Person Camera:**
- [Third Person Camera Collision - Unity Discussions](https://forum.unity.com/threads/third-person-camera-collision.465944/)
- [Tech Breakdown: Third Person Cameras in Games](https://blog.littlepolygon.com/posts/cameras/)
- [Game Camera Systems: Complete Programming Guide 2025](https://generalistprogrammer.com/tutorials/game-camera-systems-complete-programming-guide-2025)
- [Tips and Tricks for a Robust Third-Person Camera System (PDF)](http://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- [Understanding 3d camera - MonoGame Community](https://community.monogame.net/t/understanding-3d-camera/11700)

**AI Pathfinding:**
- [A* pathfinding examples - MonoGame Community](https://community.monogame.net/t/a-pathfinding-examples/8220)
- [Pathfinding Library - MonoGame.Extended Issue #215](https://github.com/craftworkgames/MonoGame.Extended/issues/215)
- [monogame-astar-pathfinding - GitHub](https://github.com/manbeardgames/monogame-astar-pathfinding)
- [Tutorial 4 – Roguelike Pathfinding using RogueSharp](https://roguesharp.wordpress.com/2014/06/09/tutorial-4-roguelike-pathfinding-using-roguesharp-and-monogame/)

**Performance & SkinnedEffect:**
- [MonoGame and XNA performance cheat sheet](https://konradzaba.github.io/blog/tech/Monogame-and-XNA-performance-cheat-sheet-low-level/)
- [MonoGame performance - GitHub Issue #1054](https://github.com/MonoGame/MonoGame/issues/1054)
- [SkinnedEffect.cs - MonoGame Source](https://github.com/MonoGame/MonoGame/blob/develop/MonoGame.Framework/Graphics/Effect/SkinnedEffect.cs)
- [Custom Effects Documentation](https://github.com/MonoGame/docs.monogame.github.io/blob/main/articles/getting_started/content_pipeline/custom_effects.md/)

**Procedural Generation:**
- [Procedural generation in 2D platformers - MonoGame Community](https://community.monogame.net/t/procedural-generation-in-2d-platformers/18832)
- [Creating infinite tile map with SharpNoise and MonoGame](https://infiniteproductionsblog.wordpress.com/2016/03/20/creating-infinite-tile-map-with-sharpnoise-and-monogame/)
- [Analysis of Maze Generation Algorithms - IEEE](https://ieeexplore.ieee.org/document/10580178)
- [Procedural Maze Generation: How I Tested 5 Algorithms - itch.io](https://itch.io/blog/1135055/procedural-maze-generation-how-i-tested-5-algorithms-and-why-dfs-won-for-now)

**FBX Import & Rigging:**
- [FBX Import Errors in Unreal Engine](https://dev.epicgames.com/documentation/en-us/unreal-engine/fbx-import-errors-in-unreal-engine)
- [Troubleshooting: MonoGame - Using 3D Models](http://rbwhitaker.wikidot.com/troubleshoot:monogame-using-3d-models)
- [3D Platformer Starter Kit - MonoGame](https://monogame.net/blog/2025-07-16-3d-starter-kit/)

---
*Pitfalls research for: MonoGame 3D Game Development (Berzerk Clone)*
*Researched: 2026-01-31*
