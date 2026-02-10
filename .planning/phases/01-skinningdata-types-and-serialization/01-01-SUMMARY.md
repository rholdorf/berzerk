---
phase: 01-skinningdata-types-and-serialization
plan: 01
subsystem: content-pipeline
tags: [monogame, xna, skinning, animation, content-pipeline, serialization, xnb]

# Dependency graph
requires: []
provides:
  - SkinningData type with AnimationClips, BindPose, InverseBindPose, SkeletonHierarchy
  - SkinningDataClip type with Duration and flat Keyframes list
  - SkinningDataKeyframe type with Bone, Time, Transform
  - SkinningDataWriter ContentTypeWriter for XNB binary serialization
  - SkinningDataReader ContentTypeReader for XNB binary deserialization
  - Binary format contract between pipeline and runtime assemblies
affects: [01-02 (processor will produce SkinningData), phase-02 (processor), phase-03 (runtime animation player), phase-04 (rendering)]

# Tech tracking
tech-stack:
  added: []
  patterns: [dual-assembly immutable types, custom ContentTypeWriter/Reader, flat sorted keyframe list]

key-files:
  created:
    - Berzerk.ContentPipeline/SkinningData.cs
    - Berzerk.ContentPipeline/SkinningDataClip.cs
    - Berzerk.ContentPipeline/SkinningDataKeyframe.cs
    - Berzerk.ContentPipeline/SkinningDataWriter.cs
    - Berzerk/Source/Content/SkinningData.cs
    - Berzerk/Source/Content/SkinningDataClip.cs
    - Berzerk/Source/Content/SkinningDataKeyframe.cs
    - Berzerk/Source/Content/SkinningDataReader.cs
  modified: []

key-decisions:
  - "Named new types SkinningDataClip and SkinningDataKeyframe to avoid conflicts with existing AnimationClip and Keyframe classes"
  - "Used single boneCount write for all three skeleton arrays (BindPose, InverseBindPose, SkeletonHierarchy) with constructor validation"
  - "Old AnimationData types preserved alongside new SkinningData types for coexistence until Phase 2"

patterns-established:
  - "Immutable types: All SkinningData types use private setters with constructor-only initialization"
  - "Binary format contract: Writer and reader use matching inline [Type] comments for format verification"
  - "Constructor validation: SkinningData asserts bone array length consistency at construction time"

# Metrics
duration: 3min
completed: 2026-02-10
---

# Phase 1 Plan 1: SkinningData Types and Serialization Summary

**Immutable SkinningData/SkinningDataClip/SkinningDataKeyframe types in dual assemblies with ContentTypeWriter/Reader XNB binary serialization following the canonical XNA SkinnedModel pattern**

## Performance

- **Duration:** 3 min
- **Started:** 2026-02-10T17:07:26Z
- **Completed:** 2026-02-10T17:10:17Z
- **Tasks:** 2
- **Files created:** 8

## Accomplishments
- SkinningData type with all 4 fields (AnimationClips, BindPose, InverseBindPose, SkeletonHierarchy) in both pipeline and runtime assemblies
- SkinningDataClip with Duration and flat sorted Keyframes list, SkinningDataKeyframe with Bone/Time/Transform
- SkinningDataWriter serializes to XNB binary format, SkinningDataReader deserializes in matching order
- All types immutable with constructor validation, old AnimationData types untouched, solution builds with 0 errors

## Task Commits

Each task was committed atomically:

1. **Task 1: Create SkinningData types in both assemblies** - `17788de` (feat)
2. **Task 2: Create ContentTypeWriter and ContentTypeReader for SkinningData** - `743e06d` (feat)

## Files Created/Modified
- `Berzerk.ContentPipeline/SkinningData.cs` - Pipeline-side SkinningData container with AnimationClips dict, BindPose/InverseBindPose/SkeletonHierarchy lists
- `Berzerk.ContentPipeline/SkinningDataClip.cs` - Pipeline-side animation clip with Duration and flat Keyframes list
- `Berzerk.ContentPipeline/SkinningDataKeyframe.cs` - Pipeline-side keyframe with Bone index, Time, and Transform matrix
- `Berzerk.ContentPipeline/SkinningDataWriter.cs` - ContentTypeWriter that serializes SkinningData to XNB binary
- `Berzerk/Source/Content/SkinningData.cs` - Runtime SkinningData deserialized from XNB
- `Berzerk/Source/Content/SkinningDataClip.cs` - Runtime animation clip with Duration and flat Keyframes list
- `Berzerk/Source/Content/SkinningDataKeyframe.cs` - Runtime keyframe with Bone, Time, Transform
- `Berzerk/Source/Content/SkinningDataReader.cs` - ContentTypeReader that deserializes SkinningData from XNB binary

## Decisions Made
- Named new types `SkinningDataClip` and `SkinningDataKeyframe` (instead of `AnimationClip` and `Keyframe`) to avoid naming conflicts with existing types. Old types will be removed in Phase 2.
- SkinningData constructor validates `bindPose.Count == inverseBindPose.Count == skeletonHierarchy.Count` to prevent binary format mismatches where arrays have different lengths.
- Writer writes `boneCount` once, used by reader to size all three skeleton arrays. This avoids redundant count storage and prevents mismatch.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Added missing `using System;` directive to runtime SkinningDataKeyframe**
- **Found during:** Task 1 (Create SkinningData types in both assemblies)
- **Issue:** Runtime `SkinningDataKeyframe.cs` used `TimeSpan` but only had `using Microsoft.Xna.Framework;` -- `TimeSpan` was not in scope in the runtime assembly
- **Fix:** Added `using System;` to both pipeline and runtime `SkinningDataKeyframe.cs` for consistency
- **Files modified:** `Berzerk/Source/Content/SkinningDataKeyframe.cs`, `Berzerk.ContentPipeline/SkinningDataKeyframe.cs`
- **Verification:** `dotnet build Berzerk.sln` succeeds with 0 errors
- **Committed in:** `17788de` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug)
**Impact on plan:** Trivial missing using directive. No scope creep.

## Issues Encountered
None beyond the auto-fixed missing using directive above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- SkinningData type system is complete and ready for consumption by Plan 2 (processor) and subsequent phases
- Binary format contract is established and documented inline in both writer and reader
- Old AnimationData types remain for compilation; removal deferred to Phase 2 when MixamoModelProcessor is updated

## Self-Check: PASSED

- All 8 source files exist at expected paths
- SUMMARY.md exists at `.planning/phases/01-skinningdata-types-and-serialization/01-01-SUMMARY.md`
- Commit `17788de` (Task 1) verified in git log
- Commit `743e06d` (Task 2) verified in git log
- Solution builds with 0 errors

---
*Phase: 01-skinningdata-types-and-serialization*
*Completed: 2026-02-10*
