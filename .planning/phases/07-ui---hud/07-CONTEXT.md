# Phase 7: UI & HUD - Context

**Gathered:** 2026-02-09
**Status:** Ready for planning

<domain>
## Phase Boundary

Create complete heads-up display showing gameplay information: crosshair for aiming, health bar for player condition, ammo counter for weapon resources, score counter for points, game over screen for end state, and start menu for game launch. This phase presents game state visually without changing core mechanics.

</domain>

<decisions>
## Implementation Decisions

### Visual style & layout
- **Style:** Modern minimalist — clean lines, subtle elements, stays out of the way to focus on gameplay
- **Positioning:** Classic corners layout (health top-left, ammo top-right, score center-top)
- **Color palette:** Monochrome white/gray — pure minimalism, clean and unobtrusive
- **Element sizing:** Small and subtle (compact elements) — minimizes screen obstruction, true to minimalist philosophy

### Information hierarchy
- **Most prominent element:** Health bar — emphasize player survival condition as most critical information
- **Crosshair:** Always visible (persistent center dot/cross) — constant aiming reference, classic FPS approach
- **Score counter:** Always visible at top-center — constant score awareness, arcade classic
- **Visibility mode:** All elements always visible — consistent display, no fading or adaptive hiding

### Dynamic feedback
- **Damage response:** Red vignette flash + health bar flash — strong visual feedback with clear danger signal
- **Low ammo warning:** Flash ammo counter when low (<10 rounds) — proactive warning to prevent running dry
- **Score increases:** Silent update to counter only — minimal approach, score changes without fanfare
- **Pickup notifications:** Brief text popup ('+10 Ammo', '+20 Health') — clear confirmation of what was collected

### Menu flow & screens
- **Start menu:** Just 'Start Game' button — minimal entry, immediate action
- **Game over screen:** 'GAME OVER' + final score + 'Restart' / 'Quit' buttons — classic arcade feel showing achievement and next action
- **Pause menu:** Yes — ESC pauses, shows 'Resume' / 'Quit' — standard pause functionality for player control
- **Menu visual style:** Match HUD style (minimal white/gray) — consistent aesthetic with unified design language

### Claude's Discretion
- Exact font choice (as long as it's clean and readable)
- Precise element spacing and margins
- Crosshair shape (dot vs cross vs other simple shape)
- Animation timing for flashes and popups
- Health bar implementation (numeric, bar, or both)

</decisions>

<specifics>
## Specific Ideas

No specific product references provided — open to standard minimalist UI approaches that prioritize gameplay visibility and clean aesthetics.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 07-ui---hud*
*Context gathered: 2026-02-09*
