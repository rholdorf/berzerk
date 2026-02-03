# Phase 5: Enemy AI & Combat - Research

**Researched:** 2026-02-03
**Domain:** Game AI, pathfinding, enemy behavior systems in MonoGame 3D
**Confidence:** HIGH

## Summary

Phase 5 implements robot enemies with detection, pursuit, melee combat, health systems, and destruction mechanics. Based on user decisions, robots spawn at safe distances, move slower than the player (70-80% speed), attack for 10 HP damage, take 2-3 hits to destroy, and drop pickups 30-40% of the time.

The research reveals that for Berzerk-style arcade gameplay in a single-room environment with minimal obstacles, **simple direct movement toward the player is more appropriate than complex pathfinding**. The original Berzerk used simple tracking with wall avoidance, which created emergent challenge through simplicity. For MonoGame 3D, the established patterns are: finite state machines for AI behavior, BoundingSphere collision detection, event-driven health systems with C# events/delegates, and object pooling for enemy/effect management.

The existing codebase already demonstrates these patterns: HealthSystem uses events (OnDamageTaken, OnDeath), TargetManager uses object pooling (Queue<AmmoPickup>), collision uses BoundingSphere.Intersects(), and AnimatedModel supports Mixamo FBX with skeletal animation.

**Primary recommendation:** Build enemy system mirroring existing patterns—EnemyController (like PlayerController), EnemyManager (like TargetManager), simple FSM for AI states, direct movement with collision avoidance, event-driven health, pooled explosion effects.

## Standard Stack

### Core Components (MonoGame Built-in)

| Component | Purpose | Why Standard |
|-----------|---------|--------------|
| BoundingSphere | 3D collision detection | Built into MonoGame, fast sphere-sphere tests, rotation-independent |
| Vector3 math | Movement, direction calculations | MonoGame framework standard, used throughout existing code |
| Matrix transforms | World space positioning | Existing Transform class pattern established in codebase |
| C# events/delegates | Health system, AI triggers | Established in HealthSystem (OnDamageTaken, OnDeath) |
| Queue<T> pooling | Object pooling | Already used for AmmoPickup pooling (O(1) Push/Pop) |

### Animation & Models

| Component | Version | Purpose | Notes |
|-----------|---------|---------|-------|
| Mixamo | Current (2026) | Robot character models + animations | Free, rigged, FBX 2013 format, already used for player |
| AnimatedModel | Custom | Skeletal animation playback | Existing system extracts keyframes from Model.Tag |
| BasicEffect | MonoGame | 3D rendering | Current player rendering approach |

### Not Needed

| Component | Why Not Used |
|-----------|--------------|
| A* pathfinding libraries | Single room, minimal obstacles—direct movement sufficient |
| Navigation mesh | No complex level geometry in Phase 5 (Room System is Phase 6) |
| MonoGame.Extended particles | Simple explosion effect via expanding sphere is adequate |
| External FSM libraries | Simple 3-4 state FSM can be implemented directly |

## Architecture Patterns

### Recommended Project Structure

```
Berzerk/Source/
├── Enemies/
│   ├── EnemyController.cs      # Individual enemy behavior, movement, combat
│   ├── EnemyHealth.cs          # Enemy-specific health system with events
│   ├── EnemyManager.cs         # Spawning, pooling, manager pattern
│   └── EnemyState.cs           # FSM states (Idle, Chase, Attack, Dying)
├── Combat/
│   ├── HealthPickup.cs         # Like AmmoPickup pattern
│   └── ExplosionEffect.cs      # Pooled visual effect
└── (existing structure maintained)
```

### Pattern 1: Finite State Machine (Enum-based)

**What:** Simple state machine using enum and switch statements for AI behavior.
**When to use:** AI with 3-5 distinct states that don't share much behavior.

**Example:**
```csharp
// Source: Game Programming Patterns (State chapter)
public enum EnemyState { Idle, Chase, Attack, Dying }

public class EnemyController
{
    private EnemyState _currentState = EnemyState.Idle;

    public void Update(GameTime gameTime, Vector3 playerPos)
    {
        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState(playerPos);
                break;
            case EnemyState.Chase:
                UpdateChaseState(gameTime, playerPos);
                break;
            case EnemyState.Attack:
                UpdateAttackState(gameTime, playerPos);
                break;
            case EnemyState.Dying:
                UpdateDyingState(gameTime);
                break;
        }
    }

    private void TransitionToState(EnemyState newState)
    {
        OnStateExit(_currentState);
        _currentState = newState;
        OnStateEnter(newState);
    }
}
```

### Pattern 2: Event-Driven Health System

**What:** Health system using C# events for decoupled damage/death reactions.
**When to use:** When multiple systems need to respond to health changes.

**Example (from existing HealthSystem):**
```csharp
// Source: Existing Berzerk.Source.Player.HealthSystem
public class EnemyHealth
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; } = 30; // 2-3 hits at 15 HP per laser
    public bool IsDead => CurrentHealth <= 0;

    public event System.Action? OnDamageTaken;
    public event System.Action? OnDeath;

    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        CurrentHealth = System.Math.Max(0, CurrentHealth - amount);
        OnDamageTaken?.Invoke();
        if (IsDead) OnDeath?.Invoke();
    }
}
```

### Pattern 3: Manager + Object Pooling

**What:** Manager class handles spawning, updating, pooling of enemies and effects.
**When to use:** Multiple instances of same type, frequent spawn/destroy.

**Example (from existing TargetManager pattern):**
```csharp
// Source: Existing Berzerk.Source.Combat.TargetManager
public class EnemyManager
{
    private List<EnemyController> _enemies = new();
    private Queue<EnemyController> _enemyPool = new();
    private Queue<ExplosionEffect> _explosionPool = new();

    public void Initialize(int poolSize = 20)
    {
        for (int i = 0; i < poolSize; i++)
        {
            _enemyPool.Enqueue(new EnemyController());
            _explosionPool.Enqueue(new ExplosionEffect());
        }
    }

    public void SpawnEnemy(Vector3 position)
    {
        EnemyController enemy;
        if (_enemyPool.Count > 0)
        {
            enemy = _enemyPool.Dequeue();
            enemy.Activate(position);
        }
        else
        {
            enemy = new EnemyController();
            enemy.Activate(position);
        }
        _enemies.Add(enemy);
    }
}
```

### Pattern 4: Direct Movement with Collision Avoidance

**What:** Simple seek behavior that moves directly toward target, backing away from walls.
**When to use:** Open arena, minimal obstacles, arcade-style pursuit.

**Example:**
```csharp
// Inspired by: Retrogamedeconstructionzone.com Berzerk AI analysis
private Vector3 CalculateMovementDirection(Vector3 playerPos, List<BoundingBox> walls)
{
    // Basic direction toward player
    Vector3 toPlayer = playerPos - Transform.Position;
    toPlayer.Y = 0; // Keep movement horizontal

    if (toPlayer.LengthSquared() < 0.01f) return Vector3.Zero;

    Vector3 desiredDir = Vector3.Normalize(toPlayer);

    // Simple wall avoidance: check if movement would get too close to wall
    Vector3 nextPos = Transform.Position + desiredDir * MoveSpeed * 0.1f;
    BoundingSphere predictedSphere = new BoundingSphere(nextPos, Radius);

    foreach (var wall in walls)
    {
        if (predictedSphere.Intersects(wall))
        {
            // Back away from wall
            return -desiredDir * 0.5f;
        }
    }

    return desiredDir;
}
```

### Pattern 5: Knockback via Velocity Modification

**What:** Apply impulse velocity away from damage source for knockback feel.
**When to use:** Light knockback on hit without full physics system.

**Example:**
```csharp
// Based on Unity knockback patterns (adapted for MonoGame)
public void ApplyKnockback(Vector3 damageSource, float knockbackForce)
{
    Vector3 knockbackDir = Transform.Position - damageSource;
    knockbackDir.Y = 0; // Keep horizontal
    if (knockbackDir.LengthSquared() > 0)
    {
        knockbackDir = Vector3.Normalize(knockbackDir);
        _velocity += knockbackDir * knockbackForce;
    }
}

// In PlayerController.Update(), apply knockback velocity then decay
public void Update(GameTime gameTime)
{
    if (!IsEnabled) return;
    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

    // Apply knockback velocity (decays over time)
    if (_knockbackVelocity.LengthSquared() > 0.01f)
    {
        Transform.Position += _knockbackVelocity * deltaTime;
        _knockbackVelocity *= (1f - Deceleration * deltaTime); // Exponential decay
    }

    // ... rest of normal input/movement
}
```

### Anti-Patterns to Avoid

- **Complex pathfinding for simple space:** A* is overkill for single-room arena with minimal obstacles. Adds complexity, CPU cost, makes enemies too predictable.
- **Perfect tracking:** Original Berzerk's charm came from imperfect AI that could be outsmarted. Don't make robots perfectly optimal.
- **Frame-dependent movement:** Always multiply velocity by deltaTime. Existing code follows this pattern.
- **Collision after movement:** Check collisions BEFORE applying movement, not after. Prevents tunneling through walls.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Animation blending | Custom interpolation system | Separate FBX per animation | Established in codebase, Mixamo limitation, simpler to manage |
| Object pooling container | Custom pool data structure | Queue<T> (C# built-in) | O(1) Push/Pop, already used in TargetManager |
| Collision detection | Ray-triangle intersections | BoundingSphere.Intersects | Built-in MonoGame, fast, proven pattern in existing code |
| Random spawning | Complex spawn algorithms | Try-retry with distance check | Good enough for single room, prevents infinite loops |
| State management | Custom FSM framework | Enum + switch | Sufficient for 3-4 states, easier to debug |

**Key insight:** For arcade-style single-room combat, simple solutions create better gameplay than complex systems. Berzerk's original AI charm came from simplicity creating emergent challenge, not sophisticated algorithms.

## Common Pitfalls

### Pitfall 1: Infinite Spawn Loops

**What goes wrong:** When spawning at "safe distance from player," if room is small or many enemies exist, might never find valid position.

**Why it happens:** While loop checks distance to player but not to other enemies or walls.

**How to avoid:**
- Implement retry limit (max 20 attempts)
- Fall back to spawning at predefined safe zones if random placement fails
- Check distance to ALL entities (player, other enemies, walls)

**Warning signs:** Game freezes during enemy spawn, inconsistent spawn counts

**Example:**
```csharp
// Source: GameDev.net forums, enemy spawning algorithm discussions
private Vector3? TryFindSpawnPosition(Vector3 playerPos, float minDistance, int maxAttempts = 20)
{
    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        // Random position within room bounds
        Vector3 candidate = new Vector3(
            Random.Next(-10, 10),
            0.5f,
            Random.Next(-10, 10)
        );

        // Check distance to player
        if (Vector3.Distance(candidate, playerPos) < minDistance)
            continue;

        // Check distance to existing enemies
        bool tooCloseToEnemy = _enemies.Any(e =>
            Vector3.Distance(e.Position, candidate) < 3f);
        if (tooCloseToEnemy) continue;

        // Check wall collision
        BoundingSphere testSphere = new BoundingSphere(candidate, 1f);
        if (_walls.Any(wall => testSphere.Intersects(wall)))
            continue;

        return candidate; // Valid position found
    }

    return null; // Failed to find position
}
```

### Pitfall 2: BoundingSphere False Positives

**What goes wrong:** Long narrow robots have large empty space in bounding sphere, causing hits when visually no contact.

**Why it happens:** BoundingSphere bounds the entire model, creating sphere around longest dimension.

**How to avoid:**
- Use smaller radius than model bounds (like TestTarget: 0.7f for 1.0f cube)
- Consider multiple smaller spheres for elongated models
- Accept some inaccuracy for arcade feel (player-favoring hitboxes)

**Warning signs:** Player complains about "unfair hits," collisions don't match visuals

**From MonoGame docs:** "Unless the object being approximated is sphere shaped, the bounding sphere will have some empty space. One commonly used solution is to approximate the object with multiple spheres rather than one."

### Pitfall 3: State Transition Thrashing

**What goes wrong:** Enemy rapidly switches between Chase and Attack states, causing jittery animation.

**Why it happens:** State transition uses exact distance threshold (e.g., attackRange = 2.0f), so at distance 2.01 chases, at 1.99 attacks, causing rapid switching.

**How to avoid:** Use hysteresis—different thresholds for entering vs exiting states.

**Warning signs:** Enemy animation flickers between walk/attack, movement stutters

**Example:**
```csharp
// Use different thresholds for enter vs exit
private const float ATTACK_RANGE_ENTER = 2.0f;
private const float ATTACK_RANGE_EXIT = 3.0f; // Larger exit threshold

private void UpdateChaseState(Vector3 playerPos)
{
    float distance = Vector3.Distance(Transform.Position, playerPos);

    // Only enter attack if within tight range
    if (distance <= ATTACK_RANGE_ENTER)
    {
        TransitionToState(EnemyState.Attack);
    }
}

private void UpdateAttackState(Vector3 playerPos)
{
    float distance = Vector3.Distance(Transform.Position, playerPos);

    // Only exit attack if beyond wider range
    if (distance > ATTACK_RANGE_EXIT)
    {
        TransitionToState(EnemyState.Chase);
    }
}
```

### Pitfall 4: Explosion Effect Memory Leaks

**What goes wrong:** Creating new explosion effects on each enemy death causes GC pressure and eventual slowdown.

**Why it happens:** Not using object pooling for short-lived visual effects.

**How to avoid:** Pool explosion effects like existing ImpactEffect pooling.

**Warning signs:** Game slows down after many enemy deaths, GC spikes in profiler

**Example (from existing pattern):**
```csharp
// Source: Existing ProjectileManager impact effect pooling
private Queue<ExplosionEffect> _explosionPool = new();
private List<ExplosionEffect> _activeExplosions = new();

public void Initialize(int poolSize = 20)
{
    for (int i = 0; i < poolSize; i++)
    {
        _explosionPool.Enqueue(new ExplosionEffect());
    }
}

public void SpawnExplosion(Vector3 position)
{
    ExplosionEffect effect;
    if (_explosionPool.Count > 0)
    {
        effect = _explosionPool.Dequeue();
        effect.Activate(position);
    }
    else
    {
        effect = new ExplosionEffect();
        effect.Activate(position);
    }
    _activeExplosions.Add(effect);
}
```

### Pitfall 5: Animation Doesn't Match State

**What goes wrong:** Enemy plays walk animation while in Attack state, breaking visual feedback.

**Why it happens:** Animation and state logic separated, no synchronization.

**How to avoid:** Call PlayAnimation() in OnStateEnter(), ensure animation names match Mixamo exports.

**Warning signs:** Robot sliding without walking, attacking without attack animation

**Example:**
```csharp
private void OnStateEnter(EnemyState newState)
{
    switch (newState)
    {
        case EnemyState.Idle:
            _animatedModel.PlayAnimation("idle");
            break;
        case EnemyState.Chase:
            _animatedModel.PlayAnimation("walk");
            break;
        case EnemyState.Attack:
            _animatedModel.PlayAnimation("attack");
            _attackTimer = 0f; // Reset attack cooldown
            break;
        case EnemyState.Dying:
            _animatedModel.PlayAnimation("death");
            _deathTimer = 0f;
            break;
    }
}
```

## Code Examples

Verified patterns from official sources and existing codebase:

### Enemy Spawn with Safe Distance

```csharp
// Source: GameDev.net enemy spawning discussions + user CONTEXT decisions
public void SpawnWave(int enemyCount, Vector3 playerPos, float minDistance)
{
    Vector3[] predefinedSafeZones = new[]
    {
        new Vector3(-8, 0.5f, -8),
        new Vector3(8, 0.5f, -8),
        new Vector3(-8, 0.5f, 8),
        new Vector3(8, 0.5f, 8)
    };

    for (int i = 0; i < enemyCount; i++)
    {
        Vector3? spawnPos = TryFindSpawnPosition(playerPos, minDistance);

        // Fallback to predefined safe zones if random placement fails
        if (!spawnPos.HasValue && i < predefinedSafeZones.Length)
        {
            spawnPos = predefinedSafeZones[i];
        }

        if (spawnPos.HasValue)
        {
            SpawnEnemy(spawnPos.Value);
        }
    }
}
```

### Melee Attack Detection

```csharp
// Source: MonoGame collision detection docs + user CONTEXT (10 HP damage)
private float _attackCooldown = 0f;
private const float ATTACK_RATE = 1.0f; // 1 second between attacks
private const float ATTACK_RANGE = 2.5f; // Units
private const int ATTACK_DAMAGE = 10; // Per user CONTEXT decisions

private void UpdateAttackState(GameTime gameTime, Vector3 playerPos, HealthSystem playerHealth)
{
    float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
    _attackCooldown -= deltaTime;

    float distance = Vector3.Distance(Transform.Position, playerPos);

    // Check if in range and cooldown ready
    if (distance <= ATTACK_RANGE && _attackCooldown <= 0f)
    {
        // Deal damage
        playerHealth.TakeDamage(ATTACK_DAMAGE);

        // Apply knockback to player
        Vector3 knockbackDir = playerPos - Transform.Position;
        knockbackDir.Y = 0;
        if (knockbackDir.LengthSquared() > 0)
        {
            _playerController.ApplyKnockback(Vector3.Normalize(knockbackDir), 8f);
        }

        _attackCooldown = ATTACK_RATE;
        Console.WriteLine($"Enemy attacked player for {ATTACK_DAMAGE} damage!");
    }

    // Exit attack state if player escapes
    if (distance > ATTACK_RANGE + 1f) // Hysteresis
    {
        TransitionToState(EnemyState.Chase);
    }
}
```

### Drop Chance System

```csharp
// Source: User CONTEXT decisions (30-40% drop rate)
private Random _random = new Random();

private void OnEnemyDeath(EnemyController enemy)
{
    // Spawn explosion effect
    SpawnExplosion(enemy.Transform.Position);

    // Random drop with 35% chance (middle of 30-40% range)
    float dropRoll = (float)_random.NextDouble();

    if (dropRoll < 0.35f)
    {
        // 50/50 split between ammo and health
        if (_random.NextDouble() < 0.5)
        {
            SpawnAmmoPickup(enemy.Transform.Position);
        }
        else
        {
            SpawnHealthPickup(enemy.Transform.Position);
        }
    }
}
```

### Progressive Difficulty Scaling

```csharp
// Source: Medium articles on wave-based difficulty scaling + user CONTEXT
private int _currentWave = 1;

public void StartNextWave(Vector3 playerPos)
{
    // Start with 2-3 robots, increase gradually
    int enemyCount = 2 + _currentWave;
    float enemySpeed = 3.5f + (_currentWave * 0.2f); // Gradually faster

    // Cap to prevent overwhelming difficulty
    enemyCount = Math.Min(enemyCount, 10);
    enemySpeed = Math.Min(enemySpeed, 5f); // Never faster than player

    // Spawn distance increases slightly with wave (harder to avoid)
    float minSpawnDistance = 10f + (_currentWave * 0.5f);

    SpawnWave(enemyCount, playerPos, minSpawnDistance);

    _currentWave++;
    Console.WriteLine($"Wave {_currentWave} started: {enemyCount} enemies");
}
```

### Explosion Effect (Expanding Sphere)

```csharp
// Source: Existing ProjectileRenderer sphere mesh pattern + user CONTEXT
public class ExplosionEffect
{
    public Vector3 Position { get; private set; }
    public bool IsActive { get; private set; }

    private float _timer = 0f;
    private const float DURATION = 0.3f; // 0.3 seconds total
    private const float MAX_RADIUS = 2f;

    public void Activate(Vector3 position)
    {
        Position = position;
        IsActive = true;
        _timer = 0f;
    }

    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        _timer += deltaTime;
        if (_timer >= DURATION)
        {
            IsActive = false;
        }
    }

    public float GetRadius()
    {
        // Expand over first half, then shrink
        float progress = _timer / DURATION;
        if (progress < 0.5f)
        {
            return MAX_RADIUS * (progress * 2f); // 0 to 1 over first half
        }
        else
        {
            return MAX_RADIUS * (2f - progress * 2f); // 1 to 0 over second half
        }
    }

    public float GetAlpha()
    {
        // Fade out linearly
        return 1f - (_timer / DURATION);
    }

    public Color GetColor()
    {
        // Orange explosion color from user CONTEXT for impact effects
        return new Color(1.0f, 0.8f, 0.3f) * GetAlpha();
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Complex A* pathfinding for all AI | Direct movement for simple spaces, A* for complex levels | ~2010s | Better performance, more predictable behavior for arcade games |
| Boolean flags for AI states | Enum-based FSM or state pattern | ~2000s | Eliminates invalid state combinations, cleaner code |
| Instantiate/Destroy for effects | Object pooling | ~2005+ | Reduces GC pressure, consistent performance |
| Frame-dependent movement | Delta-time multiplied movement | ~2000s XNA era | Frame-rate independent, consistent across hardware |
| Polling for events | C# event/delegate pattern | Modern C# | Decoupled systems, reactive design |

**Deprecated/outdated:**
- **XNA Game Studio collision helpers:** Use MonoGame built-in BoundingSphere/BoundingBox instead
- **Separate state class per state:** For simple 3-4 state AI, enum + switch is simpler and performs better than polymorphic state classes
- **Behavior trees for simple AI:** Overkill for chase/attack pattern; use simple FSM

## Open Questions

### Question 1: Room boundaries not yet defined

- **What we know:** Current code has test walls (ThirdPersonCamera.CreateTestWalls()), Phase 6 is "Room System & Progression"
- **What's unclear:** Exact room dimensions for spawn distance calculations
- **Recommendation:** Use current test wall bounds (-10 to 10 on X/Z) for Phase 5, adapt in Phase 6

### Question 2: Health pickup implementation

- **What we know:** AmmoPickup exists with bobbing animation, auto-collect radius 2f
- **What's unclear:** Health pickup visual distinction (color/shape)
- **Recommendation:** Mirror AmmoPickup (yellow) but use green color for health (universal health indicator)

### Question 3: Multiple Mixamo models vs single model with animations

- **What we know:** Current code loads separate FBX per animation (idle.fbx, walk.fbx, run.fbx)
- **What's unclear:** Whether robot enemy uses same approach or single model
- **Recommendation:** Follow existing pattern—separate FBX per animation, switch AnimatedModel references

## Sources

### Primary (HIGH confidence)

- [MonoGame Official Docs - BoundingSphere Collision Testing](https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Test_for_Collisions.html) - Collision detection implementation
- [Game Programming Patterns - State Pattern](https://gameprogrammingpatterns.com/state.html) - FSM architecture and best practices
- Existing Berzerk codebase - HealthSystem, TargetManager, ProjectileManager patterns verified
- [Retrogamedeconstructionzone.com - Decoding Berzerk AI](https://www.retrogamedeconstructionzone.com/2020/03/decoding-berzerk-ai.html) - Original Berzerk AI behavior analysis

### Secondary (MEDIUM confidence)

- [Medium - Event-Driven Architecture in Game Development](https://medium.com/@ahmadrezakml/event-driven-architecture-in-game-development-unity-gamemaker-c76915361ff0) - Health system event patterns
- [Medium - Mastering Enemy Waves](https://medium.com/@victormct/unleashing-chaos-mastering-enemy-waves-9be16f92e673) - Wave-based difficulty scaling
- [GameDev.net - Enemy Spawning Algorithm](https://gamedev.net/forums/topic/564630-enemy-spawning-algorithm/) - Safe distance spawn patterns
- [Medium - Object Pooling Guide](https://medium.com/@mikaznavodya/object-pooling-in-game-development-the-complete-guide-8c52bef04597) - Pooling best practices
- [Tono Game Consultants - AI Movement Beyond A*](https://tonogameconsultants.com/game-ai-movement/) - When to use simple vs complex pathfinding

### Tertiary (LOW confidence)

- Unity-specific knockback tutorials (adapted patterns for MonoGame Vector3 math)
- Mixamo 2026 search results (confirms service still active, robot models available)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - MonoGame built-ins verified in docs, existing codebase demonstrates patterns
- Architecture: HIGH - State pattern verified from Game Programming Patterns, existing code structure established
- Pitfalls: HIGH - Based on documented MonoGame limitations and common FSM issues

**Research date:** 2026-02-03
**Valid until:** ~60 days (MonoGame stable, patterns mature, user decisions locked)
