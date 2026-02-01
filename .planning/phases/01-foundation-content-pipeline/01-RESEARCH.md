# Phase 1: Foundation & Content Pipeline - Research

**Researched:** 2026-01-31
**Domain:** MonoGame 3D game development with FBX animation import
**Confidence:** MEDIUM

## Summary

MonoGame 3.8.4.1 with .NET 8 provides a mature cross-platform game framework with built-in FBX import capabilities through the Content Pipeline. However, importing Mixamo FBX animations specifically is a known technical challenge in the MonoGame ecosystem. The framework's FBX importer uses AssimpNetter (Assimp 5.x as of 3.8.4), which supports FBX 2013 format but has historically struggled with Mixamo's skeletal animation exports.

The standard MonoGame Content Pipeline workflow processes FBX files at build-time, converting them into optimized XNB binary format. For basic static models this works well, but animated skinned meshes from Mixamo often require custom Content Pipeline processors to handle bone transforms and animation data correctly. Community solutions exist (XnaMixamoImporter, MonoGameAnimatedModel) that demonstrate working approaches, though they typically require multi-step conversion workflows using Blender and the Autodesk FBX Converter.

The core technical risk identified in project context is validated: FBX animation import from Mixamo is indeed the highest complexity item. Standard project setup, input handling, and basic 3D rendering are well-documented with stable APIs.

**Primary recommendation:** Start with the standard MonoGame template and Content Pipeline for project foundation, but plan for custom Content Pipeline processor development to handle Mixamo animations. Use verbose logging and fail-fast build configuration to surface FBX import issues immediately during development.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.4.1 | Cross-platform game framework | Official MonoGame framework for desktop targets (Windows/Linux/macOS) |
| .NET SDK | 9.0 (min: 8.0) | Runtime and build tooling | MonoGame 3.8.4.1 recommends .NET 9, supports .NET 10, minimum .NET 8 |
| MonoGame.Content.Builder.Task | 3.8.4.1 | MSBuild integration for content pipeline | Automatically builds .mgcb content files during project build |
| MonoGame.Framework.Content.Pipeline | 3.8.4.1 | Content importers and processors | Required for custom content pipeline extensions |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Wine (macOS) | Stable | DirectX shader compilation on macOS | Required for effect (.fx) compilation on non-Windows platforms |
| Autodesk FBX Converter | 2013.3 | FBX format conversion | When Mixamo FBX files need conversion to compatible versions |
| AssimpNetter | 5.x | 3D model import library | Bundled with MonoGame, used internally by FbxImporter |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| DesktopGL | WindowsDX | DirectX offers better performance on Windows but loses Linux/macOS support |
| .NET 9 | .NET 8 | .NET 8 minimum supported but .NET 9 recommended for latest features |
| MonoGame templates | Manual setup | Templates provide correct project structure and references automatically |

**Installation:**
```bash
# Install .NET SDK 9.0 (or minimum .NET 8)
# Download from: https://dotnet.microsoft.com/download

# Install MonoGame templates
dotnet new install MonoGame.Templates.CSharp

# Create new MonoGame project
dotnet new mgdesktopgl -n YourProjectName

# On macOS: Install Wine for shader compilation
brew install wget p7zip curl && brew install --cask wine-stable
wget -qO- https://monogame.net/downloads/net9_mgfxc_wine_setup.sh | bash
```

## Architecture Patterns

### Recommended Project Structure
```
YourProject/
├── YourProject.csproj              # Main game project
├── Content/
│   ├── Content.mgcb                # Content pipeline project file
│   ├── Models/                     # 3D models and animations
│   │   ├── Characters/
│   │   │   ├── character.fbx
│   │   │   └── Animations/         # Optional: separate animation files
│   │   └── Textures/               # Model textures (if external)
│   ├── Effects/                    # Custom shaders (.fx files)
│   └── Fonts/                      # SpriteFont files
├── YourProject.ContentPipeline/    # Custom content pipeline extension (if needed)
│   ├── YourProject.ContentPipeline.csproj
│   ├── Importers/                  # Custom importers
│   ├── Processors/                 # Custom processors
│   ├── Writers/                    # Custom content writers
│   └── Readers/                    # Runtime content readers
├── Source/
│   ├── Game1.cs                    # Main game class (rename from template)
│   ├── Content/                    # Content management
│   ├── Input/                      # Input handling
│   ├── Graphics/                   # Rendering systems
│   └── Models/                     # Runtime model/animation classes
└── .config/
    └── dotnet-tools.json           # MGCB CLI tool manifest
```

### Pattern 1: Content Pipeline Extension for Custom Formats
**What:** Separate class library project that extends MonoGame's content pipeline with custom importers, processors, writers, and readers.

**When to use:** When standard FBX importer doesn't handle your content correctly (e.g., Mixamo animations), or when using custom file formats.

**Structure:**
```
ContentPipeline/
├── [YourType]Importer.cs          # Inherits ContentImporter<T>
├── [YourType]Processor.cs         # Inherits ContentProcessor<TInput, TOutput>
├── [YourType]Writer.cs            # Inherits ContentTypeWriter<T>
└── [YourType]Reader.cs            # Inherits ContentTypeReader<T> (in main project)
```

**Key requirements:**
- Target "Any CPU" (MonoGame doesn't support x86 assemblies in content pipeline)
- Reference MonoGame.Framework.Content.Pipeline
- Add `/reference` to your extension DLL in Content.mgcb file
- Importer returns intermediate DOM (NodeContent, etc.)
- Processor validates and transforms intermediate content
- Writer serializes to XNB format
- Reader (runtime) deserializes and constructs game objects

**Example:**
```csharp
// Source: Based on MonoGame documentation and badecho.com tutorial
// Simplified processor example
[ContentProcessor(DisplayName = "Mixamo Animation Processor")]
public class MixamoAnimationProcessor : ContentProcessor<NodeContent, ModelContent>
{
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        // Validate skeleton structure
        BoneContent skeleton = MeshHelper.FindSkeleton(input);
        if (skeleton == null)
        {
            context.Logger.LogWarning(null, null,
                "No skeleton found in model. Animation may not work correctly.");
        }

        // Process with standard ModelProcessor as base
        ModelProcessor modelProcessor = new ModelProcessor();
        ModelContent model = modelProcessor.Process(input, context);

        // Custom processing for Mixamo-specific issues here
        // (e.g., bone transform corrections, animation clip extraction)

        return model;
    }
}
```

### Pattern 2: Input State Management
**What:** Track previous and current input states to detect button press/release events in a polling-based system.

**When to use:** Always - MonoGame input is polling-based, not event-driven.

**Example:**
```csharp
// Source: MonoGame documentation - Chapter 10: Handling Input
public class InputManager
{
    private KeyboardState _previousKeyboardState;
    private KeyboardState _currentKeyboardState;
    private MouseState _previousMouseState;
    private MouseState _currentMouseState;

    public void Update()
    {
        // Store previous frame's state
        _previousKeyboardState = _currentKeyboardState;
        _previousMouseState = _currentMouseState;

        // Get current frame's state
        _currentKeyboardState = Keyboard.GetState();
        _currentMouseState = Mouse.GetState();
    }

    public bool IsKeyPressed(Keys key)
    {
        // True only on the frame the key goes from up to down
        return _currentKeyboardState.IsKeyDown(key) &&
               _previousKeyboardState.IsKeyUp(key);
    }

    public bool IsKeyHeld(Keys key)
    {
        // True while key is down
        return _currentKeyboardState.IsKeyDown(key);
    }
}
```

### Pattern 3: Model Loading and Rendering with BasicEffect
**What:** Standard approach for loading and rendering 3D models processed through the content pipeline.

**When to use:** For models with BasicEffect (default for FBX imports).

**Example:**
```csharp
// Source: MonoGame documentation - How to Render a Model
public class ModelRenderer
{
    private Model _model;
    private Matrix[] _boneTransforms;

    public void LoadContent(ContentManager content, string modelName)
    {
        _model = content.Load<Model>(modelName);
        _boneTransforms = new Matrix[_model.Bones.Count];
    }

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        // Copy bone transforms (required even for non-animated models)
        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);

        // Draw each mesh in the model
        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;

                effect.World = _boneTransforms[mesh.ParentBone.Index] * world;
                effect.View = view;
                effect.Projection = projection;
            }

            mesh.Draw();
        }
    }
}
```

### Anti-Patterns to Avoid
- **Calling GetState() multiple times per frame:** Cache input states in variables at the start of Update()
- **Not copying bone transforms:** Even static models need CopyAbsoluteBoneTransformsTo() before rendering
- **Renaming Game1.cs during development:** Do this immediately after project creation, not mid-development
- **Hardcoding content paths:** Use ContentManager.Load<T>("path") consistently, not manual file I/O
- **Ignoring content build errors:** FBX import failures should fail the build, not be ignored
- **Using relative paths in .mgcb files:** Content Pipeline handles path resolution; use project-relative paths

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| FBX format conversion | Custom FBX parser | Autodesk FBX Converter + Assimp/MonoGame FbxImporter | FBX is a complex binary format with many versions; Assimp handles this |
| Skeletal animation blending | Manual bone interpolation | Custom content processor + existing examples (MonoGameAnimatedModel) | Bone hierarchies, quaternion interpolation, and skinning math are error-prone |
| Input event system | Custom event dispatcher | MonoGame's polling system + state tracking pattern | Polling is MonoGame's design; adding events is extra complexity unless needed |
| Content optimization | Manual texture/model compression | MonoGame Content Pipeline | Pipeline automatically handles platform-specific optimization (DXT, PVRTC, etc.) |
| Shader cross-compilation | Separate HLSL/GLSL shaders | MGFX effect compiler | MGFX compiles HLSL to both DirectX and OpenGL at build time |
| Content file format | XML/JSON + manual parsing | Content Pipeline with custom processor | Pipeline provides dependency tracking, incremental builds, and versioning |

**Key insight:** MonoGame's Content Pipeline is designed to handle complex build-time transformations. Custom runtime solutions for content processing sacrifice performance, increase file sizes, and lose cross-platform optimization benefits. When the standard pipeline doesn't work (e.g., Mixamo animations), extend the pipeline rather than bypass it.

## Common Pitfalls

### Pitfall 1: FBX Import Failures with Mixamo Models
**What goes wrong:** FBX files from Mixamo fail to import with cryptic Assimp errors, or import but render with distorted/broken animations.

**Why it happens:**
- Mixamo exports FBX 2013+ format with specific bone hierarchy conventions
- MonoGame's AssimpNetter has known compatibility issues with Mixamo's skeletal structure
- Bone transforms may not match what BasicEffect's skinning expects
- Animation clips are embedded in the FBX, but MonoGame doesn't expose clip extraction by default

**How to avoid:**
- Test FBX import with a simple Mixamo character immediately (don't assume it works)
- Add verbose logging in content build to see exact Assimp errors
- Plan for custom content processor development from the start
- Consider multi-step conversion: Mixamo FBX → Blender → FBX 2011/2012 → MonoGame
- Reference existing solutions (XnaMixamoImporter) for conversion approaches

**Warning signs:**
- Content build succeeds but model doesn't render
- Model renders but animations cause extreme bone stretching/distortion
- "Unable to find skeleton" errors during content build
- Black or invisible model when rendered

### Pitfall 2: Content Not Rebuilding When Expected
**What goes wrong:** Changes to FBX files or content processor code don't trigger content rebuild, causing stale content to be used.

**Why it happens:**
- Content Pipeline uses incremental builds based on file timestamps and dependency tracking
- Custom processor assembly changes don't always invalidate content cache
- MGCB Editor may cache old processor versions
- Source file modifications outside the IDE may not update timestamps correctly

**How to avoid:**
- Use `/rebuild` flag during development to force full content rebuild
- Add verbose logging to track when content is rebuilt vs. cached
- Restart MGCB Editor when changing custom processor code
- In .csproj, ensure MonoGameContentBuilderExitOnError is true to catch issues
- Consider using "Rebuild" instead of "Build" during active FBX work

**Warning signs:**
- Code changes don't appear in running game
- FBX modifications don't affect rendered model
- Content build completes in <1 second when it should process files
- Outdated textures or animations appear after model updates

### Pitfall 3: Platform-Specific Content Pipeline Issues on macOS
**What goes wrong:** Content builds successfully on Windows but fails on macOS, or vice versa.

**Why it happens:**
- Effect (.fx) compilation requires Wine setup on macOS (DirectX → OpenGL translation)
- File path case sensitivity on macOS vs. Windows
- Different native library dependencies (e.g., libminizip on Linux)
- AssimpNetter native binaries may have platform-specific bugs

**How to avoid:**
- Complete Wine setup for shader compilation before importing effects
- Use consistent file path casing in .mgcb files
- Test content pipeline on target platform early (don't assume cross-platform)
- Keep content file paths lowercase to avoid case-sensitivity issues
- Check MGCB output directory for .xnb files to verify successful build

**Warning signs:**
- "Effect compilation failed" on macOS with no Wine setup
- "File not found" errors that don't occur on Windows
- Native library errors (libminizip.so, etc.) on Linux
- Content builds but runtime ContentManager.Load() fails

### Pitfall 4: Incorrect BasicEffect Setup for 3D Models
**What goes wrong:** Models render as solid black, overly bright, or with incorrect shading.

**Why it happens:**
- BasicEffect requires explicit lighting setup (EnableDefaultLighting)
- World/View/Projection matrices must be set for each effect instance
- Bone transforms must be copied even for static models
- Model processor may corrupt normals, breaking lighting

**How to avoid:**
- Always call EnableDefaultLighting() on BasicEffect instances
- Set effect.PreferPerPixelLighting = true for better quality
- Verify camera view and projection matrices are correct
- Copy absolute bone transforms before rendering each frame
- Inspect model in MGCB Editor 3D preview to verify it processes correctly

**Warning signs:**
- Model renders as solid black silhouette
- Lighting appears flat/uniform across entire model
- Model disappears when camera moves
- Model renders but textures are missing

### Pitfall 5: Input Polling vs. Event Confusion
**What goes wrong:** Attempting to use event-driven input handling, or missing single-frame button presses.

**Why it happens:**
- MonoGame uses polling (GetState()) not events
- Developers coming from UI frameworks expect events
- Single-frame key presses can be missed without state tracking
- Calling GetState() in multiple places causes frame-inconsistency

**How to avoid:**
- Accept polling model: store states in Update(), check in logic
- Track previous frame state to detect press/release transitions
- Create input manager to centralize state tracking
- Call GetState() once per frame, cache in variables
- Use IsKeyPressed pattern (was up, now down) for single-press actions

**Warning signs:**
- Input feels "laggy" or unresponsive
- Jump/shoot actions trigger multiple times from single button press
- Input behavior differs between 60fps and 30fps
- Input checks in different classes disagree about key state

## Code Examples

Verified patterns from official sources:

### Content Pipeline Custom Processor Template
```csharp
// Source: MonoGame documentation + badecho.com tutorial
// Location: YourProject.ContentPipeline/MixamoModelProcessor.cs

using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

[ContentProcessor(DisplayName = "Mixamo Model Processor")]
public class MixamoModelProcessor : ModelProcessor
{
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        // Log processing for debugging
        context.Logger.LogMessage("Processing Mixamo model: {0}", input.Name);

        // Validate skeleton exists for animated models
        BoneContent skeleton = MeshHelper.FindSkeleton(input);
        if (skeleton == null)
        {
            context.Logger.LogWarning(null, null,
                "No skeleton found in model '{0}'. Model may not be animated correctly.",
                input.Name);
        }
        else
        {
            context.Logger.LogMessage("Found skeleton: {0} with {1} bones",
                skeleton.Name, CountBones(skeleton));
        }

        // Call base ModelProcessor
        ModelContent model = base.Process(input, context);

        // Additional Mixamo-specific processing here
        // (e.g., bone transform corrections, animation extraction)

        return model;
    }

    private int CountBones(BoneContent bone)
    {
        int count = 1;
        foreach (BoneContent child in bone.Children.OfType<BoneContent>())
        {
            count += CountBones(child);
        }
        return count;
    }
}
```

### Loading and Rendering Animated Model
```csharp
// Source: MonoGame documentation - How to Render a Model
// Location: YourProject/Source/Graphics/AnimatedModel.cs

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

public class AnimatedModel
{
    private Model _model;
    private Matrix[] _boneTransforms;

    // Animation state
    private AnimationClip _currentClip;
    private TimeSpan _animationTime;

    public void LoadContent(ContentManager content, string modelPath)
    {
        _model = content.Load<Model>(modelPath);
        _boneTransforms = new Matrix[_model.Bones.Count];

        // Initialize with default pose
        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
    }

    public void Update(GameTime gameTime)
    {
        if (_currentClip != null)
        {
            // Update animation time
            _animationTime += gameTime.ElapsedGameTime;

            // Loop animation
            while (_animationTime >= _currentClip.Duration)
            {
                _animationTime -= _currentClip.Duration;
            }

            // Apply animation transforms to bones
            // (This is simplified - real implementation needs clip data structure)
            UpdateBoneTransforms(_animationTime);
        }
    }

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        foreach (ModelMesh mesh in _model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                // Setup lighting
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;

                // Setup transforms
                effect.World = _boneTransforms[mesh.ParentBone.Index] * world;
                effect.View = view;
                effect.Projection = projection;
            }

            mesh.Draw();
        }
    }

    private void UpdateBoneTransforms(TimeSpan time)
    {
        // Simplified - real implementation needs animation clip keyframes
        // For now, just use default pose
        _model.CopyAbsoluteBoneTransformsTo(_boneTransforms);
    }
}
```

### Input Manager with State Tracking
```csharp
// Source: MonoGame documentation - Chapter 10 & 11: Input Handling
// Location: YourProject/Source/Input/InputManager.cs

using Microsoft.Xna.Framework.Input;

public class InputManager
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    public void Update()
    {
        // Shift current to previous
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        // Sample new state (once per frame)
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    // Key was just pressed this frame
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    // Key is being held down
    public bool IsKeyHeld(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key);
    }

    // Key was just released this frame
    public bool IsKeyReleased(Keys key)
    {
        return _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);
    }

    // Mouse button pressed this frame
    public bool IsLeftMousePressed()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed &&
               _previousMouse.LeftButton == ButtonState.Released;
    }

    // Mouse position
    public Point MousePosition => _currentMouse.Position;

    // Mouse delta (for camera control)
    public Point MouseDelta => new Point(
        _currentMouse.X - _previousMouse.X,
        _currentMouse.Y - _previousMouse.Y
    );
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| AssimpNet (Assimp 4.x) | AssimpNetter (Assimp 5.x) | MonoGame 3.8.4 | Breaking changes for some FBX models; improved format support but new compatibility issues |
| Global MGCB tool | Local dotnet tool (per-project) | MonoGame 3.8.2 | Each project can use different MonoGame versions; better version control |
| .NET Framework / Mono | .NET 8+ | MonoGame 3.8.2/3.8.3 | True cross-platform; better performance; modern C# features |
| UWP platform support | UWP deprecated | MonoGame 3.8.2 | Microsoft reducing UWP support; focus on Desktop/Mobile platforms |
| Manual content building | MSBuild integration | MonoGame 3.8+ | Content builds automatically with project; better CI/CD support |

**Deprecated/outdated:**
- **XNA 4.0 content pipeline:** MonoGame's pipeline is similar but has breaking changes; don't assume XNA tutorials work directly
- **FBX 2011 and older:** Assimp doesn't support very old FBX versions; Mixamo uses 2013+ which is supported
- **Global `mgcb` install:** Now installed per-project via dotnet-tools.json manifest
- **MonoGame 3.6 FBX approach:** 3.8.4's AssimpNetter change breaks some 3.6-era workarounds

## Open Questions

Things that couldn't be fully resolved:

1. **Exact Mixamo FBX compatibility in MonoGame 3.8.4.1**
   - What we know: MonoGame 3.8.4 uses AssimpNetter (Assimp 5.x) which supports FBX 2013 format. Community reports issues with Mixamo animations causing distorted bones or import failures.
   - What's unclear: Whether specific Mixamo export settings can avoid issues, or if custom processor is always required. Official MonoGame docs don't address Mixamo specifically.
   - Recommendation: Plan for custom content processor. Test with simple Mixamo character immediately to validate import. Have fallback plan for Blender-based conversion workflow if direct import fails.

2. **Animation clip extraction from FBX**
   - What we know: MonoGame's Model class contains Tag property where animation data can be stored. Standard ModelProcessor doesn't expose multiple animation clips from a single FBX file.
   - What's unclear: Whether MonoGame 3.8.4.1 has built-in support for multiple animation clips, or if this requires custom processor + reader.
   - Recommendation: Investigate Model.Tag structure after importing test Mixamo FBX. Assume custom processor will be needed to extract named animation clips into usable runtime format.

3. **Content rebuild behavior configuration**
   - What we know: MonoGame Content Pipeline uses incremental builds by default. `/rebuild` flag forces full rebuild. Some users report unexpected rebuilds on every build.
   - What's unclear: How to configure "always rebuild" behavior without using `/rebuild` flag manually, and whether this impacts build performance significantly.
   - Recommendation: Use MSBuild properties or MGCB command-line args in .csproj to set rebuild behavior. Monitor build times to ensure acceptable (<5 seconds for small content).

4. **Verbose logging configuration**
   - What we know: ContentBuildLogger provides LogMessage/LogWarning methods for custom processors. Standard FBX import errors are logged.
   - What's unclear: How to enable maximum verbosity for built-in importers (especially FbxImporter) to see detailed Assimp processing.
   - Recommendation: Add custom processor that wraps standard ModelProcessor and logs all intermediate NodeContent structure. This provides visibility into what FbxImporter actually produced.

## Sources

### Primary (HIGH confidence)
- [MonoGame Official Documentation - What's New](https://docs.monogame.net/articles/whats_new.html) - Version history and changes
- [MonoGame Official Documentation - FbxImporter API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.Pipeline.FbxImporter.html) - FBX importer capabilities
- [MonoGame Official Documentation - Content Pipeline Overview](https://docs.monogame.net/articles/getting_to_know/whatis/content_pipeline/CP_Overview.html) - Pipeline architecture
- [MonoGame Official Documentation - Why Content Pipeline](https://docs.monogame.net/articles/getting_started/content_pipeline/why_content_pipeline.html) - Pipeline benefits
- [MonoGame Official Documentation - Custom Processor Tips](https://docs.monogame.net/articles/getting_to_know/whatis/content_pipeline/CP_Tips_For_Developing.html) - Development best practices
- [MonoGame Official Documentation - Render Model](https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_RenderModel.html) - BasicEffect rendering
- [MonoGame Official Documentation - macOS Setup](https://docs.monogame.net/articles/getting_started/1_setting_up_your_os_for_development_macos.html) - Platform requirements
- [MonoGame Official Documentation - Input Handling](https://docs.monogame.net/articles/tutorials/building_2d_games/10_handling_input/) - Input patterns
- [MonoGame GitHub CHANGELOG](https://github.com/MonoGame/MonoGame/blob/develop/CHANGELOG.md) - Official release notes

### Secondary (MEDIUM confidence)
- [Omni's Hackpad - Extending Pipeline](https://badecho.com/index.php/2022/08/17/extending-pipeline/) - Comprehensive custom processor tutorial (2022)
- [XnaMixamoImporter GitHub](https://github.com/BaamStudios/XnaMixamoImporter) - Community solution for Mixamo imports
- [MonoGame Community Forums - Mixamo Issues](https://community.monogame.net/t/solved-animation-and-fbx/9342) - Community troubleshooting
- [MonoGame Community Forums - Content Pipeline Extension Tutorial](https://community.monogame.net/t/monogame-content-pipeline-extension-tutorial/19682) - Community tutorial (2023)
- [Digital Rune Documentation - FBX Support](https://digitalrune.github.io/DigitalRune-Documentation/html/6cc96ddc-4127-4ec7-889a-19cb71af0d3d.htm) - Assimp FBX version details

### Tertiary (LOW confidence - requires validation)
- Web search results about AssimpNetter vs AssimpNet change - community reports, not official documentation
- Web search results about specific Mixamo import issues - anecdotal, no official confirmation
- Community forum posts about content rebuild behavior - varying experiences, needs testing

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official MonoGame documentation confirms versions, templates, and .NET requirements
- Architecture: MEDIUM - Official docs provide patterns, but Mixamo-specific approaches are from community sources
- Pitfalls: MEDIUM - Identified issues confirmed by multiple community reports, but not all officially documented

**Research date:** 2026-01-31
**Valid until:** 2026-03-31 (60 days) - MonoGame is stable; FBX import issues are longstanding, not rapidly changing
