# Phase 2: Content Pipeline Processor - Research

**Researched:** 2026-02-10
**Domain:** MonoGame Content Pipeline processor for Mixamo FBX skeletal animation data extraction
**Confidence:** HIGH

## Summary

Phase 2 rewrites the existing `MixamoModelProcessor` to produce `SkinningData` (the Phase 1 types) instead of the old `AnimationData`. The processor must do six things correctly: flatten the skeleton to establish canonical bone ordering, extract bind pose matrices, compute inverse bind pose matrices, build the parent-child skeleton hierarchy, extract animation keyframes with correct bone indices, and force SkinnedEffect usage. All of this must happen before or around the call to `base.Process()`, with the final `SkinningData` attached to `Model.Tag`.

The canonical reference is the XNA SkinningSample_4_0 `SkinnedModelProcessor`, which MonoGame itself ships an internal copy of. The pattern is well-established and battle-tested. The existing codebase already has a `MixamoModelProcessor` that inherits from `ModelProcessor` and does some of these steps, but it produces the wrong output type (`AnimationData` instead of `SkinningData`), uses incorrect bone indexing (a flawed custom traversal instead of `MeshHelper.FlattenSkeleton()`), does not compute inverse bind poses, and does not force `SkinnedEffect`. The work is to replace the processor's internals to follow the canonical pattern while keeping the Mixamo-specific logging and animation extraction that already works.

The existing FBX assets (test-character.fbx at 2.0MB, idle.fbx, walk.fbx, run.fbx, bash.fbx) are already configured in `Content.mgcb` with the `MixamoModelProcessor` and `FbxImporter`. The content pipeline reference to `Berzerk.ContentPipeline.dll` is already in place. This means the processor change is a drop-in replacement -- no `.mgcb` configuration changes needed.

**Primary recommendation:** Rewrite `MixamoModelProcessor.Process()` to follow the XNA SkinnedModelProcessor pattern exactly: FlattenSkeleton for bone ordering, extract bind/inverse bind pose, build hierarchy, extract animations with flattened indices, call `base.Process()`, attach `SkinningData` to `Model.Tag`. Override `DefaultEffect` to return `MaterialProcessorDefaultEffect.SkinnedEffect` as the primary workaround for MonoGame Issue #3057.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.Content.Pipeline | 3.8.4.1 | Content Pipeline extension APIs: `ModelProcessor`, `ContentProcessorContext`, `MeshHelper`, `NodeContent`, `BoneContent`, `AnimationContent` | Already in use in `Berzerk.ContentPipeline.csproj`. Provides all build-time APIs needed. |
| MonoGame.Framework.DesktopGL | 3.8.4.1 | `Matrix`, `Vector3`, `Quaternion` math types used by the processor | Already in use. |
| .NET 8.0 | 8.0 | Runtime, `List<T>`, `Dictionary<T,V>` | Already in use. |

### Supporting
No additional libraries needed. Phase 2 uses only MonoGame Content Pipeline APIs that are already referenced.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `MeshHelper.FlattenSkeleton()` for bone ordering | Custom depth-first traversal | Custom traversal is what the current processor does and it produces incorrect bone indices. `FlattenSkeleton()` produces the same ordering that `base.Process()` uses internally for vertex channel conversion, which is the ONLY correct approach. **Use FlattenSkeleton.** |
| `DefaultEffect` property override for SkinnedEffect | `ConvertMaterial()` override to return `SkinnedMaterialContent` | MonoGame Issue #3057 reported that `DefaultEffect` had no effect, but PRs #3068 and #3842 were merged to fix it. The issue is closed. MonoGame's own internal `SkinnedModelProcessor` uses the `DefaultEffect` override approach. If it still does not work in practice, `ConvertMaterial()` override is the fallback. **Try DefaultEffect first, ConvertMaterial as fallback.** |
| Rewriting MixamoModelProcessor from scratch | Patching the existing processor incrementally | The existing processor has the right skeleton-finding and animation-extraction logic for Mixamo's FBX format, but the core data flow (bone indexing, data types) must change completely. Incremental patching risks leaving stale code paths. **Rewrite the Process() method and animation extraction; keep skeleton-finding and logging utilities.** |

## Architecture Patterns

### Recommended Project Structure
```
Berzerk.ContentPipeline/
    MixamoModelProcessor.cs       # Rewritten to produce SkinningData
    SkinningData.cs               # Phase 1 type (unchanged)
    SkinningDataClip.cs           # Phase 1 type (unchanged)
    SkinningDataKeyframe.cs       # Phase 1 type (unchanged)
    SkinningDataWriter.cs         # Phase 1 serializer (unchanged)
    AnimationData.cs              # OLD - to be deleted after processor rewrite
    AnimationClip.cs              # OLD - to be deleted after processor rewrite
    Keyframe.cs                   # OLD - to be deleted after processor rewrite
    AnimationDataWriter.cs        # OLD - to be deleted after processor rewrite
```

### Pattern 1: Canonical SkinnedModelProcessor Flow

**What:** The processor follows a specific order of operations that matches the XNA SkinnedModel sample and MonoGame's internal `SkinnedModelProcessor`. The order matters because `MeshHelper.FlattenSkeleton()` establishes bone ordering that `base.Process()` relies on for vertex channel conversion.

**When to use:** Always. This is not flexible -- the order is dictated by data dependencies.

**Critical order of operations:**

```csharp
public override ModelContent Process(NodeContent input, ContentProcessorContext context)
{
    // Step 1: Find skeleton
    BoneContent skeleton = MeshHelper.FindSkeleton(input);
    // Validate skeleton exists

    // Step 2: Bake non-bone transforms into geometry
    FlattenTransforms(input, skeleton);

    // Step 3: Flatten skeleton to establish canonical bone ordering
    IList<BoneContent> bones = MeshHelper.FlattenSkeleton(skeleton);
    // Validate bone count <= SkinnedEffect.MaxBones (72)

    // Step 4: Extract skeleton data from flattened list
    List<Matrix> bindPose = new List<Matrix>();
    List<Matrix> inverseBindPose = new List<Matrix>();
    List<int> skeletonHierarchy = new List<int>();

    foreach (BoneContent bone in bones)
    {
        bindPose.Add(bone.Transform);
        inverseBindPose.Add(Matrix.Invert(bone.AbsoluteTransform));
        skeletonHierarchy.Add(bones.IndexOf(bone.Parent as BoneContent));
    }

    // Step 5: Extract animations using flattened bone indices
    Dictionary<string, SkinningDataClip> animationClips = ProcessAnimations(skeleton, bones, context);

    // Step 6: Call base.Process() -- handles mesh compilation, vertex channels, materials
    ModelContent model = base.Process(input, context);

    // Step 7: Attach SkinningData to Model.Tag
    model.Tag = new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);

    return model;
}
```

**Source:** XNA SkinningSample_4_0/SkinnedModelPipeline/SkinnedModelProcessor.cs, MonoGame internal SkinnedModelProcessor.cs

### Pattern 2: FlattenTransforms Before Processing

**What:** Before calling `base.Process()`, bake all non-bone node transforms into the geometry. This ensures that mesh geometry is in the coordinate space the skeleton expects. Without this, meshes may be offset or scaled relative to the skeleton.

**When to use:** Always. Mixamo FBX files often have non-identity transforms on non-bone nodes (scale adjustments, root transforms). These must be baked into geometry before processing.

**Example:**
```csharp
// Source: XNA SkinningSample_4_0
private static void FlattenTransforms(NodeContent node, BoneContent skeleton)
{
    foreach (NodeContent child in node.Children)
    {
        // Do NOT flatten the skeleton itself -- it stores bone rest transforms
        if (child == skeleton)
            continue;

        // Bake this node's transform into its geometry
        MeshHelper.TransformScene(child, child.Transform);

        // Reset the node's transform to identity (it's now in the geometry)
        child.Transform = Matrix.Identity;

        // Recurse into children
        FlattenTransforms(child, skeleton);
    }
}
```

### Pattern 3: DefaultEffect Override for SkinnedEffect (Issue #3057 Workaround)

**What:** Override the `DefaultEffect` property to force `MaterialProcessorDefaultEffect.SkinnedEffect`. This tells the base `ModelProcessor` to assign `SkinnedEffect` instead of `BasicEffect` to all mesh parts.

**When to use:** For all skinned models. This is the canonical workaround from the XNA SkinnedModel sample.

**Example:**
```csharp
[DefaultValue(MaterialProcessorDefaultEffect.SkinnedEffect)]
public override MaterialProcessorDefaultEffect DefaultEffect
{
    get { return MaterialProcessorDefaultEffect.SkinnedEffect; }
    set { } // Ignore external attempts to change this
}
```

**Fallback (if DefaultEffect still does not work):** Override `ConvertMaterial()` to force `SkinnedMaterialContent`:
```csharp
protected override MaterialContent ConvertMaterial(
    MaterialContent material, ContentProcessorContext context)
{
    var skinnedMaterial = new SkinnedMaterialContent();
    // Copy basic material properties (texture, diffuse color, etc.)
    foreach (var texture in material.Textures)
        skinnedMaterial.Textures.Add(texture.Key, texture.Value);
    return base.ConvertMaterial(skinnedMaterial, context);
}
```

**Verification:** At runtime, check `mesh.Effects[0] is SkinnedEffect`. If it is `BasicEffect`, the workaround did not work and the `ConvertMaterial` fallback is needed.

### Pattern 4: Animation Extraction with Correct Bone Indices

**What:** Build a bone-name-to-index mapping from the flattened skeleton, then use it to resolve animation channel bone names to correct indices. This ensures keyframe bone indices match the bind pose / inverse bind pose / hierarchy arrays.

**When to use:** Always. This is the critical fix for the current processor's incorrect bone indexing.

**Example:**
```csharp
private Dictionary<string, SkinningDataClip> ProcessAnimations(
    BoneContent skeleton, IList<BoneContent> bones, ContentProcessorContext context)
{
    // Build name-to-index mapping from flattened skeleton
    var boneMap = new Dictionary<string, int>();
    for (int i = 0; i < bones.Count; i++)
        boneMap[bones[i].Name] = i;

    var clips = new Dictionary<string, SkinningDataClip>();

    // AnimationContent objects are stored on the skeleton root bone
    foreach (var animation in skeleton.Animations)
    {
        var keyframes = new List<SkinningDataKeyframe>();

        foreach (var channel in animation.Value.Channels)
        {
            string boneName = channel.Key;

            // Skip bones not in our skeleton (e.g., extra nodes)
            if (!boneMap.TryGetValue(boneName, out int boneIndex))
            {
                context.Logger.LogWarning(null, null,
                    "Animation channel '{0}' not found in skeleton, skipping", boneName);
                continue;
            }

            // Convert each keyframe
            foreach (var kf in channel.Value)
            {
                keyframes.Add(new SkinningDataKeyframe(boneIndex, kf.Time, kf.Transform));
            }
        }

        // Sort by time, then by bone index (canonical ordering)
        keyframes.Sort((a, b) =>
        {
            int cmp = a.Time.CompareTo(b.Time);
            return cmp != 0 ? cmp : a.Bone.CompareTo(b.Bone);
        });

        clips[animation.Key] = new SkinningDataClip(animation.Value.Duration, keyframes);
    }

    return clips;
}
```

### Pattern 5: Separate Animation File Handling (Mixamo Workflow)

**What:** Mixamo exports separate FBX files for each animation. Animation-only files may not have a skeleton recognized by `MeshHelper.FindSkeleton()`. The processor must handle both cases: model+skeleton+animation files and animation-only files.

**When to use:** When processing animation-only FBX files that may lack mesh data or a recognized skeleton.

**Approach:** The existing `MixamoModelProcessor` already handles this case (the `isAnimationOnly` path). For animation-only files, animations may be on the root `NodeContent` rather than on a `BoneContent` skeleton. The processor should still extract them, building bone indices from the animation channel names. At runtime, these are merged via `AddAnimationsFrom()` and the bone names must match the base model's skeleton.

**Key insight:** For animation-only Mixamo FBX files, the processor should still produce a `SkinningData` object (with bind pose from whatever skeleton is available, even if minimal), because the bone names in the animation channels serve as the mapping key when merging at runtime.

### Anti-Patterns to Avoid

- **Custom bone indexing (current `BuildBoneIndices` method):** Produces indices that do not match `MeshHelper.FlattenSkeleton()` ordering. Must use the flattened list for ALL bone indices.
- **Extracting skeleton data AFTER `base.Process()`:** The base processor may modify the node hierarchy. Extract all skeleton/animation data BEFORE calling `base.Process()`.
- **Skipping FlattenTransforms:** Non-bone transforms not baked into geometry cause mesh-skeleton misalignment. Always call `FlattenTransforms` before processing.
- **Using `Model.Bones` indices at runtime for animation:** `Model.Bones` includes non-bone nodes (mesh nodes, root transforms). Bone indices from `FlattenSkeleton` are a SUBSET of `Model.Bones`. Always use the SkinningData indices.
- **Storing old AnimationData alongside SkinningData in Model.Tag:** Only one object can go in `Model.Tag`. Switch to SkinningData exclusively.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Bone ordering / flattening | Custom depth-first traversal with manual counter | `MeshHelper.FlattenSkeleton(skeleton)` | MonoGame's built-in method produces the exact ordering that `base.Process()` uses for vertex channel conversion. Any other ordering causes bone index mismatch between animation data and vertex blend indices. |
| Skeleton finding | Manual node tree search for bones | `MeshHelper.FindSkeleton(input)` | Built-in method knows how to find the skeleton root even when it is nested several levels deep (common with Mixamo FBX). |
| Transform baking | Manual vertex position multiplication | `MeshHelper.TransformScene(child, child.Transform)` | Built-in method correctly handles all vertex channels (positions, normals, tangents, binormals) when baking transforms into geometry. |
| Inverse bind pose computation | Manual matrix math on individual elements | `Matrix.Invert(bone.AbsoluteTransform)` | MonoGame's `Matrix.Invert()` handles all general-case 4x4 matrix inversion correctly. `bone.AbsoluteTransform` is automatically computed by the Content Pipeline from the node hierarchy. |
| Mesh compilation and vertex channel conversion | Custom vertex buffer creation | `base.Process(input, context)` | The base `ModelProcessor` handles mesh compilation, vertex channel conversion (including `BoneWeights` to `BlendIndices`/`BlendWeight`), material processing, and XNB serialization. Never replicate this. |

**Key insight:** The Content Pipeline provides high-level helpers (`MeshHelper`) specifically for skeletal animation processing. These are tested and proven. The processor's job is to orchestrate these helpers in the correct order, not to reimplementthem.

## Common Pitfalls

### Pitfall 1: MeshHelper.FindSkeleton() Returns Null for Animation-Only Files
**What goes wrong:** Animation-only Mixamo FBX files (downloaded "without skin") may not have a mesh that establishes bone-to-skeleton relationships. `MeshHelper.FindSkeleton()` looks for bones that influence a skinned mesh, so it may return null even though the file contains a full bone hierarchy.
**Why it happens:** MonoGame's FBX importer (via Assimp) classifies nodes as `BoneContent` based on whether they influence a skinned mesh. No mesh = potentially no `BoneContent` nodes.
**How to avoid:** The processor must handle the `skeleton == null` case for animation-only files. For animation-only files, extract animations from the root `NodeContent` hierarchy instead, using bone names as the mapping key. The SkinningData for animation-only files can have empty bind/inverse bind pose arrays -- the skeleton data comes from the base model when clips are merged at runtime.
**Warning signs:** Pipeline log says "No skeleton found" for animation FBX files. This is expected and not an error for animation-only files.

### Pitfall 2: Bone Count Exceeds SkinnedEffect.MaxBones (72)
**What goes wrong:** `SkinnedEffect.SetBoneTransforms()` throws at runtime if the bone array exceeds 72 elements.
**Why it happens:** Mixamo standard skeleton has ~65 bones, which fits. But some Mixamo characters have additional bones (fingers, face bones) or the FBX importer adds extra "end" bones (leaf bones).
**How to avoid:** Validate bone count after `FlattenSkeleton()` and fail with a clear error message. Log bone count prominently.
**Warning signs:** Pipeline succeeds but runtime crashes on `SetBoneTransforms()` with array-too-large error.

### Pitfall 3: FlattenTransforms Not Called Before base.Process()
**What goes wrong:** The mesh geometry is offset from the skeleton. Vertices appear to be in the wrong position relative to bones. The model may appear scaled incorrectly or positioned at the wrong origin.
**Why it happens:** Mixamo FBX files often have a root node with a scale transform (e.g., 1cm to 1m conversion). If this transform is not baked into the mesh geometry before processing, the mesh and skeleton end up in different coordinate spaces.
**How to avoid:** Always call `FlattenTransforms(input, skeleton)` before `base.Process()`. This bakes all non-skeleton node transforms into the mesh geometry and resets the node transforms to identity.
**Warning signs:** Model appears very small or very large at runtime. Mesh is offset from where the skeleton is. Animation looks correct in bone debug visualization but mesh does not follow.

### Pitfall 4: Incorrect Parent Index for Root Bone
**What goes wrong:** The root bone gets a parent index that is not -1. This causes the three-stage transform pipeline at runtime to try to look up a non-existent parent, producing garbage transforms or an index-out-of-bounds exception.
**Why it happens:** `bones.IndexOf(bone.Parent as BoneContent)` returns -1 when `bone.Parent` is not a `BoneContent` (it is a `NodeContent` or null). This is actually the correct behavior -- the root bone's parent is NOT in the flattened skeleton list, so `IndexOf` returns -1. However, if the code does not handle this case or uses a different approach, the root bone may get a wrong parent index.
**How to avoid:** Use `bones.IndexOf(bone.Parent as BoneContent)` exactly as the XNA sample does. The `as BoneContent` cast returns null for non-bone parents, and `IndexOf(null)` returns -1 in `IList<T>`. Verify the first entry in `skeletonHierarchy` is -1 in tests.
**Warning signs:** First bone's parent index is not -1. Runtime animation shows the entire model flying to an unexpected position.

### Pitfall 5: Animation Channels Not on Skeleton Root
**What goes wrong:** `skeleton.Animations` is empty even though the FBX file contains animations.
**Why it happens:** In some Mixamo FBX files, animations are stored on the root `NodeContent` or on child nodes, not directly on the skeleton `BoneContent`. The XNA SkinnedModelProcessor expects animations on the skeleton root.
**How to avoid:** Search for animations in multiple locations: first on the skeleton root (`skeleton.Animations`), then on the root `NodeContent` (`input.Animations`), then recursively on child nodes. The existing `MixamoModelProcessor.ExtractAnimations()` already does this and should be preserved.
**Warning signs:** Pipeline log shows "0 animations found" for a file that should have animations. Verify by checking both `skeleton.Animations` and `input.Animations`.

### Pitfall 6: Old AnimationData Types Still Referenced After Rewrite
**What goes wrong:** Build fails because `AnimatedModel.cs` and other runtime files still reference `AnimationData` instead of `SkinningData`.
**Why it happens:** Phase 2 changes the pipeline output from `AnimationData` to `SkinningData`, but the runtime consumers are not updated until Phase 3/4.
**How to avoid:** Phase 2 should delete the old pipeline-side types (`AnimationData.cs`, `AnimationClip.cs`, `Keyframe.cs`, `AnimationDataWriter.cs` in `Berzerk.ContentPipeline/`). The runtime-side old types (`AnimationData.cs`, `AnimationClip.cs`, `AnimationDataReader.cs`, `Keyframe.cs` in `Berzerk/Source/Content/`) should remain temporarily since `AnimatedModel.cs` still references them. The runtime will be updated in Phase 3/4 to use `SkinningData`. This means the game will not be able to successfully LOAD content between Phase 2 and Phase 3 (the pipeline writes SkinningData but the runtime still expects AnimationData in Model.Tag). This is acceptable -- the game is not expected to run correctly between phases.
**Warning signs:** Build errors referencing old type names. Runtime `Model.Tag as AnimationData` returns null because Tag is now `SkinningData`.

## Code Examples

Verified patterns from official sources and codebase analysis:

### Complete Processor Skeleton (Recommended Implementation Pattern)
```csharp
// Source: XNA SkinningSample_4_0 adapted for current codebase
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Berzerk.ContentPipeline;

[ContentProcessor(DisplayName = "Mixamo Model Processor")]
public class MixamoModelProcessor : ModelProcessor
{
    // Force SkinnedEffect for all materials (workaround for Issue #3057)
    [DefaultValue(MaterialProcessorDefaultEffect.SkinnedEffect)]
    public override MaterialProcessorDefaultEffect DefaultEffect
    {
        get => MaterialProcessorDefaultEffect.SkinnedEffect;
        set { }
    }

    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        context.Logger.LogImportantMessage("=== Processing Mixamo model: {0} ===", input.Name);

        // Step 1: Find skeleton
        BoneContent? skeleton = MeshHelper.FindSkeleton(input);

        if (skeleton == null)
        {
            // Animation-only file or static model -- handle gracefully
            context.Logger.LogWarning(null, null,
                "No skeleton found in '{0}'. Processing as static/animation-only model.", input.Name);
            return ProcessWithoutSkeleton(input, context);
        }

        // Step 2: Flatten non-bone transforms into geometry
        FlattenTransforms(input, skeleton);

        // Step 3: Flatten skeleton -- canonical bone ordering
        IList<BoneContent> bones = MeshHelper.FlattenSkeleton(skeleton);
        context.Logger.LogImportantMessage("Skeleton: {0} bones (max {1})", bones.Count, 72);

        if (bones.Count > 72)
            throw new InvalidContentException(
                $"Skeleton has {bones.Count} bones, exceeding SkinnedEffect.MaxBones (72).");

        // Step 4: Extract skeleton data
        var bindPose = new List<Matrix>();
        var inverseBindPose = new List<Matrix>();
        var skeletonHierarchy = new List<int>();

        foreach (BoneContent bone in bones)
        {
            bindPose.Add(bone.Transform);
            inverseBindPose.Add(Matrix.Invert(bone.AbsoluteTransform));
            skeletonHierarchy.Add(bones.IndexOf(bone.Parent as BoneContent));
        }

        // Step 5: Extract animations
        var animationClips = ProcessAnimations(skeleton, bones, input, context);

        // Step 6: Standard ModelProcessor handles mesh, vertex channels, materials
        ModelContent model = base.Process(input, context);

        // Step 7: Attach SkinningData
        model.Tag = new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
        context.Logger.LogImportantMessage(
            "Attached SkinningData: {0} bones, {1} clips", bones.Count, animationClips.Count);

        return model;
    }
}
```

### FlattenTransforms Helper
```csharp
// Source: XNA SkinningSample_4_0
private static void FlattenTransforms(NodeContent node, BoneContent skeleton)
{
    foreach (NodeContent child in node.Children)
    {
        if (child == skeleton)
            continue;

        MeshHelper.TransformScene(child, child.Transform);
        child.Transform = Matrix.Identity;

        FlattenTransforms(child, skeleton);
    }
}
```

### Bind Pose / Inverse Bind Pose Consistency Check
```csharp
// Validate that bindPose * inverseBindPose approximates identity
// This check is for pipeline-time validation logging, not a hard requirement
for (int i = 0; i < bones.Count; i++)
{
    // Compute absolute transform from bind pose by composing up the hierarchy
    Matrix absoluteFromBind = bones[i].AbsoluteTransform;
    Matrix product = absoluteFromBind * inverseBindPose[i];

    // product should approximate Matrix.Identity
    float deviation = Math.Abs(product.M11 - 1) + Math.Abs(product.M22 - 1)
                    + Math.Abs(product.M33 - 1) + Math.Abs(product.M44 - 1);
    if (deviation > 0.01f)
    {
        context.Logger.LogWarning(null, null,
            "Bone {0} ({1}): bindPose * inverseBindPose deviates from identity by {2:F4}",
            i, bones[i].Name, deviation);
    }
}
```

## State of the Art

| Old Approach (Current Code) | Current Approach (Target) | Why It Changed |
|------------------------------|---------------------------|----------------|
| Custom `BuildBoneIndices()` with incrementing counter | `MeshHelper.FlattenSkeleton()` for canonical bone ordering | Custom ordering does not match `base.Process()` vertex channel conversion, causing bone index mismatch |
| `AnimationData` without bind/inverse bind pose | `SkinningData` with bind pose, inverse bind pose, hierarchy | Missing inverse bind pose makes three-stage transform pipeline impossible; T-pose results |
| Per-bone `Dictionary<string, List<Keyframe>>` in AnimationClip | Flat `List<SkinningDataKeyframe>` sorted by time | Flat list is cache-friendly, matches canonical XNA pattern, simpler for runtime playback |
| No `DefaultEffect` override (uses BasicEffect) | `DefaultEffect` returns `MaterialProcessorDefaultEffect.SkinnedEffect` | Without SkinnedEffect, GPU cannot perform vertex skinning; mesh stays in T-pose |
| No `FlattenTransforms` call before processing | `FlattenTransforms(input, skeleton)` before `base.Process()` | Non-skeleton transforms not baked into geometry cause mesh-skeleton coordinate space mismatch |

**Deprecated/outdated:**
- `AnimationData`, `AnimationClip`, `Keyframe`, `AnimationDataWriter` in `Berzerk.ContentPipeline/`: Replaced by `SkinningData`, `SkinningDataClip`, `SkinningDataKeyframe`, `SkinningDataWriter` from Phase 1.
- `BuildBoneIndices()` method: Produces incorrect bone ordering. Replaced by `MeshHelper.FlattenSkeleton()`.
- `ExtractKeyframes()` with animation-channel bone index building: Must use flattened skeleton indices instead.

## Existing Code Inventory

### Files to Modify
| File | Change | Why |
|------|--------|-----|
| `Berzerk.ContentPipeline/MixamoModelProcessor.cs` | Rewrite `Process()` method and animation extraction | Must produce `SkinningData` instead of `AnimationData`, use `FlattenSkeleton()`, compute inverse bind pose, override `DefaultEffect` |

### Files to Delete (Pipeline-Side Old Types)
| File | Replaced By | Safe to Delete? |
|------|-------------|-----------------|
| `Berzerk.ContentPipeline/AnimationData.cs` | `SkinningData.cs` (Phase 1) | YES -- only referenced by old processor code being rewritten |
| `Berzerk.ContentPipeline/AnimationClip.cs` | `SkinningDataClip.cs` (Phase 1) | YES -- only referenced by old processor code |
| `Berzerk.ContentPipeline/Keyframe.cs` | `SkinningDataKeyframe.cs` (Phase 1) | YES -- only referenced by old processor code |
| `Berzerk.ContentPipeline/AnimationDataWriter.cs` | `SkinningDataWriter.cs` (Phase 1) | YES -- only referenced by old types |

### Files to Keep (Runtime-Side, Modified in Later Phases)
| File | Current State | Future Change (Phase 3/4) |
|------|--------------|--------------------------|
| `Berzerk/Source/Content/AnimationData.cs` | References old types | Will be replaced by `SkinningData` usage in Phase 3 |
| `Berzerk/Source/Content/AnimationClip.cs` | References old types | Will be deleted in Phase 3 |
| `Berzerk/Source/Content/AnimationDataReader.cs` | Reads old format | Will be deleted in Phase 3 |
| `Berzerk/Source/Content/Keyframe.cs` | Old keyframe type | Will be deleted in Phase 3 |
| `Berzerk/Source/Graphics/AnimatedModel.cs` | Uses `AnimationData` | Will be rewritten in Phase 3/4 to use `SkinningData` |

### Content Pipeline Configuration (No Changes Needed)
| File | Current Config | Status |
|------|---------------|--------|
| `Berzerk/Content/Content.mgcb` | References `Berzerk.ContentPipeline.dll`, uses `MixamoModelProcessor` for all FBX files | Correct -- processor name unchanged, reference unchanged |

## Testing Strategy

### Pipeline-Time Validation (In Processor Logs)
The processor should log extensively during build:
1. Bone count from `FlattenSkeleton()` (expect ~65 for Mixamo)
2. Root bone name (expect `mixamorig:Hips` or similar)
3. First few bones in hierarchy with parent indices
4. Bind pose / inverse bind pose identity consistency check
5. Animation clip names, durations, and keyframe counts
6. Whether `skeleton.Animations` or fallback animation search was used

### Build-Time Verification
After the processor rewrite, `dotnet build` must succeed for the `Berzerk.ContentPipeline` project. The content build (MGCB) may or may not succeed depending on FBX compatibility -- if the content build fails, that is a separate issue (FBX import, not processor logic). The processor code should compile cleanly regardless.

### Unit Tests (Optional for Phase 2)
The processor is difficult to unit test because it depends on `ContentProcessorContext`, `NodeContent`, and `BoneContent` which cannot be easily instantiated outside the pipeline. The primary verification is: does the content pipeline build succeed, and does the output XNB contain correct SkinningData? This is verified in Phase 3 when the runtime loads the XNB.

However, a useful test would be to verify that the old pipeline types are deleted and the project still compiles.

## Open Questions

1. **Does DefaultEffect override actually work in MonoGame 3.8.4.1?**
   - What we know: Issue #3057 was closed with PRs merged in 2014-2015. MonoGame's own internal `SkinnedModelProcessor` uses this approach. The fix should be in 3.8.4.1.
   - What's unclear: No one has explicitly confirmed it works in 3.8.4.1 for custom processors inheriting from `ModelProcessor`.
   - Recommendation: Implement `DefaultEffect` override first. If runtime verification shows `BasicEffect` instead of `SkinnedEffect`, add `ConvertMaterial()` override as fallback. Both approaches should be documented in the code as comments.

2. **How do animation-only Mixamo FBX files behave with FlattenSkeleton?**
   - What we know: Animation-only files may not have a skeleton recognized by `MeshHelper.FindSkeleton()`. The current processor already handles this case.
   - What's unclear: Whether `FlattenSkeleton` works on animation-only files, and whether the bone names from animation channels match the base model's flattened skeleton bone names.
   - Recommendation: Handle the `skeleton == null` case separately. For animation-only files, produce `SkinningData` with animation clips extracted from the node hierarchy but with empty skeleton arrays (0 bones). At runtime, only the animation clips from these files will be merged into the base model's SkinningData.

3. **Should the content build be tested as part of Phase 2?**
   - What we know: Content builds require FBX files and the full MGCB pipeline. The FBX files exist in `Berzerk/Content/Models/`.
   - What's unclear: Whether the content build will succeed with the rewritten processor, or whether Assimp FBX parsing will produce unexpected node structures.
   - Recommendation: Attempt a content build after the processor rewrite. If it succeeds, examine the pipeline logs for bone counts and animation data. If it fails, log the error and investigate -- the processor code is correct regardless; failures would indicate FBX compatibility issues.

## Sources

### Primary (HIGH confidence)
- [XNA SkinningSample_4_0 -- SkinnedModelProcessor.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModelPipeline/SkinnedModelProcessor.cs) -- Canonical processor pattern: FlattenSkeleton, bind pose, inverse bind pose, hierarchy, animations
- [MonoGame MeshHelper API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.Pipeline.Graphics.MeshHelper.html) -- FlattenSkeleton, FindSkeleton, TransformScene method signatures
- [MonoGame SkinnedEffect API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SkinnedEffect.html) -- MaxBones=72, SetBoneTransforms, WeightsPerVertex
- [MonoGame ModelProcessor API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.Pipeline.Processors.ModelProcessor.html) -- ConvertMaterial, DefaultEffect, Process virtual methods
- [MonoGame Internal SkinnedModelProcessor](https://github.com/MonoGame/MonoGame/blob/f3d2d7f688de91824bd9139a7895838eff53cfcf/MonoGame.Framework.Content.Pipeline/Processors/SkinnedModelProcessor.cs) -- MonoGame's own implementation showing DefaultEffect pattern
- Direct codebase analysis -- `MixamoModelProcessor.cs`, `Content.mgcb`, all Phase 1 SkinningData types

### Secondary (MEDIUM confidence)
- [MonoGame Issue #3057: Content Pipeline SkinnedEffect bug](https://github.com/MonoGame/MonoGame/issues/3057) -- Documents the DefaultEffect bug and its fix via PRs #3068/#3842 (closed)
- [Tutorial: XNA SkinnedSample in MonoGame](https://community.monogame.net/t/tutorial-how-to-get-xnas-skinnedsample-working-with-monogame/7609) -- Community port guidance
- [BetterSkinned-Monogame](https://github.com/olossss/BetterSkinned-Monogame) -- XNA BetterSkinned port, reference for ConvertMaterial approach
- [MonoGame Community: Skinned Animation skeleton issues](https://community.monogame.net/t/skinned-animation-unable-to-find-skeleton/12069) -- FindSkeleton failure patterns
- Prior research: `.planning/research/ARCHITECTURE.md`, `.planning/research/PITFALLS.md`, `.planning/research/STACK.md`

### Tertiary (LOW confidence)
- [Lofionic/MonoGameAnimatedModel](https://github.com/Lofionic/MonoGameAnimatedModel) -- Reference implementation (custom HLSL approach, different from our SkinnedEffect approach)
- [MGCB SkinnedEffect does not get set](http://community.monogame.net/t/mgcb-skinnedeffect-does-not-get-set/2841) -- Community report suggesting develop branch fixed DefaultEffect

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- MonoGame 3.8.4.1 Content Pipeline with `MeshHelper` APIs is the established approach. Verified against official docs and canonical XNA sample.
- Architecture: HIGH -- XNA SkinnedModelProcessor pattern is the universal standard for MonoGame skeletal animation processing. MonoGame ships an internal copy. Order of operations verified from source.
- Pitfalls: HIGH -- MeshHelper.FindSkeleton null returns, bone count limits, FlattenTransforms requirement, and bone indexing issues are all well-documented in multiple community sources and confirmed by codebase analysis.
- SkinnedEffect forcing: MEDIUM -- DefaultEffect override is the canonical approach and Issue #3057 is closed, but no explicit confirmation for 3.8.4.1. ConvertMaterial fallback is well-understood.

**Research date:** 2026-02-10
**Valid until:** 2026-03-12 (30 days -- MonoGame 3.8.4.1 is stable, XNA patterns are decade-old standard)
