# Stack Research

**Domain:** 3D Action Game with MonoGame
**Researched:** 2026-01-31
**Confidence:** MEDIUM

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| MonoGame | 3.8.4.1 (stable) or 3.8.5-preview.3 | Cross-platform game framework | Industry standard for C# game development, proven track record with commercial releases, active development with DirectX 12 and Vulkan support in preview |
| .NET | 8.0 | Runtime framework | Required by MonoGame 3.8.x, provides excellent cross-platform support and performance with RyuJIT compiler |
| MonoGame.Extended | 5.3.1 | Quality of life extensions | Official extension library providing cameras, sprite batching, input helpers, and particle systems - fills common gaps in base MonoGame |

**Confidence: HIGH** - Versions verified from official GitHub releases (January 2025). MonoGame 3.8.4.1 released October 2024 is stable; 3.8.5-preview.3 from January 17, 2025 adds DirectX 12 and Vulkan.

### 3D Model & Animation System

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| MonoGame Content Pipeline (FBX) | Built-in | FBX model import | Native support for FBX 2013 format via Assimp library, handles meshes and basic animations |
| MonoSkelly | Latest (NuGet) | Skeletal animation runtime | Purpose-built for MonoGame, provides editor + library + file format for skeleton-based animations, supports animation blending |
| Aether.Animation | Latest (NuGet) | GPU/CPU skinned animation | Alternative option for Mixamo FBX animations, provides both GPU and CPU animation paths |

**Confidence: MEDIUM** - MonoGame's FBX support is well-documented (FBX 2011-2013 formats). MonoSkelly is actively maintained but smaller community. Mixamo integration requires custom content processors - no plug-and-play solution exists.

**Important caveat:** Mixamo FBX animations require custom content pipeline work. The AnimatedModelImporter/Processor pattern is community-standard but requires manual implementation. MonoSkelly provides an alternative workflow with its own editor.

### Physics Engine

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| BEPUphysics v2 | 2.5.0-beta.27 | 3D physics simulation | Pure C#, high performance, actively maintained, .NET 8 compatible, comprehensive feature set (collision shapes, constraints, continuous collision detection, character controller) |

**Confidence: MEDIUM** - BEPUphysics v2 is the only viable pure C# 3D physics option for MonoGame. Version verified from GitHub (September 2025 release). Book published in 2024: "MonoGame Physics by Example" specifically covers BEPUphysics v2 integration. **However**, integration requires custom work - no official MonoGame adapter exists.

**Alternative consideration:** For simple arcade gameplay like Berzerk, custom AABB collision may suffice and avoid physics engine complexity.

### Pathfinding & AI

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Roy-T AStar | Latest (NuGet) | 2D/3D pathfinding | Lightweight, pure C#, no external dependencies, targets .NET Standard 1.0+ |
| Custom implementation | N/A | Simple grid-based pathfinding | For room-based maze navigation, A* on grid may be simpler than full navmesh |

**Confidence: HIGH** - Roy-T AStar is well-maintained and suitable for grid-based pathfinding. For Berzerk's room-based gameplay, a simple grid-based A* implementation is industry-standard and sufficient.

**Note:** BrainAI library exists but appears less maintained. For simple enemy AI (chase player, avoid walls), custom state machine may be more appropriate than full AI framework.

### Procedural Generation

| Technology | Version | Purpose | When to Use |
|------------|---------|---------|-------------|
| Custom algorithms | N/A | Room/maze generation | **Recommended** - Berzerk-style room generation is simple grid-based maze generation, well-understood algorithms (DFS, Prim's, etc.) |
| PlayersWorlds.Maps | Latest | Dungeon/maze generation library | If you want pre-built algorithms with features like dead-end markers for loot placement |

**Confidence: MEDIUM** - No MonoGame-specific procedural generation library is standard. PlayersWorlds.Maps targets .NET Framework 4.7.2 and should work with MonoGame but isn't specifically tested. Custom implementation recommended for learning and control.

**Algorithms for Berzerk-style rooms:**
- Recursive backtracker (DFS) for maze generation
- Binary Space Partitioning (BSP) for room placement
- Simple grid-based algorithms are sufficient for 2D room layouts in 3D space

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| MLEM | 8.0.0 | UI system, input helpers | If building menus or HUD - provides mouse/keyboard/gamepad-ready UI |
| ImGui.NET | Latest | Debug UI | Development-time debugging and parameter tweaking |

**Confidence: HIGH** - MLEM is actively maintained MonoGame extension. ImGui.NET is standard for C# debug UI.

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| MonoGame Content Pipeline (MGCB) | Asset compilation | Built-in, .NET 8 local tool, handles FBX, textures, audio |
| Blender | 3D modeling and level editing | Free, open-source, used in MonoGame 3D Platformer Starter Kit (July 2025) |
| Mixamo | Character animations | Free Adobe service for rigging and animation |
| Visual Studio 2022 | IDE | Official MonoGame extension available |

**Confidence: HIGH** - All tools are official recommendations from MonoGame documentation and 3D Platformer Starter Kit.

## Installation

```bash
# Create new MonoGame project (DesktopGL for cross-platform)
dotnet new mgdesktopgl -n Berzerk3D

# Core MonoGame (included in template)
# MonoGame 3.8.4.1 is installed by template

# MonoGame Extensions
dotnet add package MonoGame.Extended --version 5.3.1

# Physics (if using full physics engine)
dotnet add package BEPUphysics --version 2.5.0-beta.27

# Animation (choose one approach)
dotnet add package MonoSkelly  # Standalone animation system
# OR
dotnet add package Aether.Animation  # Mixamo-focused

# Pathfinding
dotnet add package RoyT.AStar --version 3.1.0

# UI/Input helpers
dotnet add package MonoGame.Extended.Input --version 5.3.1

# Dev tools
dotnet add package ImGui.NET --version 1.91.5
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| BEPUphysics v2 | Custom AABB collision | For arcade-style gameplay like Berzerk, custom collision is simpler and may be sufficient |
| MonoSkelly | Custom animation system | If you need very specific animation features or want to minimize dependencies |
| Roy-T AStar | Custom A* implementation | For learning purposes or if you need very specific pathfinding behavior |
| DesktopGL platform | WindowsDX platform | If targeting Windows-only and want DirectX-specific features |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| BEPUphysics v1 | Deprecated, slow, incompatible with newer .NET | BEPUphysics v2 |
| FBX 2014+ format | Not supported by MonoGame Content Pipeline | Convert to FBX 2013 using Autodesk FBX Converter or Blender export |
| .NET Framework | No longer supported by MonoGame 3.8+ | .NET 6+ (currently .NET 8) |
| UWP platform | Removed in MonoGame 3.8.2 | DesktopGL or WindowsDX |
| Visual Studio 2019 | No longer supported by MonoGame 3.8+ | Visual Studio 2022 |
| XNA Content Processors | Outdated for 3D animation | Custom Content Pipeline Extensions or MonoSkelly |

## Stack Patterns by Variant

**If targeting maximum cross-platform compatibility (Windows, Linux, macOS):**
- Use DesktopGL platform (not WindowsDX)
- Use OpenGL/Vulkan render paths
- Test input with Keyboard/Mouse AND GamePad (not all platforms have controllers)

**If prioritizing development speed over physics realism:**
- Skip BEPUphysics, use custom AABB collision
- Simple raycasting for laser projectiles
- Grid-based movement can simplify collision dramatically

**If using Mixamo animations:**
- Export from Mixamo as FBX 2013 format
- Create custom Content Pipeline Extension (AnimatedModelImporter/Processor pattern)
- Consider MonoSkelly as simpler alternative with manual keyframing
- Reference: github.com/Lofionic/MonoGameAnimatedModel for example implementation

**If building room-based procedural generation:**
- 2D grid algorithms (maze generation) applied to 3D rooms
- Generate room layout in 2D, then instantiate 3D geometry
- Keep it simple: DFS maze + enemy spawn points + item placement

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| MonoGame 3.8.4.1 | .NET 8 | Stable release (October 2024) |
| MonoGame 3.8.5-preview.3 | .NET 8 | Preview with DirectX 12 and Vulkan support (January 2025) |
| MonoGame.Extended 5.3.1 | MonoGame 3.8.x | Latest stable (November 2025) |
| BEPUphysics 2.5.0-beta.27 | .NET 8 | Requires RyuJIT compiler for performance |
| MonoSkelly | MonoGame 3.x | Available via NuGet, framework-agnostic |
| Roy-T AStar 3.1.0 | .NET Standard 1.0+ | Works with all modern .NET versions |

**Critical compatibility note:** BEPUphysics v2 requires System.Numerics.Vectors and RyuJIT compiler for good performance. This is standard with .NET 8 but worth noting for optimization.

## MonoGame-Specific Considerations

### Content Pipeline for FBX

MonoGame Content Pipeline supports FBX via three importers:
1. **FbxImporter** - Designed for FBX 2013 format
2. **OpenAssetImporter** - Uses Assimp, supports .fbx, .dae, .3ds, .blend, .obj
3. **XImporter** - DirectX .x format (legacy)

**Recommended workflow for Mixamo:**
1. Download character/animation from Mixamo
2. Export as FBX 2013 (NOT newer versions)
3. Import via MGCB using FbxImporter or OpenAssetImporter
4. Use custom Content Processor to extract bone data and animations
5. Runtime: Custom shader for GPU skinning OR MonoSkelly for simpler approach

### Camera Systems

MonoGame requires manual camera implementation. For third-person camera:

**Option 1: MonoGame.Extended OrthographicCamera** (2D-focused)
- Designed for 2D games but provides good foundation
- Features: smooth following, zoom, rotation, world bounds

**Option 2: Custom 3D Camera** (Recommended for 3D)
- Implement View and Projection matrices manually
- Follow patterns from MonoGame 3D Platformer Starter Kit (July 2025)
- Community examples available for first-person (can adapt to third-person)

**Third-person camera essentials:**
- Position: Player position + offset (behind and above)
- LookAt: Player position
- Smooth interpolation: Lerp camera position over time
- Collision: Raycast to prevent camera clipping through walls

### Platform-Specific Notes

**DesktopGL (recommended for cross-platform):**
- Uses OpenGL on all platforms (Windows, macOS, Linux)
- Vulkan support in 3.8.5-preview
- Can cross-compile builds (build Linux game from Windows)
- Supports Windows 8.1+, macOS Catalina 10.15+, Linux

**WindowsDX (Windows-only optimization):**
- DirectX 9.0c or newer
- XAudio for audio
- Slightly better performance on Windows but locks you to one platform

**Apple Silicon note:** MonoGame 3.8.1+ has native M1+ support for DesktopGL platform.

### Input Handling

MonoGame provides:
- `Keyboard.GetState()` - Key press detection
- `Mouse.GetState()` - Mouse position, buttons, scroll wheel
- `GamePad.GetState(PlayerIndex)` - Controller input

**Pattern for Berzerk controls:**
```csharp
// Keyboard movement (WASD or arrows)
var keyboardState = Keyboard.GetState();
// Mouse aiming (point and shoot)
var mouseState = Mouse.GetState();
// Track previous frame state for "just pressed" detection
bool wasJustPressed = previousState.IsKeyUp(Keys.Space) && currentState.IsKeyDown(Keys.Space);
```

MonoGame.Extended provides InputManager helper to simplify this pattern.

## Architecture Recommendations

For a Berzerk-style 3D game:

**Recommended component structure:**
1. **Game loop** - MonoGame.Game base class
2. **Room Manager** - Procedural generation, room transitions
3. **Entity system** - Player, enemies, projectiles (consider simple ECS or game objects)
4. **Physics/Collision** - AABB for walls/entities, raycasts for projectiles
5. **Animation** - MonoSkelly or custom skinned mesh renderer
6. **AI** - State machines for enemies, A* pathfinding on grid
7. **Camera** - Third-person follow with smooth interpolation
8. **Audio** - MonoGame.Framework.Audio (built-in)

**Keep it simple:** Berzerk is arcade action, not simulation. Prefer simple solutions over complex frameworks.

## Sources

**Official MonoGame:**
- [MonoGame Releases](https://github.com/MonoGame/MonoGame/releases) - Version 3.8.4.1 and 3.8.5 preview info
- [MonoGame 3D Platformer Starter Kit](https://monogame.net/blog/2025-07-16-3d-starter-kit/) - July 2025 official 3D example
- [What's New in MonoGame](https://docs.monogame.net/articles/whats_new.html) - Feature updates
- [Standard Content Importers](https://docs.monogame.net/articles/getting_to_know/whatis/content_pipeline/CP_StdImpsProcs.html) - FBX and 3D model import details
- [Supported Platforms](https://docs.monogame.net/articles/getting_started/platforms.html) - Cross-platform information
- [Input Management Tutorial](https://docs.monogame.net/articles/tutorials/building_2d_games/11_input_management/index.html) - Official input handling guide

**MonoGame.Extended:**
- [MonoGame.Extended January 2025 Update](https://www.monogameextended.net/blog/update-2025-01/) - Version 5.3.1 release info
- [MonoGame.Extended Camera](https://www.monogameextended.net/docs/features/camera/) - Camera system documentation

**Physics:**
- [BEPUphysics v2 GitHub](https://github.com/bepu/bepuphysics2) - Version 2.5.0-beta.27
- [MonoGame Community: 3D Physics Engine](https://community.monogame.net/t/3d-physics-engine/789) - Community discussion
- [Book: MonoGame Physics by Example](https://www.amazon.com/MONOGAME-PHYSICS-EXAMPLE-REALISTIC-BEPUPHYSICS-ebook/dp/B0FSZJ3XHT) - 2024 book on BEPUphysics v2 + MonoGame

**Animation:**
- [MonoSkelly GitHub](https://github.com/RonenNess/MonoSkelly) - Skeletal animation library
- [MonoGameAnimatedModel](https://github.com/Lofionic/MonoGameAnimatedModel) - Example FBX animation implementation
- [MonoGame Community: Mixamo Animations](https://community.monogame.net/t/solved-animation-and-fbx/9342) - FBX animation discussion
- [MonoGame Community: Skeletal Animation State](https://community.monogame.net/t/state-of-skeleton-animation-in-monogame/20121) - Community overview

**Pathfinding & AI:**
- [MonoGame Community: A* Pathfinding](https://community.monogame.net/t/a-pathfinding-examples/8220) - Examples and discussion
- [RogueSharp Tutorial](https://roguesharp.wordpress.com/2014/06/09/tutorial-4-roguelike-pathfinding-using-roguesharp-and-monogame/) - Grid-based pathfinding
- [Awesome MonoGame](https://github.com/aloisdeniel/awesome-monogame) - Curated library list including BrainAI and Roy-T AStar

**Procedural Generation:**
- [MonoGame Maze Generator Gist](https://gist.github.com/ambs/37147a7fa8f0b01f758142f95ddd2620) - Simple maze generation
- [DungeonRaider GitHub](https://github.com/ChrisPritchard/DungeonRaider) - F# procedural dungeon example
- [MonoGame Community: Procedural Dungeon](https://community.monogame.net/t/procedurally-generated-rooms-binding-of-isaac-style/16019) - Isaac-style room generation

**General Resources:**
- [MonoGame Community Discussions](https://community.monogame.net/) - Active community
- [GameFromScratch MonoGame Tutorials](https://gamefromscratch.com/monogame-tutorial-beginning-3d-programming/) - 3D programming tutorials

---
*Stack research for: Berzerk 3D - MonoGame 3D action game*
*Researched: 2026-01-31*
