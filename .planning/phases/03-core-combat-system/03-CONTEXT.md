# Phase 3: Core Combat System - Context

**Gathered:** 2026-02-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Player laser weapon shooting with visible projectiles that collide with walls and test targets. Ammunition counter and pickups included. Enemy behavior and AI are Phase 5 - this phase focuses on the shooting mechanics themselves working correctly.

</domain>

<decisions>
## Implementation Decisions

### Aiming & Firing Controls
- **Aim method:** Screen center crosshair (Red Dead Redemption 2 style) - fixed crosshair at screen center, aims in camera-forward direction
- **Fire trigger:** Hold mouse button to auto-fire (automatic weapon)
- **Fire rate:** Medium rate (5-8 shots/sec) - balanced automatic fire, responsive but not spam
- **Character facing:** Claude's discretion - choose whether character rotates to aim direction or maintains movement facing based on what feels better with tank controls

### Projectile Behavior
- **Travel type:** Fast visible projectiles - laser bolts travel through space with visible movement and slight travel time (not instant hitscan)
- **Visual style:** Glowing sphere/bolt - simple geometric shape with emissive glow for clarity and easy implementation
- **Lifetime:** Distance-based (50-100 units) - projectiles disappear after traveling a set distance
- **Speed:** Fast (40-60 units/sec) - quick but visible, arcade-responsive feel

### Hit Detection & Feedback
- **Wall impacts:** Impact effect + disappear - visual effect (spark/flash/mark) appears at impact point, then projectile vanishes
- **Target hits:** Impact effect + disappear - visual feedback (flash/sparks) at hit point, then projectile vanishes
- **Test targets:** Simple colored cubes - basic geometric shapes that change color when hit (validates hit detection)
- **Collision precision:** Bounding sphere - balanced collision detection, good precision without expensive mesh checks

### Ammunition System
- **Starting ammo:** High (100-150 shots) - generous starting ammunition, focus on combat not scarcity
- **Ammo restoration:** Auto-reload + pickups - magazine automatically reloads from reserve when empty, pickups refill reserve ammo
- **Pickup amount:** Medium (30-50 shots) - moderate pickups that feel meaningful
- **Pickup spawning:** Dropped on target destruction - test targets drop ammo pickups when destroyed (positioned at death location)

### Claude's Discretion
- Character rotation behavior during aiming (face aim direction vs maintain movement facing)
- Exact fire rate within 5-8 shots/sec range
- Exact projectile speed within 40-60 units/sec range
- Exact projectile lifetime distance within 50-100 units range
- Exact starting ammo count within 100-150 range
- Impact effect visual design (sparks, flash, etc.)
- Magazine size and reserve capacity structure
- Reload animation timing (if any visual feedback added)

</decisions>

<specifics>
## Specific Ideas

- "Red Dead Redemption 2 style" aiming - screen center crosshair, fixed aim point
- Arcade shooter feel - fast projectiles, generous ammo, responsive controls
- Visual clarity important - glowing projectiles easy to see, clear impact feedback

</specifics>

<deferred>
## Deferred Ideas

None - discussion stayed within phase scope

</deferred>

---

*Phase: 03-core-combat-system*
*Context gathered: 2026-02-02*
