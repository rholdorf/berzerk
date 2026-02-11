# Roadmap: MonoGame Mixamo Animation System

## Overview

Fix the T-pose problem by building a correct skeletal animation pipeline from Mixamo FBX through MonoGame's Content Pipeline to GPU skinning at runtime. The work follows a strict dependency chain: data types define the contract, the processor uses those types to build assets, the runtime reads the assets and computes transforms, rendering displays the result, and finally integration connects everything to gameplay with polish. Each phase produces outputs consumed by the next -- skipping or reordering causes compile errors or runtime failures.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: SkinningData Types and Serialization** - Define data structures and Content Pipeline serialization for skeletal animation
- [x] **Phase 2: Content Pipeline Processor** - Build-time FBX processing that extracts skeleton, skinning data, and animation keyframes
- [ ] **Phase 3: Animation Runtime** - Three-stage transform pipeline that computes skinning matrices from keyframes
- [ ] **Phase 4: Rendering and Game Integration** - Connect animation math to SkinnedEffect rendering and refactor game code
- [ ] **Phase 5: Verification and Polish** - Verify all animations play correctly and add crossfade transitions

## Phase Details

### Phase 1: SkinningData Types and Serialization
**Goal**: Animation data has a correct, serializable representation that defines the contract between build-time and runtime
**Depends on**: Nothing (first phase)
**Requirements**: DATA-01, DATA-02, DATA-03, DATA-04
**Success Criteria** (what must be TRUE):
  1. SkinningData class exists with bind pose, inverse bind pose, skeleton hierarchy, and animation clips fields
  2. AnimationClip and Keyframe data structures exist and can represent a full animation timeline with per-bone transforms
  3. ContentTypeWriter serializes SkinningData to binary at build time without errors
  4. ContentTypeReader deserializes SkinningData at runtime and produces identical data to what was written
**Plans:** 2 plans

Plans:
- [ ] 01-01-PLAN.md — Create SkinningData types and ContentTypeWriter/Reader serialization pair
- [ ] 01-02-PLAN.md — Round-trip serialization tests proving writer/reader binary format correctness

### Phase 2: Content Pipeline Processor
**Goal**: The Content Pipeline produces correct XNB files from Mixamo FBX with all skeleton, skinning, and animation data intact
**Depends on**: Phase 1
**Requirements**: PIPE-01, PIPE-02, PIPE-03, PIPE-04, PIPE-05, PIPE-06
**Success Criteria** (what must be TRUE):
  1. Building test-character.fbx through the Content Pipeline succeeds and produces an XNB with SkinningData attached to Model.Tag
  2. The processor extracts the correct number of bones (~65 for Mixamo) with proper parent-child hierarchy
  3. Bind pose and inverse bind pose matrices are both present and mathematically consistent (bindPose * inverseBindPose approximates identity)
  4. Animation keyframes are extracted from separate Mixamo FBX files with frame counts and durations that match the source animations
  5. The processor forces SkinnedEffect (workaround for MonoGame Issue #3057) so meshes use GPU skinning
**Plans:** 1 plan

Plans:
- [x] 02-01-PLAN.md — Rewrite MixamoModelProcessor to produce SkinningData and delete old pipeline types

### Phase 3: Animation Runtime
**Goal**: The runtime can read animation data and compute correct skinning matrices each frame via the three-stage transform pipeline
**Depends on**: Phase 2
**Requirements**: ANIM-01, ANIM-02, ANIM-03, ANIM-04, ANIM-05, ANIM-06, ANIM-07
**Success Criteria** (what must be TRUE):
  1. AnimationPlayer implements the three-stage pipeline: local bone transforms from keyframes, world transforms via hierarchy composition, skinning matrices via inverse bind pose multiplication
  2. Playing an animation clip advances through keyframes over time and produces changing skinning matrices each frame
  3. Multiple animation clips loaded from separate FBX files are available in the animation dictionary
  4. Calling a method to switch clips changes which animation is playing, and the new clip plays from the beginning
  5. Loop control works: a looping animation restarts when it reaches the end
**Plans:** 2 plans

Plans:
- [ ] 03-01-PLAN.md — Re-download animation FBX files from Mixamo with "With Skin" to fix bone coverage
- [ ] 03-02-PLAN.md — Create AnimationPlayer, rewrite AnimatedModel to use SkinningData, delete old runtime types

### Phase 4: Rendering and Game Integration
**Goal**: The animated character renders with correct vertex deformation (no more T-pose) and game code uses the new animation system
**Depends on**: Phase 3
**Requirements**: REND-01, REND-02, REND-03, REND-04, GAME-01, GAME-02
**Success Criteria** (what must be TRUE):
  1. The character model renders using SkinnedEffect instead of BasicEffect, with SetBoneTransforms() called each frame
  2. The character visually deforms according to the playing animation -- no T-pose, no exploded mesh, no distortion
  3. AnimatedModel class exposes a clean API (PlayAnimation, Update, Draw) that game code can call
  4. EnemyRenderer shares a single Model instance across animation clips instead of loading duplicate models
**Plans**: TBD

Plans:
- [ ] 04-01: TBD
- [ ] 04-02: TBD

### Phase 5: Verification and Polish
**Goal**: All four Mixamo animations play correctly on the character and transitions between them are smooth
**Depends on**: Phase 4
**Requirements**: GAME-03, PLSH-01
**Success Criteria** (what must be TRUE):
  1. Idle animation plays with visible breathing/weight-shifting motion when the character is stationary
  2. Walk, run, and bash animations each play with correct limb movement matching their intended action
  3. Switching between any two animations produces a smooth crossfade (0.15-0.25s) instead of an instant pop
**Plans**: TBD

Plans:
- [ ] 05-01: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 -> 2 -> 3 -> 4 -> 5

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. SkinningData Types | 2/2 | Complete | 2026-02-10 |
| 2. Content Pipeline Processor | 1/1 | Complete | 2026-02-11 |
| 3. Animation Runtime | 2/2 | Complete | 2026-02-11 |
| 4. Rendering and Game Integration | 0/TBD | Not started | - |
| 5. Verification and Polish | 0/TBD | Not started | - |
