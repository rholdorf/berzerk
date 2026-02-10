# Architecture Research

**Domain:** 3D Skeletal Animation System (MonoGame + Mixamo)
**Researched:** 2026-02-09
**Confidence:** HIGH

Evidence from: XNA SkinningSample_4_0 canonical reference (SkinningData, AnimationPlayer, SkinnedModelProcessor), MonoGame official API docs (SkinnedEffect, Model, ModelBone), MonoGame community tutorials and ports, reference implementations (MonoGameAnimatedModel, MonoSkelly, BaamStudios/XnaMixamoImporter), and direct analysis of the current Berzerk codebase. Confidence is HIGH because the canonical XNA skinned animation architecture is well-documented, battle-tested, and the MonoGame community universally references it as the standard approach.

## Standard Architecture

### System Overview

```
BUILD TIME (Content Pipeline)                    RUNTIME (Game)
==============================                   ================================

  Mixamo FBX Files                                  MonoGame Game Loop
  (.fbx on disk)                                    (Update + Draw each frame)
       |                                                   |
       v                                                   v
 +-----------+                                    +------------------+
 | FbxImporter|                                   | ContentManager   |
 | (built-in) |                                   | .Load<Model>()   |
 +-----+------+                                   +--------+---------+
       |                                                   |
       v                                                   v
 +------------------+                              +-------+--------+
 | NodeContent tree |                              | Model          |
 | (bones, meshes,  |                              |  .Bones[]      |
 |  animations,     |                              |  .Meshes[]     |
 |  vertex channels)|                              |  .Tag = -----+----> SkinningData
 +--------+---------+                              +-------+------+      |
          |                                                |             |
          v                                                v             v
 +--------------------+                            +---------------+ +------------------+
 | SkinnedModel       |                            | AnimationPlayer| | SkinningData     |
 | Processor          |                            |  .Update()     | |  .BindPose[]     |
 | (custom)           |                            |  boneTransforms| |  .InverseBindPose|
 |                    |                            |  worldTransforms| |  .SkeletonHierarchy|
 | 1. FlattenSkeleton |                            |  skinTransforms| |  .AnimationClips |
 | 2. Extract BindPose|                            +-------+-------+ +------------------+
 | 3. Compute InvBind |                                    |
 | 4. Build Hierarchy |                                    v
 | 5. Extract Anims   |                            +------------------+
 | 6. ProcessSkin     |                            | SkinnedEffect    |
 |    (vertex weights)|                            |  .SetBoneTransforms|
 | 7. Tag=SkinningData|                            |  (skinTransforms)  |
 +--------+-----------+                            +--------+---------+
          |                                                 |
          v                                                 v
    +----------+                                    +-------+--------+
    | .xnb file|                                    | GPU renders     |
    | (compiled |  -------- loaded at --------->    | skinned mesh    |
    |  binary)  |           runtime                 | (deformed verts)|
    +----------+                                    +----------------+
```

### Component Responsibilities

| Component | Responsibility | Communicates With |
|-----------|----------------|-------------------|
| **FbxImporter** (built-in) | Reads FBX files from disk into MonoGame's `NodeContent` intermediate representation. Parses bones, meshes, vertex channels, animation channels. | Feeds `NodeContent` tree to Processor |
| **SkinnedModelProcessor** (custom, build-time) | Extracts skeleton hierarchy, bind pose, inverse bind pose, animation keyframes, and vertex skinning weights. Attaches everything as `SkinningData` in `Model.Tag`. Converts vertex weight channels into `BlendIndices`/`BlendWeight` vertex elements. | Reads from FbxImporter output, writes to `.xnb` via ContentTypeWriter |
| **SkinningData** (shared data type) | Immutable container holding all skeleton + animation data: bind pose matrices, inverse bind pose matrices, skeleton hierarchy (parent indices), and animation clips dictionary. | Created by Processor at build time, read by AnimationPlayer at runtime |
| **AnimationClip** (shared data type) | Single named animation (e.g. "idle", "walk"). Contains duration and a flat list of Keyframes sorted by time. | Stored inside SkinningData, consumed by AnimationPlayer |
| **Keyframe** (shared data type) | Single bone pose at a moment in time. Contains: bone index (int), time (TimeSpan), transform (Matrix -- local-space relative to parent bone). | Stored inside AnimationClip |
| **AnimationPlayer** (runtime) | Drives animation playback. Maintains three transform arrays: boneTransforms (local), worldTransforms (absolute), skinTransforms (final for GPU). Advances time, reads keyframes, computes hierarchy, applies inverse bind pose. | Reads SkinningData, outputs skinTransforms to SkinnedEffect |
| **SkinnedEffect** (built-in, runtime) | MonoGame shader that deforms mesh vertices on GPU using bone weight data and skinning matrices. Supports up to 72 bones, 1/2/4 weights per vertex. | Receives skinTransforms from AnimationPlayer, renders deformed mesh |
| **AnimatedModel** (runtime, game-level) | Game-facing wrapper that owns a Model, an AnimationPlayer, and exposes Play/Stop/Update/Draw. Manages animation state for one character instance. | Uses AnimationPlayer internally, exposes simple API to game code |

## Recommended Project Structure

```
Berzerk.ContentPipeline/          # Build-time content processing (separate assembly)
    SkinnedModelProcessor.cs      # Custom ModelProcessor subclass
    ContentTypeWriters.cs         # Serializes SkinningData to .xnb binary

Berzerk/Source/
    Content/                      # Runtime content type readers + data types
        SkinningData.cs           # BindPose, InverseBindPose, Hierarchy, Clips
        AnimationClip.cs          # Duration + List<Keyframe> (flat, sorted by time)
        Keyframe.cs               # BoneIndex, Time, Transform (Matrix)
        SkinningDataReader.cs     # ContentTypeReader<SkinningData> (XNB deserialization)

    Animation/                    # Runtime animation playback
        AnimationPlayer.cs        # Keyframe decoding, 3-stage transform pipeline
        AnimatedModel.cs          # Game-facing wrapper: Load, Play, Update, Draw

    Graphics/                     # Rendering
        (AnimatedModel.Draw uses SkinnedEffect, not BasicEffect)

    Controllers/                  # Game logic that selects animations
        PlayerController.cs       # Calls animatedModel.PlayAnimation("walk") etc.
        EnemyController.cs        # FSM selects animation based on state
```

### Structure Rationale

- **Berzerk.ContentPipeline/:** Must be a separate assembly because content pipeline extensions are loaded by MGCB at build time, not at runtime. This assembly references `MonoGame.Framework.Content.Pipeline` and is never deployed with the game.
- **Source/Content/:** Runtime mirror types for what the pipeline serializes. `SkinningData` here is the runtime twin of the pipeline's output. The `ContentTypeReader` must exactly match the `ContentTypeWriter`'s binary format.
- **Source/Animation/:** Separated from Content because AnimationPlayer contains runtime logic (time advancement, hierarchy traversal, interpolation) while Content types are pure data. AnimationPlayer depends on SkinningData but not vice versa.
- **Source/Graphics/ and Source/Controllers/:** AnimatedModel bridges animation and rendering. Controllers consume AnimatedModel's public API without knowing about bone math. This separation keeps game logic animation-agnostic.

## Architectural Patterns

### Pattern 1: Three-Stage Transform Pipeline (Critical)

**What:** Animation transforms are computed in three sequential stages, each building on the previous. This is the canonical XNA approach and the industry standard for skeletal animation.

**When to use:** Always. This is not optional -- skipping any stage produces incorrect results.

**Trade-offs:** Requires three Matrix arrays per animated model (~72 bones x 64 bytes x 3 = ~14KB). Negligible memory cost. The math is straightforward but order-dependent.

**The three stages:**

```
Stage 1: boneTransforms[] (local space)
    For each bone: read keyframe transform at current time
    This is the bone's pose RELATIVE TO ITS PARENT
    Fallback: use SkinningData.BindPose[i] if no keyframe

Stage 2: worldTransforms[] (model/world space)
    For each bone:
        if root: worldTransforms[i] = boneTransforms[i] * rootTransform
        else:    worldTransforms[i] = boneTransforms[i] * worldTransforms[parent[i]]
    This composes the full chain from root to leaf

Stage 3: skinTransforms[] (final, for GPU)
    For each bone:
        skinTransforms[i] = SkinningData.InverseBindPose[i] * worldTransforms[i]
    This converts from model space to "difference from bind pose" space
    This is what SkinnedEffect.SetBoneTransforms() receives
```

**Why inverse bind pose matters:**

The inverse bind pose converts a vertex from its original model-space position into bone-local space. When multiplied with the animated world transform, the result is "how far has this bone moved from its rest position?" Without inverse bind pose, vertices do not deform -- they stay in T-pose because the shader does not know the reference frame.

**Example:**
```csharp
// Stage 1: read keyframes into local bone transforms
Matrix[] boneTransforms = new Matrix[boneCount];
for (int i = 0; i < boneCount; i++)
    boneTransforms[i] = skinningData.BindPose[i]; // default to bind pose
// Override with animated keyframe transforms...

// Stage 2: compose hierarchy
Matrix[] worldTransforms = new Matrix[boneCount];
worldTransforms[0] = boneTransforms[0] * rootTransform;
for (int i = 1; i < boneCount; i++)
{
    int parent = skinningData.SkeletonHierarchy[i];
    worldTransforms[i] = boneTransforms[i] * worldTransforms[parent];
}

// Stage 3: apply inverse bind pose
Matrix[] skinTransforms = new Matrix[boneCount];
for (int i = 0; i < boneCount; i++)
    skinTransforms[i] = skinningData.InverseBindPose[i] * worldTransforms[i];

// Send to GPU
skinnedEffect.SetBoneTransforms(skinTransforms);
```

### Pattern 2: SkinningData Attached to Model.Tag

**What:** The content pipeline processor attaches a `SkinningData` object to `Model.Tag`. At runtime, cast `Model.Tag` to `SkinningData` to access bind pose, inverse bind pose, skeleton hierarchy, and animation clips.

**When to use:** Always. This is MonoGame's official mechanism for attaching custom data to processed models. `Model.Tag` is explicitly documented as "Custom attached object. Skinning data is example of attached object for model."

**Trade-offs:** Requires type agreement between pipeline writer and runtime reader. If the writer serializes `SkinningData` and the reader expects a different type, deserialization fails silently (Tag is null). The ContentTypeWriter's `GetRuntimeReader()` must return the exact fully-qualified type name.

**Example:**
```csharp
// Content Pipeline (build-time):
model.Tag = new SkinningData(animationClips, bindPose, inverseBindPose, hierarchy);

// Runtime (load-time):
Model model = content.Load<Model>("character");
SkinningData skinningData = model.Tag as SkinningData;
if (skinningData == null)
    throw new InvalidOperationException("Model has no skinning data");
```

### Pattern 3: SkinnedEffect Replaces BasicEffect for Animated Models

**What:** MonoGame's built-in `SkinnedEffect` performs GPU vertex skinning. It deforms mesh vertices based on per-vertex bone weights and the skinning matrices provided via `SetBoneTransforms()`. `BasicEffect` has no skinning capability whatsoever.

**When to use:** For any model that needs skeletal animation with mesh deformation. Static (non-animated) models can continue to use `BasicEffect`.

**Trade-offs:** `SkinnedEffect` is limited to 72 bones maximum (GPU constant buffer constraint). Mixamo's standard skeleton has ~65 bones, which fits. `SkinnedEffect` supports 1, 2, or 4 bone influences per vertex (set via `WeightsPerVertex`). For most Mixamo models, 4 weights produces best quality. The content pipeline must preserve vertex skinning channels (`BlendIndices` and `BlendWeight`) or the effect has nothing to skin against.

**Example:**
```csharp
// Drawing with SkinnedEffect instead of BasicEffect
foreach (ModelMesh mesh in model.Meshes)
{
    foreach (SkinnedEffect effect in mesh.Effects)
    {
        effect.SetBoneTransforms(skinTransforms);
        effect.World = world;
        effect.View = view;
        effect.Projection = projection;
        effect.EnableDefaultLighting();
    }
    mesh.Draw();
}
```

### Pattern 4: Flat Keyframe List Sorted by Time

**What:** The canonical XNA approach stores ALL keyframes for ALL bones in a single flat `List<Keyframe>`, sorted by time. Each Keyframe contains a bone index, time, and transform matrix. The AnimationPlayer scans forward through this list sequentially.

**When to use:** This is the standard approach from the XNA skinning sample. It is simple, cache-friendly, and works well for sequential playback.

**Trade-offs compared to current Berzerk approach:** The current Berzerk system uses `Dictionary<string, List<Keyframe>>` (keyframes grouped by bone name). The XNA approach uses a flat list with bone indices. The flat list is simpler for the AnimationPlayer (one scan pointer vs. N per-bone searches), but the grouped approach makes it easier to add/remove bone tracks. Either works; the critical fix is not the data structure but the three-stage transform pipeline and SkinnedEffect.

### Pattern 5: Separate Model + Animation FBX Files (Mixamo Workflow)

**What:** Mixamo exports character mesh and animations as separate FBX files: one file with the mesh + skeleton (e.g., `test-character.fbx`) and separate files with just skeleton + animation data (e.g., `idle.fbx`, `walk.fbx`). At build time, the content pipeline processes each FBX independently. At runtime, the game loads the base model and merges animation clips from animation-only models.

**When to use:** Always when using Mixamo assets. Mixamo does not support exporting multiple animations in a single FBX file.

**Trade-offs:** Loading animation-only FBX files still produces a full `Model` with meshes (just the skeleton mesh, not the character). The meshes are discarded at runtime; only the `SkinningData.AnimationClips` are extracted and merged into the base model's SkinningData. This wastes some build time and XNB disk space but is architecturally clean.

**Critical requirement:** Bone names and indices in animation-only files must match the base model's skeleton. Mixamo guarantees this when all animations are downloaded for the same character. The content pipeline should validate bone name agreement.

## Data Flow

### Build-Time Data Flow (Content Pipeline)

```
Mixamo FBX File
    |
    v
FbxImporter (MonoGame built-in)
    |  Parses FBX binary format
    |  Produces: NodeContent tree with:
    |    - BoneContent hierarchy (skeleton)
    |    - MeshContent with vertex channels (position, normal, UV, BoneWeights)
    |    - AnimationContent with channels per bone (transform keyframes)
    v
SkinnedModelProcessor (custom, extends ModelProcessor)
    |
    |  Step 1: MeshHelper.FindSkeleton(input)
    |    Find the root BoneContent node
    |
    |  Step 2: MeshHelper.FlattenSkeleton(skeleton)
    |    Convert bone tree into flat List<BoneContent>
    |    This defines the bone INDEX order used everywhere
    |
    |  Step 3: Extract BindPose
    |    For each bone: bindPose[i] = bone.Transform (local-space matrix)
    |
    |  Step 4: Compute InverseBindPose
    |    For each bone: inverseBindPose[i] = Matrix.Invert(bone.AbsoluteTransform)
    |    AbsoluteTransform = accumulated transform from root to this bone
    |
    |  Step 5: Build SkeletonHierarchy
    |    For each bone: hierarchy[i] = index of bone's parent in flat list
    |    Root bone has parent index -1 or is handled specially
    |
    |  Step 6: Extract AnimationClips
    |    For each animation in content:
    |      For each channel (per bone):
    |        Map bone name -> bone index
    |        Extract time + transform for each keyframe
    |      Create AnimationClip with flat sorted keyframe list
    |
    |  Step 7: base.Process(input, context)
    |    Standard ModelProcessor handles:
    |      - Mesh geometry compilation
    |      - Vertex channel conversion (BoneWeights -> BlendIndices/BlendWeight)
    |      - Material/texture processing
    |      - Effect assignment (SkinnedEffect for skinned meshes)
    |
    |  Step 8: Attach SkinningData to Model.Tag
    |    model.Tag = new SkinningData(clips, bindPose, inverseBind, hierarchy)
    |
    v
ContentTypeWriter<SkinningData>
    |  Serializes SkinningData to binary .xnb format
    |  Must write in exact order that ContentTypeReader expects
    v
.xnb file on disk (deployed with game)
```

### Runtime Data Flow

```
Game.LoadContent()
    |
    v
ContentManager.Load<Model>("character")
    |  Reads .xnb binary
    |  Reconstructs Model with Meshes, Bones, Effects
    |  ContentTypeReader<SkinningData> deserializes Model.Tag
    v
AnimatedModel.LoadContent(content, "character")
    |  model = content.Load<Model>(path)
    |  skinningData = model.Tag as SkinningData
    |  animationPlayer = new AnimationPlayer(skinningData)
    v
AnimatedModel.AddAnimationsFrom(content, "idle")
    |  Load animation-only model
    |  Extract SkinningData.AnimationClips from its Tag
    |  Merge clips into base model's SkinningData
    v
AnimatedModel.PlayAnimation("idle")
    |  animationPlayer.StartClip(skinningData.AnimationClips["idle"])
    v
Game.Update(gameTime)  [every frame]
    |
    v
AnimatedModel.Update(gameTime)
    |
    v
AnimationPlayer.Update(gameTime)
    |
    |  Stage 1: UpdateBoneTransforms(gameTime)
    |    Advance currentTime
    |    Scan keyframes forward from current position
    |    Set boneTransforms[keyframe.Bone] = keyframe.Transform
    |
    |  Stage 2: UpdateWorldTransforms()
    |    worldTransforms[0] = boneTransforms[0] * rootTransform
    |    for i = 1..N:
    |      worldTransforms[i] = boneTransforms[i] * worldTransforms[parent[i]]
    |
    |  Stage 3: UpdateSkinTransforms()
    |    for i = 0..N:
    |      skinTransforms[i] = inverseBindPose[i] * worldTransforms[i]
    |
    v
Game.Draw()  [every frame]
    |
    v
AnimatedModel.Draw(world, view, projection)
    |  foreach mesh in model.Meshes:
    |    foreach effect in mesh.Effects:  (these are SkinnedEffect instances)
    |      effect.SetBoneTransforms(skinTransforms)
    |      effect.World = world
    |      effect.View = view
    |      effect.Projection = projection
    |    mesh.Draw()
    v
GPU
    |  For each vertex:
    |    finalPos = vertex.Position
    |               * skinTransforms[vertex.BlendIndices.x] * vertex.BlendWeight.x
    |             + vertex.Position
    |               * skinTransforms[vertex.BlendIndices.y] * vertex.BlendWeight.y
    |             + ... (up to 4 bone influences)
    |  Rasterize and shade
    v
Pixels on screen (deformed, animated mesh)
```

### Key Data Flows

1. **FBX to XNB (build-time, one-time):** Mixamo FBX is imported by MonoGame's FbxImporter into NodeContent, processed by custom SkinnedModelProcessor to extract skeleton data and skinning weights, serialized to .xnb binary. This runs during `dotnet build` via MGCB, not at game runtime.

2. **XNB to GPU (runtime, every frame):** Model is loaded from XNB, AnimationPlayer reads keyframes and computes the three-stage transform pipeline (local -> world -> skin), resulting skinTransforms are sent to SkinnedEffect via SetBoneTransforms, GPU deforms vertices per-vertex using blend weights baked into the vertex buffer at build time.

3. **Animation merging (runtime, load-time):** Multiple animation-only XNB files are loaded, their AnimationClip data is extracted from Model.Tag, and merged into the base model's SkinningData. The mesh data from animation-only models is discarded.

## What the Current System Gets Wrong

The current Berzerk implementation has the right high-level structure but is missing three critical architectural pieces that together cause the T-pose:

### Problem 1: No Inverse Bind Pose Matrices

**Current:** The `MixamoModelProcessor` does not compute `Matrix.Invert(bone.AbsoluteTransform)` for each bone. The `AnimationData` class has no inverse bind pose storage.

**Impact:** Without inverse bind pose, Stage 3 of the transform pipeline is impossible. The GPU shader cannot know "how far has this bone moved from its rest position?" because it has no reference frame.

**Fix:** Add `List<Matrix> InverseBindPose` to the data model. Compute it in the processor using `Matrix.Invert(bone.AbsoluteTransform)` after calling `MeshHelper.FlattenSkeleton()`.

### Problem 2: Using BasicEffect Instead of SkinnedEffect

**Current:** `AnimatedModel.Draw()` casts mesh effects to `BasicEffect` and sets `effect.World = _boneTransforms[mesh.ParentBone.Index] * world`. This treats each mesh as a rigid body moved by its parent bone -- no vertex-level deformation occurs.

**Impact:** The mesh is a single connected skin. Moving it rigidly by one bone index does nothing meaningful. The mesh stays in T-pose because individual vertices are not being repositioned based on their bone weight assignments.

**Fix:** Let the content pipeline assign `SkinnedEffect` to skinned meshes (the base `ModelProcessor` does this automatically when it detects skinning vertex channels). At draw time, cast to `SkinnedEffect` and call `SetBoneTransforms(skinTransforms)`.

### Problem 3: No Vertex Skinning Data (BlendIndices/BlendWeight)

**Current:** The `MixamoModelProcessor` calls `base.Process()` which should handle vertex channel conversion. However, because the processor does not call `MeshHelper.FlattenSkeleton()` to establish canonical bone ordering, the vertex channel conversion may produce incorrect bone indices or fail silently.

**Impact:** Even if SkinnedEffect were used, without correct per-vertex BlendIndices and BlendWeight data, the GPU has no information about which bones influence which vertices.

**Fix:** Ensure `MeshHelper.FlattenSkeleton()` is called before `base.Process()` so the base ModelProcessor can correctly map bone weights to bone indices during vertex channel processing.

### Problem 4: Incorrect Bone Index Mapping

**Current:** `BuildBoneIndices()` uses a simple incrementing counter that may not match the order produced by `MeshHelper.FlattenSkeleton()` or `Model.Bones` ordering. Animation keyframes reference bone indices that may not correspond to the model's actual bone order.

**Impact:** Even if all other pieces were correct, mismatched bone indices would cause wrong bones to receive wrong transforms -- producing distorted, garbled animation rather than T-pose.

**Fix:** Use `MeshHelper.FlattenSkeleton()` to establish bone ordering. Build the bone name-to-index mapping from the flattened list. Use these same indices for both skinning data and animation keyframes.

## Build Order (Dependency Chain)

The components must be built in this order because each depends on the previous:

```
Phase 1: SkinningData + ContentTypeWriter/Reader
    Define the data contract between build-time and runtime.
    SkinningData must include: BindPose, InverseBindPose, SkeletonHierarchy, AnimationClips.
    Writer and Reader must serialize/deserialize in exact same binary format.
    NO rendering changes needed yet -- this is pure data plumbing.

Phase 2: SkinnedModelProcessor
    Replace current MixamoModelProcessor with one that:
    a) Calls MeshHelper.FlattenSkeleton() to establish bone ordering
    b) Extracts BindPose from bone.Transform for each bone
    c) Computes InverseBindPose from Matrix.Invert(bone.AbsoluteTransform)
    d) Builds SkeletonHierarchy (parent index array)
    e) Extracts AnimationClips using bone-name-to-flat-index mapping
    f) Lets base.Process() handle vertex skinning channels (BlendIndices/BlendWeight)
    g) Attaches SkinningData to model.Tag
    Rebuild content after this phase to produce correct .xnb files.

Phase 3: AnimationPlayer
    Implement the three-stage transform pipeline:
    a) UpdateBoneTransforms -- read keyframes, produce local bone transforms
    b) UpdateWorldTransforms -- compose parent-child hierarchy
    c) UpdateSkinTransforms -- multiply by inverse bind pose
    Output: Matrix[] skinTransforms ready for GPU

Phase 4: AnimatedModel (rendering with SkinnedEffect)
    a) Replace BasicEffect with SkinnedEffect in Draw()
    b) Call effect.SetBoneTransforms(skinTransforms) before drawing
    c) Remove rigid-body bone transform approach (_boneTransforms[mesh.ParentBone.Index])
    d) Update animation merging (AddAnimationsFrom) to work with SkinningData

Phase 5: Integration with Game Code
    a) Update EnemyRenderer to use new AnimatedModel API
    b) Update PlayerController animation calls
    c) Verify all Mixamo animations play correctly
    d) Validate animation switching works
```

**Why this order:**
- Phase 1 (data types) has zero dependencies -- it is pure type definitions
- Phase 2 (processor) depends on Phase 1 types to serialize
- Phase 3 (player) depends on Phase 1 types to read, and Phase 2 to produce correct data
- Phase 4 (rendering) depends on Phase 3 to provide skinTransforms
- Phase 5 (integration) depends on all previous phases

## Anti-Patterns

### Anti-Pattern 1: Rigid-Body Bone Animation (Current Approach)

**What people do:** Move entire meshes by `_boneTransforms[mesh.ParentBone.Index]` using BasicEffect. Each mesh is treated as a rigid body attached to one bone.

**Why it's wrong:** Skinned characters are a single continuous mesh deformed by multiple bones per vertex. Moving the whole mesh by one bone's transform does nothing -- the mesh stays in T-pose because no per-vertex deformation occurs. This only works for mechanical models where each part is genuinely a separate rigid mesh (robot arms, vehicle parts).

**Do this instead:** Use SkinnedEffect with per-vertex bone weights (BlendIndices/BlendWeight). Send skinTransforms to the GPU. Let the vertex shader deform each vertex individually based on its bone weight assignments.

### Anti-Pattern 2: Skipping Inverse Bind Pose

**What people do:** Compute worldTransforms from animation keyframes and send them directly to the GPU as skin transforms, skipping the inverse bind pose multiplication.

**Why it's wrong:** The GPU shader expects transforms that represent "delta from rest pose." Without inverse bind pose, the transforms represent absolute bone positions, which double-applies the bind pose -- vertices end up in wrong positions or stay at origin.

**Do this instead:** Always compute `skinTransform[i] = inverseBindPose[i] * worldTransform[i]`. The inverse bind pose "undoes" the rest position, and the world transform "re-applies" the animated position, producing the net delta.

### Anti-Pattern 3: Custom Bone Index Numbering

**What people do:** Assign bone indices using a custom traversal order (e.g., depth-first counter) that does not match `MeshHelper.FlattenSkeleton()`'s ordering.

**Why it's wrong:** The base `ModelProcessor` uses `FlattenSkeleton` internally to establish bone ordering for vertex channel processing (BlendIndices). If the animation system uses a different ordering, bone index N in the animation maps to a different bone than index N in the vertex weights, producing garbled animation.

**Do this instead:** Use `MeshHelper.FlattenSkeleton()` to establish THE canonical bone ordering. Use this same ordering for bind pose arrays, inverse bind pose arrays, skeleton hierarchy, and animation keyframe bone indices. All must agree.

### Anti-Pattern 4: One AnimatedModel Per Animation Clip

**What people do:** Load a separate `AnimatedModel` for each animation (idle model, walk model, attack model) and swap between them.

**Why it's wrong:** Each AnimatedModel loads the full mesh geometry, creating redundant copies of vertex/index buffers in GPU memory. For N enemies with M animations, this creates N*M model instances instead of N model instances with M merged clips. Current `EnemyRenderer.LoadRobotModels()` does exactly this.

**Do this instead:** Load the base model once. Use `AddAnimationsFrom()` to merge all animation clips into one SkinningData. One AnimatedModel instance per character, with all clips available via `PlayAnimation(clipName)`.

## Integration Points

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| ContentPipeline -> Runtime | Binary XNB serialization (ContentTypeWriter/Reader pair) | Must agree on exact binary format. Writer in pipeline assembly, Reader in game assembly. Type name in `GetRuntimeReader()` must be fully qualified: `"Namespace.TypeName, AssemblyName"`. |
| AnimationPlayer -> SkinnedEffect | `Matrix[] skinTransforms` via `SetBoneTransforms()` | Array length must be <= 72 (SkinnedEffect.MaxBones). Mixamo standard skeleton is ~65 bones -- fits. |
| AnimatedModel -> Game Controllers | Public API: `PlayAnimation(string)`, `Update(GameTime)`, `Draw(...)` | Controllers should not access bone data directly. AnimatedModel encapsulates all animation internals. |
| Base Model -> Animation-Only Models | Bone name matching during clip merging | Bone names must be identical strings. Mixamo guarantees this for same character. Processor should validate. |

### MonoGame Framework Integration

| Integration Point | Mechanism | Notes |
|-------------------|-----------|-------|
| Content Pipeline | `/reference:` in Content.mgcb + `[ContentProcessor]` attribute | Pipeline DLL must be built before content build. MGCB loads it via reflection. |
| Model Loading | `ContentManager.Load<Model>()` | Standard MonoGame content loading. Model.Tag deserialization happens automatically via registered ContentTypeReader. |
| Rendering | SkinnedEffect (built-in to MonoGame) | No custom shaders needed for basic skinning. SkinnedEffect handles vertex deformation, lighting, texturing. |
| Game Loop | `Game.Update()` -> `AnimatedModel.Update()`, `Game.Draw()` -> `AnimatedModel.Draw()` | Standard MonoGame update/draw pattern. No special timing or threading required. |

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 1-5 animated characters | Current architecture is fine. One AnimatedModel per character. AnimationPlayer per instance. ~14KB bone data per instance is negligible. |
| 10-50 animated characters | Share Model instances across characters of the same type. Each character gets its own AnimationPlayer (different playback time) but references the same Model/SkinningData. Mesh data is shared via ContentManager caching. |
| 50+ animated characters | Consider LOD: reduce bone count for distant characters (SkinnedEffect.WeightsPerVertex = 1 or 2 instead of 4). Consider animation instancing: characters in same state share AnimationPlayer output. Consider compute shader skinning if SkinnedEffect becomes bottleneck. |

### Scaling Priorities

1. **First bottleneck:** Draw calls. Each AnimatedModel.Draw() iterates meshes and sets effect parameters. Batch characters with same animation state. Unlikely to hit with <20 characters.
2. **Second bottleneck:** Animation update cost. Three-stage transform pipeline runs per character per frame. For 50+ characters, consider updating only visible characters or reducing update frequency for distant ones.

## Sources

- [XNA SkinningSample_4_0 -- SkinningData.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModel/SkinningData.cs) -- HIGH confidence: canonical reference, defines BindPose/InverseBindPose/SkeletonHierarchy
- [XNA SkinningSample_4_0 -- AnimationPlayer.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModel/AnimationPlayer.cs) -- HIGH confidence: canonical three-stage transform pipeline
- [XNA SkinningSample_4_0 -- SkinnedModelProcessor.cs](https://github.com/SimonDarksideJ/XNAGameStudio/tree/archive/Samples/SkinningSample_4_0/SkinnedModelPipeline/SkinnedModelProcessor.cs) -- HIGH confidence: canonical processor showing FlattenSkeleton, BindPose extraction, InverseBindPose computation
- [MonoGame SkinnedEffect API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SkinnedEffect.html) -- HIGH confidence: official docs, MaxBones=72, SetBoneTransforms(), WeightsPerVertex
- [MonoGame Model API](https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Model.html) -- HIGH confidence: official docs, Model.Tag for custom data
- [MonoGame ModelBone docs](https://docs.monogame.net/articles/getting_to_know/whatis/graphics/WhatIs_ModelBone.html) -- HIGH confidence: official docs, CopyAbsoluteBoneTransformsTo
- [Tutorial: XNA SkinnedSample in MonoGame](https://community.monogame.net/t/tutorial-how-to-get-xnas-skinnedsample-working-with-monogame/7609) -- MEDIUM confidence: community port guide, documents common problems
- [MonoGame Issue #3825: SkinnedModel in core](https://github.com/MonoGame/MonoGame/issues/3825) -- MEDIUM confidence: documents importer keyframe differences, GPU constant buffer limits
- [MonoGameAnimatedModel (Lofionic)](https://github.com/Lofionic/MonoGameAnimatedModel) -- MEDIUM confidence: reference implementation with custom HLSL shader
- [BaamStudios/XnaMixamoImporter](https://github.com/BaamStudios/XnaMixamoImporter) -- MEDIUM confidence: documents Mixamo FBX format issues (FBX 2013 vs 2011), Blender conversion workflow
- [MonoGame Community: State of Skeleton Animation](https://community.monogame.net/t/state-of-skeleton-animation-in-monogame/20121) -- MEDIUM confidence: current ecosystem survey
- [MonoGame Community: Skinned Animation skeleton issues](https://community.monogame.net/t/skinned-animation-unable-to-find-skeleton/12069) -- MEDIUM confidence: documents per-mesh inverse bind pose variations
- Codebase analysis: `MixamoModelProcessor.cs`, `AnimatedModel.cs`, `AnimationData.cs`, `AnimationDataWriter.cs`, `AnimationDataReader.cs`, `EnemyRenderer.cs` -- HIGH confidence: direct code inspection

---
*Architecture research for: 3D Skeletal Animation System (MonoGame + Mixamo)*
*Researched: 2026-02-09*
