# Phase 6: Room System & Progression - Research

**Researched:** 2026-02-04
**Domain:** 3D room structure, collision detection, state machines, level progression
**Confidence:** HIGH

## Summary

Phase 6 implements a room-based progression system inspired by the classic Berzerk arcade game, where players navigate through connected rooms with door-based transitions. The research confirms that MonoGame's BoundingBox collision system (already used in phases 2-5) is the standard approach for 3D room geometry and door triggers. Room transitions follow a clear state machine pattern (doors closed → all enemies defeated → doors open → player enters door → new room loads). The codebase's existing patterns—BoundingBox collision, GameState enum for state flow, Manager/Controller architecture, and object pooling—all align well with the requirements.

The classic Berzerk arcade used a simple but effective design: 65,536 rooms in a grid (256×256), each room had 4 doors (N/S/E/W), doors opened when all robots were destroyed, and the player could exit to adjacent rooms. For v1, we'll implement a single handcrafted room with maze-like wall layout, establish the door state machine pattern, and create the foundation for room transitions—procedural generation is deferred to v2.

**Primary recommendation:** Build a Room class that encapsulates wall BoundingBoxes, door positions/states, and enemy spawn points. Use a DoorState enum (Closed/Opening/Open) for each cardinal direction. Implement room transitions by detecting player collision with open door triggers, then resetting the room state with fresh enemy spawns. This mirrors MonoGame's Scene pattern but simplified for single-room v1.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame | 3.8.4.1 | Framework foundation | Already established in phases 1-5 |
| Microsoft.Xna.Framework.BoundingBox | Built-in | Wall/door collision | Official MonoGame 3D collision primitive, already used for camera and projectile collision |
| System.Collections.Generic.List | Built-in | Room geometry storage | Standard C# collection, matches existing wall list pattern |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Enum | Built-in | State machine implementation | Door states (Closed/Opening/Open), room transition states |
| Random (deterministic seed) | Built-in | Room generation consistency | Classic Berzerk used room coordinates as RNG seed for consistent layouts |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| BoundingBox | Custom mesh collision | BoundingBox is simpler, faster, and sufficient for axis-aligned walls |
| Enum state machine | Class-based State pattern | Enum is lighter for simple states without per-state data (doors have no per-state logic beyond transitions) |
| Room class | Scene system (MonoGame.Extended) | Room class is simpler for v1 (single room), Scene pattern better for multi-room loading |

**Installation:**
```bash
# No additional packages needed - all built into MonoGame 3.8.4.1
# Future: MonoGame.Extended for tilemap system (deferred to v2)
```

## Architecture Patterns

### Recommended Project Structure
```
Source/
├── Rooms/
│   ├── Room.cs              # Room data: walls, doors, spawn points
│   ├── RoomManager.cs       # Room lifecycle: load, reset, transition
│   ├── Door.cs              # Door state, position, collision trigger
│   └── DoorState.cs         # Enum: Closed, Opening, Open
├── Enemies/
│   └── EnemyManager.cs      # Already exists - wire to Room events
└── BerzerkGame.cs           # Wire room cleared event to door state
```

### Pattern 1: Room as Data Container
**What:** Room class holds static geometry (wall BoundingBoxes), door positions/states, and spawn metadata. No Update/Draw—purely data.

**When to use:** When room is a passive container, not an active system. Room logic lives in RoomManager.

**Example:**
```csharp
// Source: MonoGame community patterns + classic Berzerk design
public class Room
{
    public List<BoundingBox> Walls { get; private set; }
    public Dictionary<Direction, Door> Doors { get; private set; }
    public List<Vector3> EnemySpawnPoints { get; private set; }

    // Constructor builds handcrafted maze layout
    public Room()
    {
        Walls = CreateMazeWalls();
        Doors = CreateDoors();
        EnemySpawnPoints = CreateSpawnPoints();
    }

    private List<BoundingBox> CreateMazeWalls()
    {
        // Handcrafted layout (like ThirdPersonCamera.CreateTestWalls)
        return new List<BoundingBox>
        {
            new BoundingBox(new Vector3(-15, 0, -15), new Vector3(-14, 5, 15)),
            // ... more walls forming maze layout
        };
    }
}
```

### Pattern 2: Door State Machine with Enum
**What:** DoorState enum (Closed/Opening/Open) with transition logic in Door class. Simple state machine without per-state classes.

**When to use:** When states have no unique data or behavior—just transitions. Door opening is a simple time-based animation.

**Example:**
```csharp
// Source: Game Programming Patterns - State chapter
public enum DoorState { Closed, Opening, Open }

public class Door
{
    public Vector3 Position { get; }
    public Direction Facing { get; }  // North, South, East, West
    public DoorState State { get; private set; } = DoorState.Closed;
    public BoundingBox TriggerVolume { get; private set; }

    private float _openingProgress = 0f;
    private const float OPENING_DURATION = 0.5f;

    public void Open()
    {
        if (State == DoorState.Closed)
            State = DoorState.Opening;
    }

    public void Update(float deltaTime)
    {
        if (State == DoorState.Opening)
        {
            _openingProgress += deltaTime;
            if (_openingProgress >= OPENING_DURATION)
            {
                _openingProgress = OPENING_DURATION;
                State = DoorState.Open;
            }
        }
    }

    public bool CanPlayerEnter() => State == DoorState.Open;

    public bool IsPlayerInTrigger(Vector3 playerPos)
    {
        return TriggerVolume.Contains(playerPos) == ContainmentType.Contains;
    }
}
```

### Pattern 3: Event-Driven Room Clear Detection
**What:** EnemyManager fires OnAllEnemiesDefeated event, RoomManager listens and opens doors. Decouples systems.

**When to use:** Always. Prevents tight coupling between combat and room systems.

**Example:**
```csharp
// Source: Existing codebase pattern (EnemyHealth.OnDeath event)
public class RoomManager
{
    private Room _currentRoom;
    private EnemyManager _enemyManager;

    public void Initialize()
    {
        _enemyManager.OnAllEnemiesDefeated += HandleRoomCleared;
    }

    private void HandleRoomCleared()
    {
        Console.WriteLine("Room cleared! Opening doors...");
        foreach (var door in _currentRoom.Doors.Values)
        {
            door.Open();
        }
    }
}
```

### Pattern 4: Room Transition via Trigger Detection
**What:** Check player position against open door triggers each frame. When player enters trigger, reset room and respawn enemies.

**When to use:** For instant room transitions (v1). Deferred: smooth camera transitions, loading screens (v2+).

**Example:**
```csharp
// Source: MonoGame BoundingBox Contains method
public void Update(GameTime gameTime, Vector3 playerPos)
{
    // Check all doors for player entry
    foreach (var door in _currentRoom.Doors.Values)
    {
        if (door.CanPlayerEnter() && door.IsPlayerInTrigger(playerPos))
        {
            TransitionRoom(door.Facing);
            break;
        }
    }
}

private void TransitionRoom(Direction exitDirection)
{
    Console.WriteLine($"Player exited {exitDirection}! Loading new room...");

    // Reset room state
    foreach (var door in _currentRoom.Doors.Values)
    {
        door.Close();  // Reset all doors to closed
    }

    // Respawn enemies
    _enemyManager.Reset();
    _enemyManager.SpawnWave(3, Vector3.Zero);  // Use current wave difficulty

    // Optional: Adjust player position to opposite door
    // (e.g., exit North -> spawn at South door)
}
```

### Anti-Patterns to Avoid
- **Tight coupling room-to-enemies:** Don't have Room directly spawn enemies. Use events or dependency injection.
- **Global room state:** Don't store room data in BerzerkGame. Use RoomManager to encapsulate lifecycle.
- **Over-engineering for v1:** Don't build procedural generation or multi-room grid yet. Single handcrafted room first.
- **Missing trigger volumes:** Don't just check exact door position. Use BoundingBox trigger zones (player approaching door from inside room).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Collision detection | Raycasting or manual AABB math | BoundingBox.Contains / Intersects | MonoGame provides optimized, tested collision primitives |
| State machine framework | Custom event system with delegates | Simple enum + switch OR State pattern classes | Enum sufficient for doors, State pattern for complex cases |
| Scene management (v2+) | Custom room loading system | MonoGame Scene pattern or MonoGame.Extended | Established pattern with automatic content lifecycle |
| Random room generation seed | DateTime or incremental seed | Room coordinates as seed (XY hash) | Classic Berzerk pattern ensures deterministic, repeatable rooms |

**Key insight:** MonoGame's BoundingBox is purpose-built for 3D collision and already used throughout the codebase (camera collision, projectile walls, enemy collision). Don't reinvent it—extend the existing wall list pattern to include door triggers.

## Common Pitfalls

### Pitfall 1: Door Trigger Too Small or Misaligned
**What goes wrong:** Player walks past door without triggering transition, or triggers from wrong side (outside room).

**Why it happens:** Door trigger volume placed exactly at door position (thin plane) or extends outside room boundary.

**How to avoid:**
- Make trigger volume a box extending inward from door (e.g., 2x2x2 units inside room)
- Use BoundingBox.Contains to detect player center point, not just intersection
- Visualize triggers with DebugRenderer during development

**Warning signs:** Player "ghosts through" doors, or doors trigger when approaching from outside room.

### Pitfall 2: Race Condition on Room Clear
**What goes wrong:** Last enemy destroyed, doors open, but enemy still rendering for a frame. Or doors open before explosion effect plays.

**Why it happens:** Checking `_enemies.Count == 0` immediately after enemy deactivation, before explosion spawns.

**How to avoid:**
- Check `EnemyManager.AllEnemiesDefeated` property that accounts for active enemies only
- Fire OnAllEnemiesDefeated AFTER explosion effect spawned (in enemy death handler)
- Use short delay (0.5s) before opening doors for visual clarity

**Warning signs:** Doors open mid-explosion, or enemies still visible when doors open.

### Pitfall 3: Resetting Player State on Transition
**What goes wrong:** Player health/ammo reset to defaults when entering new room, or player position snaps incorrectly.

**Why it happens:** Calling RestartGame() logic instead of room-only reset, or misunderstanding transition direction.

**How to avoid:**
- Separate concerns: Room.Reset() != Game.Restart()
- Room transition only resets: enemy spawns, door states, pickups
- Preserve: player health, ammo, position (adjusted for entry direction)
- Map exit direction to entry position (North exit → spawn at South door of new room)

**Warning signs:** Player stats reset between rooms, or player spawns at wrong door.

### Pitfall 4: Forgetting Collision Passthrough for Open Doors
**What goes wrong:** Doors open but player still collides with invisible wall, can't exit room.

**Why it happens:** Door BoundingBox added to wall collision list, never removed when door opens.

**How to avoid:**
- Two separate lists: `_wallColliders` (always active) and `_doorColliders` (state-dependent)
- Only add closed doors to collision checks
- OR: Use door state to conditionally skip collision check

**Warning signs:** Player blocked at open doors, collision sound/feedback at doorway.

### Pitfall 5: Door Animation State Desync
**What goes wrong:** Door state shows "Open" but visual animation hasn't completed, or door closes mid-opening.

**Why it happens:** Changing state directly without respecting animation transitions, or interrupting Opening state.

**How to avoid:**
- Use three states: Closed → Opening (0.5s) → Open
- Don't allow state transitions during Opening (locked state machine)
- Update animation progress in Update(), check completion before state change
- Visual feedback must match collision state (if door is solid, look closed)

**Warning signs:** Doors snap open instantly, or visual state doesn't match collision behavior.

## Code Examples

Verified patterns from official sources and existing codebase:

### BoundingBox Collision Detection (MonoGame Official)
```csharp
// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.BoundingBox.html
// Check if player is inside door trigger volume
Vector3 playerPos = _playerController.Transform.Position;
BoundingBox doorTrigger = door.TriggerVolume;

ContainmentType containment = doorTrigger.Contains(playerPos);
if (containment == ContainmentType.Contains)
{
    // Player fully inside trigger - transition room
}

// Check if projectile intersects wall
BoundingSphere projectileSphere = projectile.GetBoundingSphere();
foreach (var wall in _currentRoom.Walls)
{
    if (wall.Intersects(projectileSphere))
    {
        // Projectile hit wall
        projectile.OnHitWall();
        break;
    }
}
```

### Event-Driven Enemy Clear (Existing Codebase Pattern)
```csharp
// Source: Berzerk/Source/Enemies/EnemyHealth.cs (OnDeath event pattern)
public class EnemyManager
{
    public event Action OnAllEnemiesDefeated;

    private void OnEnemyDeath(EnemyController enemy)
    {
        // Spawn explosion, handle drops (existing code)
        SpawnExplosion(enemy.Transform.Position);

        // Check if all enemies defeated AFTER explosion spawned
        if (AllEnemiesDefeated)
        {
            OnAllEnemiesDefeated?.Invoke();
        }
    }

    public bool AllEnemiesDefeated => _enemies.Count == 0;
}
```

### State Machine with Enum (Game Programming Patterns)
```csharp
// Source: https://gameprogrammingpatterns.com/state.html
public enum DoorState { Closed, Opening, Open }

public void Update(float deltaTime)
{
    switch (State)
    {
        case DoorState.Closed:
            // No logic needed - waiting for Open() call
            break;

        case DoorState.Opening:
            _openingProgress += deltaTime / OPENING_DURATION;
            if (_openingProgress >= 1.0f)
            {
                _openingProgress = 1.0f;
                State = DoorState.Open;
                Console.WriteLine("Door fully open!");
            }
            break;

        case DoorState.Open:
            // No logic needed - door stays open until room transition
            break;
    }
}
```

### Wall List Pattern (Existing Codebase)
```csharp
// Source: Berzerk/Source/Graphics/ThirdPersonCamera.cs CreateTestWalls()
public class Room
{
    private List<BoundingBox> CreateMazeWalls()
    {
        return new List<BoundingBox>
        {
            // Outer perimeter (4 walls)
            new BoundingBox(new Vector3(-15, 0, -15), new Vector3(-14, 5, 15)),  // West
            new BoundingBox(new Vector3(14, 0, -15), new Vector3(15, 5, 15)),     // East
            new BoundingBox(new Vector3(-15, 0, -15), new Vector3(15, 5, -14)),   // North
            new BoundingBox(new Vector3(-15, 0, 14), new Vector3(15, 5, 15)),     // South

            // Interior maze walls (handcrafted layout)
            new BoundingBox(new Vector3(-8, 0, -8), new Vector3(-6, 4, -2)),      // Pillar 1
            new BoundingBox(new Vector3(6, 0, -8), new Vector3(8, 4, -2)),        // Pillar 2
            new BoundingBox(new Vector3(-8, 0, 2), new Vector3(-6, 4, 8)),        // Pillar 3
            new BoundingBox(new Vector3(6, 0, 2), new Vector3(8, 4, 8)),          // Pillar 4

            // Connecting walls
            new BoundingBox(new Vector3(-2, 0, -5), new Vector3(2, 4, -4)),       // Center wall 1
            new BoundingBox(new Vector3(-2, 0, 4), new Vector3(2, 4, 5)),         // Center wall 2
        };
    }
}
```

### Door Trigger Zones (Recommended Pattern)
```csharp
// Source: Research - trigger volumes for 3D door transitions
public class Door
{
    public Direction Facing { get; }
    public Vector3 Position { get; }
    public BoundingBox TriggerVolume { get; private set; }
    public BoundingBox CollisionBox { get; private set; }  // Only active when Closed

    public Door(Direction facing, Vector3 position)
    {
        Facing = facing;
        Position = position;

        // Create trigger volume (extends inward from door)
        // Example for North door (facing -Z): trigger extends south into room
        switch (facing)
        {
            case Direction.North:
                TriggerVolume = new BoundingBox(
                    new Vector3(position.X - 2, 0, position.Z),      // Door position
                    new Vector3(position.X + 2, 3, position.Z + 2)   // Extends south
                );
                CollisionBox = new BoundingBox(
                    new Vector3(position.X - 2, 0, position.Z - 0.5f),
                    new Vector3(position.X + 2, 5, position.Z + 0.5f)
                );
                break;

            // Similar for South, East, West...
        }
    }

    public BoundingBox? GetActiveCollision()
    {
        // Only block player when closed
        return State == DoorState.Closed ? CollisionBox : null;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate scene per room (Unity/Unreal pattern) | Single persistent scene with room data reload | MonoGame 3.8+ Scene pattern (2020) | Lighter memory footprint, faster transitions for arcade games |
| Mesh-based collision for walls | BoundingBox primitives for axis-aligned geometry | MonoGame Framework 3.0+ (2013) | Simpler, faster, sufficient for Berzerk-style orthogonal mazes |
| Class-based State pattern for all FSMs | Enum + switch for simple states, classes for complex | Game Programming Patterns book (2014) | Pragmatic: use simplest tool that works |
| Procedural generation from start | Handcrafted → procedural in phases | Indie best practice | Faster iteration, establish mechanics before algorithms |

**Deprecated/outdated:**
- **XNA 4.0 ContentManager per-room:** MonoGame Scene pattern (ContentManager per scene) replaced manual content lifecycle management (2020+)
- **Hardcoded wall positions in BerzerkGame:** Extract to Room class for modularity and testability
- **Global EnemyManager.SpawnWave() calls:** Wire to Room transition events for encapsulation

## Open Questions

Things that couldn't be fully resolved:

1. **Door Opening Animation Visual**
   - What we know: State machine handles timing (0.5s), collision passthrough on Open state
   - What's unclear: Visual representation—sliding door geometry vs. fading vs. just color change?
   - Recommendation: Start with color change (Red=Closed, Green=Opening, None=Open) using DebugRenderer. Defer geometry animation to polish phase.

2. **Player Spawn Position on Room Entry**
   - What we know: Classic Berzerk places player at opposite door (exit North → enter new room from South)
   - What's unclear: Exact offset from door (spawn 2 units inside? 5 units?), handle corner cases (spawn inside wall?)
   - Recommendation: Spawn 3 units inside room from door center, validate against wall collision, fallback to room center if blocked.

3. **Room Transition with Active Projectiles**
   - What we know: ProjectileManager pools projectiles, can deactivate all
   - What's unclear: Preserve projectiles across transition (fly into new room) or clear on transition?
   - Recommendation: Clear all projectiles on room transition (simpler, matches classic Berzerk where rooms are isolated).

4. **Door State Reset Timing**
   - What we know: Doors should close on room transition to reset for next clear
   - What's unclear: Close doors immediately on transition, or wait for player to fully enter new room?
   - Recommendation: Close immediately on transition to avoid confusion (new room = fresh challenge).

5. **Deterministic Room Generation (v2 scope)**
   - What we know: Classic Berzerk used room XY coords as RNG seed for 1024 unique layouts
   - What's unclear: Handcrafted v1 room sufficient, or build seed foundation now?
   - Recommendation: Defer to v2. Focus v1 on single handcrafted room mechanics, establish room data structure that can accept procedural layouts later.

## Sources

### Primary (HIGH confidence)
- [MonoGame BoundingBox API Documentation](https://docs.monogame.net/api/Microsoft.Xna.Framework.BoundingBox.html) - Official API reference for collision detection
- [MonoGame Collision Detection Overview](https://docs.monogame.net/articles/getting_to_know/howto/HowTo_CollisionDetectionOverview.html) - Official guide on bounding volumes
- [MonoGame Scene Management Tutorial](https://docs.monogame.net/articles/tutorials/building_2d_games/17_scenes/) - Official pattern for scene lifecycle
- [Game Programming Patterns - State](https://gameprogrammingpatterns.com/state.html) - Authoritative FSM pattern reference
- Existing codebase: `BerzerkGame.cs`, `ThirdPersonCamera.cs`, `EnemyManager.cs` - Established patterns

### Secondary (MEDIUM confidence)
- [How The Mazes Were Generated For Classic Berzerk Game | Hackaday](https://hackaday.com/2013/07/21/how-the-mazes-were-generated-for-classic-berzerk-game/) - Classic Berzerk technical analysis (RNG seeding, 1024 layouts)
- [MazeGenerator | Robotron 2084 Guidebook](http://www.robotron2084guidebook.com/home/games/berzerk/mazegenerator/) - Berzerk room structure details (65,536 grid, 4 doors per room)
- [MonoGame Community: Room System Thread](https://community.monogame.net/t/room-system/15674) - Community discussion on room loading/unloading
- [MonoGame Community: Level Structure Thread](https://community.monogame.net/t/how-should-i-structure-a-level-scene-system/12830) - Architecture patterns for level systems
- [State Machines in Games - GameDev.net](https://www.gamedev.net/articles/programming/general-and-gameplay-programming/state-machines-in-games-r2982/) - FSM implementation for game logic

### Tertiary (LOW confidence)
- [Catlike Coding - Maze Unity Tutorial](https://catlikecoding.com/unity/tutorials/maze/) - Maze data structures (Unity-specific but patterns transferable)
- [C# Maze Algorithms](http://richardssoftware.net/Home/Post/73) - C# maze generation patterns (v2 scope, not verified for MonoGame)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - BoundingBox, enum state machines, List collections all verified in MonoGame docs and existing codebase
- Architecture: HIGH - Patterns sourced from official MonoGame tutorials, Game Programming Patterns book, and working codebase
- Pitfalls: MEDIUM - Based on community discussions and game dev best practices, not all verified in production Berzerk-like projects

**Research date:** 2026-02-04
**Valid until:** ~60 days (stable domain - MonoGame 3D collision APIs rarely change, state machine patterns evergreen)
