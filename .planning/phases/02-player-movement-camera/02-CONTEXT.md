# Phase 2: Player Movement & Camera - Context

**Gathered:** 2026-02-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Third-person character controller with responsive WASD movement and smooth camera following. Player navigates 3D space with mouse-controlled aiming and camera rotation. Collision detection prevents clipping through walls.

</domain>

<decisions>
## Implementation Decisions

### Movement Feel
- Snappy acceleration (quick ramp to full speed, not instant)
- Medium movement speed (balanced for aiming while moving)
- Free analog movement (WASD combine smoothly for any direction)
- Quick rotation blend when changing direction (brief rotation animation, not instant snap)

### Mouse Aiming Integration
- Player character faces movement direction, cursor does not affect character rotation
- Free vertical aiming (mouse Y-axis controls pitch for aiming up/down)
- Crosshair on screen for aiming visual (not deferred to UI phase)

### Camera Behavior
- Scroll wheel controls camera distance (starts medium, scroll up = closer, scroll down = farther)
- Camera angle transitions smoothly: eye-level when close, high angle when far
- Light spring smoothing for camera follow (gentle interpolation, not locked or heavy lag)
- Free camera rotation controlled by mouse or keys (player can orbit around character)

### Camera Collision Handling
- Camera zooms in automatically when hitting walls/obstacles
- Smooth zoom out when returning to normal distance after collision
- Permissive collision (minor clipping into walls OK if it maintains better view)
- Everything solid blocks camera (walls, large obstacles, all collision geometry)

### Claude's Discretion
- Camera cursor offset decision (whether cursor affects camera positioning slightly)
- Exact smoothing coefficients and interpolation curves
- Scroll wheel zoom speed and distance limits
- Camera angle transition curve between close/far distances

</decisions>

<specifics>
## Specific Ideas

- Arcade-style responsiveness preserved from original Berzerk, but adapted to 3D third-person format
- Camera should feel natural and non-intrusive \u2014 player focuses on combat, not fighting camera

</specifics>

<deferred>
## Deferred Ideas

None \u2014 discussion stayed within phase scope

</deferred>

---

*Phase: 02-player-movement-camera*
*Context gathered: 2026-02-01*
