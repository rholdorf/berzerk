# Stack Research

**Domain:** MonoGame 3D Skeletal Animation with Mixamo FBX Assets
**Researched:** 2026-02-09
**Confidence:** MEDIUM -- MonoGame's 3D animation story is under-documented; recommendations synthesized from community consensus, official docs, and verified library versions.

## Executive Summary

MonoGame does **not** ship a built-in runtime animation system. The framework provides only the plumbing: an FBX importer (via Assimp internally), `ModelProcessor` for content pipeline processing, `SkinnedEffect` for GPU skinning (max 72 bones), and bone weight vertex channel conversion. Everything else -- animation clip extraction, bone transform interpolation, animation playback -- must be implemented by the developer or sourced from community libraries.

The Mixamo-to-MonoGame pipeline has **two proven approaches** with different tradeoff profiles:

1. **Approach A (Recommended): Custom Content Pipeline Processor** based on the XNA SkinnedModel sample architecture. Process Mixamo FBX files through MonoGame's Content Pipeline with a custom processor that extracts animation data into `Model.Tag`. This is the most MonoGame-native approach, avoids runtime dependencies, and produces optimized XNB files.

2. **Approach B (Alternative): Runtime glTF loading via SharpGLTF.** Convert Mixamo FBX to glTF in Blender, load at runtime with SharpGLTF. Bypasses the Content Pipeline entirely. More robust format handling but requires Blender as a preprocessing step and loses Content Pipeline benefits.

**Critical insight:** The project already has the correct architecture (Approach A) with `MixamoModelProcessor`, `AnimationData`, `AnimationClip`, `Keyframe`, and `AnimatedModel`. The T-pose issue is almost certainly a bug in how bone transforms are applied at runtime, not a fundamental architecture problem.

## Recommended Stack

### Core Framework

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| MonoGame (DesktopGL) | 3.8.4.1 | Game framework | Current stable release. The project already uses `3.8.*` wildcard which resolves to this. Includes Assimp 5.x internally (via AssimpNetter) for FBX import. DesktopGL is correct for macOS. | HIGH |
| .NET | 8.0 | Runtime | MonoGame 3.8.2+ targets .NET 8. The project already uses `net8.0`. Stable and well-supported. | HIGH |
| MonoGame Content Pipeline | 3.8.4.1 | Asset processing | Processes FBX files into optimized XNB format at build time. Built-in `FbxImporter` handles Mixamo FBX 7.4 binary format. Custom processors extend `ModelProcessor`. | HIGH |

### GPU Skinning

| Technology | Version | Purpose | Why Recommended | Confidence |
|------------|---------|---------|-----------------|------------|
| `SkinnedEffect` | Built-in (3.8.4.1) | GPU skeletal animation rendering | MonoGame's built-in shader for hardware-accelerated bone-weighted vertex skinning. Supports up to **72 bones** (Mixamo uses ~65 bones standard -- fits within limit). Supports 1, 2, or 4 weights per vertex, 3 directional lights, fog, per-pixel lighting. **Must replace `BasicEffect` in the current code.** | HIGH |

### Content Pipeline Extension (Custom -- Already Exists)

| Component | Location | Purpose | Status |
|-----------|----------|---------|--------|
| `MixamoModelProcessor` | `Berzerk.ContentPipeline/` | Custom processor extending `ModelProcessor`. Extracts bone hierarchy and animation keyframes from FBX, builds `AnimationData`, attaches to `Model.Tag`. | Exists but needs fixes |
| `AnimationData` / `AnimationClip` / `Keyframe` | `Berzerk.ContentPipeline/` | Data structures for serialized animation data. Writer serializes to XNB. | Exists |
| `AnimationDataReader` | `Berzerk/Source/Content/` | Runtime deserialization of animation data from XNB files. | Exists |
| `AnimatedModel` | `Berzerk/Source/Graphics/` | Runtime animation playback: clip management, keyframe interpolation, bone transform composition. | Exists but T-pose bug |

### Supporting Libraries

| Library | Version | Purpose | When to Use | Confidence |
|---------|---------|---------|-------------|------------|
| SharpGLTF.Core | 1.0.6 | glTF 2.0 file reading/writing | Only if switching to Approach B (glTF pipeline). Not needed for Approach A. | HIGH (version verified) |
| SharpGLTF.Runtime | 1.0.5 | Runtime scene evaluation for glTF | Only if switching to Approach B. Provides animation evaluation. | HIGH (version verified) |
| Blender | 4.2 LTS or 5.0.1 | 3D asset preprocessing/conversion | FBX re-export, glTF conversion, animation baking, debugging bone hierarchies. Recommended as a diagnostic tool even for Approach A. | HIGH (version verified) |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| MGCB Editor | Content Pipeline management | Built into MonoGame SDK. Configure `.mgcb` files, set processor references, build content. Already configured in `Content.mgcb`. |
| Blender 4.2 LTS / 5.0.1 | FBX inspection and preprocessing | Import Mixamo FBX to inspect bone hierarchy, verify animation data exists, optionally re-export. Use **FBX 7.4 Binary** export. Do NOT enable "Apply Transform" (corrupts animations). |
| Visual Studio 2022 / Rider | IDE | Required for .NET 8 + MonoGame 3.8.x. VS Code with C# Dev Kit also works. |

## Architecture Decision: Approach A vs Approach B

### Approach A: Custom Content Pipeline Processor (RECOMMENDED)

**How it works:**
```
Mixamo FBX --> FbxImporter --> MixamoModelProcessor --> XNB file --> Runtime Model + AnimationData
```

**Why recommended:**
- The project **already has this architecture built**. The bones of the system exist.
- Content Pipeline processes at build time, producing optimized binary XNB files.
- No runtime file I/O or parsing overhead.
- MonoGame-native: uses `Model`, `ModelMesh`, `ModelBone` -- standard MonoGame types.
- Separate model + animation FBX files (Mixamo's export pattern) are handled by loading each FBX and merging `AnimationData` at runtime (already implemented in `AnimatedModel.AddAnimationsFrom()`).

**What needs fixing (the T-pose bug):**
The current `AnimatedModel` uses `BasicEffect` for rendering (line 128: `foreach (BasicEffect effect in mesh.Effects)`). `BasicEffect` does **not** support bone transforms -- it renders the mesh in bind pose (T-pose) regardless of animation state. **The fix is to use `SkinnedEffect`** and call `SetBoneTransforms()` with the animated bone matrices each frame.

Additionally, the bone transform computation in `ApplyKeyframes()` needs to produce the correct "bone palette" format that `SkinnedEffect` expects: `inverseBindPose[i] * worldTransform[i]` for each bone, not just the world transforms.

**Confidence:** HIGH that this is the correct approach. The XNA SkinnedModel sample (canonical reference) uses exactly this pattern.

### Approach B: Runtime glTF via SharpGLTF (ALTERNATIVE)

**How it works:**
```
Mixamo FBX --> Blender (re-export) --> glTF/GLB --> SharpGLTF (runtime load) --> Custom rendering
```

**Why it's a valid alternative:**
- glTF is an open, well-documented format. SharpGLTF handles it reliably.
- Bypasses MonoGame's sometimes-buggy Assimp-based FBX importer entirely.
- MonoScene (by the SharpGLTF author) provides a working example with SkinnedEffect.
- Better for projects that need PBR materials or complex scene graphs.

**Why NOT recommended for this project:**
- Requires Blender as a mandatory preprocessing step for every asset.
- Loses Content Pipeline benefits (build-time validation, XNB optimization, incremental builds).
- Requires building a completely new model loading/rendering system from scratch.
- The project already has a working Content Pipeline architecture that just needs bug fixes.
- MonoScene is in maintenance-only mode (author stopped active development).
- glTF cannot be imported through MonoGame's Content Pipeline (confirmed by MonoGame issue #6932).

**Confidence:** MEDIUM. Works reliably but is the wrong tool for this project's situation.

## Alternatives Considered

| Recommended | Alternative | Why Not |
|-------------|-------------|---------|
| Custom Content Pipeline Processor | MonoSkelly (custom INI-based system) | Completely different asset format. Would need to convert all Mixamo FBX to custom INI skeleton format. Decoupled from MonoGame's Model system. Not appropriate when FBX pipeline already exists. |
| Custom Content Pipeline Processor | Lovebirb.MonoGame.SkinnedMeshImporter (NuGet) | Last updated May 2023 (v1.0.4). Tested only with Blender 2.93 exports. Does not load textures. Could be useful as a reference implementation but too stale to depend on directly. |
| SkinnedEffect (built-in) | Custom HLSL skinning shader | Premature complexity. SkinnedEffect handles 72 bones, 4 weights/vertex, 3 lights. Sufficient for Mixamo characters. Custom shader only needed if requiring PBR or >72 bones. |
| Fix existing MixamoModelProcessor | Port XNA SkinnedModel sample wholesale | The existing processor already does most of what the XNA sample does. Porting the full sample would mean throwing away working code. Better to identify and fix the specific bugs. |
| MonoGame 3.8.4.1 (stable) | MonoGame 3.8.5-preview.2 | Preview release (Jan 2026). Includes new content builder, Vulkan/DX12 previews. Too unstable for production. Stick with 3.8.4.1 stable. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| `BasicEffect` for skinned models | Does not support bone transforms. Renders bind pose (T-pose) only. **This is the primary cause of the current T-pose bug.** | `SkinnedEffect` -- built-in, supports `SetBoneTransforms()` for GPU skinning. |
| Direct `Model.CopyAbsoluteBoneTransformsTo()` for animation | This copies the bind pose transforms, not animated transforms. Only useful for static model rendering. | Compute animated bone palette manually: `inverseBindPose[i] * animatedWorldTransform[i]` per frame. |
| Mixamo "FBX for Unity" export format | Deprecated by Mixamo (removed after FBX 2019 update). Previously included Unity-specific root motion data. No longer available. | "FBX Binary (.fbx)" format from Mixamo download dialog. |
| Mixamo FBX ASCII format | Larger file size. Some importers have issues with ASCII FBX. MonoGame's Assimp-based importer works better with binary. | "FBX Binary (.fbx)" -- always select binary. |
| `AssimpNet` NuGet (standalone) | Version 4.1.0 wraps Assimp 4.x. MonoGame 3.8.4 already uses Assimp 5.x internally via AssimpNetter. Adding standalone AssimpNet creates version conflicts and is redundant. | Use MonoGame's built-in FBX importer (which uses Assimp internally). For direct Assimp access, use `AssimpNetter` 6.0.2.1 (Oct 2025). |
| XnaMixamoImporter | Archived (2022). Required Blender 2.49b + 2.65 (ancient versions). Used Collada intermediate format. Completely obsolete. | Custom Content Pipeline Processor (already built in the project). |
| MonoGame 3.8.5-preview | Unstable preview. Major internal changes (new content builder, Vulkan/DX12). Not suitable for production work. | MonoGame 3.8.4.1 stable. |

## Mixamo-Specific Guidance

### Export Settings from Mixamo.com

| Setting | Value | Rationale |
|---------|-------|-----------|
| Format | **FBX Binary (.fbx)** | Binary is more compact and better supported by Assimp. |
| Skin | **With Skin** (for character model) | Includes mesh + skeleton + skin weights. Required for the base character. |
| Skin | **Without Skin** (for animation-only files) | Animation-only FBX files are smaller. Skeleton hierarchy + animation channels only. |
| Frames per Second | **30** | Standard. Matches Mixamo default. Higher values increase file size without visible quality improvement for game use. |
| Keyframe Reduction | **none** | Preserves all keyframes. Reduction can cause interpolation artifacts. Optimize later if needed. |

### Mixamo Bone Naming Convention

Mixamo bones use the `mixamorig:` prefix (e.g., `mixamorig:Hips`, `mixamorig:Spine`, `mixamorig:LeftArm`). Key facts:

- **Standard skeleton:** ~65 bones (varies slightly based on character finger detection)
- **Root bone:** `mixamorig:Hips` -- Mixamo does NOT include a separate root bone above Hips
- **Bone hierarchy:** Hips > Spine > Spine1 > Spine2 > Neck > Head (and limb chains)
- **The `mixamorig:` prefix must match between model and animation files** for bone mapping to work
- MonoGame's FBX importer may place the skeleton **two nodes down** from the root (documented in MonoGame community: `MeshHelper.FindSkeleton()` can fail for this reason)

### Separate Model + Animation Files Pattern

Mixamo exports one FBX per animation. The standard pattern is:

1. Download character model (e.g., `test-character.fbx`) -- "With Skin"
2. Download each animation (e.g., `idle.fbx`, `walk.fbx`) -- "Without Skin"
3. At runtime: load model FBX, then load each animation FBX and merge animation data

The project already implements this in `AnimatedModel.AddAnimationsFrom()`. The bone names in animation files must match the model's bone names (guaranteed when using the same Mixamo character).

### Known Mixamo + MonoGame Compatibility Issues

| Issue | Cause | Mitigation | Confidence |
|-------|-------|------------|------------|
| `MeshHelper.FindSkeleton()` returns null | Mixamo skeleton is 2+ nodes deep from root; MonoGame expects direct child | Custom skeleton finding logic in processor, or restructure in Blender | MEDIUM |
| First frame shows T-pose | Animation starts from bind pose, not first keyframe | Ensure animation starts at frame 0 with correct pose, or skip frame 0 at runtime | MEDIUM |
| Bone index mismatch between model and animation FBX | Different bone traversal order | Map bones by **name** (not index) between model and animation data | HIGH |
| MonoGame 3.8.4 Assimp 5.x regressions | Upgrade from Assimp 4.x to 5.x changed mesh welding tolerances and skeleton processing | Test with current version; Blender re-export as fallback | LOW |

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| MonoGame.Framework.DesktopGL 3.8.4.1 | .NET 8.0 | Requires VS 2022 or Rider. macOS supported via DesktopGL. |
| MonoGame.Framework.Content.Pipeline 3.8.4.1 | .NET 8.0 | Used by `Berzerk.ContentPipeline` project for build-time processing. |
| MonoGame 3.8.4.1 (internal Assimp) | FBX 7.4 Binary | Assimp 5.x handles FBX 7.4 binary natively. ASCII FBX also supported but binary preferred. |
| SkinnedEffect | Max 72 bones, 1/2/4 weights per vertex | Mixamo ~65 bones fits. Use 4 weights per vertex for best quality. |
| Blender 4.2 LTS / 5.0.1 | FBX 7.4 import/export | For preprocessing if needed. Do NOT use "Apply Transform" on import (corrupts animations). |
| SharpGLTF.Core 1.0.6 | .NET Standard 2.0+ | Only needed if pursuing Approach B (glTF pipeline). |

## Installation

The project is already configured correctly. For reference:

```xml
<!-- Berzerk.csproj (game project) -->
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.4.1" />
<PackageReference Include="MonoGame.Content.Builder.Task" Version="3.8.4.1" />

<!-- Berzerk.ContentPipeline.csproj (content pipeline extension) -->
<PackageReference Include="MonoGame.Framework.Content.Pipeline" Version="3.8.4.1" />
<PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.4.1" />
```

**Note:** The current `.csproj` uses `Version="3.8.*"` wildcard for the game project and `Version="3.8.4.1"` pinned for the pipeline project. Recommend **pinning both to `3.8.4.1`** for reproducible builds.

```bash
# If adding Blender for preprocessing (optional, for diagnostics):
# Download from https://www.blender.org/download/lts/4-2/

# If pursuing Approach B (NOT recommended):
dotnet add package SharpGLTF.Core --version 1.0.6
dotnet add package SharpGLTF.Runtime --version 1.0.5
```

## Root Cause Analysis: The T-Pose Bug

Based on the code review and research, the T-pose bug has **two compounding causes:**

### Cause 1: BasicEffect Cannot Render Skinned Animation (CRITICAL)

`AnimatedModel.Draw()` casts effects to `BasicEffect`:
```csharp
foreach (BasicEffect effect in mesh.Effects)  // Line 128
```

`BasicEffect` has no concept of bone transforms. It renders the mesh using `_boneTransforms[mesh.ParentBone.Index]` as a simple world transform -- this positions the mesh part in space but does NOT deform the mesh vertices based on bone weights. The mesh stays in bind pose (T-pose).

**Fix:** Use `SkinnedEffect` and call `SetBoneTransforms()` with the computed bone palette.

### Cause 2: Missing Inverse Bind Pose in Bone Palette (CRITICAL)

`ApplyKeyframes()` computes `_boneTransforms` as animated world transforms, but `SkinnedEffect.SetBoneTransforms()` expects the **bone palette format**:

```
bonePalette[i] = inverseBindPose[i] * animatedAbsoluteTransform[i]
```

The inverse bind pose "undoes" the initial skeleton pose so that a vertex returns to origin, then the animated transform moves it to the correct animated position. Without this, vertices would be double-transformed.

**Fix:** Store inverse bind poses from the original skeleton, compute proper bone palette each frame.

### Cause 3: Content Pipeline May Not Extract Animation Channels Correctly (NEEDS INVESTIGATION)

The `MixamoModelProcessor.ExtractKeyframes()` logic is complex and handles both skeleton-attached and animation-only FBX files. The Mixamo FBX format stores animation channels in a specific hierarchy that may not match what the code expects. Needs runtime debugging to verify keyframes are actually being extracted.

**Confidence:** HIGH for Causes 1 and 2 (verified from code + MonoGame documentation). MEDIUM for Cause 3 (needs runtime validation).

## Sources

- [MonoGame Official Docs: SkinnedEffect](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SkinnedEffect.html) -- Confirmed MaxBones=72, SetBoneTransforms API. HIGH confidence.
- [MonoGame Official Docs: What's New](https://docs.monogame.net/articles/whats_new.html) -- Version history, .NET 8 targeting in 3.8.2+. HIGH confidence.
- [MonoGame 3.8.5-preview.2 Release](https://monogame.net/blog/2026-01-02-MonoGame385.preview.2-release/) -- Confirmed preview status. HIGH confidence.
- [MonoGame GitHub Discussion #8985](https://github.com/MonoGame/MonoGame/discussions/8985) -- Assimp 4.x to 5.x breaking changes in 3.8.4. MEDIUM confidence.
- [MonoGame Community: Robust FBX Import](https://community.monogame.net/t/robust-method-to-import-skinned-fbx-models/12936) -- Community consensus on FBX pipeline issues, glTF alternative. MEDIUM confidence.
- [MonoGame Community: SkinnedMesh Import](https://community.monogame.net/t/skinnedmesh-import-question/8902) -- Mixamo skeleton depth issue, `MeshHelper.FindSkeleton()` failure. MEDIUM confidence.
- [MonoGame Community: Animation and FBX](https://community.monogame.net/t/solved-animation-and-fbx/9342) -- T-pose first frame issue, NodeContent/BoneContent differences. MEDIUM confidence.
- [MonoGame Community: Skinned Animation Tutorial](https://community.monogame.net/t/tutorial-how-to-get-xnas-skinnedsample-working-with-monogame/7609) -- XNA SkinnedModel sample port instructions. MEDIUM confidence.
- [MonoGame Community: Unable to Find Skeleton](https://community.monogame.net/t/skinned-animation-unable-to-find-skeleton/12069) -- Root bone naming, animation baking, multi-mesh issues. MEDIUM confidence.
- [XNA SkinnedModel Sample Wiki](https://github.com/SimonDarksideJ/XNAGameStudio/wiki/Skinned-Model) -- Canonical reference architecture. HIGH confidence.
- [MonoGame PR #4952](https://github.com/MonoGame/MonoGame/pull/4952) -- Skeleton hierarchy fix (not merged, closed May 2024). HIGH confidence.
- [MonoGame Issue #3825](https://github.com/MonoGame/MonoGame/issues/3825) -- Request for built-in SkinnedModel (not implemented). HIGH confidence.
- [SharpGLTF GitHub](https://github.com/vpenades/SharpGLTF) -- glTF library, v1.0.6. HIGH confidence.
- [MonoScene GitHub](https://github.com/vpenades/MonoScene) -- SharpGLTF + MonoGame animated model example (maintenance mode). MEDIUM confidence.
- [LiruJ/MonoGame-Skinned-Mesh-Importer](https://github.com/LiruJ/MonoGame-Skinned-Mesh-Importer) -- Community skinned mesh importer reference. MEDIUM confidence.
- [NuGet: Lovebirb.MonoGame.SkinnedMeshImporter](https://www.nuget.org/packages/Lovebirb.MonoGame.SkinnedMeshImporter/1.0.2) -- v1.0.4, May 2023. HIGH confidence (version verified).
- [NuGet: AssimpNet](https://www.nuget.org/packages/AssimpNet) -- v4.1.0 stable. HIGH confidence.
- [AssimpNetter GitHub](https://github.com/Saalvage/AssimpNetter) -- v6.0.2.1, Oct 2025. MonoGame 3.8.4 uses this internally. HIGH confidence.
- [Adobe Mixamo Community: 65-bone skeleton](https://community.adobe.com/t5/mixamo/mixamo-standard-65-bone-skeleton/m-p/11442179) -- Standard bone count. MEDIUM confidence.
- [Lofionic/MonoGameAnimatedModel](https://github.com/Lofionic/MonoGameAnimatedModel) -- Example skinned animation with custom pipeline. MEDIUM confidence.
- [BetterSkinned-Monogame](https://github.com/olossss/BetterSkinned-Monogame) -- XNA BetterSkinned port to MonoGame. MEDIUM confidence (legacy code).

---
*Stack research for: MonoGame 3D Skeletal Animation with Mixamo FBX Assets*
*Researched: 2026-02-09*
