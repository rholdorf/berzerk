# Phase 4: Player Health & Survival - Context

**Gathered:** 2026-02-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Implement damage mechanics so the player can take damage and die. This includes health tracking, damage response with visual feedback, death state handling, and game-over functionality. Testing will use a key-press trigger to simulate damage. Health pickups and enemy damage belong in future phases.

</domain>

<decisions>
## Implementation Decisions

### Health system design
- Starting health: 100 HP
- Damage is permanent (no auto-regeneration) - health only restores via pickups
- Overheal allowed: health pickups can push above 100 HP
- Maximum health: 200 HP (double the starting value)

### Damage feedback
- Screen flash/vignette: Red vignette that fades in quickly then fades out over 0.3-0.5 seconds
- No invincibility frames - player can be hit continuously (brutal arcade difficulty)
- Audio feedback deferred to Phase 8 (Animation & Visual Polish)

### Death behavior
- Death sequence then game over (not instant)
- Visual: Fade to black over 1-2 seconds
- Physics: Character momentum continues during death (slides to natural stop)
- Game over state: Simple black screen with "Press R to restart" text (minimal for Phase 4 testing)

### Test damage source
- Key press trigger: Press H key to damage player by 10 HP
- Takes 10 hits to die from full health
- No heal key - restart game is the only way to restore health
- Test damage ignored when player is already dead

### Claude's Discretion
- Exact vignette fade timing curve (as long as it feels responsive)
- Health system implementation details (component structure, event handling)
- Fade to black animation curve
- Restart key implementation details

</decisions>

<specifics>
## Specific Ideas

No specific requirements - open to standard approaches that fit the arcade aesthetic established in previous phases.

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 04-player-health-&-survival*
*Context gathered: 2026-02-02*
