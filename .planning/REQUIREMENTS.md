# Requirements: MonoGame Mixamo Animation System

**Defined:** 2026-02-09
**Core Value:** Create a reusable pipeline that works with any Mixamo model

## v1 Requirements

Requirements for getting Mixamo animations working correctly in MonoGame. Each maps to roadmap phases.

### Data Types

- [ ] **DATA-01**: Define SkinningData class with bind pose, inverse bind pose, skeleton hierarchy, and animation clips
- [ ] **DATA-02**: Implement ContentTypeWriter for build-time serialization of SkinningData
- [ ] **DATA-03**: Implement ContentTypeReader for runtime deserialization of SkinningData
- [ ] **DATA-04**: Define AnimationClip and Keyframe data structures

### Content Pipeline

- [ ] **PIPE-01**: Extract bind pose matrices from Mixamo FBX using MeshHelper.FlattenSkeleton()
- [ ] **PIPE-02**: Compute and store inverse bind pose matrices (Matrix.Invert of bind pose)
- [ ] **PIPE-03**: Build skeleton hierarchy with correct parent-child bone relationships
- [ ] **PIPE-04**: Extract animation keyframes from separate Mixamo FBX files with correct bone indices
- [ ] **PIPE-05**: Force SkinnedEffect usage (workaround for MonoGame Issue #3057)
- [ ] **PIPE-06**: Attach SkinningData to Model.Tag for runtime access

### Animation Runtime

- [ ] **ANIM-01**: Implement three-stage transform pipeline (local -> world -> skin)
- [ ] **ANIM-02**: Read and interpolate keyframes into local bone transforms
- [ ] **ANIM-03**: Compose parent-child hierarchy into world transforms
- [ ] **ANIM-04**: Multiply by inverse bind pose to produce skinning matrices for GPU
- [ ] **ANIM-05**: Support animation playback with loop control
- [ ] **ANIM-06**: Load multiple animation clips from separate FBX files
- [ ] **ANIM-07**: Switch between animation clips at runtime

### Rendering

- [ ] **REND-01**: Replace BasicEffect with SkinnedEffect for mesh rendering
- [ ] **REND-02**: Call SkinnedEffect.SetBoneTransforms() with correct skinning matrices
- [ ] **REND-03**: Verify bone weight vertex data is preserved through Content Pipeline
- [ ] **REND-04**: Render animated character with proper vertex deformation (fix T-pose)

### Game Integration

- [ ] **GAME-01**: Update AnimatedModel to use new SkinningData-based animation system
- [ ] **GAME-02**: Refactor EnemyRenderer to share Model instance across animation clips
- [ ] **GAME-03**: Verify all Mixamo animations play correctly (idle, walk, run, bash)

### Polish

- [ ] **PLSH-01**: Implement animation crossfade/blending (0.15-0.25s smooth transitions)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Enhanced Playback

- **PLSH-02**: Playback speed control (match animation speed to character velocity)
- **PLSH-03**: Animation events/callbacks (sync game logic to animation frames)
- **PLSH-04**: Velocity-based animation auto-selection (replace manual switching)

### Advanced Systems

- **SYST-01**: Animation state machine for managing complex transition logic
- **SYST-02**: Root motion extraction from animations
- **SYST-03**: Per-bone animation masking (upper/lower body split)
- **SYST-04**: Animation compression for large clip libraries

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Custom animation editor | Use Mixamo + Blender instead — building editor is massive scope |
| Custom skeleton format | Fix FBX pipeline, don't replace it — FBX is industry standard |
| Morph targets / facial animation | Out of scope — Mixamo characters lack blend shapes, not needed for action/adventure gameplay |
| IK (inverse kinematics) | Overkill for flat-ground arcade game — not needed for current gameplay |
| Ragdoll physics | Not needed for current game design — enemies don't need physics-based death |
| Procedural animation | Not needed — Mixamo library covers all required animations |
| Runtime FBX loading | Content Pipeline approach is correct — compile-time processing is faster |
| Multiple Mixamo characters | Defer to v2+ — focus on single character pipeline first |
| Weapon attachment bones | Defer until bone count confirmed within 72-bone limit |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| | | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 0
- Unmapped: 24 ⚠️

---
*Requirements defined: 2026-02-09*
*Last updated: 2026-02-09 after initial definition*
