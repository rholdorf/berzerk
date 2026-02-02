# Phase 3: Core Combat System - Research

**Researched:** 2026-02-02
**Domain:** MonoGame 3D combat system with projectile mechanics, collision detection, and ammunition management
**Confidence:** MEDIUM-HIGH

## Summary

This phase implements a complete shooting system for MonoGame including projectile spawning/movement, collision detection, ammunition management, and visual feedback. The research focused on MonoGame-specific patterns since the project uses MonoGame 3.8.4.1 with existing InputManager, Transform, and ThirdPersonCamera systems from Phase 2.

The standard approach for combat systems in MonoGame uses a **manager pattern** where a ProjectileManager handles all active projectiles, combined with **object pooling** to prevent garbage collection spikes from frequent projectile spawning. Collision detection uses MonoGame's built-in **BoundingSphere.Intersects()** method for fast sphere-to-sphere tests. Visual feedback uses **BasicEffect's EmissiveColor** for glowing projectiles and simple geometry primitives.

The user has locked key decisions: screen-center crosshair aiming (camera-forward direction), fast visible projectiles (not hitscan), automatic fire on mouse hold, and magazine+reserve ammunition system with auto-reload. Research validated these are standard arcade shooter patterns well-supported by MonoGame.

**Primary recommendation:** Implement ProjectileManager with object pooling, use camera.Forward for projectile direction, leverage existing InputManager.IsLeftMouseHeld() for auto-fire, and create sphere geometry with BasicEffect for glowing projectiles.

## Standard Stack

MonoGame 3.8.4.1 provides all necessary built-in functionality. No external libraries required for this phase.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework | 3.8.4.1 | Game engine, collision, rendering | Already project foundation, built-in BoundingSphere collision |
| .NET | 8.0 | Runtime | Phase 1 decision, stable baseline |

### Supporting
| Class/Pattern | Purpose | When to Use |
|---------------|---------|-------------|
| BoundingSphere | 3D collision detection | Fast sphere-to-sphere tests, rotation-independent |
| BasicEffect | Material rendering | Emissive glow for projectiles, simple lighting |
| GameTime.ElapsedGameTime.TotalSeconds | Delta time | Frame-rate independent movement/timers |
| Object Pool Pattern | Projectile recycling | Prevent GC spikes from frequent spawn/destroy |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| BoundingSphere | Ray casting | More precise but slower, overkill for fast arcade projectiles |
| Object pooling | new Projectile() each shot | Simpler code but causes GC spikes, frame drops |
| BasicEffect emissive | Custom shader effects | More visual fidelity but adds complexity, Phase 1 chose BasicEffect |
| Manager pattern | Entity Component System | More scalable for large games, unnecessary for prototype scope |

**Installation:**
No additional packages needed. All components are part of MonoGame.Framework 3.8.4.1.

## Architecture Patterns

### Recommended Project Structure
```
Berzerk/Source/
├── Combat/              # NEW - Combat system components
│   ├── Projectile.cs    # Individual projectile state/behavior
│   ├── ProjectileManager.cs  # Manages all active projectiles + pool
│   ├── AmmoSystem.cs    # Magazine/reserve ammo tracking
│   └── ImpactEffect.cs  # Visual feedback for hits
├── Core/                # EXISTING - Shared components
│   └── Transform.cs     # Reuse for projectile position/rotation
├── Input/               # EXISTING - Input handling
│   └── InputManager.cs  # Already has IsLeftMouseHeld()
└── Controllers/         # EXISTING - Entity controllers
    └── PlayerController.cs  # Extend with shooting behavior
```

### Pattern 1: Projectile Manager with Object Pool
**What:** Central manager maintains list of active projectiles and pool of inactive ones for reuse.
**When to use:** Any system spawning/destroying objects frequently (bullets, particles, pickups).
**Benefits:** Prevents GC spikes (77% faster than instantiation per WebSearch), smooth frame rate.

**Example structure:**
```csharp
// Based on MonoGame community patterns
public class ProjectileManager
{
    private List<Projectile> _activeProjectiles;
    private Queue<Projectile> _projectilePool;
    private const int POOL_SIZE = 50; // Pre-allocate for performance

    public void Initialize(int poolSize)
    {
        _activeProjectiles = new List<Projectile>();
        _projectilePool = new Queue<Projectile>(poolSize);

        // Pre-create projectiles
        for (int i = 0; i < poolSize; i++)
        {
            _projectilePool.Enqueue(new Projectile());
        }
    }

    public Projectile Spawn(Vector3 position, Vector3 direction, float speed)
    {
        Projectile proj = _projectilePool.Count > 0
            ? _projectilePool.Dequeue()
            : new Projectile(); // Grow pool if needed

        proj.Activate(position, direction, speed);
        _activeProjectiles.Add(proj);
        return proj;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            _activeProjectiles[i].Update(deltaTime);

            if (!_activeProjectiles[i].IsActive)
            {
                // Return to pool
                _projectilePool.Enqueue(_activeProjectiles[i]);
                _activeProjectiles.RemoveAt(i);
            }
        }
    }
}
```

### Pattern 2: Frame-Rate Independent Fire Rate
**What:** Timer-based cooldown using delta time to limit shots per second.
**When to use:** Any rate-limited action (firing, abilities, animations).
**MonoGame approach:** Use GameTime.ElapsedGameTime.TotalSeconds (returns double, cast to float).

**Example structure:**
```csharp
// Based on MonoGame GameTime documentation and community patterns
public class WeaponController
{
    private float _fireRate = 6.0f; // 6 shots/sec
    private float _timeSinceLastShot = 0f;

    public void Update(GameTime gameTime, bool isFiring)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timeSinceLastShot += deltaTime;

        float fireInterval = 1.0f / _fireRate; // 0.166 seconds for 6 shots/sec

        if (isFiring && _timeSinceLastShot >= fireInterval)
        {
            Fire();
            _timeSinceLastShot = 0f; // Reset timer
        }
    }
}
```

### Pattern 3: Screen-Center Aiming (Camera-Forward Shooting)
**What:** Projectiles spawn from player position but travel in camera's forward direction.
**When to use:** Third-person shooters with fixed crosshair (RDR2 style per CONTEXT.md decision).
**Critical insight:** Camera forward direction, NOT player forward direction for aim.

**Example structure:**
```csharp
// Based on third-person shooter patterns from WebSearch
public void Fire(ThirdPersonCamera camera, Transform playerTransform)
{
    Vector3 spawnPosition = playerTransform.Position + Vector3.Up * 1.5f; // Shoulder height
    Vector3 shootDirection = camera.Forward; // CAMERA forward, not player

    _projectileManager.Spawn(spawnPosition, shootDirection, projectileSpeed);
}
```

**Important:** Player model may not face camera direction (per CONTEXT.md discretion). Projectile always follows crosshair (camera forward), creates authentic third-person shooter feel.

### Pattern 4: Magazine + Reserve Ammo System
**What:** Two-tier ammo storage - current magazine (20-30 rounds) and reserve pool (100-150 total).
**When to use:** Shooters with reload mechanics and resource management.
**User decision:** Auto-reload when magazine empties, pickups refill reserve.

**Example structure:**
```csharp
public class AmmoSystem
{
    public int CurrentMagazine { get; private set; }
    public int MaxMagazineSize { get; private set; } = 25;
    public int ReserveAmmo { get; private set; }
    public int MaxReserveAmmo { get; private set; } = 125; // 100-150 per CONTEXT.md

    public bool TryConsumeAmmo()
    {
        if (CurrentMagazine > 0)
        {
            CurrentMagazine--;
            return true;
        }

        // Auto-reload if reserve available (per CONTEXT.md decision)
        if (ReserveAmmo > 0)
        {
            Reload();
            return TryConsumeAmmo(); // Retry after reload
        }

        return false; // Out of ammo
    }

    public void Reload()
    {
        int ammoNeeded = MaxMagazineSize - CurrentMagazine;
        int ammoToReload = Math.Min(ammoNeeded, ReserveAmmo);

        CurrentMagazine += ammoToReload;
        ReserveAmmo -= ammoToReload;
    }

    public void AddAmmo(int amount)
    {
        ReserveAmmo = Math.Min(ReserveAmmo + amount, MaxReserveAmmo);
    }
}
```

### Pattern 5: BoundingSphere Collision Detection
**What:** MonoGame's built-in sphere-based collision using BoundingSphere.Intersects().
**When to use:** Fast-moving projectiles against multiple targets (per CONTEXT.md decision).
**Official source:** MonoGame documentation on collision detection.

**Example structure:**
```csharp
// Source: MonoGame official documentation
// https://docs.monogame.net/articles/getting_to_know/howto/HowTo_CollisionDetectionOverview.html
public void CheckCollisions(Projectile projectile, List<BoundingBox> walls)
{
    BoundingSphere projectileSphere = new BoundingSphere(
        projectile.Position,
        projectile.Radius
    );

    // Check walls
    foreach (var wall in walls)
    {
        if (projectileSphere.Intersects(wall))
        {
            projectile.OnHitWall();
            return;
        }
    }

    // Check targets (Phase 3 uses simple cubes per CONTEXT.md)
    foreach (var target in _testTargets)
    {
        BoundingSphere targetSphere = new BoundingSphere(
            target.Position,
            target.Radius
        );

        if (projectileSphere.Intersects(targetSphere))
        {
            projectile.OnHitTarget(target);
            target.OnHit();
            return;
        }
    }
}
```

**Key insight from MonoGame docs:** BoundingSphere is fast (distance check between centers), compact (Vector3 + float), rotation-independent (no recalculation needed), and simple to move (update center only).

### Pattern 6: Glowing Projectile with BasicEffect
**What:** Create simple sphere geometry with emissive color for self-illuminated look.
**When to use:** CONTEXT.md specifies "glowing sphere/bolt" visual style.
**MonoGame approach:** BasicEffect.EmissiveColor property for glow without lighting.

**Example structure:**
```csharp
// Source: MonoGame BasicEffect API documentation
// https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BasicEffect.html
public void DrawProjectile(Projectile projectile, Matrix view, Matrix projection)
{
    BasicEffect effect = new BasicEffect(GraphicsDevice);
    effect.World = Matrix.CreateTranslation(projectile.Position);
    effect.View = view;
    effect.Projection = projection;

    // Emissive color creates glow (range 0-1)
    effect.EmissiveColor = new Vector3(0.2f, 0.8f, 1.0f); // Cyan laser
    effect.DiffuseColor = new Vector3(0.2f, 0.8f, 1.0f);

    // Draw sphere geometry
    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
    {
        pass.Apply();
        GraphicsDevice.DrawUserIndexedPrimitives(
            PrimitiveType.TriangleList,
            _sphereVertices, 0, _sphereVertices.Length,
            _sphereIndices, 0, _sphereIndices.Length / 3
        );
    }
}
```

**Note:** BasicEffect.EmissiveColor makes surface appear self-illuminated (doesn't light other objects). For simple glowing projectiles, this is sufficient per Phase 1 BasicEffect decision.

### Anti-Patterns to Avoid

- **Raycast every frame for every projectile:** Expensive and unnecessary. Use BoundingSphere for projectiles, reserve raycasting for instant-hit weapons (not in scope).

- **New GameObject per shot without pooling:** Causes GC pressure, frame rate drops. MonoGame community strongly recommends pooling for projectiles.

- **Using player.Forward for aim direction:** In third-person, player faces movement direction (tank controls per Phase 2), but shoots where camera aims. Always use camera.Forward for projectile direction.

- **Single ammo counter:** User wants magazine+reserve system. Single counter loses reload gameplay and resource tension.

- **Checking collision after movement:** Can cause tunneling (projectile jumps through thin walls). For arcade speeds (40-60 units/sec per CONTEXT.md), BoundingSphere per-frame checks are sufficient, but consider continuous collision if issues arise.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 3D sphere geometry generation | Manual vertex/index calculation | MonoGame-Samples SpherePrimitive class | Complex trigonometry for sphere tessellation, normals, UV coords. Community has battle-tested implementations. |
| Particle effects for impacts | Custom particle system | MonoGame.Extended particle system (optional) OR simple billboard sprites | Particle systems have complex modifiers, pooling, emitters. For Phase 3, even billboards + fade-out is sufficient. Don't over-engineer. |
| Continuous collision detection | Custom swept-sphere algorithm | BoundingSphere + smaller time steps if needed | CCD is mathematically complex. Arcade projectiles (40-60 units/sec) work fine with per-frame sphere checks at 60fps. |
| Timer accumulation for fire rate | Manual tick counting | GameTime.ElapsedGameTime.TotalSeconds | MonoGame's GameTime handles variable frame rates, pause states, fixed-step vs variable-step complexity. |

**Key insight:** MonoGame provides robust timing (GameTime) and collision (BoundingSphere) primitives. Don't reinvent. Community patterns (pooling, managers) are well-established from XNA heritage. Focus on game-specific logic, not framework capabilities.

## Common Pitfalls

### Pitfall 1: Projectile Tunneling Through Walls
**What goes wrong:** Fast projectiles skip past thin walls between frames, no collision detected.
**Why it happens:** At 60fps, projectile moves ~1 unit per frame (60 units/sec / 60fps). Thin walls (<1 unit) can be missed.
**How to avoid:**
- Keep projectile speed reasonable (40-60 units/sec per CONTEXT.md is safe)
- Use thick walls (2+ units) in level geometry
- If tunneling occurs, enable continuous collision (ray cast from previous to current position)
**Warning signs:** Projectiles occasionally pass through walls, especially at corners or when frame rate drops.

### Pitfall 2: Garbage Collection Spikes from Projectile Spam
**What goes wrong:** Frame rate drops/stutters when firing rapidly or many projectiles on screen.
**Why it happens:** Creating new Projectile() objects allocates heap memory. Garbage collector pauses game to clean up.
**How to avoid:** Object pooling pattern (see Architecture Patterns). Pre-allocate pool of 50-100 projectiles at initialization.
**Warning signs:** Profiler shows GC collections correlated with firing, frame time spikes during combat.

### Pitfall 3: Fire Rate Not Frame-Rate Independent
**What goes wrong:** Weapon fires faster on high-FPS machines, slower on low-FPS machines.
**Why it happens:** Using frame counter instead of elapsed time. "Fire every 10 frames" depends on frame rate.
**How to avoid:** Use GameTime.ElapsedGameTime.TotalSeconds for cooldown timers (see Pattern 2). Never use frame counting for rate limiting.
**Warning signs:** Fire rate changes when toggling VSync or resolution, inconsistent on different hardware.

### Pitfall 4: Aim Direction Misalignment in Third-Person
**What goes wrong:** Projectiles don't go where crosshair points, especially when player faces different direction.
**Why it happens:** Using player.Transform.Forward instead of camera.Forward for projectile direction.
**How to avoid:** Always use camera.Forward for aim direction (see Pattern 3). Crosshair represents camera aim, not player facing.
**Warning signs:** Shots veer left/right when player isn't facing camera direction, user can't hit targets under crosshair.

### Pitfall 5: Forgetting to Offset BoundingSphere to World Position
**What goes wrong:** Collision detection always tests at origin (0,0,0), never detects actual hits.
**Why it happens:** BoundingSphere from Model.Mesh.BoundingSphere is in model-local space (relative to model origin).
**How to avoid:** Always offset sphere center by entity's world position: `sphere.Center += entity.Position` before Intersects() check.
**Warning signs:** No collisions detected at all, even with overlapping visuals. Official MonoGame example shows this pattern.

**Source:** MonoGame collision detection documentation explicitly shows offsetting sphere center in collision check examples.

### Pitfall 6: Mouse Button Edge Detection for Auto-Fire
**What goes wrong:** Using IsLeftMousePressed() (edge detection) only fires one shot per click, not continuous.
**Why it happens:** IsLeftMousePressed() returns true only on button-down frame. Auto-fire needs held state.
**How to avoid:** Use InputManager.IsLeftMouseHeld() for continuous fire. Check fire rate timer, not button edges.
**Warning signs:** Hold mouse button but only one shot fires. User must spam-click for automatic fire.

**Note:** Project's existing InputManager already has IsLeftMouseHeld() from Phase 2. Use this, not IsLeftMousePressed().

### Pitfall 7: Distance-Based Lifetime Without Travel Tracking
**What goes wrong:** Projectile lifetime based on time instead of distance traveled. Speed changes break lifetime.
**Why it happens:** Using fixed timer (e.g., 2 seconds) regardless of projectile speed.
**How to avoid:** CONTEXT.md specifies "distance-based (50-100 units)" lifetime. Track `distanceTraveled += speed * deltaTime`, deactivate when distance exceeds max.
**Warning signs:** Fast projectiles disappear too soon, slow projectiles travel too far. Inconsistent range at different speeds.

## Code Examples

### Creating a Projectile Class
```csharp
// Based on MonoGame community patterns and official collision documentation
using Microsoft.Xna.Framework;
using Berzerk.Source.Core;

namespace Berzerk.Source.Combat
{
    public class Projectile
    {
        public Transform Transform { get; private set; }
        public bool IsActive { get; private set; }
        public float Speed { get; private set; }
        public float Radius { get; private set; } = 0.2f; // Collision radius
        public float MaxDistance { get; private set; } = 75f; // 50-100 per CONTEXT.md

        private Vector3 _velocity;
        private float _distanceTraveled;

        public Projectile()
        {
            Transform = new Transform();
            IsActive = false;
        }

        public void Activate(Vector3 position, Vector3 direction, float speed)
        {
            Transform.Position = position;
            _velocity = direction * speed; // direction should be normalized
            Speed = speed;
            _distanceTraveled = 0f;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsActive) return;

            // Move projectile
            Vector3 movement = _velocity * deltaTime;
            Transform.Position += movement;
            _distanceTraveled += movement.Length();

            // Deactivate after max distance (per CONTEXT.md decision)
            if (_distanceTraveled >= MaxDistance)
            {
                Deactivate();
            }
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public BoundingSphere GetBoundingSphere()
        {
            return new BoundingSphere(Transform.Position, Radius);
        }

        public void OnHitWall()
        {
            // TODO Phase 3: Spawn impact effect
            Deactivate();
        }

        public void OnHitTarget(object target)
        {
            // TODO Phase 3: Spawn impact effect, notify target
            Deactivate();
        }
    }
}
```

### Frame-Rate Independent Fire Rate Timer
```csharp
// Based on MonoGame GameTime patterns
using Microsoft.Xna.Framework;

public class WeaponSystem
{
    private float _fireRate = 6.5f; // 6.5 shots/sec (within 5-8 range per CONTEXT.md)
    private float _cooldownTimer = 0f;

    public void Update(GameTime gameTime, bool wantsToFire)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Cooldown decreases over time
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
        }

        // Fire if ready and button held
        if (wantsToFire && _cooldownTimer <= 0f)
        {
            Fire();
            _cooldownTimer = 1.0f / _fireRate; // Reset cooldown (0.154 sec)
        }
    }

    private void Fire()
    {
        // Spawn projectile logic
    }
}
```

### Camera-Forward Aiming Integration
```csharp
// Integrates with existing Phase 2 systems
using Berzerk.Source.Input;
using Berzerk.Source.Graphics;

public class PlayerShooting
{
    private ProjectileManager _projectileManager;
    private ThirdPersonCamera _camera;
    private Transform _playerTransform;
    private WeaponSystem _weapon;

    public void Update(GameTime gameTime, InputManager input)
    {
        bool isFiring = input.IsLeftMouseHeld(); // Use held, not pressed
        _weapon.Update(gameTime, isFiring);
    }

    private void SpawnProjectile()
    {
        // Spawn offset from player center (shoulder height)
        Vector3 spawnOffset = Vector3.Up * 1.5f;
        Vector3 spawnPosition = _playerTransform.Position + spawnOffset;

        // Aim direction from camera forward (CRITICAL for crosshair accuracy)
        Vector3 aimDirection = Vector3.Normalize(_camera.Forward);

        float projectileSpeed = 50f; // 40-60 range per CONTEXT.md
        _projectileManager.Spawn(spawnPosition, aimDirection, projectileSpeed);
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual GC.Collect() calls | Object pooling patterns | ~2010s (mobile era) | Eliminated manual GC management, smoother frame rates |
| Fixed frame rate (30/60fps locked) | Delta time for all movement | XNA → MonoGame transition | Works across variable refresh rates (120Hz+ monitors) |
| Per-vertex collision checks | BoundingSphere/BoundingBox | XNA 2.0+ (2007) | 10-100x faster collision detection |
| Custom particle systems | MonoGame.Extended particles | 2016-present | Standardized particle API, visual editor coming |
| Raycasting for all projectiles | Sphere collision for non-hitscan | Established pattern | Raycasts for instant hit (sniper), spheres for projectiles |

**Deprecated/outdated:**
- **XNA's built-in Input.GetState() edge detection helpers:** MonoGame requires manual previous/current state tracking (already implemented in project's InputManager per Phase 2).
- **Fixed-function pipeline effects:** MonoGame still supports BasicEffect but modern games use custom shaders. For this prototype, BasicEffect is appropriate (Phase 1 decision).

## Open Questions

### 1. Sphere Geometry Source
**What we know:** MonoGame-Samples repository has SpherePrimitive class, can generate sphere mesh.
**What's unclear:** Should we vendor this code into project, or implement minimal sphere generation? Impact effect also needs geometry.
**Recommendation:** Start with simplest approach - even a cube with emissive color reads as "glowing projectile" at arcade speeds. Defer proper sphere geometry unless visuals fail playtesting. CONTEXT.md says "simple geometric shape" - don't over-engineer.

### 2. Character Rotation During Aiming
**What we know:** CONTEXT.md marks this as "Claude's discretion" - either rotate character to face aim direction, or maintain movement-based facing from Phase 2.
**What's unclear:** Phase 2 implemented "player rotates to face movement direction" (tank controls). Does shooting override this, or do they shoot sideways?
**Recommendation:** Test both during implementation. Likely answer: keep Phase 2 behavior (face movement), allow side/back shooting. True to arcade roots (Robotron, Smash TV style). If it feels wrong, add "aim rotation" but this risks fighting tank control feel.

### 3. Impact Effect Complexity
**What we know:** CONTEXT.md specifies "visual effect (spark/flash/mark)" at impact points.
**What's unclear:** How elaborate? Simple billboard sprite fade-out? Particle burst? Decal on wall?
**Recommendation:** Start minimal - spawn glowing sphere at impact point, scale down + fade out over 0.2 seconds. Validates "impact feedback" requirement without complex particle systems or decal rendering. Can enhance in polish phase if needed.

### 4. Ammo Pickup Collision
**What we know:** Phase 2 already has player-wall collision using BoundingBox list. CONTEXT.md says pickups spawn at target destruction location.
**What's unclear:** Pickup collision detection approach - sphere vs box? Trigger radius vs overlap? Auto-collect vs press-to-collect?
**Recommendation:** BoundingSphere collision, auto-collect on overlap. Consistent with projectile collision method, simpler UX (no button press needed). Generous radius (2-3 units) for forgiving arcade feel.

## Sources

### Primary (HIGH confidence)
- MonoGame Official Documentation - Collision Detection Overview: https://docs.monogame.net/articles/getting_to_know/howto/HowTo_CollisionDetectionOverview.html
- MonoGame Official Documentation - BasicEffect API: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.BasicEffect.html
- MonoGame Official Documentation - GameTime Class: https://docs.monogame.net/api/Microsoft.Xna.Framework.GameTime.html
- Existing project code - InputManager.cs, Transform.cs, BerzerkGame.cs (Phase 2 implementations)

### Secondary (MEDIUM confidence)
- [MonoGame collision detection with BoundingSphere](https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Test_for_Collisions.html) - Official tutorial with code examples
- [RB Whitaker's MonoGame tutorials](http://rbwhitaker.wikidot.com/) - Community-trusted XNA/MonoGame patterns
- [MonoGame-Samples SpherePrimitive](https://github.com/CartBlanche/MonoGame-Samples/blob/master/PerformanceMeasuring/Primitives/SpherePrimitive.cs) - Geometry generation reference
- [Game Programming Patterns - Object Pool](https://gameprogrammingpatterns.com/object-pool.html) - Design pattern documentation

### Tertiary (LOW confidence - require validation)
- [Third-person crosshair aiming discussions](https://gamedev.net/forums/topic/168589-crosshair-with-third-person-camera/) - GameDev.net community patterns
- [MonoGame community object pooling discussion](https://community.monogame.net/t/garbage-free-performant-object-pooling/17486) - Performance insights (2020)
- [Projectile tunneling problem overview](https://school.gdquest.com/glossary/tunneling) - GDQuest glossary entry
- [MonoGame.Extended particle system](https://www.monogameextended.net/docs/features/particles/quick_start/) - Optional enhancement, not required for Phase 3

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - MonoGame 3.8.4.1 built-in features verified via official docs
- Architecture: HIGH - Manager pattern, object pooling, delta time are established MonoGame community practices
- Pitfalls: MEDIUM-HIGH - Tunneling, GC pressure, aim direction are known issues confirmed by community discussions; specific solutions verified against official docs
- Code examples: HIGH - Based on official MonoGame documentation and existing project patterns from Phase 2

**Research date:** 2026-02-02
**Valid until:** ~60 days (March 2026) - MonoGame 3.8.x is stable, patterns are mature. Next major version (3.9/4.0) not announced. Core collision/rendering APIs unlikely to change.
