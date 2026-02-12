# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-09)

**Core value:** Create a reusable pipeline that works with any Mixamo model
**Current focus:** Phase 4 - Rendering and Game Integration

## Current Position

Phase: 4 of 5 (Rendering and Game Integration)
Plan: 1 of 2 in current phase
Status: In Progress
Last activity: 2026-02-12 -- Completed 04-01-PLAN.md (Skinned Rendering Pipeline)

Progress: [███████░░░] 70%

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 2min
- Total execution time: 0.2 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-skinningdata-types-and-serialization | 2/2 | 5min | 2.5min |
| 02-content-pipeline-processor | 1/1 | 2min | 2min |
| 03-animation-runtime | 2/2 | 3min | 1.5min |
| 04-rendering-and-game-integration | 1/2 | 2min | 2min |

**Recent Trend:**
- Last 5 plans: 2min, 2min, 1min, 2min, 2min
- Trend: Consistent

*Updated after each plan completion*

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Roadmap]: Strict 5-phase dependency chain (types -> processor -> runtime -> rendering -> integration) derived from architecture research
- [Roadmap]: GAME-01 and GAME-02 assigned to Phase 4 (rendering) rather than Phase 5 because they are prerequisites for visual verification
- [01-01]: Named types SkinningDataClip/SkinningDataKeyframe to avoid naming conflict with existing AnimationClip/Keyframe
- [01-01]: SkinningData constructor validates bone array length consistency (BindPose == InverseBindPose == SkeletonHierarchy)
- [01-01]: Old AnimationData types preserved alongside new SkinningData types until Phase 2
- [01-02]: Used BinaryWriter/BinaryReader manual round-trip instead of ContentWriter/ContentReader (MonoGame pipeline types cannot be instantiated in unit tests)
- [01-02]: Type aliases (PipelineSkinningData vs RuntimeSkinningData) to disambiguate identical class names across assemblies
- [02-01]: Used MeshHelper.FlattenSkeleton for canonical bone ordering (replaces custom depth-first BuildBoneIndices)
- [02-01]: DefaultEffect override confirmed working in MonoGame 3.8.4.1 (no ConvertMaterial fallback needed)
- [02-01]: ~~Animation-only FBX files produce SkinningData with empty skeleton arrays (0 bones) -- clips merged at runtime~~ (superseded by 03-01)
- [02-01]: Old pipeline types (AnimationData, AnimationClip, Keyframe, AnimationDataWriter) deleted
- [03-01]: Animation FBX files must be downloaded "With Skin" to embed full skeleton -- all 4 animations now produce 65 bones matching test-character.fbx
- [03-01]: Previous 0-bone animation observation superseded -- all animations produce full 65-bone SkinningData via FlattenSkeleton
- [03-02]: No interpolation in AnimationPlayer -- Mixamo 30fps keyframe density makes it unnecessary
- [03-02]: AddAnimationsFrom refuses to create SkinningData from nothing (requires base model with skeleton)
- [04-01]: Used lazy EnsureSkinnedEffects in Draw (not LoadContent) to avoid changing LoadContent signature and all callers
- [04-01]: BasicEffect fallback kept in Draw loop for static models without skinning data

### Pending Todos

None yet.

### Blockers/Concerns

- [RESOLVED] MonoGame Issue #3057 (SkinnedEffect silently ignored) -- DefaultEffect override works correctly in 3.8.4.1
- [RESOLVED] FBX compatibility with Assimp 5.x -- content build succeeds for all 5 Mixamo FBX files
- [RESOLVED] Animation-only FBX files extracted only 1 bone channel -- fixed by re-downloading with "With Skin" option (03-01)
- [RESOLVED] Game mismatch between Phase 2 and Phase 3 (pipeline writes SkinningData but runtime expected AnimationData) -- resolved by 03-02 rewriting AnimatedModel to use SkinningData

## Session Continuity

Last session: 2026-02-12
Stopped at: Completed 04-01-PLAN.md -- Skinned rendering pipeline active, ready for 04-02 (game integration)
Resume file: None
