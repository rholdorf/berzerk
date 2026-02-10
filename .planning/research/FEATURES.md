# Feature Research

**Domain:** 3D Skeletal Animation System (MonoGame + Mixamo)
**Researched:** 2026-02-09
**Confidence:** MEDIUM-HIGH

Evidence from: codebase analysis (current AnimatedModel system), MonoGame official docs (SkinnedEffect API), MonoGame community forums, reference implementations (MonoGameAnimatedModel, MonoSkelly, BetterSkinned), and skeletal animation systems literature. Confidence is MEDIUM-HIGH because MonoGame's 3D animation ecosystem is well-documented but fragmented -- no single authoritative "this is how you do it" source exists.

## Feature Landscape

### Table Stakes (Users Expect These)

Features the animation system must have or characters stay in T-pose / look broken.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **FBX Import via Content Pipeline** | Mixamo assets are FBX. No import = no models. | MEDIUM | Current `MixamoModelProcessor` exists but uses `BasicEffect` instead of `SkinnedEffect`. The processor extracts keyframes but does not extract skinning data (bone weights, bone indices per vertex). This is likely the root cause of T-pose. |
| **Skeleton/Bone Hierarchy Parsing** | Bones define how mesh deforms. Without correct hierarchy, nothing moves. | MEDIUM | Current system extracts bone names and builds hierarchy in `ApplyKeyframes()`. However, bone indices from the content pipeline may not match MonoGame's `Model.Bones` indices -- the `BuildBoneIndices` method uses a simple counter that may diverge from the model's actual bone ordering. |
| **Skinning (Bone Weights on Vertices)** | Each vertex must know which bones affect it and by how much. Without skinning, bones move but mesh stays in T-pose. | HIGH | **This is the critical missing piece.** Current system uses `BasicEffect` which has no skinning support. Must use `SkinnedEffect` (supports up to 72 bones, 1/2/4 weights per vertex) or a custom skinned shader. MonoGame's content pipeline `ModelProcessor` can extract skinning data when configured correctly. |
| **Keyframe Interpolation** | Smooth animation between poses. Without it, animation stutters or snaps. | LOW | Already implemented in `InterpolateTransform()` using matrix decomposition with Quaternion SLERP for rotation and Vector3 LERP for scale/translation. This is correct. |
| **Animation Playback (Play/Stop/Loop)** | Basic ability to play an animation clip. Core functionality. | LOW | Already implemented in `AnimatedModel.Update()` and `PlayAnimation()`. Time advances, loops at end. Works correctly assuming bone transforms are applied. |
| **Multiple Animation Clips** | Need idle, walk, run, bash at minimum. Single animation is useless. | MEDIUM | Already implemented via `AddAnimationsFrom()` which merges clips from separate FBX files. Mixamo exports one animation per FBX, so merging is required. Current approach of loading separate `AnimatedModel` per animation (in `EnemyRenderer`) is wasteful -- the clip merging approach in `BerzerkGame.LoadContent()` is better. |
| **Animation Switching** | Change from idle to walk when player moves. Without this, character plays one animation forever. | LOW | Already implemented via `PlayAnimation(clipName)`. Resets time to zero and switches clip. Currently instant-switch (no blending), which causes visual "popping" but is functional. |
| **Correct Bone Transform Composition** | Child bones must inherit parent transforms. Incorrect composition = limbs detach or distort. | MEDIUM | Already implemented in `ApplyKeyframes()` -- builds absolute transforms by walking hierarchy. But currently uses `Model.Bones[i].Transform` as bind pose fallback, which may be identity matrices if the model wasn't processed with skeleton awareness. |
| **Separate Model + Animation Loading** | Mixamo workflow: one character mesh + N animation-only FBX files. Must support loading animations separately from mesh. | MEDIUM | Already implemented via `AddAnimationsFrom()`. The content pipeline `ExtractKeyframes()` handles both skeleton-based and animation-only files. Bone name matching between model and animation files is critical and currently works by string name. |

### Differentiators (Competitive Advantage)

Features that make the animation system polished rather than just functional. Not required for basic playback, but high value for gameplay quality.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Animation Crossfade/Blending** | Eliminates jarring "pop" when switching animations (e.g., idle to walk). Interpolates between outgoing and incoming clip over ~0.2s. | MEDIUM | Not currently implemented. Requires storing previous clip's bone transforms and SLERP-ing toward new clip over a transition window. MonoSkelly provides a reference `AnimationsBlender` utility. BetterSkinned community implementations exist. Key challenge: matching bone indices between clips. |
| **Animation State Machine** | Formalizes which animations can transition to which (idle->walk->run, idle->bash). Prevents invalid states. | MEDIUM | Current `EnemyController` uses a manual FSM with `EnemyState` enum that selects animation models. A proper animation state machine would decouple animation selection from game logic, making it reusable. Not needed for initial playback but prevents spaghetti as animation count grows. |
| **Animation Events/Callbacks** | Trigger game logic at specific animation frames (e.g., "bash deals damage at frame 12", "footstep sound at frame 5"). | LOW-MEDIUM | Not currently implemented. Would require marking keyframe times with named events. Extremely valuable for combat -- currently `EnemyController` uses timer-based attack cooldowns which don't sync with animation. |
| **Playback Speed Control** | Play animations faster or slower (e.g., walk animation speed scales with movement speed). | LOW | Not implemented but trivial to add -- multiply `gameTime.ElapsedGameTime` by a speed factor in `Update()`. High value-to-effort ratio. |
| **Root Motion Extraction** | Extract translation from hip/root bone animation and apply to character position instead of animating in-place. | HIGH | Not implemented. MonoGame has no built-in support. Requires identifying the root bone, extracting its XZ translation per frame, and applying it to the entity transform while zeroing the bone's local translation. Useful for attacks with lunges but complex to get right. Defer unless gameplay requires it. |
| **Per-Bone Animation Masking** | Play different animations on upper and lower body (e.g., running legs + shooting torso). | HIGH | Not implemented. Requires splitting bone hierarchy into groups and blending separate clips per group. Overkill for current scope but becomes important if adding ranged combat to characters. |
| **GPU Skinning via SkinnedEffect** | Move bone transform computation to GPU. Renders faster, supports proper mesh deformation with bone weights. | MEDIUM | **This is the bridge between table stakes and differentiator.** MonoGame's built-in `SkinnedEffect` supports up to 72 bones and 1/2/4 bone influences per vertex. Using it means the content pipeline must preserve `BoneWeightsPerVertex` and mesh skinning data. Current system does CPU-side bone hierarchy math but doesn't actually deform the mesh -- it just moves mesh parts as rigid bodies via `_boneTransforms[mesh.ParentBone.Index]`. |
| **Animation Compression** | Reduce memory footprint of keyframe data (quantize rotations, remove redundant keys). | LOW-MEDIUM | Not implemented. Mixamo FBX files have keyframes at every frame (30fps typically). Could reduce to key poses only with interpolation. Lower priority -- memory is not the current bottleneck. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems in this specific context (MonoGame + Mixamo + action/adventure game).

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Custom Animation Editor** | "We need to tweak animations" | Massive scope. Mixamo provides free animations. Blender exists for editing. Building an editor is months of work for marginal gain. | Use Mixamo for download, Blender for tweaks, export to FBX. |
| **Custom Skeleton/Rig Format** | "FBX is buggy, let's make our own format" | Creates a conversion step that must be maintained. Isolates from Mixamo ecosystem. MonoSkelly went this route and adoption suffered ("hard to import from a generic asset" per community feedback). | Fix FBX pipeline issues directly. Use MonoGame's content pipeline as intended. |
| **Morph Targets / Blend Shapes** | "We need facial animation" | MonoGame has no built-in morph target support. Requires custom shader work. Mixamo characters don't ship with blend shapes. Facial animation is out of scope for action/adventure with distant camera. | Skip facial animation entirely. Character expressiveness comes from body animation. |
| **IK (Inverse Kinematics)** | "Feet should plant on uneven ground" | Complex math (FABRIK/CCD solvers). Ground is flat in current game. IK requires runtime bone override which conflicts with keyframe animation. High effort, low visual payoff for flat-ground arcade game. | Use pre-baked animations. Add IK only if terrain becomes uneven. |
| **Physics-Based Animation (Ragdoll)** | "Enemies should ragdoll on death" | Requires physics engine integration (no built-in in MonoGame). Bone-to-rigidbody mapping is error-prone. Current death animation (0.5s timer + deactivate) is sufficient for arcade style. | Use canned death animations from Mixamo. Explosion effect already provides visual feedback. |
| **Procedural Animation** | "Generate walk cycles procedurally" | Extremely complex. Results often look worse than Mixamo mocap. Not appropriate when free mocap data is available. | Download animations from Mixamo's library (thousands available). |
| **Runtime FBX Loading** | "Load models without content pipeline" | Bypasses MonoGame's asset compilation. Requires bundling FBX SDK or AssimpNet at runtime. Content pipeline exists to optimize assets at build time. | Use content pipeline. Pre-process all assets at build time. |
| **Bone Count > 72** | "We need more detailed skeletons" | MonoGame `SkinnedEffect` hard-caps at 72 bones. Custom shader required to exceed this. Mixamo standard skeleton is ~65 bones which fits within limit. | Stay within 72-bone limit. Mixamo skeletons fit naturally. |

## Feature Dependencies

```
[FBX Import]
    +--requires--> [Skeleton Parsing]
                       +--requires--> [Bone Transform Composition]
                                          +--requires--> [Keyframe Interpolation]
                                                             +--enables--> [Animation Playback]

[Skinning (Bone Weights)]
    +--requires--> [Skeleton Parsing]
    +--requires--> [SkinnedEffect or Custom Shader]
    +--enables---> [Correct Mesh Deformation] (fixes T-pose)

[Multiple Clips]
    +--requires--> [Separate Model + Animation Loading]
    +--requires--> [Animation Playback]
    +--enables---> [Animation Switching]

[Animation Switching]
    +--enhances--> [Animation Crossfade] (eliminates pop)

[Animation Crossfade]
    +--enhances--> [Animation State Machine] (managed transitions)

[Animation Events]
    +--requires--> [Animation Playback]
    +--enhances--> [Animation State Machine] (event-driven transitions)

[Root Motion]
    +--requires--> [Skinning]
    +--requires--> [Skeleton Parsing] (root bone identification)
    +--conflicts--> [Current velocity-based movement] (must choose one movement source)

[Per-Bone Masking]
    +--requires--> [Animation Crossfade]
    +--requires--> [Skeleton Parsing] (bone group definitions)
```

### Dependency Notes

- **Skinning requires SkinnedEffect:** The current `BasicEffect` cannot deform a mesh based on bone weights. Switching to `SkinnedEffect` (or a custom skinned shader) is the prerequisite for all mesh deformation. This is the single most important missing piece.
- **Crossfade requires storing previous state:** Cannot blend between animations if you don't keep the outgoing clip's bone transforms. Requires a small architectural change to cache "previous frame" bone state.
- **Animation Events require time-indexed metadata:** Need a way to associate named events with keyframe times. Could be stored in `AnimationClip` alongside keyframe data, or in a separate sidecar data structure.
- **Root Motion conflicts with current movement:** Current `PlayerController` and `EnemyController` use velocity-based movement. Root motion would replace this for animated characters. Cannot use both simultaneously on the same entity without careful layering.

## MVP Definition

### Launch With (v1)

Minimum to get characters animating correctly (fix T-pose, prove pipeline works).

- [ ] **GPU Skinning via SkinnedEffect** -- Replace `BasicEffect` with `SkinnedEffect` in rendering. Extract and pass bone weight data through content pipeline. This single change likely fixes T-pose.
- [ ] **Correct Bone Index Mapping** -- Ensure content pipeline bone indices match `SkinnedEffect.SetBoneTransforms()` expectations. Current `BuildBoneIndices` may produce incorrect mappings.
- [ ] **Bind Pose + Inverse Bind Pose Matrices** -- Compute and store inverse bind pose matrices. Animation transforms are relative to bind pose; without inverse bind pose the math is wrong.
- [ ] **Animation Playback (verified)** -- Confirm existing playback code works once skinning is correct. Time advance, looping, and interpolation are already implemented.
- [ ] **Multiple Clip Support (verified)** -- Confirm existing `AddAnimationsFrom()` works with corrected skinning. Bone name matching across files must be validated.
- [ ] **Animation Switching (instant)** -- Confirm existing `PlayAnimation()` works. Popping is acceptable for v1.

### Add After Validation (v1.x)

Features to add once characters are animating correctly.

- [ ] **Animation Crossfade** -- Add 0.15-0.25s blend between clips to eliminate popping. Trigger: "animations play but transitions look jarring"
- [ ] **Playback Speed Control** -- Trivial addition, enables walk speed matching. Trigger: "walk animation doesn't match movement speed"
- [ ] **Animation Events** -- Named callbacks at specific times in clips. Trigger: "need attack damage to sync with animation frame"
- [ ] **Movement-Based Animation Selection** -- Auto-switch between idle/walk/run based on velocity magnitude. Trigger: "manual 1/2/3 keys replaced with automatic selection"

### Future Consideration (v2+)

Features to defer until core system is proven and gameplay demands them.

- [ ] **Animation State Machine** -- Formalize transitions with conditions, blend times, exit rules. Defer: current manual FSM in EnemyController is sufficient for 4 animations.
- [ ] **Root Motion** -- Extract movement from animations. Defer: velocity-based movement works for current flat-ground gameplay.
- [ ] **Per-Bone Masking** -- Upper/lower body split. Defer: no gameplay requires simultaneous different animations on body parts yet.
- [ ] **Animation Compression** -- Reduce keyframe data size. Defer: not a bottleneck with <10 animation clips.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| GPU Skinning (SkinnedEffect) | HIGH | MEDIUM | **P0** |
| Correct Bone Index Mapping | HIGH | MEDIUM | **P0** |
| Inverse Bind Pose Matrices | HIGH | MEDIUM | **P0** |
| Animation Playback (loop/time) | HIGH | LOW (exists) | **P0** |
| Multiple Clips (merge from FBX) | HIGH | LOW (exists) | **P0** |
| Animation Switching (instant) | HIGH | LOW (exists) | **P0** |
| Animation Crossfade | MEDIUM | MEDIUM | **P1** |
| Playback Speed Control | MEDIUM | LOW | **P1** |
| Animation Events | MEDIUM | LOW-MEDIUM | **P1** |
| Auto Animation Selection | MEDIUM | LOW | **P1** |
| Animation State Machine | LOW-MEDIUM | MEDIUM | **P2** |
| Root Motion | LOW | HIGH | **P3** |
| Per-Bone Masking | LOW | HIGH | **P3** |
| Animation Compression | LOW | LOW-MEDIUM | **P3** |

**Priority key:**
- **P0:** Must fix -- system literally doesn't work without these (T-pose)
- P1: Should have, add once P0 works. High polish, reasonable effort.
- P2: Nice to have, add when animation count grows
- P3: Future, only if gameplay demands

## Competitor/Reference Feature Analysis

| Feature | MonoSkelly | MonoGameAnimatedModel (Lofionic) | BetterSkinned (XNA port) | Our System (Current) | Our System (Target) |
|---------|------------|----------------------------------|--------------------------|---------------------|---------------------|
| FBX Import | No (custom format) | Yes (content pipeline) | Yes (content pipeline) | Yes (MixamoModelProcessor) | Yes |
| GPU Skinning | No (CPU only) | Yes (custom HLSL shader) | Yes (SkinnedEffect) | **No (BasicEffect)** | Yes (SkinnedEffect) |
| Bone Weights | N/A (own format) | Yes (FBX extracted) | Yes (FBX extracted) | **No** | Yes |
| Multiple Clips | Yes | Yes (baked timeline slices) | Yes | Yes (merged from files) | Yes (merged from files) |
| Animation Blending | Yes (AnimationsBlender) | No | Yes (community patch) | No | v1.x |
| Animation Events | No | No | No | No | v1.x |
| Mixamo Compatibility | No (requires conversion) | Partial (Kenney assets) | Partial (XNA-era FBX) | **Native (MixamoModelProcessor)** | Native |
| Editor | Yes (standalone) | No | No | No | No (anti-feature) |

## Sources

- [MonoGame SkinnedEffect API Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SkinnedEffect.html) -- HIGH confidence: official docs
- [MonoGame Community: State of Skeleton Animation](https://community.monogame.net/t/state-of-skeleton-animation-in-monogame/20121) -- MEDIUM confidence: community discussion
- [MonoGameAnimatedModel (Lofionic)](https://github.com/Lofionic/MonoGameAnimatedModel) -- MEDIUM confidence: reference implementation
- [MonoSkelly](https://github.com/RonenNess/MonoSkelly) -- MEDIUM confidence: alternative approach reference
- [Anatomy of a Skeletal Animation System](https://blog.demofox.org/2012/09/21/anatomy-of-a-skeletal-animation-system-part-1/) -- HIGH confidence: established reference
- [MonoGame Community: BetterSkinned Animation Blending](https://community.monogame.net/t/solved-betterskinned-animation-blending-transitions/11765) -- MEDIUM confidence: solved community implementation
- [MonoGame Community: Skinned FBX Pipeline](https://community.monogame.net/t/skinned-fbx-using-pipeline-tool/2393) -- MEDIUM confidence: community guidance
- [MonoGame Issue #7371: New 3D Model Architecture](https://github.com/MonoGame/MonoGame/issues/7371) -- MEDIUM confidence: documents known limitations
- [Mixamo Standard 65 Bone Skeleton](https://community.adobe.com/t5/mixamo/mixamo-standard-65-bone-skeleton/m-p/11442179) -- MEDIUM confidence: community-reported bone count
- Codebase analysis: `/Users/rui/src/pg/berzerk/Berzerk/Source/Graphics/AnimatedModel.cs`, `MixamoModelProcessor.cs`, `EnemyController.cs` -- HIGH confidence: direct inspection

---
*Feature research for: 3D Skeletal Animation System (MonoGame + Mixamo)*
*Researched: 2026-02-09*
