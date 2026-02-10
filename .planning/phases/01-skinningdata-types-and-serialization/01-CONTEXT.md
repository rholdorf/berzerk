# Phase 1: SkinningData Types and Serialization - Context

**Gathered:** 2026-02-10
**Status:** Ready for planning

<domain>
## Phase Boundary

Define data structures (SkinningData, AnimationClip, Keyframe) and Content Pipeline serialization (ContentTypeWriter/Reader) that establish the contract between build-time FBX processing and runtime animation playback. This phase creates the foundational types that all subsequent phases depend on.

</domain>

<decisions>
## Implementation Decisions

### Claude's Discretion

The user has delegated all implementation decisions for this phase to Claude. This includes:

- **Data structure organization** — How SkinningData, AnimationClip, and Keyframe are structured (nested vs flat, required vs optional fields)
- **Serialization format** — Binary layout, version handling, compression decisions, endianness
- **Animation clip storage** — How multiple clips are organized (dictionary keys, file structure)
- **Matrix representation** — How to store bind pose and inverse bind pose (full 4x4 matrices vs decomposed, bone index mapping)

**Guidance:** Follow MonoGame Content Pipeline conventions and prioritize:
1. Correctness — data must survive serialization round-trip intact
2. Clarity — structures should be easy to understand and debug
3. Efficiency — reasonable binary size and deserialization speed
4. Compatibility — work with standard Mixamo FBX structure

</decisions>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches.

**Context from requirements:**
- Must support ~65 bones (typical Mixamo skeleton)
- Must store bind pose, inverse bind pose, skeleton hierarchy, and animation clips (DATA-01)
- Must serialize to binary at build time and deserialize at runtime without data loss (DATA-03, DATA-04)
- Animation clips need per-bone keyframe data with transforms over time (DATA-02)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 01-skinningdata-types-and-serialization*
*Context gathered: 2026-02-10*
