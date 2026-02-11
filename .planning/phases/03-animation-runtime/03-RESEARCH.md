# Phase 3: Animation Runtime - Research

**Researched:** 2026-02-11
**Domain:** Skeletal Animation Runtime (MonoGame, XNA AnimationPlayer pattern)
**Confidence:** HIGH

## Summary

Phase 3 implements the AnimationPlayer -- the runtime component that reads SkinningData from Model.Tag (produced by Phase 2's MixamoModelProcessor) and computes correct skinning matrices each frame via the canonical three-stage transform pipeline (local bone transforms from keyframes, world transforms via hierarchy composition, skinning matrices via inverse bind pose multiplication). This phase also handles loading multiple animation clips from separate Mixamo FBX files and merging them into a single animation dictionary, replacing the current broken AnimatedModel implementation that uses the old AnimationData types.

The implementation follows the canonical XNA SkinningSample_4_0 AnimationPlayer architecture. The XNA sample is the universal reference in the MonoGame community and maps directly to the SkinningData types created in Phase 1. The core algorithm is straightforward: initialize bone transforms from bind pose, scan keyframes forward overwriting bone transforms as time advances, compose hierarchy from root to leaf, multiply by inverse bind pose for GPU. The critical design decision is that the XNA AnimationPlayer does NOT interpolate between keyframes -- it simply applies the most recent keyframe transform for each bone. This is correct because Mixamo exports keyframes at the FBX sampling rate (typically every frame at 30fps), making interpolation unnecessary for standard playback.

Phase 3's scope is pure math and data management -- no rendering changes. The AnimationPlayer produces a `Matrix[] skinTransforms` array, and the AnimatedModel is refactored to use SkinningData instead of AnimationData and to expose the skin transforms for Phase 4's rendering work. The old AnimationData/AnimationClip/Keyframe runtime types and AnimationDataReader are deleted.

**Primary recommendation:** Port the XNA SkinningSample_4_0 AnimationPlayer directly, adapting field names to match the existing SkinningData/SkinningDataClip/SkinningDataKeyframe types. Refactor AnimatedModel to use SkinningData with clip merging by name. Delete old runtime types.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.4.1 | Framework (Matrix, TimeSpan, GameTime) | Already in use; provides all math types needed |

### Supporting
No additional libraries needed. Phase 3 is pure C# math operating on types already defined in Phase 1.

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Flat keyframe scan (XNA pattern) | Per-bone keyframe interpolation | XNA pattern is simpler, cache-friendly, sufficient for 30fps Mixamo data. Interpolation adds complexity with no visual benefit when keyframe density matches frame rate. |
| Matrix-only keyframes | SRT (Scale/Rotation/Translation) decomposition | SRT enables proper Slerp interpolation for slow-motion or keyframe reduction scenarios. Not needed for v1 where Mixamo exports dense keyframes. Phase 5 (crossfade blending) may revisit this. |

## Architecture Patterns

### Recommended Project Structure
```
Berzerk/Source/
    Content/                      # Runtime content type readers + data types
        SkinningData.cs           # [EXISTS] BindPose, InverseBindPose, Hierarchy, Clips
        SkinningDataClip.cs       # [EXISTS] Duration + List<SkinningDataKeyframe>
        SkinningDataKeyframe.cs   # [EXISTS] Bone (int), Time (TimeSpan), Transform (Matrix)
        SkinningDataReader.cs     # [EXISTS] ContentTypeReader<SkinningData>
        AnimationData.cs          # [DELETE] Old type, replaced by SkinningData
        AnimationClip.cs          # [DELETE] Old type, replaced by SkinningDataClip
        Keyframe.cs               # [DELETE] Old type, replaced by SkinningDataKeyframe
        AnimationDataReader.cs    # [DELETE] Old reader, replaced by SkinningDataReader

    Graphics/
        AnimatedModel.cs          # [REWRITE] Refactored to use SkinningData + AnimationPlayer
        AnimationPlayer.cs        # [NEW] Three-stage transform pipeline (port of XNA canonical)
```

### Pattern 1: XNA AnimationPlayer - Three-Stage Transform Pipeline (CRITICAL)

**What:** The canonical algorithm for computing skinning matrices from keyframe animation data. Three sequential stages, each dependent on the previous.

**When to use:** Always. This is the only correct way to produce skinning matrices for SkinnedEffect.

**Implementation (adapted from XNA SkinningSample_4_0):**

```csharp
// Source: XNA SkinningSample_4_0/SkinnedModel/AnimationPlayer.cs
// Adapted to use Berzerk SkinningData/SkinningDataClip/SkinningDataKeyframe types

public class AnimationPlayer
{
    // Current clip state
    private SkinningDataClip currentClip;
    private TimeSpan currentTime;
    private int currentKeyframe;

    // Three transform arrays (one per bone)
    private Matrix[] boneTransforms;   // Stage 1: local space
    private Matrix[] worldTransforms;  // Stage 2: model space
    private Matrix[] skinTransforms;   // Stage 3: for GPU

    // Reference to skeleton data
    private SkinningData skinningData;

    public AnimationPlayer(SkinningData skinningData)
    {
        this.skinningData = skinningData;
        int boneCount = skinningData.BindPose.Count;
        boneTransforms = new Matrix[boneCount];
        worldTransforms = new Matrix[boneCount];
        skinTransforms = new Matrix[boneCount];
    }

    public void StartClip(SkinningDataClip clip)
    {
        currentClip = clip;
        currentTime = TimeSpan.Zero;
        currentKeyframe = 0;
        // Initialize bone transforms to bind pose (rest position)
        skinningData.BindPose.CopyTo(boneTransforms, 0);
    }

    // Stage 1: Decode keyframes into local bone transforms
    public void UpdateBoneTransforms(TimeSpan time, bool relativeToCurrentTime)
    {
        if (relativeToCurrentTime)
        {
            time += currentTime;
            // Loop: wrap time around duration
            while (time >= currentClip.Duration)
                time -= currentClip.Duration;
        }

        // If time went backwards (e.g., loop wrap), reset scan position
        if (time < currentTime)
        {
            currentKeyframe = 0;
            skinningData.BindPose.CopyTo(boneTransforms, 0);
        }

        currentTime = time;

        // Scan forward through flat keyframe list
        IList<SkinningDataKeyframe> keyframes = currentClip.Keyframes;
        while (currentKeyframe < keyframes.Count)
        {
            SkinningDataKeyframe kf = keyframes[currentKeyframe];
            if (kf.Time > currentTime)
                break;
            boneTransforms[kf.Bone] = kf.Transform;
            currentKeyframe++;
        }
    }

    // Stage 2: Compose hierarchy (root to leaf)
    public void UpdateWorldTransforms(Matrix rootTransform)
    {
        worldTransforms[0] = boneTransforms[0] * rootTransform;
        for (int bone = 1; bone < worldTransforms.Length; bone++)
        {
            int parentBone = skinningData.SkeletonHierarchy[bone];
            worldTransforms[bone] = boneTransforms[bone] * worldTransforms[parentBone];
        }
    }

    // Stage 3: Apply inverse bind pose
    public void UpdateSkinTransforms()
    {
        for (int bone = 0; bone < skinTransforms.Length; bone++)
        {
            skinTransforms[bone] = skinningData.InverseBindPose[bone] *
                                   worldTransforms[bone];
        }
    }

    public Matrix[] GetSkinTransforms() => skinTransforms;
}
```

### Pattern 2: Animation Clip Merging from Separate FBX Files

**What:** Mixamo exports one FBX per animation. The base model FBX (test-character.fbx) has the mesh + skeleton + possibly one embedded clip ("mixamo.com"). Animation-only FBX files have clips but no skeleton (0 bones in SkinningData). At runtime, load the base model to get SkinningData with full skeleton, then load each animation-only model and copy their clips into the base SkinningData's AnimationClips dictionary.

**Critical issue from Phase 2:** Animation-only FBX files currently extract only 1 bone channel each (mixamorig:Hips) because without a skeleton, the processor's `ProcessWithoutSkeleton` method assigns incrementing bone indices from animation channel names. The bone indices in animation-only clips do NOT match the base model's FlattenSkeleton ordering. This means **animation clips from animation-only files cannot be used directly** -- the bone index mapping is wrong.

**Resolution options (choose during planning):**
1. **Re-map bone indices at load time:** When merging an animation-only clip, look up each keyframe's bone by name (requires storing bone names in the animation-only SkinningData, which is NOT currently done -- only indices are stored).
2. **Fix the processor to include bone name data in animation-only clips:** Add a bone name array to SkinningData for animation-only files.
3. **Download animations "with skin" so the processor gets a full skeleton:** Then bone indices match because both files go through FlattenSkeleton.
4. **Use only the embedded "mixamo.com" clip from each file:** The base model has 1 clip with 24 keyframes. Animation-only files have clips but with wrong bone indices.

**Recommendation:** Option 3 is the simplest -- re-download animation FBX files "with skin" from Mixamo. Then the processor produces correct FlattenSkeleton bone indices for both the base model and each animation file. The 1-bone-channel limitation only applies to "without skin" animation files where no skeleton is detected.

**If re-downloading is not feasible:** Option 2 requires modifying SkinningData types (Phase 1 output) and SkinningDataWriter/Reader (add bone name list). This is a Phase 2 insertion. Option 1 cannot work because the current data format stores bone indices only, not names.

### Pattern 3: AnimatedModel Refactored to Use SkinningData

**What:** The current AnimatedModel uses old AnimationData types (bone name-keyed keyframes, string lookup per bone per frame). Refactor to use SkinningData + AnimationPlayer for correct three-stage pipeline.

**Key changes:**
- Replace `AnimationData? _animationData` with `SkinningData? _skinningData` + `AnimationPlayer? _animationPlayer`
- Replace `ApplyKeyframes()` and `InterpolateTransform()` with `AnimationPlayer.Update()` call
- Replace `AddAnimationsFrom()` to merge SkinningDataClip entries by name from animation-only models
- Keep public API: `LoadContent()`, `Update()`, `PlayAnimation()`, `GetAnimationNames()`
- **Do NOT change Draw()** -- that is Phase 4's scope. Keep the existing BasicEffect Draw for now. The skinTransforms will be computed but not yet sent to GPU.

### Anti-Patterns to Avoid

- **Per-bone string lookup each frame:** The old AnimatedModel did `for (int i = 0; i < _model.Bones.Count; i++) if (_model.Bones[i].Name == boneName)` every frame. The new AnimationPlayer uses integer bone indices from keyframes -- zero string operations during playback.

- **Using Model.Bones ordering instead of SkinningData ordering:** `Model.Bones` includes non-skeleton nodes (mesh nodes, root transform nodes). SkinningData's bone ordering comes from `MeshHelper.FlattenSkeleton()` which is skeleton-only. These are different index spaces. The AnimationPlayer must use SkinningData's ordering exclusively.

- **Skipping bind pose initialization in StartClip:** If you don't reset boneTransforms to bind pose when starting a new clip, leftover transforms from the previous clip persist for any bones not touched by the new clip's early keyframes. The XNA pattern copies BindPose at the start of each clip.

- **Per-frame Matrix.Decompose for interpolation:** The old code decomposed matrices to SRT for interpolation every frame. The XNA pattern avoids this entirely by not interpolating -- it just applies the most recent keyframe. For 30fps Mixamo data at 60fps game rate, the worst case is a 1-frame lag (~16ms) which is imperceptible.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Three-stage transform pipeline | Custom transform composition | XNA AnimationPlayer pattern verbatim | Battle-tested, matches exactly what SkinnedEffect expects |
| Keyframe interpolation | Matrix decomposition + Slerp every frame | Flat keyframe scan (overwrite boneTransforms[kf.Bone]) | Mixamo data is dense (30fps); interpolation is unnecessary overhead |
| Bone hierarchy traversal | Recursive tree walk with name lookups | Linear array scan using SkeletonHierarchy parent indices | O(n) vs O(n log n), no allocations, cache-friendly |
| Animation looping | Custom timer with manual wrap-around | `while (time >= duration) time -= duration` (XNA pattern) | Handles multi-loop-per-frame edge case correctly |

**Key insight:** The XNA AnimationPlayer is ~80 lines of code. It is simple, correct, and complete. There is no reason to deviate from it.

## Common Pitfalls

### Pitfall 1: Animation-Only FBX Bone Index Mismatch
**What goes wrong:** Animation-only FBX files (downloaded "without skin") produce SkinningData with 0 bones and incrementally-assigned bone indices that don't match the base model's FlattenSkeleton ordering. Merging these clips and playing them produces garbage -- wrong bones get wrong transforms.
**Why it happens:** Without a skeleton, the processor assigns bone indices by channel order (first channel seen = index 0, etc.), which is arbitrary and different from FlattenSkeleton's depth-first order.
**How to avoid:** Either (a) re-download animations "with skin" so they have a proper skeleton, or (b) add bone name mapping to the data format for runtime re-indexing.
**Warning signs:** Only 1 bone channel per animation-only file in build output (see Phase 2 Summary table). Animation plays but only the hip bone moves.

### Pitfall 2: BindPose.CopyTo Requires List-to-Array
**What goes wrong:** The XNA sample uses `skinningData.BindPose.CopyTo(boneTransforms, 0)` but `SkinningData.BindPose` is `List<Matrix>`, not `Matrix[]`. `List<Matrix>.CopyTo(Matrix[], int)` works correctly in .NET -- no issue here. This is NOT a pitfall after all, just worth noting the type difference.
**How to avoid:** `List<T>.CopyTo(T[], int)` is the correct API call. No adaptation needed.

### Pitfall 3: Root Transform vs Identity
**What goes wrong:** Passing `Matrix.Identity` as rootTransform to `UpdateWorldTransforms()` when the game applies model scale/rotation externally is correct. But if someone passes the game's world matrix (with scale 0.01f for Mixamo), the skin transforms will be double-transformed -- once by skinning and once by the world matrix in the shader.
**Why it happens:** Confusion about what rootTransform means. In the XNA pattern, rootTransform is for adjusting the skeleton's root relative to model space (usually Identity). The model's position/rotation/scale in the game world is set separately on the effect's World matrix.
**How to avoid:** Always pass `Matrix.Identity` as rootTransform (or expose it for advanced use cases like root motion). The model-to-world transform goes on SkinnedEffect.World, not on rootTransform.
**Warning signs:** Character appears at 0.01x scale or rotated 180 degrees in the animation but correct in world space (or vice versa).

### Pitfall 4: Forgetting to Reset currentKeyframe on Loop
**What goes wrong:** When animation loops (time wraps to 0), if currentKeyframe isn't reset to 0, no keyframes are scanned and bones keep their last-frame transforms forever. The animation freezes on the last frame.
**Why it happens:** The `time < currentTime` check in UpdateBoneTransforms handles this -- when time goes backwards (loop wrap), it resets currentKeyframe to 0 and re-copies bind pose. But if the loop logic uses modulo instead of the while-subtract pattern, time might not actually go below currentTime.
**How to avoid:** Use the exact XNA pattern: `while (time >= duration) time -= duration` for looping, and `if (time < currentTime) { reset; }` for detecting backwards time.

### Pitfall 5: Old Runtime Types Still Referenced
**What goes wrong:** After deleting AnimationData/AnimationClip/Keyframe/AnimationDataReader from the runtime project, the solution fails to compile because BerzerkGame.cs, AnimatedModel.cs, and EnemyRenderer.cs still reference these types.
**Why it happens:** The old types are used by the current AnimatedModel (which casts Model.Tag to AnimationData) and by EnemyRenderer (which creates AnimatedModel instances).
**How to avoid:** The refactored AnimatedModel casts Model.Tag to SkinningData instead. All downstream code (EnemyRenderer, BerzerkGame) uses AnimatedModel's public API (LoadContent, Update, PlayAnimation, Draw) which doesn't expose the internal type. Delete old types and fix the AnimatedModel internals. The public API surface is preserved.
**Warning signs:** Compile errors referencing AnimationData, AnimationClip, or Keyframe.

## Code Examples

### Example 1: AnimationPlayer.Update() - Full Update Cycle

```csharp
// Source: XNA SkinningSample_4_0 AnimationPlayer, adapted for Berzerk types
// Called every frame from AnimatedModel.Update(gameTime)

public void Update(TimeSpan time, bool relativeToCurrentTime, Matrix rootTransform)
{
    UpdateBoneTransforms(time, relativeToCurrentTime);
    UpdateWorldTransforms(rootTransform);
    UpdateSkinTransforms();
}

// Usage in AnimatedModel:
public void Update(GameTime gameTime)
{
    if (_animationPlayer == null) return;
    _animationPlayer.Update(
        gameTime.ElapsedGameTime,  // delta time
        true,                       // relative to current time
        Matrix.Identity             // no root transform adjustment
    );
}
```

### Example 2: StartClip - Switching Animations

```csharp
// Source: XNA SkinningSample_4_0 AnimationPlayer
public void StartClip(SkinningDataClip clip)
{
    if (clip == null)
        throw new ArgumentNullException(nameof(clip));

    currentClip = clip;
    currentTime = TimeSpan.Zero;
    currentKeyframe = 0;

    // Critical: reset bone transforms to bind pose
    // Without this, old animation's transforms leak into the new clip
    skinningData.BindPose.CopyTo(boneTransforms, 0);
}

// Usage in AnimatedModel:
public void PlayAnimation(string clipName)
{
    if (_skinningData == null) return;
    if (_skinningData.AnimationClips.TryGetValue(clipName, out var clip))
    {
        _animationPlayer.StartClip(clip);
        _currentClipName = clipName;
    }
}
```

### Example 3: Animation Clip Merging

```csharp
// Merging animation clips from animation-only FBX files
// The base model's SkinningData has the skeleton; animation-only models have just clips
public void AddAnimationsFrom(ContentManager content, string animationPath, string? animationName = null)
{
    if (_skinningData == null) return;

    var animModel = content.Load<Model>(animationPath);
    var animSkinningData = animModel.Tag as SkinningData;

    if (animSkinningData == null) return;

    foreach (var clipEntry in animSkinningData.AnimationClips)
    {
        string targetName = animationName ?? clipEntry.Key;
        if (!_skinningData.AnimationClips.ContainsKey(targetName))
        {
            _skinningData.AnimationClips[targetName] = clipEntry.Value;
        }
    }
}
```

## Critical Issue: Animation-Only FBX Bone Coverage

### The Problem

Phase 2's content build output reveals a significant limitation:

| FBX File | Bones | Clips | Keyframes | Bone Channels |
|----------|-------|-------|-----------|---------------|
| test-character.fbx | 65 | 1 ("mixamo.com") | 24 | Full skeleton |
| idle.fbx | 0 | 1 ("mixamo.com") | 251 | ~1 (mixamorig:Hips only) |
| walk.fbx | 0 | 1 ("mixamo.com") | 32 | ~1 |
| run.fbx | 0 | 1 ("mixamo.com") | 26 | ~1 |
| bash.fbx | 0 | 1 ("mixamo.com") | 121 | ~1 |

The animation-only files have **only 1 bone channel** because without a skeleton, `ProcessWithoutSkeleton` only finds 1 animation channel node (the Hips). The full 65-bone animation data is present in the FBX file but the processor can't map it to bone indices without a skeleton reference.

### Impact on Phase 3

If we proceed with the current XNB output, playing the merged "idle" clip would animate only the Hips bone (and with wrong index mapping). All other 64 bones stay in bind pose = T-pose with slight hip wiggle.

### Recommended Resolution

**Re-download the 4 animation FBX files from Mixamo with "With Skin" option enabled.** This adds the skeleton to each file, causing the processor to use the `ProcessAnimations` path (with FlattenSkeleton) instead of `ProcessWithoutSkeleton`. The bone indices will match the base model because Mixamo uses the same skeleton for all animations of the same character. The XNB files will be larger (include mesh data) but the mesh is simply ignored at runtime during merging.

This is a one-time asset change that requires no code modifications to Phase 1 or Phase 2. The processor already handles "with skin" files correctly (test-character.fbx proves it).

**Alternative:** Modify the processor to use bone names from the base model's skeleton when processing animation-only files. This requires Phase 2 rework and is more complex.

### Clip Naming

All clips from Mixamo are named "mixamo.com" (the default animation name). The `AddAnimationsFrom` method already accepts a custom `animationName` parameter to rename clips during merging (e.g., `AddAnimationsFrom(content, "Models/idle", "idle")`). This existing pattern works correctly.

## State of the Art

| Old Approach (Current Code) | New Approach (Phase 3) | Impact |
|---------------------------|----------------------|--------|
| AnimationData with Dictionary<string, List<Keyframe>> per bone | SkinningData with flat List<SkinningDataKeyframe> per clip | Simpler playback, cache-friendly, correct bone indices |
| String-based bone lookup every frame | Integer bone index from keyframe data | Zero string operations during playback |
| Matrix.Decompose + Slerp interpolation | Direct keyframe matrix application (no interpolation) | Faster, simpler, sufficient for dense keyframe data |
| Model.Bones ordering (includes non-bone nodes) | SkinningData ordering (FlattenSkeleton, skeleton only) | Correct bone index space for GPU skinning |
| No inverse bind pose | InverseBindPose[i] * worldTransforms[i] | Required for SkinnedEffect to deform vertices correctly |
| One AnimatedModel per animation clip (EnemyRenderer pattern) | One AnimatedModel with merged clips | Correct architecture for Phase 4 rendering |

**Deprecated/outdated:**
- `AnimationData` / `AnimationClip` / `Keyframe` / `AnimationDataReader` -- old runtime types from before Phase 1. Delete in Phase 3.
- `AnimatedModel.ApplyKeyframes()` / `InterpolateTransform()` -- replaced by AnimationPlayer three-stage pipeline.

## Scope Boundaries

### Phase 3 INCLUDES:
- New AnimationPlayer class implementing three-stage transform pipeline
- Refactored AnimatedModel using SkinningData + AnimationPlayer
- Animation clip merging from separate FBX files
- Clip switching and loop control
- Deletion of old runtime types (AnimationData, AnimationClip, Keyframe, AnimationDataReader)
- Re-downloading animation FBX files "with skin" (if needed to fix bone coverage)

### Phase 3 does NOT include:
- Changing the Draw() method to use SkinnedEffect (that is Phase 4: REND-01, REND-02)
- Refactoring EnemyRenderer to share Model instances (that is Phase 4: GAME-02)
- Crossfade blending between animations (that is Phase 5: PLSH-01)
- Animation speed control, events, state machines (v2 scope)

### Verification Strategy
Since Phase 3 produces skinning matrices but Phase 4 renders them, visual verification is not possible within Phase 3 alone. Verification must be structural:
1. AnimationPlayer exists and implements three stages
2. SkinningData is loaded from Model.Tag (not AnimationData)
3. Multiple clips are available in the animation dictionary
4. PlayAnimation switches clips; looping restarts at the end
5. skinTransforms array is populated with boneCount matrices each frame
6. Old types deleted; solution compiles
7. Unit tests can verify: playing a clip produces non-identity skinTransforms that change over time

## Open Questions

1. **Animation-only FBX file bone coverage**
   - What we know: Current "without skin" animation FBX files extract only 1 bone channel each
   - What's unclear: Whether re-downloading "with skin" will fully resolve this (highly likely based on test-character.fbx having 65 bones and 1 clip)
   - Recommendation: Re-download with "with skin" and rebuild content. If not feasible, modify processor to accept a reference skeleton from external file.

2. **Clip naming from Mixamo**
   - What we know: All Mixamo clips are named "mixamo.com" by default
   - What's unclear: Whether the base model's embedded clip should be kept or discarded after merging
   - Recommendation: Keep the base model's "mixamo.com" clip but it will likely be overwritten by the first merged animation. The custom naming via `AddAnimationsFrom(content, path, "idle")` handles this cleanly.

3. **AnimatedModel.Draw() compatibility during Phase 3**
   - What we know: Phase 3 refactors AnimatedModel internals but Phase 4 changes Draw()
   - What's unclear: Whether the game will render at all between Phase 3 and Phase 4
   - Recommendation: Keep a simplified Draw() that still uses BasicEffect (current behavior). The character will still show T-pose, but the game compiles and runs. Phase 4 flips to SkinnedEffect.

## Sources

### Primary (HIGH confidence)
- XNA SkinningSample_4_0/AnimationPlayer.cs -- complete source code verified via GitHub raw fetch. Canonical three-stage pipeline: UpdateBoneTransforms (flat keyframe scan), UpdateWorldTransforms (hierarchy composition), UpdateSkinTransforms (inverse bind pose multiplication).
- XNA SkinningSample_4_0/SkinningData.cs -- BindPose, InverseBindPose, SkeletonHierarchy, AnimationClips pattern.
- Berzerk codebase direct analysis -- SkinningData.cs, SkinningDataClip.cs, SkinningDataKeyframe.cs, SkinningDataReader.cs, AnimatedModel.cs, EnemyRenderer.cs, EnemyController.cs, BerzerkGame.cs, MixamoModelProcessor.cs.
- Phase 2 Summary (02-01-SUMMARY.md) -- Content build results showing 65 bones for test-character, 0 bones for animation-only files, keyframe counts per file.

### Secondary (MEDIUM confidence)
- Architecture research (.planning/research/ARCHITECTURE.md) -- Three-stage pipeline documentation, data flow diagrams.
- Pitfalls research (.planning/research/PITFALLS.md) -- BasicEffect vs SkinnedEffect, bone index mismatch, transform space issues.
- Stack research (.planning/research/STACK.md) -- MonoGame 3.8.4.1 versions, SkinnedEffect 72-bone limit.

### Tertiary (LOW confidence)
- None. All findings are verified from primary or secondary sources.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH -- no new dependencies, pure C# math on existing types
- Architecture: HIGH -- direct port of canonical XNA AnimationPlayer, verified source code
- Pitfalls: HIGH -- bone index mismatch issue verified from Phase 2 build output data
- Animation-only FBX resolution: MEDIUM -- "with skin" download is expected to fix it but unverified until re-downloaded

**Research date:** 2026-02-11
**Valid until:** Indefinite (XNA animation architecture is stable and won't change)
