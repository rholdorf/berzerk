# Phase 1: SkinningData Types and Serialization - Research

**Researched:** 2026-02-10
**Domain:** MonoGame Content Pipeline data types and binary serialization for skeletal animation
**Confidence:** HIGH

## Summary

Phase 1 defines the data contract between build-time FBX processing and runtime animation playback. The work is to replace the existing `AnimationData` type system (which lacks bind pose, inverse bind pose, and skeleton hierarchy) with a `SkinningData` type system that matches the canonical XNA SkinnedModel sample architecture. This is pure data plumbing -- no rendering, no animation playback, no content processing changes. The types defined here will be consumed by every subsequent phase.

The existing codebase already has `AnimationData`, `AnimationClip`, `Keyframe`, `AnimationDataWriter`, and `AnimationDataReader` across two assemblies (`Berzerk.ContentPipeline` for build-time and `Berzerk/Source/Content` for runtime). These types are missing three critical fields that cause the T-pose: bind pose matrices (local-space bone transforms at rest), inverse bind pose matrices (used in the three-stage transform pipeline), and skeleton hierarchy (parent index array for hierarchy traversal). The AnimationClip also uses a per-bone dictionary structure (`Dictionary<string, List<Keyframe>>`) instead of the canonical flat sorted list (`List<Keyframe>`), and Keyframe stores a bone index that is built incorrectly by the current processor.

The recommended approach is to create new `SkinningData`, `AnimationClip`, and `Keyframe` types following the XNA SkinnedModel pattern exactly, with a custom `ContentTypeWriter<SkinningData>` in the pipeline assembly and a matching `ContentTypeReader<SkinningData>` in the game assembly. The XNA sample uses `[ContentSerializer]` attributes for automatic serialization, but a custom writer/reader is more robust for MonoGame (avoids reflection-based serialization issues, gives explicit control over binary format, and the current codebase already uses this pattern). The existing files should be replaced, not extended.

**Primary recommendation:** Replace the current AnimationData/AnimationClip/Keyframe types with SkinningData-based types that include bind pose, inverse bind pose, and skeleton hierarchy arrays, using a custom ContentTypeWriter/ContentTypeReader pair with explicit binary serialization matching the XNA SkinnedModel data layout.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
None -- all implementation decisions are delegated to Claude's discretion.

### Claude's Discretion
The user has delegated all implementation decisions for this phase to Claude. This includes:

- **Data structure organization** -- How SkinningData, AnimationClip, and Keyframe are structured (nested vs flat, required vs optional fields)
- **Serialization format** -- Binary layout, version handling, compression decisions, endianness
- **Animation clip storage** -- How multiple clips are organized (dictionary keys, file structure)
- **Matrix representation** -- How to store bind pose and inverse bind pose (full 4x4 matrices vs decomposed, bone index mapping)

**Guidance:** Follow MonoGame Content Pipeline conventions and prioritize:
1. Correctness -- data must survive serialization round-trip intact
2. Clarity -- structures should be easy to understand and debug
3. Efficiency -- reasonable binary size and deserialization speed
4. Compatibility -- work with standard Mixamo FBX structure

### Deferred Ideas (OUT OF SCOPE)
None -- discussion stayed within phase scope.
</user_constraints>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.4.1 | Game framework with Matrix, TimeSpan, ContentReader | Already in use, provides all math types needed for bone data |
| MonoGame.Framework.Content.Pipeline | 3.8.4.1 | ContentTypeWriter base class, ContentWriter for serialization | Already in use in Berzerk.ContentPipeline project |
| .NET 8.0 | 8.0 | Runtime, provides List, Dictionary, TimeSpan | Already in use |

### Supporting
No additional libraries needed. Phase 1 is pure data types using only MonoGame and .NET built-in types.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom ContentTypeWriter/Reader | [ContentSerializer] attribute (automatic serialization) | Automatic serialization requires shared assembly between pipeline and game projects, uses reflection, has known issues with AOT and trimming in MonoGame. Custom writer/reader is more explicit, matches current codebase pattern, and gives full control over binary format. **Use custom writer/reader.** |
| Full 4x4 Matrix storage | Decomposed SRT (Scale/Rotation/Translation) | SRT is more compact (10 floats vs 16) but requires decompose/recompose at read time. MonoGame's ContentWriter/ContentReader have built-in `Write(Matrix)` / `ReadMatrix()` methods that handle 4x4 matrices natively. Full matrices are simpler and match what the GPU expects. **Use full 4x4 matrices.** |
| Flat keyframe list (XNA canonical) | Per-bone keyframe dictionary (current approach) | Flat list is simpler for AnimationPlayer (single scan pointer), more cache-friendly, and matches the canonical XNA pattern that all MonoGame community code references. Per-bone dictionary makes it easier to look up specific bones but complicates sequential playback. **Use flat keyframe list.** |

## Architecture Patterns

### Recommended Project Structure
```
Berzerk.ContentPipeline/               # Build-time (pipeline assembly)
    SkinningData.cs                    # Pipeline-side SkinningData (same structure as runtime)
    AnimationClip.cs                   # Pipeline-side AnimationClip
    Keyframe.cs                        # Pipeline-side Keyframe
    SkinningDataWriter.cs              # ContentTypeWriter<SkinningData> - serializes to XNB
    MixamoModelProcessor.cs            # (exists, modified in Phase 2, not Phase 1)

Berzerk/Source/Content/                # Runtime (game assembly)
    SkinningData.cs                    # Runtime SkinningData - deserialized from XNB
    AnimationClip.cs                   # Runtime AnimationClip
    Keyframe.cs                        # Runtime Keyframe
    SkinningDataReader.cs              # ContentTypeReader<SkinningData> - reads from XNB
```

### Pattern 1: Dual Assembly Types with Matching Binary Contract

**What:** The same logical data type (SkinningData) exists in two separate assemblies -- one in the Content Pipeline extension (build-time) and one in the game project (runtime). They are NOT the same class (different namespaces, different assemblies) but must have identical structure. The ContentTypeWriter in the pipeline assembly serializes the pipeline version, and the ContentTypeReader in the game assembly deserializes into the runtime version.

**When to use:** Always for MonoGame Content Pipeline custom types. The pipeline assembly (`Berzerk.ContentPipeline`) references `MonoGame.Framework.Content.Pipeline` which is a build-time-only dependency. The game project must NOT reference the pipeline package. Therefore types cannot be shared directly.

**Why not a shared library:** The XNA tutorial approach uses a third "shared" assembly referenced by both. This adds project complexity and the types are small enough that duplication is manageable. The current codebase already uses the dual-assembly pattern with separate `AnimationData` in each assembly. Keep this pattern.

**Critical requirement:** The `GetRuntimeReader()` method in the writer must return the fully-qualified type name of the reader: `"Berzerk.Content.SkinningDataReader, Berzerk"`. And `GetRuntimeType()` must return: `"Berzerk.Content.SkinningData, Berzerk"`. If these strings are wrong, deserialization fails silently and `Model.Tag` is null at runtime.

### Pattern 2: SkinningData as Immutable Container

**What:** SkinningData is constructed once (at build time by the processor, or at read time by the reader) and is then immutable. All fields are set in the constructor. Properties have public getters and private setters (or are set only in the constructor).

**When to use:** Always. SkinningData is shared across animation player instances and should never be modified at runtime.

**Example:**
```csharp
// Source: XNA SkinningSample_4_0/SkinnedModel/SkinningData.cs
public class SkinningData
{
    public Dictionary<string, AnimationClip> AnimationClips { get; private set; }
    public List<Matrix> BindPose { get; private set; }
    public List<Matrix> InverseBindPose { get; private set; }
    public List<int> SkeletonHierarchy { get; private set; }

    public SkinningData(
        Dictionary<string, AnimationClip> animationClips,
        List<Matrix> bindPose,
        List<Matrix> inverseBindPose,
        List<int> skeletonHierarchy)
    {
        AnimationClips = animationClips;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        SkeletonHierarchy = skeletonHierarchy;
    }
}
```

### Pattern 3: Flat Keyframe List Sorted by Time

**What:** AnimationClip stores ALL keyframes for ALL bones in a single flat `List<Keyframe>`, sorted by time then by bone index. Each Keyframe contains a bone index, time, and transform matrix. The AnimationPlayer scans forward through this list sequentially.

**When to use:** This is the canonical XNA approach. The flat list is cache-friendly and simple for the runtime to consume.

**Example:**
```csharp
// Source: XNA SkinningSample_4_0/SkinnedModel/AnimationClip.cs
public class AnimationClip
{
    public TimeSpan Duration { get; private set; }
    public List<Keyframe> Keyframes { get; private set; }

    public AnimationClip(TimeSpan duration, List<Keyframe> keyframes)
    {
        Duration = duration;
        Keyframes = keyframes;
    }
}
```

### Pattern 4: Keyframe as Value-Like Struct or Small Class

**What:** Keyframe is a small data container with three fields: bone index (int), time (TimeSpan), and transform (Matrix). In the XNA sample it is a class with `[ContentSerializer]` attributes.

**Example:**
```csharp
// Source: XNA SkinningSample_4_0/SkinnedModel/Keyframe.cs
public class Keyframe
{
    public int Bone { get; private set; }
    public TimeSpan Time { get; private set; }
    public Matrix Transform { get; private set; }

    public Keyframe(int bone, TimeSpan time, Matrix transform)
    {
        Bone = bone;
        Time = time;
        Transform = transform;
    }
}
```

### Anti-Patterns to Avoid

- **Shared assembly between pipeline and game:** Adds a third project just for data types. Increases build complexity. The types are small -- duplication across two files is acceptable and matches the current codebase pattern.
- **Mutable data types:** SkinningData, AnimationClip, and Keyframe should be constructed once and not modified. Mutable setters invite bugs where runtime code accidentally mutates shared animation data.
- **Per-bone dictionary in AnimationClip:** The current `Dictionary<string, List<Keyframe>>` approach requires name-based lookups during playback and doesn't match the canonical pattern. The flat list approach is simpler for the AnimationPlayer to consume.
- **Storing bone names in Keyframe:** Bone names are resolved to indices at build time. Keyframes should only contain the integer bone index, not the string name. String lookups during animation playback are wasteful.
- **Parameterless default constructors as primary constructors:** The reader needs a way to construct types, but the preferred approach is to read all fields and pass them to a parameterful constructor, not to create an empty object and set fields one by one. However, having a private parameterless constructor for the reader's convenience is acceptable.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Matrix binary serialization | Custom float-by-float matrix writing (16 floats manually) | `ContentWriter.Write(Matrix)` and `ContentReader.ReadMatrix()` | MonoGame's built-in methods handle 4x4 matrix serialization correctly, including endianness. Writing 16 floats manually risks ordering mistakes. |
| TimeSpan serialization | Custom serialization of hours/minutes/seconds | Write `ticks` as `Int64`, reconstruct with `TimeSpan.FromTicks()` | Ticks is lossless and compact. The current codebase already uses this approach correctly. |
| Dictionary serialization | Custom hash table format | Write count, then key-value pairs sequentially | Standard pattern for dictionary serialization in content pipelines. No need for a custom format. |
| List serialization | Custom linked-list or array format | Write count, then elements sequentially | Standard pattern. ContentWriter does not have a built-in `WriteList<T>()`, so manual count+elements is the norm. |

**Key insight:** MonoGame's ContentWriter and ContentReader provide type-safe binary serialization for all XNA math types (Matrix, Vector2/3/4, Quaternion, Color) plus all .NET primitives. Use these instead of manual byte-level serialization.

## Common Pitfalls

### Pitfall 1: GetRuntimeReader Returns Wrong Type Name
**What goes wrong:** `Model.Tag` is null at runtime even though the pipeline builds successfully. No error is thrown.
**Why it happens:** `ContentTypeWriter.GetRuntimeReader()` returns an incorrect fully-qualified type name. MonoGame cannot find the ContentTypeReader class and silently fails to deserialize the Tag data.
**How to avoid:** The string must be exact: `"Namespace.ClassName, AssemblyName"`. For this project: `"Berzerk.Content.SkinningDataReader, Berzerk"`. Double-check namespace, class name, and assembly name. The assembly name is the `.csproj` project name, NOT the namespace.
**Warning signs:** Model loads fine, mesh renders, but `model.Tag` is null. No build errors, no runtime exceptions.

### Pitfall 2: Writer/Reader Binary Format Mismatch
**What goes wrong:** Runtime crashes with `EndOfStreamException` or `InvalidOperationException` during content loading, or data is silently corrupted (wrong values in matrices, wrong bone counts).
**Why it happens:** The ContentTypeWriter writes fields in one order and the ContentTypeReader reads them in a different order. Binary serialization is positional -- there are no field names or markers in the stream.
**How to avoid:** Write and Read methods must serialize fields in EXACTLY the same order. Document the binary format as comments in both files. Write a round-trip unit test: serialize -> deserialize -> compare.
**Warning signs:** Deserialization crashes, matrix values are garbage (e.g., extremely large numbers), bone counts are wrong.

### Pitfall 3: Forgetting Private Parameterless Constructor
**What goes wrong:** ContentTypeReader fails to construct the type during deserialization.
**Why it happens:** If using automatic serialization, MonoGame needs a parameterless constructor (can be private). With custom reader this is less of an issue since the reader constructs the object explicitly.
**How to avoid:** With custom writer/reader: read all fields from the stream, then call the parameterful constructor. No parameterless constructor needed. If you add one for convenience, make it private to prevent accidental use.
**Warning signs:** Exception during Load<Model>() saying it cannot create an instance of the type.

### Pitfall 4: Replacing Existing Types Without Updating All References
**What goes wrong:** Build errors across the solution because `AnimationData`, `AnimationClip`, `Keyframe` are referenced by `MixamoModelProcessor`, `AnimatedModel`, `EnemyRenderer`, and other classes.
**Why it happens:** Phase 1 creates new types (SkinningData) but the existing code still references the old types (AnimationData).
**How to avoid:** Phase 1 should create the NEW types alongside the old ones, NOT delete the old types yet. The old types will be replaced in Phase 2 (processor) and Phase 3/4 (runtime). Phase 1 only needs to ensure the new types compile and the writer/reader round-trip works. Alternatively, replace the old types but update all compile-breaking references to use the new types (with stub/placeholder behavior where the new types don't yet have data).
**Warning signs:** Build fails after adding new types. Existing tests or game code breaks.

### Pitfall 5: Incorrect Bone Count Assumptions in Binary Format
**What goes wrong:** Serialization writes bind pose, inverse bind pose, and skeleton hierarchy with different counts, causing deserialization to read past array boundaries.
**Why it happens:** The three arrays (bind pose, inverse bind pose, skeleton hierarchy) MUST all have the same length (one entry per bone). If the processor produces mismatched counts, serialization succeeds but deserialization reads incorrect data.
**How to avoid:** Assert that `bindPose.Count == inverseBindPose.Count == skeletonHierarchy.Count` in the SkinningData constructor. Write this count once in the binary stream, not three times. Read it once and use it to size all three arrays.
**Warning signs:** Bone count from deserialized data doesn't match expected count. Matrix values in inverse bind pose are actually skeleton hierarchy integers (or vice versa).

## Code Examples

Verified patterns from official sources and current codebase analysis:

### SkinningData Type (Runtime, Game Assembly)
```csharp
// Based on: XNA SkinningSample_4_0/SkinnedModel/SkinningData.cs
// Location: Berzerk/Source/Content/SkinningData.cs
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Berzerk.Content;

public class SkinningData
{
    /// <summary>
    /// Animation clips keyed by name (e.g., "idle", "walk", "run", "bash").
    /// </summary>
    public Dictionary<string, AnimationClip> AnimationClips { get; private set; }

    /// <summary>
    /// Bind pose matrices for each bone, in local space relative to parent.
    /// Index matches skeleton hierarchy order from MeshHelper.FlattenSkeleton().
    /// </summary>
    public List<Matrix> BindPose { get; private set; }

    /// <summary>
    /// Inverse bind pose matrices: Matrix.Invert(bone.AbsoluteTransform).
    /// Transforms vertices from model space to bone-local space.
    /// Used in Stage 3 of the three-stage transform pipeline.
    /// </summary>
    public List<Matrix> InverseBindPose { get; private set; }

    /// <summary>
    /// For each bone, the index of its parent bone. Root bone has parent -1.
    /// Index order matches BindPose and InverseBindPose arrays.
    /// </summary>
    public List<int> SkeletonHierarchy { get; private set; }

    public SkinningData(
        Dictionary<string, AnimationClip> animationClips,
        List<Matrix> bindPose,
        List<Matrix> inverseBindPose,
        List<int> skeletonHierarchy)
    {
        AnimationClips = animationClips;
        BindPose = bindPose;
        InverseBindPose = inverseBindPose;
        SkeletonHierarchy = skeletonHierarchy;
    }
}
```

### AnimationClip Type (Runtime, Game Assembly)
```csharp
// Based on: XNA SkinningSample_4_0/SkinnedModel/AnimationClip.cs
// Location: Berzerk/Source/Content/AnimationClip.cs
using System;
using System.Collections.Generic;

namespace Berzerk.Content;

public class AnimationClip
{
    /// <summary>
    /// Total duration of the animation.
    /// </summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// All keyframes for all bones, sorted by time.
    /// The AnimationPlayer scans through this list sequentially.
    /// </summary>
    public List<Keyframe> Keyframes { get; private set; }

    public AnimationClip(TimeSpan duration, List<Keyframe> keyframes)
    {
        Duration = duration;
        Keyframes = keyframes;
    }
}
```

### Keyframe Type (Runtime, Game Assembly)
```csharp
// Based on: XNA SkinningSample_4_0/SkinnedModel/Keyframe.cs
// Location: Berzerk/Source/Content/Keyframe.cs
using System;
using Microsoft.Xna.Framework;

namespace Berzerk.Content;

public class Keyframe
{
    /// <summary>
    /// Index of the bone this keyframe affects.
    /// Matches the index in SkinningData.BindPose/InverseBindPose/SkeletonHierarchy.
    /// </summary>
    public int Bone { get; private set; }

    /// <summary>
    /// Time offset from the start of the animation.
    /// </summary>
    public TimeSpan Time { get; private set; }

    /// <summary>
    /// Local-space bone transform at this point in time.
    /// Relative to the bone's parent, not absolute/world space.
    /// </summary>
    public Matrix Transform { get; private set; }

    public Keyframe(int bone, TimeSpan time, Matrix transform)
    {
        Bone = bone;
        Time = time;
        Transform = transform;
    }
}
```

### ContentTypeWriter (Pipeline Assembly)
```csharp
// Location: Berzerk.ContentPipeline/SkinningDataWriter.cs
// Binary format documented inline -- Reader MUST match this exactly.
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace Berzerk.ContentPipeline;

[ContentTypeWriter]
public class SkinningDataWriter : ContentTypeWriter<SkinningData>
{
    protected override void Write(ContentWriter output, SkinningData value)
    {
        // --- Skeleton data ---
        // All three arrays share the same count (one per bone)
        int boneCount = value.BindPose.Count;
        output.Write(boneCount);

        // Bind pose: boneCount matrices
        for (int i = 0; i < boneCount; i++)
            output.Write(value.BindPose[i]);

        // Inverse bind pose: boneCount matrices
        for (int i = 0; i < boneCount; i++)
            output.Write(value.InverseBindPose[i]);

        // Skeleton hierarchy: boneCount ints (parent indices)
        for (int i = 0; i < boneCount; i++)
            output.Write(value.SkeletonHierarchy[i]);

        // --- Animation clips ---
        output.Write(value.AnimationClips.Count);
        foreach (var kvp in value.AnimationClips)
        {
            // Clip name (dictionary key)
            output.Write(kvp.Key);

            // Clip duration as ticks
            output.Write(kvp.Value.Duration.Ticks);

            // Keyframes: count then each keyframe
            output.Write(kvp.Value.Keyframes.Count);
            foreach (var keyframe in kvp.Value.Keyframes)
            {
                output.Write(keyframe.Bone);
                output.Write(keyframe.Time.Ticks);
                output.Write(keyframe.Transform);
            }
        }
    }

    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
        return "Berzerk.Content.SkinningDataReader, Berzerk";
    }

    public override string GetRuntimeType(TargetPlatform targetPlatform)
    {
        return "Berzerk.Content.SkinningData, Berzerk";
    }
}
```

### ContentTypeReader (Game Assembly)
```csharp
// Location: Berzerk/Source/Content/SkinningDataReader.cs
// Binary format MUST match SkinningDataWriter.Write() exactly.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace Berzerk.Content;

public class SkinningDataReader : ContentTypeReader<SkinningData>
{
    protected override SkinningData Read(ContentReader input, SkinningData existingInstance)
    {
        // --- Skeleton data ---
        int boneCount = input.ReadInt32();

        var bindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            bindPose.Add(input.ReadMatrix());

        var inverseBindPose = new List<Matrix>(boneCount);
        for (int i = 0; i < boneCount; i++)
            inverseBindPose.Add(input.ReadMatrix());

        var skeletonHierarchy = new List<int>(boneCount);
        for (int i = 0; i < boneCount; i++)
            skeletonHierarchy.Add(input.ReadInt32());

        // --- Animation clips ---
        int clipCount = input.ReadInt32();
        var animationClips = new Dictionary<string, AnimationClip>(clipCount);

        for (int i = 0; i < clipCount; i++)
        {
            string clipName = input.ReadString();
            var duration = TimeSpan.FromTicks(input.ReadInt64());

            int keyframeCount = input.ReadInt32();
            var keyframes = new List<Keyframe>(keyframeCount);
            for (int j = 0; j < keyframeCount; j++)
            {
                int bone = input.ReadInt32();
                var time = TimeSpan.FromTicks(input.ReadInt64());
                var transform = input.ReadMatrix();
                keyframes.Add(new Keyframe(bone, time, transform));
            }

            animationClips[clipName] = new AnimationClip(duration, keyframes);
        }

        return new SkinningData(animationClips, bindPose, inverseBindPose, skeletonHierarchy);
    }
}
```

## Existing Code Analysis

### What Exists Now (Must Be Replaced)

The current codebase has a parallel type system that is structurally insufficient:

**Pipeline assembly (`Berzerk.ContentPipeline/`):**
- `AnimationData.cs` -- Has `Clips` dictionary and `BoneIndices` dictionary, but NO bind pose, NO inverse bind pose, NO skeleton hierarchy
- `AnimationClip.cs` -- Has `Name`, `Duration`, and `Dictionary<string, List<Keyframe>>` (per-bone dictionary, not flat list)
- `Keyframe.cs` -- Has `Time`, `BoneIndex`, `Transform` (correct fields, but `BoneIndex` is populated incorrectly by the processor)
- `AnimationDataWriter.cs` -- Serializes the current AnimationData format including the per-bone dictionary structure

**Game assembly (`Berzerk/Source/Content/`):**
- `AnimationData.cs` -- Mirror of pipeline version
- `AnimationClip.cs` -- Mirror of pipeline version
- `Keyframe.cs` -- Mirror of pipeline version
- `AnimationDataReader.cs` -- Deserializes the current format

**Files that reference the old types (will need updates in later phases):**
- `Berzerk.ContentPipeline/MixamoModelProcessor.cs` -- Creates `AnimationData`, calls `BuildBoneIndices`, `ExtractKeyframes`
- `Berzerk/Source/Graphics/AnimatedModel.cs` -- Uses `AnimationData` at runtime
- `Berzerk/Source/Enemies/EnemyRenderer.cs` -- Uses `AnimatedModel` which uses `AnimationData`

### Key Differences: Current vs Target

| Aspect | Current (AnimationData) | Target (SkinningData) |
|--------|------------------------|----------------------|
| Bind pose | Not stored | `List<Matrix> BindPose` -- local-space bone rest transforms |
| Inverse bind pose | Not stored | `List<Matrix> InverseBindPose` -- vertex-to-bonespace transforms |
| Skeleton hierarchy | Not stored | `List<int> SkeletonHierarchy` -- parent index per bone |
| Bone name mapping | `Dictionary<string, int> BoneIndices` | Not stored in SkinningData (indices come from `MeshHelper.FlattenSkeleton()` order) |
| Clip keyframe format | `Dictionary<string, List<Keyframe>>` per bone name | `List<Keyframe>` flat sorted list for all bones |
| Clip name storage | `AnimationClip.Name` property + dictionary key | Dictionary key only (no `Name` property on clip) |
| Immutability | Mutable (public setters, parameterless constructors) | Immutable (private setters, parameterful constructors) |

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `[ContentSerializer]` automatic serialization (XNA 3.1+) | Custom ContentTypeWriter/ContentTypeReader with explicit binary format | Became common practice in MonoGame 3.x era | Custom writer/reader avoids reflection issues, AOT problems, and gives explicit format control |
| Shared assembly for pipeline/runtime types | Dual assembly with matching types | MonoGame convention | Eliminates complex project reference chains, keeps pipeline deps out of game assembly |
| Per-bone keyframe dictionaries | Flat sorted keyframe list | XNA SkinningSample (canonical since 2007) | Simpler playback code, better cache locality, matches all community reference implementations |

**Deprecated/outdated:**
- `AnimationData` type in the current codebase: Missing bind pose, inverse bind pose, and skeleton hierarchy. Must be replaced with `SkinningData`.
- `BoneIndices` dictionary in AnimationData: Bone indices should come from `MeshHelper.FlattenSkeleton()` ordering, not a custom mapping stored alongside animation data.

## Binary Format Specification

The binary format for SkinningData written by ContentTypeWriter and read by ContentTypeReader:

```
[Int32]  boneCount                    -- number of bones in skeleton

[Matrix] bindPose[0..boneCount-1]     -- boneCount * 64 bytes (4x4 float matrices)
[Matrix] inverseBindPose[0..boneCount-1]  -- boneCount * 64 bytes
[Int32]  skeletonHierarchy[0..boneCount-1]  -- boneCount * 4 bytes (parent indices)

[Int32]  clipCount                    -- number of animation clips

For each clip (clipCount times):
    [String]  clipName                -- dictionary key name
    [Int64]   durationTicks           -- TimeSpan.Ticks
    [Int32]   keyframeCount           -- total keyframes in flat list

    For each keyframe (keyframeCount times):
        [Int32]   bone               -- bone index
        [Int64]   timeTicks          -- TimeSpan.Ticks
        [Matrix]  transform          -- 4x4 local-space bone transform
```

**Size estimate for typical Mixamo model:**
- 65 bones: (65 * 64) + (65 * 64) + (65 * 4) = 8,580 bytes for skeleton data
- 1 animation clip at 30fps for 2 seconds, 65 bones: 60 * 65 = 3,900 keyframes
- Per keyframe: 4 + 8 + 64 = 76 bytes
- Per clip: ~296,400 bytes (~290 KB)
- 4 clips (idle, walk, run, bash): ~1.16 MB total
- Manageable size; no compression needed.

## Open Questions

1. **Should old types be deleted or kept alongside new types in Phase 1?**
   - What we know: The old types are referenced by MixamoModelProcessor, AnimatedModel, and EnemyRenderer. Deleting them in Phase 1 would cause build errors in files that are not modified until Phase 2-4.
   - What's unclear: Whether the planner prefers a "create new, delete old later" approach or a "replace and fix all references" approach.
   - Recommendation: Create new SkinningData types alongside old AnimationData types. Mark old types with `[Obsolete]` comments. Delete old types in Phase 2 when the processor is updated to use SkinningData.

2. **Should Phase 1 include a round-trip test?**
   - What we know: The success criteria say "ContentTypeReader deserializes SkinningData at runtime and produces identical data to what was written." This implies verification of round-trip correctness.
   - What's unclear: Whether to write an automated test or rely on manual verification during Phase 2 when real data flows through the pipeline.
   - Recommendation: Write a simple in-memory round-trip test that creates a SkinningData with known values, serializes it, deserializes it, and asserts equality. This catches writer/reader mismatches before Phase 2 begins.

3. **Namespace for pipeline-side types**
   - What we know: Current pipeline types use `Berzerk.ContentPipeline` namespace. Runtime types use `Berzerk.Content`.
   - What's unclear: Whether the pipeline-side SkinningData should stay in `Berzerk.ContentPipeline` (matching current convention) or use `Berzerk.Content` (matching runtime names but in a different assembly).
   - Recommendation: Keep `Berzerk.ContentPipeline` for pipeline-side types and `Berzerk.Content` for runtime types. This matches the current convention and avoids confusion about which assembly a type belongs to.

## Sources

### Primary (HIGH confidence)
- [XNA SkinningSample_4_0 -- SkinningData.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModel/SkinningData.cs) -- Canonical SkinningData type with BindPose, InverseBindPose, SkeletonHierarchy, AnimationClips
- [XNA SkinningSample_4_0 -- AnimationClip.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModel/AnimationClip.cs) -- Canonical AnimationClip with Duration and flat List<Keyframe>
- [XNA SkinningSample_4_0 -- Keyframe.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModel/Keyframe.cs) -- Canonical Keyframe with Bone (int), Time (TimeSpan), Transform (Matrix)
- [MonoGame ContentWriter API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler.ContentWriter.html) -- Write(Matrix), Write(Int32), Write(Int64), Write(String) method signatures
- [MonoGame ContentReader API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.ContentReader.html) -- ReadMatrix(), ReadInt32(), ReadInt64(), ReadString() method signatures
- [MonoGame ContentTypeWriter<T> API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler.ContentTypeWriter-1.html) -- Write(), GetRuntimeReader(), GetRuntimeType() methods
- Direct codebase analysis -- All 8 existing type/serialization files in `Berzerk.ContentPipeline/` and `Berzerk/Source/Content/`

### Secondary (MEDIUM confidence)
- [Tutorial: XNA SkinnedSample in MonoGame](https://community.monogame.net/t/tutorial-how-to-get-xnas-skinnedsample-working-with-monogame/7609) -- Shared type assembly approach, pipeline setup
- [Extending MonoGame's Content Pipeline](https://badecho.com/index.php/2022/08/17/extending-pipeline/) -- ContentTypeWriter/Reader architecture, GetRuntimeReader best practices
- [Custom ContentTypeReader exception discussion](https://community.monogame.net/t/solved-custom-contenttypereader-exception/6332) -- Assembly name mismatch pitfall
- [Automatic XNB serialization](https://shawnhargreaves.com/blog/automatic-xnb-serialization-in-xna-game-studio-3-1.html) -- [ContentSerializer] attribute behavior explanation

### Tertiary (LOW confidence)
- [MonoGame Issue #8622: Custom content and AOT](https://github.com/MonoGame/MonoGame/issues/8622) -- Documents issues with automatic serialization under AOT (supports decision to use custom writer/reader)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- MonoGame 3.8.4.1 with custom ContentTypeWriter/Reader is the established approach, verified against official docs and canonical XNA sample
- Architecture: HIGH -- XNA SkinnedModel SkinningData type is the universal standard, verified against source code. Dual-assembly pattern matches current codebase convention.
- Pitfalls: HIGH -- GetRuntimeReader string mismatch and writer/reader format mismatch are well-documented and verified across multiple community sources and official docs. The "old types still referenced" pitfall is verified from direct codebase inspection.
- Binary format: HIGH -- Uses only MonoGame built-in Write/Read methods for Matrix, Int32, Int64, String. Format is straightforward sequential serialization.

**Research date:** 2026-02-10
**Valid until:** 2026-03-12 (30 days -- MonoGame 3.8.4.1 is stable, XNA patterns are decade-old standard)
