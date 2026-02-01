---
phase: 01-foundation-content-pipeline
plan: 01
subsystem: infrastructure
tags: [monogame, dotnet, content-pipeline, project-setup]
requires: []
provides:
  - "MonoGame solution structure"
  - "Content pipeline extension project"
  - "Build system configured for .NET 8"
affects:
  - "01-02 (will use content pipeline extensions)"
  - "All subsequent development (foundation)"
tech-stack:
  added:
    - MonoGame.Framework.DesktopGL 3.8.4.1
    - MonoGame.Content.Builder.Task 3.8.4.1
    - MonoGame.Framework.Content.Pipeline 3.8.4.1
  patterns:
    - Content Pipeline Extension architecture
    - Fail-fast build configuration
decisions:
  - id: use-net8
    title: Target .NET 8 instead of .NET 9/10
    rationale: MonoGame 3.8.4.1 minimum requirement, more stable baseline
  - id: fail-fast-content
    title: Enable MonoGameContentBuilderExitOnError
    rationale: Catch FBX import issues immediately during development
  - id: anycpu-pipeline
    title: Content pipeline targets AnyCPU
    rationale: MonoGame content pipeline doesn't support x86
key-files:
  created:
    - Berzerk.sln
    - Berzerk/Berzerk.csproj
    - Berzerk/BerzerkGame.cs
    - Berzerk/Program.cs
    - Berzerk/Content/Content.mgcb
    - Berzerk.ContentPipeline/Berzerk.ContentPipeline.csproj
    - .config/dotnet-tools.json
  modified: []
metrics:
  duration: 3.1
  completed: 2026-02-01
---

# Phase 1 Plan 1: Foundation Setup Summary

**One-liner:** MonoGame DesktopGL solution with content pipeline extension project on .NET 8 for macOS

## What Was Built

Created the foundational MonoGame project structure with two projects:

1. **Berzerk** - Main game project using MonoGame.Framework.DesktopGL
   - Renamed Game1 to BerzerkGame class (following research anti-pattern guidance)
   - Configured for .NET 8 with fail-fast on content errors
   - Project reference to content pipeline extension

2. **Berzerk.ContentPipeline** - Content pipeline extension project
   - Class library targeting .NET 8 with AnyCPU platform
   - MonoGame.Framework.Content.Pipeline package reference
   - Referenced in Content.mgcb for custom content processor support

The solution builds cleanly on macOS and is ready for FBX animation processor development in the next plan.

## Key Implementation Details

### Project Configuration

**Main game project (Berzerk.csproj):**
- Target: .NET 8 (net8.0)
- MonoGameContentBuilderExitOnError: true (fail-fast on content errors)
- Project reference to Berzerk.ContentPipeline

**Content pipeline project (Berzerk.ContentPipeline.csproj):**
- Target: .NET 8 (net8.0)
- PlatformTarget: AnyCPU (required by MonoGame content pipeline)
- Package: MonoGame.Framework.Content.Pipeline 3.8.4.1

**Content.mgcb reference:**
```
/reference:../../Berzerk.ContentPipeline/bin/Debug/net8.0/Berzerk.ContentPipeline.dll
```

### Build System

- Local dotnet tools manifest at `.config/dotnet-tools.json`
- MGCB CLI tools version 3.8.4.1
- Content builds as part of solution build via MonoGame.Content.Builder.Task
- Both projects build successfully in ~0.86 seconds (incremental)

## Deviations from Plan

None - plan executed exactly as written. All tasks completed successfully.

## Decisions Made

| Decision | Options Considered | Chosen | Rationale |
|----------|-------------------|---------|-----------|
| .NET Version | .NET 9/10 vs .NET 8 | .NET 8 | MonoGame 3.8.4.1 supports .NET 8+ minimum; more stable baseline for development |
| Fail-fast Content | Default vs Exit on Error | Exit on Error | Research identified FBX import as highest risk; catch issues immediately |
| Platform Target | x86 vs AnyCPU | AnyCPU | MonoGame content pipeline requires AnyCPU; x86 not supported |

## Testing Performed

**Build Verification:**
- ✅ `dotnet build Berzerk.sln` succeeds with no errors
- ✅ Berzerk.ContentPipeline.dll generated at expected path
- ✅ Content.mgcb contains /reference directive
- ✅ BerzerkGame class exists (not Game1)

**Not Tested (per plan scope):**
- Game execution with window open (deferred to avoid manual window closing step)
- Cross-platform builds (Windows/Linux) - deferred to later phase per CONTEXT.md

## Technical Notes

### Project Structure
```
Berzerk/
├── .config/dotnet-tools.json          # MGCB CLI tools
├── Berzerk.sln                        # Solution file
├── Berzerk/
│   ├── Berzerk.csproj                 # Main game project
│   ├── BerzerkGame.cs                 # Main game class
│   ├── Program.cs                     # Entry point
│   └── Content/
│       └── Content.mgcb               # Content pipeline project
└── Berzerk.ContentPipeline/
    └── Berzerk.ContentPipeline.csproj # Extension project
```

### Dependencies
- Main game depends on content pipeline (ProjectReference)
- Content.mgcb references pipeline DLL (relative path)
- Build order: ContentPipeline → Content → Game

## Artifacts

**Git Commits:**
- c8ff158: feat(01-01): create MonoGame solution and game project
- c4616ca: feat(01-01): create content pipeline extension project

**Files Created:** 7 key files (see key-files section above)
**Build Output:**
- Berzerk.dll (4KB)
- Berzerk.ContentPipeline.dll (4.1KB)

## Next Phase Readiness

### Unblocked Work
- ✅ Plan 01-02: FBX animation import can proceed
- ✅ Custom content processors can be added to Berzerk.ContentPipeline
- ✅ Mixamo models can be tested

### Prerequisites Met
- ✅ Solution builds on macOS
- ✅ Content pipeline extension project compiles
- ✅ Content.mgcb references extension assembly
- ✅ Fail-fast configuration will surface FBX import issues immediately

### Known Limitations
- Content pipeline reference uses Debug configuration path (not Release)
- No verbose logging configured yet for FBX import debugging
- Cross-platform validation deferred to later phase

### Recommendations for Next Plan
1. Add verbose logging to catch FBX import issues early
2. Test with simple Mixamo model immediately (validate import works)
3. Create custom processor that wraps ModelProcessor with diagnostic output
4. Monitor content build times as assets are added

## Learnings

### What Went Well
- MonoGame template provides clean starting structure
- Rename from Game1 to BerzerkGame done immediately (anti-pattern avoided)
- Content pipeline project configured correctly on first attempt
- Build system works smoothly on macOS

### Challenges Encountered
- None - straightforward project setup

### Surprises
- Template creates .NET 10 projects by default; had to downgrade to .NET 8
- .config directory created inside Berzerk project; moved to root

### Process Notes
- Following research guidance (immediate rename, fail-fast config) prevented technical debt
- Clear project structure from start will help as complexity grows
- Content pipeline extension ready for FBX processor work

---

**Phase:** 01-foundation-content-pipeline
**Plan:** 01 of 4
**Status:** ✅ Complete
**Duration:** 3.1 minutes
**Completed:** 2026-02-01
