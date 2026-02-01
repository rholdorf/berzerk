---
phase: 01-foundation-content-pipeline
plan: 02
subsystem: content-pipeline
tags: [monogame, content-pipeline, fbx, mixamo, animation, processor]
requires:
  - "01-01 (MonoGame solution structure)"
provides:
  - "Custom content processor for Mixamo FBX models"
  - "Animation data structures for XNB serialization"
  - "Verbose logging for FBX import debugging"
affects:
  - "01-03 (will test FBX import with sample model)"
  - "01-04 (animation playback will use AnimationData)"
tech-stack:
  added: []
  patterns:
    - Content Pipeline Extension with custom processor
    - Verbose logging for build-time diagnostics
    - Animation data serialization pattern
decisions:
  - id: animation-data-in-tag
    title: Store animation data in Model.Tag
    rationale: Standard MonoGame pattern for attaching custom data to models
  - id: bone-index-at-build-time
    title: Assign bone indices during processing
    rationale: BoneContent doesn't have Index property at build time; must be tracked manually
  - id: defer-keyframe-extraction
    title: Defer actual keyframe extraction to later plan
    rationale: Processor validates and logs but doesn't extract full keyframe data yet
key-files:
  created:
    - Berzerk.ContentPipeline/Keyframe.cs
    - Berzerk.ContentPipeline/AnimationClip.cs
    - Berzerk.ContentPipeline/AnimationData.cs
    - Berzerk.ContentPipeline/MixamoModelProcessor.cs
    - Berzerk.ContentPipeline/AnimationDataWriter.cs
  modified:
    - Berzerk.ContentPipeline/Berzerk.ContentPipeline.csproj
metrics:
  duration: 2.5
  completed: 2026-02-01
---

# Phase 1 Plan 2: Mixamo Model Processor Summary

**One-liner:** Custom content processor with verbose logging for Mixamo FBX validation and animation extraction

## What Was Built

Created a custom MonoGame content pipeline processor specifically for Mixamo FBX models:

1. **Animation Data Structures** - Serializable classes for runtime animation playback
   - Keyframe: Time, BoneIndex, Transform (Matrix)
   - AnimationClip: Name, Duration, Keyframes dictionary
   - AnimationData: Top-level container with Clips and BoneIndices

2. **MixamoModelProcessor** - Custom processor extending ModelProcessor
   - Validates skeleton structure using MeshHelper.FindSkeleton()
   - Logs bone count and bone hierarchy for debugging
   - Extracts animation clip names and durations from NodeContent
   - Builds AnimationData and attaches to Model.Tag
   - Comprehensive logging via ContentProcessorContext.Logger
   - Fails build on errors, warns on missing skeleton/animations

3. **AnimationDataWriter** - Content type writer for XNB serialization
   - Serializes bone indices mapping (name -> index)
   - Serializes animation clips with keyframes
   - Specifies runtime reader/type paths for deserialization

The processor is ready to validate Mixamo FBX imports and will surface issues immediately with verbose logging.

## Key Implementation Details

### Animation Data Structures

**Design for XNB serialization:**
- All types use simple primitives (string, int, TimeSpan, Matrix, Dictionary)
- AnimationData is top-level container attached to Model.Tag
- BoneIndices mapping allows efficient runtime lookup by bone name
- Keyframes organized by bone name for efficient animation application

**File structure:**
```
AnimationData
  ├── BoneIndices: Dictionary<string, int>
  └── Clips: Dictionary<string, AnimationClip>
        ├── Name: string
        ├── Duration: TimeSpan
        └── Keyframes: Dictionary<string, List<Keyframe>>
              ├── Time: TimeSpan
              ├── BoneIndex: int
              └── Transform: Matrix
```

### MixamoModelProcessor Logging

**Entry point:**
```csharp
context.Logger.LogImportantMessage("=== Processing Mixamo model: {0} ===", input.Name);
```

**Skeleton validation:**
- Finds skeleton with MeshHelper.FindSkeleton()
- Logs warning if skeleton missing (may be static model)
- Counts bones recursively and logs total
- Logs full bone hierarchy with indentation

**Animation extraction:**
- Checks NodeContent.Animations for clip data
- Logs clip names and durations
- Logs info message if no animations (expected for static models)

**Error handling:**
- Wraps base.Process() in try-catch
- Logs detailed error messages
- Throws InvalidContentException with context on failure
- Build fails immediately (fail-fast per research guidance)

### Animation Data Writer

**Serialization format:**
```
1. Bone indices count
2. For each bone: name (string), index (int)
3. Clips count
4. For each clip:
   - Clip name
   - Duration (long ticks)
   - Keyframes count per bone
   - For each bone:
     - Bone name
     - Keyframes count
     - For each keyframe: time (ticks), bone index, transform (Matrix)
```

**Runtime integration:**
- GetRuntimeReader() returns "Berzerk.Content.AnimationDataReader, Berzerk"
- GetRuntimeType() returns "Berzerk.Content.AnimationData, Berzerk"
- Corresponding reader will be created in game project (Plan 04)

### Package Dependencies

**Added to ContentPipeline project:**
- MonoGame.Framework.DesktopGL 3.8.4.1 (for Matrix type)

**Why both pipeline packages:**
- MonoGame.Framework.Content.Pipeline: Processor/Writer base classes
- MonoGame.Framework.DesktopGL: Core types (Matrix, Vector3, etc.)

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] BoneContent.Index property doesn't exist at build time**

- **Found during:** Task 2 (MixamoModelProcessor implementation)
- **Issue:** Attempted to log bone.Index and use it for bone indices mapping, but BoneContent doesn't have an Index property during content processing
- **Fix:** Changed LogBoneHierarchy to not log index; modified BuildBoneIndices to track index manually with currentIndex parameter
- **Files modified:** Berzerk.ContentPipeline/MixamoModelProcessor.cs
- **Commit:** 64d4278

**2. [Rule 2 - Missing Critical] Matrix type unavailable in ContentPipeline project**

- **Found during:** Task 1 (building Keyframe.cs)
- **Issue:** Keyframe uses Matrix type but MonoGame.Framework.Content.Pipeline doesn't include core types
- **Fix:** Added MonoGame.Framework.DesktopGL package reference to ContentPipeline project
- **Files modified:** Berzerk.ContentPipeline/Berzerk.ContentPipeline.csproj
- **Commit:** f0093d3

## Decisions Made

| Decision | Options Considered | Chosen | Rationale |
|----------|-------------------|---------|-----------|
| Animation Data Storage | Store in separate file vs Model.Tag | Model.Tag | Standard MonoGame pattern; keeps animation data with model; single file to load |
| Bone Index Tracking | Use BoneContent.Index vs Manual tracking | Manual tracking | BoneContent.Index doesn't exist at build time; must be assigned during processing |
| Keyframe Extraction Timing | Full extraction now vs Defer to later | Defer to later | Processor validates and logs structure; full extraction not needed until animation playback (Plan 04) |
| Logging Level | Minimal vs Verbose | Verbose | Research identified FBX import as highest risk; need maximum visibility into import process |

## Testing Performed

**Build Verification:**
- ✅ `dotnet build Berzerk.ContentPipeline/Berzerk.ContentPipeline.csproj` succeeds
- ✅ `dotnet build Berzerk.sln` succeeds with no errors
- ✅ MixamoModelProcessor has [ContentProcessor] attribute
- ✅ AnimationDataWriter has [ContentTypeWriter] attribute
- ✅ Processor uses ContentProcessorContext.Logger for all logging
- ✅ All 5 required files exist (3 data structures + processor + writer)

**Not Tested (per plan scope):**
- Actual FBX import with processor (requires test model - Plan 01-03)
- Animation data deserialization at runtime (requires reader - Plan 01-04)
- Cross-platform content pipeline (deferred to later phase)

## Technical Notes

### Content Pipeline Architecture

**Build-time (this plan):**
```
FBX File
  ↓ FbxImporter (built-in)
NodeContent (intermediate)
  ↓ MixamoModelProcessor (custom)
ModelContent + AnimationData in Tag
  ↓ AnimationDataWriter (custom)
XNB File (binary)
```

**Runtime (future plans):**
```
XNB File
  ↓ ContentManager.Load<Model>()
  ↓ AnimationDataReader (Plan 04)
Model + AnimationData in Tag
  ↓ AnimationPlayer (Plan 04)
Animated 3D model rendering
```

### Verbose Logging Strategy

**LogImportantMessage for:**
- Processing start/end markers
- Skeleton found confirmation
- Animation clips discovered
- Standard processor completion

**LogMessage for:**
- Bone hierarchy details
- Individual animation clip info
- Animation data attachment
- Build progress details

**LogWarning for:**
- Missing skeleton (may be static model)
- Potential import issues

**Exception + Log for:**
- Actual import failures
- Corrupt data
- Unreadable structure

### Processor Registration

**How MonoGame finds the processor:**
1. Content.mgcb has `/reference:../../Berzerk.ContentPipeline/bin/Debug/net8.0/Berzerk.ContentPipeline.dll`
2. MGCB CLI scans assembly for [ContentProcessor] attributes
3. Processor appears in MGCB Editor dropdown as "Mixamo Model Processor"
4. Can be assigned to .fbx files manually or via script

## Artifacts

**Git Commits:**
- f0093d3: feat(01-02): create animation data structures
- 64d4278: feat(01-02): create Mixamo model processor with verbose logging
- 69f9d09: feat(01-02): create content writer for animation data

**Files Created:** 5 files (see key-files section above)

**Build Output:**
- Berzerk.ContentPipeline.dll (now includes custom processor)
- Processor ready for use in Content.mgcb files

## Next Phase Readiness

### Unblocked Work
- ✅ Plan 01-03: Can test FBX import with sample Mixamo model
- ✅ Can assign MixamoModelProcessor to FBX files in Content.mgcb
- ✅ Verbose logging will show exact import process

### Prerequisites Met
- ✅ Custom content processor compiles and is registered
- ✅ Animation data structures exist and are serializable
- ✅ Content writer is ready to serialize AnimationData
- ✅ Fail-fast configuration will surface FBX import issues immediately

### Known Limitations
- Keyframe extraction is stubbed - only stores clip names/durations for now
- No runtime reader yet (needs implementation in game project - Plan 04)
- Not tested with actual Mixamo FBX file yet (Plan 01-03)
- Bone index assignment may need adjustment based on MonoGame's runtime expectations

### Recommendations for Next Plan
1. **Test with simple Mixamo character immediately** - Validate processor works with real FBX
2. **Watch build output carefully** - Verbose logging will show import process details
3. **Test both animated and static models** - Verify warnings vs errors behavior
4. **Document any Mixamo-specific issues** - Build knowledge for future processor enhancements

## Learnings

### What Went Well
- Clear separation of concerns: data structures, processor, writer
- Verbose logging strategy follows research guidance (fail-fast, maximum visibility)
- Build-time validation catches issues early (before runtime)
- Animation data structure is flexible and extensible

### Challenges Encountered
- BoneContent.Index property doesn't exist at build time (fixed with manual tracking)
- Matrix type required additional package reference (fixed with DesktopGL reference)
- Bone hierarchy logging needed careful indentation logic

### Surprises
- MonoGame.Framework.Content.Pipeline doesn't include core types like Matrix
- Bone indices must be manually tracked during processing
- Animation extraction API is straightforward (NodeContent.Animations)

### Process Notes
- Deviation rules worked well for auto-fixing build-time issues
- Fail-fast approach (throw exceptions on errors) aligns with research guidance
- Deferring full keyframe extraction keeps scope manageable
- Ready for real-world testing with Mixamo FBX files in next plan

---

**Phase:** 01-foundation-content-pipeline
**Plan:** 02 of 4
**Status:** ✅ Complete
**Duration:** 2.5 minutes
**Completed:** 2026-02-01
