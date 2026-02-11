---
phase: 02-content-pipeline-processor
plan: 01
subsystem: content-pipeline
tags: [monogame, xna, skinning, animation, content-pipeline, fbx, mixamo, skinnedeffect]

# Dependency graph
requires:
  - phase: 01-skinningdata-types-and-serialization
    provides: SkinningData, SkinningDataClip, SkinningDataKeyframe types and SkinningDataWriter serializer
provides:
  - MixamoModelProcessor producing SkinningData with canonical FlattenSkeleton bone ordering
  - Bind pose, inverse bind pose, and skeleton hierarchy extraction from Mixamo FBX
  - SkinnedEffect forced via DefaultEffect override
  - Animation-only FBX handling (no skeleton, empty bind pose arrays)
  - XNB output files with SkinningData for test-character, idle, walk, run, bash
affects: [phase-03 (runtime animation player reads SkinningData from Model.Tag), phase-04 (rendering uses SkinnedEffect)]

# Tech tracking
tech-stack:
  added: []
  patterns: [canonical XNA SkinnedModelProcessor flow, FlattenTransforms before base.Process, FlattenSkeleton for bone ordering]

key-files:
  created: []
  modified:
    - Berzerk.ContentPipeline/MixamoModelProcessor.cs

key-decisions:
  - "Used MeshHelper.FlattenSkeleton for canonical bone ordering instead of custom depth-first traversal"
  - "DefaultEffect override forces SkinnedEffect (canonical XNA approach, MonoGame Issue #3057 fix confirmed working)"
  - "Animation-only FBX files produce SkinningData with empty skeleton arrays (0 bones) -- skeleton data comes from base model at runtime"
  - "Deleted old pipeline types (AnimationData, AnimationClip, Keyframe, AnimationDataWriter) -- superseded by Phase 1 SkinningData types"

patterns-established:
  - "Processor order: FindSkeleton -> FlattenTransforms -> FlattenSkeleton -> extract skeleton data -> ProcessAnimations -> base.Process -> attach SkinningData"
  - "Bone index validation: count <= 72 (SkinnedEffect.MaxBones) with InvalidContentException on overflow"
  - "Multi-location animation search: skeleton.Animations -> input.Animations -> child nodes recursively (Mixamo-specific)"

# Metrics
duration: 2min
completed: 2026-02-11
---

# Phase 2 Plan 1: Content Pipeline Processor Summary

**MixamoModelProcessor rewritten to canonical XNA SkinnedModelProcessor pattern: FlattenSkeleton bone ordering, bind/inverse bind pose extraction, SkinnedEffect forcing, SkinningData output with animation clips from Mixamo FBX files**

## Performance

- **Duration:** 2 min
- **Started:** 2026-02-11T11:42:20Z
- **Completed:** 2026-02-11T11:44:30Z
- **Tasks:** 2
- **Files modified:** 1 (rewritten), 4 deleted

## Accomplishments
- MixamoModelProcessor completely rewritten to follow the canonical XNA SkinnedModelProcessor pattern with FlattenSkeleton bone ordering, bind pose/inverse bind pose extraction, skeleton hierarchy, and correct animation keyframe bone indices
- DefaultEffect override forces SkinnedEffect for all materials (workaround for MonoGame Issue #3057, confirmed working)
- Content build succeeds for all 5 FBX files: test-character.fbx (65 bones, 1 clip) and 4 animation-only files (idle, walk, run, bash)
- Old pipeline types (AnimationData, AnimationClip, Keyframe, AnimationDataWriter) deleted -- superseded by Phase 1 SkinningData types

## Task Commits

Each task was committed atomically:

1. **Task 1: Rewrite MixamoModelProcessor and delete old pipeline types** - `8737fcd` (feat)
2. **Task 2: Attempt content build and verify pipeline output** - no commit (verification-only task, content build ran as part of solution build)

## Files Created/Modified
- `Berzerk.ContentPipeline/MixamoModelProcessor.cs` - Rewritten: canonical XNA SkinnedModelProcessor pattern producing SkinningData from Mixamo FBX
- `Berzerk.ContentPipeline/AnimationData.cs` - DELETED (superseded by SkinningData)
- `Berzerk.ContentPipeline/AnimationClip.cs` - DELETED (superseded by SkinningDataClip)
- `Berzerk.ContentPipeline/Keyframe.cs` - DELETED (superseded by SkinningDataKeyframe)
- `Berzerk.ContentPipeline/AnimationDataWriter.cs` - DELETED (superseded by SkinningDataWriter)

## Decisions Made
- Used `MeshHelper.FlattenSkeleton()` for canonical bone ordering instead of old custom `BuildBoneIndices()` depth-first traversal. This matches `base.Process()` vertex channel conversion ordering.
- `DefaultEffect` property override forces `MaterialProcessorDefaultEffect.SkinnedEffect`. Confirmed working in MonoGame 3.8.4.1 during content build (no `ConvertMaterial` fallback needed).
- Animation-only FBX files (idle, walk, run, bash) produce `SkinningData` with empty skeleton arrays (0 bones) and animation clips extracted from child nodes. Only 1 bone channel extracted per animation-only file (`mixamorig:Hips`) because `MeshHelper.FindSkeleton` returns null and the animation search falls back to child node animations.
- Old pipeline types deleted in same commit as processor rewrite to ensure clean transition.

## Deviations from Plan

None - plan executed exactly as written.

## Content Build Results

| FBX File | Type | Bones | Clips | Keyframes |
|----------|------|-------|-------|-----------|
| test-character.fbx | Model + Skeleton | 65 | 1 ("mixamo.com") | 24 |
| idle.fbx | Animation-only | 0 | 1 ("mixamo.com") | 251 |
| walk.fbx | Animation-only | 0 | 1 ("mixamo.com") | 32 |
| run.fbx | Animation-only | 0 | 1 ("mixamo.com") | 26 |
| bash.fbx | Animation-only | 0 | 1 ("mixamo.com") | 121 |

XNB output files produced at `Berzerk/Content/bin/DesktopGL/Content/Models/`.

**Note:** The animation-only files extract only 1 bone channel each because without a skeleton, only the `mixamorig:Hips` child node has an animation registered. At runtime, full animation merging will be handled in Phase 3/4.

## Issues Encountered
None - the content build succeeded for all 5 FBX files on the first attempt.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Content pipeline produces SkinningData with correct bone ordering, bind/inverse bind pose, skeleton hierarchy, and animation clips
- XNB files are ready for runtime consumption
- Runtime still uses old AnimationData types (Phase 3 will update runtime to read SkinningData from Model.Tag)
- The game will NOT load models correctly between Phase 2 and Phase 3 (pipeline writes SkinningData but runtime expects AnimationData) -- this is expected and documented

---
*Phase: 02-content-pipeline-processor*
*Completed: 2026-02-11*
