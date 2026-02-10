# MonoGame Mixamo Animation System

## What This Is

A robust 3D animation system for MonoGame that reliably loads and plays animations from Mixamo. The system handles character models with skeletal animation, enabling action/adventure gameplay with animated character movement. Currently building this to solve the T-pose problem where Mixamo models load but animations don't play.

## Core Value

Create a reusable pipeline that works with any Mixamo model - not just a one-off fix, but a system that reliably handles FBX imports from Mixamo and plays animations correctly in MonoGame.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Investigate root cause of T-pose issue with Mixamo FBX in MonoGame
- [ ] Understand Mixamo FBX format structure (bones, skinning, animation data)
- [ ] Understand MonoGame Content Pipeline FBX processing
- [ ] Load and display test-character.fbx with correct bind pose
- [ ] Play individual animations (idle, walk, run, bash) on the character
- [ ] Switch between animations at runtime
- [ ] Create reusable system that works with any Mixamo model
- [ ] Document the pipeline and any conversion steps needed

### Out of Scope

- Advanced animation blending/crossfading — defer to v2
- Complex state machines — basic playback first
- IK (inverse kinematics) — not needed for initial system
- Custom model creation — using Mixamo assets only
- Multiple characters simultaneously — focus on single character pipeline first

## Context

**Current state:**
- Action/adventure 3D game in MonoGame (partially built)
- Downloaded assets from Mixamo: test-character.fbx (model) + bash.fbx, idle.fbx, run.fbx, walk.fbx (animations)
- Models render in T-pose only — animations don't play
- No modifications made to downloaded FBX files yet

**Technical environment:**
- MonoGame framework
- FBX format from Mixamo.com
- 3D skeletal animation with skinning

**Known issues:**
- T-pose rendering suggests animations aren't being applied to skeleton
- Possible causes: format incompatibility, Content Pipeline issues, incorrect loading code, missing animation data

**Approach:**
- Deep investigation preferred over quick hacks
- Want to understand the full pipeline: Mixamo → FBX → MonoGame Content Pipeline → Runtime playback
- Goal is knowledge and reusability, not just making this one model work

## Constraints

- **Asset source**: Mixamo.com — all models and animations come from this source
- **Framework**: MonoGame — committed to this framework
- **Animation scope**: Basic playback (idle, walk, run, combat) — no facial animation or complex features initially

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Research FBX format deeply | Need to understand why T-pose occurs, not just apply fixes blindly | — Pending |
| Focus on reusable system | Want solution that works for any Mixamo model, not just test-character | — Pending |

---
*Last updated: 2026-02-09 after initialization*
