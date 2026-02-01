# Architecture Research

**Domain:** MonoGame 3D Action Shooter
**Researched:** 2026-01-31
**Confidence:** MEDIUM

## Standard Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Game Class (Entry)                    │
│  - Initialize(), LoadContent(), Update(), Draw()             │
├─────────────────────────────────────────────────────────────┤
│                     Scene/Screen Manager                     │
│  - MenuScreen, GameplayScreen, GameOverScreen                │
├─────────────────────────────────────────────────────────────┤
│                       Game Systems Layer                     │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │  Input   │  │  Camera  │  │   AI     │  │ ProcGen  │    │
│  │  System  │  │  System  │  │  System  │  │  System  │    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
│       │             │             │             │           │
├───────┴─────────────┴─────────────┴─────────────┴───────────┤
│                    Entity/Component Layer                    │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────┐        │
│  │ Player  │  │ Enemies │  │ Weapons │  │ Pickups │        │
│  │ Entity  │  │ Entity  │  │ Entity  │  │ Entity  │        │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────┬────┘        │
│       │            │            │            │              │
│  ┌────┴─────────────────────────────────────┴────┐          │
│  │  Components: Transform, Model, Health,         │          │
│  │              Collider, Animator, AI            │          │
│  └────────────────────────────────────────────────┘          │
├─────────────────────────────────────────────────────────────┤
│                      Rendering Pipeline                      │
│  - BasicEffect / Custom Shaders                              │
│  - SpriteBatch (HUD/UI)                                      │
├─────────────────────────────────────────────────────────────┤
│                    Content Pipeline Assets                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                   │
│  │ FBX      │  │ Textures │  │ Sounds   │                   │
│  │ Models   │  │ (PNG)    │  │ (WAV)    │                   │
│  └──────────┘  └──────────┘  └──────────┘                   │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| **Game Class** | Entry point, game loop coordination, owns GraphicsDeviceManager | Inherits from MonoGame.Framework.Game, calls Update()/Draw() ~60fps |
| **Scene/Screen Manager** | Manages game states (menu, gameplay, pause), transitions | Stack-based screen system or custom scene manager |
| **Entity Manager** | Tracks active game objects, handles lifecycle | List/Dictionary of entities, Update() and Draw() loops |
| **Input System** | Polls keyboard/mouse, translates to game commands | KeyboardState, MouseState polling in Update() |
| **Camera System** | View/Projection matrices, third-person following | Matrix calculations, follows player with offset, handles rotation/zoom |
| **Animation System** | Skinned mesh animation, bone transforms | Custom AnimatedModel processor, keyframe interpolation |
| **AI System** | Enemy behavior, pathfinding, state machines | Per-enemy FSM, A* pathfinding on grid or navmesh |
| **Collision System** | Detects entity intersections, triggers responses | BoundingSphere/BoundingBox tests, spatial partitioning |
| **Procedural Generator** | Creates room layouts, places entities | Algorithm runs on scene load, generates walls/floors/doors |
| **Combat System** | Damage dealing, projectile spawning/tracking | Projectile entities, hit detection, health modification |
| **HUD/UI System** | Renders health bars, ammo count, score | SpriteBatch overlay, drawn after 3D scene |
| **Content Manager** | Loads/unloads FBX models, textures, sounds | MonoGame ContentManager, loads preprocessed .xnb files |

## Recommended Project Structure

```
Berzerk3D/
├── Content/                    # MonoGame Content Pipeline project
│   ├── Models/                 # FBX models from Mixamo
│   │   ├── Player/
│   │   │   ├── player.fbx
│   │   │   └── animations/     # Separate animation clips if needed
│   │   └── Enemy/
│   │       └── robot.fbx
│   ├── Textures/               # PNG/DDS textures
│   │   ├── floor.png
│   │   └── wall.png
│   ├── Sounds/                 # WAV/OGG audio
│   └── Effects/                # Custom HLSL shaders (optional)
│
├── Source/                     # Main game code
│   ├── Game1.cs                # Main Game class entry point
│   │
│   ├── Core/                   # Core game systems
│   │   ├── SceneManager.cs     # Manages screen transitions
│   │   ├── InputManager.cs     # Centralized input handling
│   │   └── ServiceLocator.cs   # Optional: access to global systems
│   │
│   ├── Entities/               # Game object definitions
│   │   ├── Entity.cs           # Base entity class
│   │   ├── Player.cs           # Player entity
│   │   ├── Enemy.cs            # Enemy robot entity
│   │   ├── Projectile.cs       # Laser projectile
│   │   └── Pickup.cs           # Ammo/health pickups
│   │
│   ├── Components/             # Reusable behaviors (if using ECS)
│   │   ├── TransformComponent.cs
│   │   ├── ModelComponent.cs
│   │   ├── AnimatorComponent.cs
│   │   ├── ColliderComponent.cs
│   │   ├── HealthComponent.cs
│   │   └── AIComponent.cs
│   │
│   ├── Systems/                # Game systems that operate on entities
│   │   ├── CameraSystem.cs     # Third-person camera controller
│   │   ├── AnimationSystem.cs  # Updates skeletal animations
│   │   ├── AISystem.cs         # Enemy AI and pathfinding
│   │   ├── CollisionSystem.cs  # Collision detection and response
│   │   ├── CombatSystem.cs     # Damage dealing, projectiles
│   │   └── RoomGenerator.cs    # Procedural maze generation
│   │
│   ├── Screens/                # Game states/scenes
│   │   ├── MenuScreen.cs
│   │   ├── GameplayScreen.cs
│   │   └── GameOverScreen.cs
│   │
│   ├── Rendering/              # Rendering-specific code
│   │   ├── AnimatedModel.cs    # Skinned mesh renderer
│   │   ├── ModelRenderer.cs    # Static model renderer
│   │   └── HUDRenderer.cs      # 2D UI overlay
│   │
│   └── Utilities/              # Helper classes
│       ├── BoundingVolumes.cs  # Collision math helpers
│       └── MathHelper.cs       # Vector/matrix utilities
│
└── Berzerk3D.csproj            # Project file
```

### Structure Rationale

- **Content/**: MonoGame Content Pipeline processes assets at build time into optimized .xnb format. FBX models are imported and processed for skeletal animation.
- **Core/**: Central systems that coordinate the game. SceneManager handles state transitions. InputManager provides a single point for input queries.
- **Entities/**: Object-oriented approach with entity classes. Each entity has Update() and Draw() methods. Good for smaller games; can migrate to full ECS if needed.
- **Components/**: If using component-based architecture, components hold data. Entities are collections of components.
- **Systems/**: Systems contain logic that operates on entities/components. CameraSystem updates view matrix, AISystem runs pathfinding, etc.
- **Screens/**: Each screen is a game state (Menu, Playing, GameOver) with its own Update/Draw logic.
- **Rendering/**: Separates rendering concerns. AnimatedModel handles bone transforms and skinning. HUDRenderer uses SpriteBatch for 2D overlay.

## Architectural Patterns

### Pattern 1: MonoGame Game Loop

**What:** The Game class provides Initialize, LoadContent, Update, Draw lifecycle methods. This is the foundation of all MonoGame games.

**When to use:** Always - this is the framework's core pattern.

**Trade-offs:**
- **Pros:** Fixed timestep (60fps by default), separates update logic from rendering, built-in content loading
- **Cons:** Tightly couples game to MonoGame framework, must work within fixed lifecycle

**Example:**
```csharp
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SceneManager _sceneManager;

    protected override void Initialize()
    {
        // Initialize systems (non-content)
        _sceneManager = new SceneManager(this);
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // Load all game assets
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _sceneManager.LoadContent(Content);
    }

    protected override void Update(GameTime gameTime)
    {
        // Game logic - called ~60fps
        _sceneManager.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Rendering - called ~60fps
        GraphicsDevice.Clear(Color.Black);
        _sceneManager.Draw(gameTime, _spriteBatch);
        base.Draw(gameTime);
    }
}
```

### Pattern 2: Component-Based Entity Architecture

**What:** Entities are containers for components. Components hold data. Systems operate on entities with specific component combinations.

**When to use:** Medium to large games with many entity types sharing behaviors.

**Trade-offs:**
- **Pros:** High reusability, easy to add new behaviors without modifying entity classes, composition over inheritance
- **Cons:** More complex than simple OOP, slight performance overhead from component lookups, requires discipline to avoid "god entities"

**Example:**
```csharp
// Entity holds components
public class Entity
{
    public List<IComponent> Components { get; } = new();

    public T GetComponent<T>() where T : IComponent
    {
        return Components.OfType<T>().FirstOrDefault();
    }

    public void Update(GameTime gameTime)
    {
        foreach (var component in Components)
            component.Update(gameTime);
    }
}

// Components are data + specific behavior
public class HealthComponent : IComponent
{
    public float CurrentHealth { get; set; }
    public float MaxHealth { get; set; }

    public void TakeDamage(float amount)
    {
        CurrentHealth = Math.Max(0, CurrentHealth - amount);
    }
}

// Systems operate on entities with specific components
public class CombatSystem
{
    public void ProcessDamage(Entity attacker, Entity target)
    {
        var health = target.GetComponent<HealthComponent>();
        if (health != null)
            health.TakeDamage(10f);
    }
}
```

### Pattern 3: Stack-Based Screen Manager

**What:** Screens (Menu, Gameplay, Pause) are managed in a stack. Active screen receives Update/Draw calls. Can push/pop screens for state transitions.

**When to use:** Games with multiple distinct states (menu, gameplay, pause, game over).

**Trade-offs:**
- **Pros:** Clean state transitions, can layer screens (pause over gameplay), easy to add new screens
- **Cons:** Only top screen is typically active, requires careful management of screen lifecycle

**Example:**
```csharp
public class SceneManager
{
    private Stack<Screen> _screenStack = new();

    public void PushScreen(Screen screen)
    {
        _screenStack.Push(screen);
        screen.OnEnter();
    }

    public void PopScreen()
    {
        var screen = _screenStack.Pop();
        screen.OnExit();
    }

    public void Update(GameTime gameTime)
    {
        if (_screenStack.Count > 0)
            _screenStack.Peek().Update(gameTime);
    }

    public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (_screenStack.Count > 0)
            _screenStack.Peek().Draw(gameTime, spriteBatch);
    }
}

public abstract class Screen
{
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public abstract void Update(GameTime gameTime);
    public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);
}
```

### Pattern 4: Third-Person Camera Follow

**What:** Camera maintains offset from player, rotates around player with mouse input, updates View matrix each frame.

**When to use:** Third-person action games where player visibility is important.

**Trade-offs:**
- **Pros:** Player remains visible, easier spatial awareness than first-person, good for showing character animations
- **Cons:** Collision with walls requires extra handling, more complex than static camera

**Example:**
```csharp
public class ThirdPersonCamera
{
    private Vector3 _position;
    private Vector3 _target;
    private float _distance = 10f;
    private float _yaw = 0f;
    private float _pitch = MathHelper.PiOver4;

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }

    public void Update(Vector3 playerPosition, float mouseX, float mouseY)
    {
        // Rotate camera with mouse
        _yaw += mouseX * 0.01f;
        _pitch = MathHelper.Clamp(_pitch + mouseY * 0.01f,
            -MathHelper.PiOver2 + 0.1f,
            MathHelper.PiOver2 - 0.1f);

        // Calculate camera position offset from player
        Vector3 offset = new Vector3(
            (float)(Math.Cos(_yaw) * Math.Cos(_pitch)),
            (float)Math.Sin(_pitch),
            (float)(Math.Sin(_yaw) * Math.Cos(_pitch))
        ) * _distance;

        _position = playerPosition + offset;
        _target = playerPosition;

        // Update view matrix
        View = Matrix.CreateLookAt(_position, _target, Vector3.Up);
    }
}
```

### Pattern 5: Skeletal Animation with Bone Transforms

**What:** FBX models contain skeleton (bones) and animation clips. Processor extracts bone transforms. At runtime, interpolate keyframes and apply to skeleton.

**When to use:** Character animations from Mixamo or other skinned meshes.

**Trade-offs:**
- **Pros:** Smooth character animation, industry-standard pipeline, Mixamo provides free animations
- **Cons:** MonoGame doesn't provide built-in animation system, must implement custom processor, performance cost for many animated characters

**Example:**
```csharp
// Simplified animated model structure
public class AnimatedModel
{
    public Model Model { get; set; }
    public Matrix[] BoneTransforms { get; set; }
    public AnimationClip CurrentAnimation { get; set; }
    private float _currentTime;

    public void Update(GameTime gameTime)
    {
        if (CurrentAnimation == null) return;

        _currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        _currentTime %= CurrentAnimation.Duration;

        // Interpolate bone transforms based on current time
        for (int i = 0; i < BoneTransforms.Length; i++)
        {
            BoneTransforms[i] = CurrentAnimation.GetBoneTransform(i, _currentTime);
        }
    }

    public void Draw(Matrix world, Matrix view, Matrix projection)
    {
        foreach (var mesh in Model.Meshes)
        {
            foreach (var effect in mesh.Effects)
            {
                if (effect is IEffectMatrices matrices)
                {
                    matrices.World = BoneTransforms[mesh.ParentBone.Index] * world;
                    matrices.View = view;
                    matrices.Projection = projection;
                }
            }
            mesh.Draw();
        }
    }
}
```

### Pattern 6: Grid-Based Pathfinding for AI

**What:** Discretize room into grid cells. Use A* or similar algorithm to find path from enemy to player. Enemy follows waypoints.

**When to use:** Maze/dungeon games where enemies need to navigate around walls.

**Trade-offs:**
- **Pros:** Simple to implement, works well for grid-based levels, deterministic behavior
- **Cons:** Not smooth movement (requires path smoothing), grid resolution tradeoff (fine grid = more memory/computation), doesn't handle dynamic obstacles well

**Example:**
```csharp
public class AISystem
{
    private Grid _levelGrid;

    public void UpdateEnemy(Enemy enemy, Vector3 playerPosition, GameTime gameTime)
    {
        // Simplified AI state machine
        switch (enemy.State)
        {
            case AIState.Patrol:
                if (Vector3.Distance(enemy.Position, playerPosition) < 15f)
                {
                    enemy.State = AIState.Chase;
                    enemy.Path = FindPath(enemy.Position, playerPosition);
                }
                break;

            case AIState.Chase:
                if (enemy.Path.Count > 0)
                {
                    Vector3 nextWaypoint = enemy.Path[0];
                    Vector3 direction = nextWaypoint - enemy.Position;

                    if (direction.Length() < 0.5f)
                    {
                        enemy.Path.RemoveAt(0); // Reached waypoint
                    }
                    else
                    {
                        direction.Normalize();
                        enemy.Position += direction * enemy.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
                    }
                }

                // Attack if close to player
                if (Vector3.Distance(enemy.Position, playerPosition) < 2f)
                {
                    enemy.State = AIState.Attack;
                }
                break;

            case AIState.Attack:
                // Deal damage to player
                break;
        }
    }

    private List<Vector3> FindPath(Vector3 start, Vector3 goal)
    {
        // A* pathfinding implementation
        // Returns list of waypoints from start to goal
        return new List<Vector3>(); // Simplified
    }
}
```

## Data Flow

### Request Flow

```
[Player Input]
    ↓
[InputManager] → Translates to game commands
    ↓
[Player Entity] → Updates position, fires weapon
    ↓
[Systems Update] → Camera follows, AI reacts, collision checks
    ↓
[Entity State] → Health changes, positions update
    ↓
[Rendering] → Draw 3D models with updated transforms
    ↓
[HUD Update] → Display health, ammo, score
```

### Combat Data Flow

```
[Player Presses Fire]
    ↓
[InputManager detects mouse click]
    ↓
[CombatSystem.SpawnProjectile(position, direction)]
    ↓
[Projectile Entity created, added to entity list]
    ↓
[Update Loop: Projectile moves forward each frame]
    ↓
[CollisionSystem detects Projectile intersects Enemy.BoundingSphere]
    ↓
[CombatSystem.ProcessHit(projectile, enemy)]
    ↓
[Enemy.HealthComponent.TakeDamage(10)]
    ↓
[If health <= 0: Enemy.Destroy(), Score += 100]
    ↓
[HUDRenderer displays updated score]
```

### Procedural Generation Flow

```
[Room Transition Triggered]
    ↓
[RoomGenerator.GenerateRoom(difficulty)]
    ↓
[Algorithm: Select maze layout (DFS, BSP, or cellular automata)]
    ↓
[Generate walls, floors, doors as 3D models]
    ↓
[Spawn enemies at valid positions (difficulty determines count)]
    ↓
[Spawn pickups (ammo, health) at valid positions]
    ↓
[Load generated geometry into scene]
    ↓
[Player spawns at entrance position]
    ↓
[Gameplay begins]
```

### Animation Data Flow

```
[AnimationSystem.Update(gameTime)]
    ↓
[For each AnimatedModel: Update current animation time]
    ↓
[Interpolate keyframes based on time]
    ↓
[Calculate bone transforms (matrices)]
    ↓
[Rendering: Apply bone transforms to mesh vertices]
    ↓
[GPU: Vertex shader applies skinning]
    ↓
[Animated character rendered to screen]
```

### Key Data Flows

1. **Input → Player Movement:** Keyboard state polled → Player.Velocity calculated → Position updated → Camera follows
2. **AI Behavior:** Player position → Enemy calculates distance → Path calculated if chase → Move along path → Attack if in range
3. **Content Loading:** FBX file → Content Pipeline processes → .xnb file → ContentManager.Load<Model>() → AnimatedModel wrapper → Ready to render
4. **Room Progression:** All enemies dead → Door.IsOpen = true → Player collides with door → RoomGenerator.GenerateRoom() → New room loaded

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| **Prototype (1-2 rooms)** | Simple OOP with entity classes. No ECS needed. Single GameplayScreen. Hardcoded room layout. BasicEffect for rendering. |
| **MVP (10+ procedural rooms)** | Add procedural generation system. Implement basic pathfinding (A*). Component-based entities for flexibility. Custom animation system for Mixamo models. Screen manager for menu/gameplay/gameover. |
| **Polished Game (100+ rooms, multiple enemy types)** | Full ECS architecture for performance. Spatial partitioning for collision (quadtree/octree). Object pooling for projectiles and enemies. Asset streaming for large levels. Deferred rendering if many lights. Multi-threaded pathfinding. |

### Scaling Priorities

1. **First bottleneck: Collision detection with many entities**
   - **Symptoms:** Frame rate drops when >50 entities active
   - **Fix:** Spatial partitioning (octree/grid), broad-phase/narrow-phase collision
   - **When:** After adding room progression with increasing enemy counts

2. **Second bottleneck: Animation updates for many characters**
   - **Symptoms:** CPU-bound when >20 animated characters on screen
   - **Fix:** LOD system (reduce animation updates for distant enemies), GPU skinning, animation pooling
   - **When:** After adding difficulty scaling with more enemies per room

3. **Third bottleneck: Procedural generation causing load hitches**
   - **Symptoms:** Noticeable freeze when generating new room
   - **Fix:** Generate next room asynchronously while player is in current room, cache generated rooms
   - **When:** After implementing room transitions

## Anti-Patterns

### Anti-Pattern 1: Deep Inheritance Hierarchies

**What people do:** `Sprite → Collider → PhysicsObject → DynamicObject → Enemy → RobotEnemy`

**Why it's wrong:**
- Tightly couples unrelated behaviors
- Difficult to add new entity types with different behavior combinations
- "Diamond problem" when you need behaviors from multiple branches
- Hard to test individual behaviors in isolation

**Do this instead:**
- Use composition: Entity has components (Transform, Model, Health, AI)
- Or use interfaces: `class Enemy : IMovable, IDamageable, IDrawable`
- Maximum 2-3 inheritance layers

**Example of correct approach:**
```csharp
// BAD: Deep inheritance
public class Entity { }
public class MovableEntity : Entity { }
public class AnimatedEntity : MovableEntity { }
public class CombatEntity : AnimatedEntity { }
public class RobotEnemy : CombatEntity { } // 5 layers deep!

// GOOD: Composition
public class Entity
{
    public TransformComponent Transform { get; set; }
    public ModelComponent Model { get; set; }
    public HealthComponent Health { get; set; }
    public AIComponent AI { get; set; }
    // Add/remove components as needed
}
```

### Anti-Pattern 2: Polling ContentManager Every Frame

**What people do:** `Content.Load<Texture2D>("player")` inside Draw() or Update()

**Why it's wrong:**
- ContentManager caches, but still causes dictionary lookups every frame
- Defeats purpose of LoadContent() lifecycle method
- Performance penalty for no benefit
- Can cause stuttering if loading uncached content

**Do this instead:**
- Load all content once in LoadContent()
- Store references in fields
- Use content manager only at scene transitions

**Example of correct approach:**
```csharp
// BAD: Loading every frame
public void Draw(GameTime gameTime)
{
    var texture = Content.Load<Texture2D>("player"); // SLOW
    spriteBatch.Draw(texture, position, Color.White);
}

// GOOD: Load once, reuse
private Texture2D _playerTexture;

protected override void LoadContent()
{
    _playerTexture = Content.Load<Texture2D>("player"); // Load once
}

public void Draw(GameTime gameTime)
{
    spriteBatch.Draw(_playerTexture, position, Color.White); // Fast
}
```

### Anti-Pattern 3: Update-Order Dependencies

**What people do:** Assume entities update in specific order. Player.Update() must run before Camera.Update(), which must run before Enemy.Update().

**Why it's wrong:**
- Fragile when adding new entities
- Hard to parallelize updates
- Causes subtle bugs when order changes
- Makes testing difficult

**Do this instead:**
- Make updates order-independent
- Use previous frame's data for reads
- Or explicitly define update phases (input → logic → physics → rendering)

**Example of correct approach:**
```csharp
// BAD: Order-dependent
public void Update(GameTime gameTime)
{
    player.Update(); // MUST run first
    camera.Update(); // MUST run second (reads player.Position)
    enemy.Update();  // MUST run third (reads player.Position)
}

// GOOD: Explicit phases
public void Update(GameTime gameTime)
{
    // Phase 1: Input
    inputManager.Update();

    // Phase 2: Logic (can be parallel)
    Parallel.ForEach(entities, e => e.UpdateLogic(gameTime));

    // Phase 3: Physics
    collisionSystem.Update();

    // Phase 4: Late update (camera, effects)
    camera.Update(player.Position);
}
```

### Anti-Pattern 4: Using DrawableGameComponent for Everything

**What people do:** Create a DrawableGameComponent subclass for every entity, add to Game.Components.

**Why it's wrong:**
- Overhead of MonoGame's component system not necessary for simple entities
- Harder to control update/draw order
- No performance benefit over plain entity list
- Locks you into MonoGame's update pattern

**Do this instead:**
- Use DrawableGameComponent only for major systems (ScreenManager, InputManager)
- Use plain entity classes with Update/Draw methods
- Manage entity lifecycle manually

**Example of correct approach:**
```csharp
// BAD: Everything is a DrawableGameComponent
public class Player : DrawableGameComponent { } // Unnecessary overhead
public class Bullet : DrawableGameComponent { } // Way too heavy for simple projectile

// GOOD: Simple entity class
public class Player
{
    public void Update(GameTime gameTime) { }
    public void Draw(Matrix view, Matrix projection) { }
}

// Game1.cs manages entities directly
private List<Entity> _entities = new();

protected override void Update(GameTime gameTime)
{
    foreach (var entity in _entities)
        entity.Update(gameTime);
}
```

### Anti-Pattern 5: Creating New GameTime Each Frame

**What people do:** Pass `new GameTime()` or create custom GameTime instances.

**Why it's wrong:**
- MonoGame's Update/Draw already provide accurate GameTime
- Custom GameTime breaks frame timing
- Causes animation/physics to run at wrong speed

**Do this instead:**
- Always use the GameTime parameter provided by Update/Draw
- If you need elapsed time, use `gameTime.ElapsedGameTime.TotalSeconds`

**Example of correct approach:**
```csharp
// BAD: Creating custom GameTime
public void UpdateAnimation()
{
    var gameTime = new GameTime(); // WRONG - no actual time data
    animation.Update(gameTime);
}

// GOOD: Use provided GameTime
protected override void Update(GameTime gameTime)
{
    animation.Update(gameTime); // Correct - has actual delta time
}
```

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **Mixamo FBX Import** | Content Pipeline Importer/Processor | Use MonoGame Pipeline Tool to add FBX, set importer to "FBX Importer - MonoGame". Outputs .xnb file. Custom processor needed for animations. |
| **Audio (if added)** | SoundEffect.Play() for one-shots, SoundEffectInstance for looping | Load .wav or .xnb in LoadContent, play in response to events. |
| **Input Devices** | Keyboard/Mouse classes polled in Update() | `Keyboard.GetState()` and `Mouse.GetState()` each frame. |
| **Physics Engine (if added)** | Wrapper around Jitter/Bepu/Farseer | Create RigidBody for each entity, step simulation in Update(), sync positions. |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| **Game ↔ ScreenManager** | ScreenManager.Update()/Draw() called from Game | Game owns ScreenManager, delegates lifecycle calls. |
| **ScreenManager ↔ Screen** | Active screen receives Update/Draw | Stack-based, only top screen is active. |
| **Screen ↔ Systems** | Direct method calls or event subscriptions | Screen owns systems (Camera, AI, Combat), calls Update(). |
| **Systems ↔ Entities** | Systems iterate entity lists, read/write components | Systems know about components, entities don't know about systems. |
| **Entities ↔ Components** | Entity owns components, exposes GetComponent<T>() | Loose coupling - entities can add/remove components at runtime. |
| **Player ↔ InputManager** | Player reads InputManager state | InputManager provides "IsKeyPressed()" API, player translates to movement. |
| **AI ↔ Pathfinding** | AI requests path, receives waypoint list | Pathfinding is stateless utility, AI maintains path state. |
| **Combat ↔ Collision** | Collision system invokes combat system on hit | Event-based: `OnCollision(entity1, entity2)` → CombatSystem checks if projectile hit enemy. |
| **Procedural Gen ↔ EntityManager** | Generator creates entities, adds to manager | Generator returns list of entities, screen adds to active list. |

## MonoGame-Specific Patterns

### Content Pipeline Build-Time Processing

**What:** Assets are processed at compile time, not runtime. FBX models → .xnb binary format.

**Why:**
- Faster load times (pre-processed, optimized format)
- Platform-specific optimizations
- Catches asset errors at build time

**How:**
1. Add asset to Content project via MonoGame Pipeline Tool
2. Set importer (e.g., "FBX Importer") and processor (e.g., "Model Processor")
3. Build content project → generates .xnb files
4. Runtime: `Content.Load<Model>("modelName")` loads .xnb

**Custom Processor Example (for animations):**
```csharp
[ContentProcessor(DisplayName = "Animated Model Processor")]
public class AnimatedModelProcessor : ModelProcessor
{
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
        // Extract animation data, bone hierarchy
        // Custom logic to store in .xnb
        return base.Process(input, context);
    }
}
```

### BasicEffect vs Custom Shaders

**BasicEffect (start here):**
- Built-in lighting, textures, fog
- No shader coding required
- Good for prototyping
- Limited to simple rendering

**Custom Effects (HLSL shaders):**
- Full control over rendering
- Required for skinned mesh animation
- Required for advanced lighting (PBR, deferred)
- More complex to set up

**When to transition:** Start with BasicEffect, move to custom shaders when you need skeletal animation or advanced lighting.

### DrawOrder and UpdateOrder

**What:** DrawableGameComponent has DrawOrder and UpdateOrder properties. Lower numbers = earlier execution.

**When to use:**
- Critical for layered rendering (3D scene DrawOrder=0, HUD DrawOrder=100)
- Useful for system dependencies

**Example:**
```csharp
// Ensure HUD draws after 3D scene
var sceneRenderer = new SceneComponent(this) { DrawOrder = 0 };
var hudRenderer = new HUDComponent(this) { DrawOrder = 100 };
Components.Add(sceneRenderer);
Components.Add(hudRenderer);
```

## Build Order Recommendations

Based on dependencies between systems, recommended implementation order:

### Phase 1: Foundation (Core Loop)
1. **Game1 class** - Basic structure, clear screen to color
2. **Basic entity system** - Player entity with position, simple Update/Draw
3. **Input handling** - WASD movement for player
4. **Camera system** - Static camera first, then third-person follow
5. **Content loading** - Load simple 3D model (cube or Mixamo character)

**Why this order:** Establishes game loop and basic player control. Can see character move in 3D space.

### Phase 2: Rendering & Animation
6. **Model rendering** - Draw FBX models with BasicEffect
7. **Animation system** - Custom processor for Mixamo animations, basic playback
8. **Camera improvement** - Add mouse rotation, zoom

**Why this order:** Animation requires custom content processor, which is complex. Get static models working first.

### Phase 3: Combat & Interaction
9. **Projectile system** - Spawn laser on mouse click, move forward
10. **Collision detection** - BoundingSphere checks for projectile vs enemy
11. **Health/damage** - HealthComponent, taking damage, death

**Why this order:** Combat depends on collision, which depends on entities existing. Build bottom-up.

### Phase 4: AI & Enemies
12. **Enemy entity** - Basic robot that stands still
13. **AI system** - State machine (patrol, chase, attack)
14. **Pathfinding** - A* on grid for enemy navigation

**Why this order:** Enemy needs to exist before it can have AI. AI can be dumb at first (chase in straight line), then add pathfinding.

### Phase 5: Procedural Generation
15. **Room data structure** - Grid-based room layout
16. **Simple generator** - Hardcoded layouts first, then algorithm
17. **Room transition** - Detect all enemies dead, open door, load new room

**Why this order:** Procedural generation is complex. Hardcoded rooms let you test gameplay first. Algorithm comes later.

### Phase 6: Polish & Features
18. **HUD/UI** - SpriteBatch for health bar, ammo, score
19. **Pickups** - Ammo/health items, collision detection
20. **Screen manager** - Menu screen, game over screen
21. **Difficulty scaling** - More enemies/complex layouts as rooms progress

**Why this order:** These are additive features. Core gameplay loop works without them.

## Scene/Entity Management Approaches

### Approach 1: Simple Object-Oriented (Recommended for Berzerk 3D)

**Structure:**
- Base `Entity` class with Update/Draw
- Specific classes: `Player`, `Enemy`, `Projectile`
- `EntityManager` holds `List<Entity>`
- Systems are service classes (CameraSystem, CollisionSystem)

**Pros:**
- Simple, easy to understand
- Fast to implement
- Good for smaller games (<100 entity types)

**Cons:**
- Less flexible than ECS
- Harder to share behaviors across entity types

**Code:**
```csharp
public class EntityManager
{
    private List<Entity> _entities = new();

    public void Add(Entity entity) => _entities.Add(entity);

    public void Update(GameTime gameTime)
    {
        foreach (var entity in _entities)
            entity.Update(gameTime);
    }

    public void Draw(Matrix view, Matrix projection)
    {
        foreach (var entity in _entities)
            entity.Draw(view, projection);
    }
}
```

### Approach 2: Entity Component System (ECS)

**Structure:**
- `Entity` is just an ID
- `Component` holds data (TransformComponent, HealthComponent)
- `System` operates on entities with specific components
- Central registry maps entities to components

**Pros:**
- Highly flexible (add/remove components at runtime)
- Better performance for large numbers of entities (cache-friendly)
- Easy to serialize/network (entities are just data)

**Cons:**
- More complex to set up
- Steeper learning curve
- Overkill for small games

**Libraries:**
- Custom implementation
- MonoECS (GitHub: jjenber/MonoECS)
- Arch (high-performance C# ECS)

**When to use:** If you expect 100+ simultaneous entities or need high flexibility.

### Approach 3: Hybrid (Components Without Full ECS)

**Structure:**
- Entities are classes (like OOP)
- Entities contain component lists
- Components are behaviors/data
- No separate systems - components update themselves

**Pros:**
- More flexible than pure OOP
- Simpler than full ECS
- Good middle ground

**Cons:**
- Components updating themselves can create order dependencies
- Not as cache-friendly as ECS

**Code:**
```csharp
public class Entity
{
    public List<IComponent> Components { get; } = new();

    public void Update(GameTime gameTime)
    {
        foreach (var comp in Components)
            comp.Update(gameTime);
    }
}

// Example usage
var player = new Entity();
player.Components.Add(new TransformComponent { Position = Vector3.Zero });
player.Components.Add(new ModelComponent { Model = playerModel });
player.Components.Add(new HealthComponent { MaxHealth = 100 });
```

### Recommendation for Berzerk 3D:

**Start with Approach 1 (Simple OOP):**
- You have ~4-5 entity types (Player, Enemy, Projectile, Pickup, Wall)
- Inheritance 2 layers max: `Entity` → `Player/Enemy`
- Use components for shared data (HealthComponent, ColliderComponent) but keep them simple
- Migrate to ECS only if you add many more entity types or need performance optimization

## Sources

### HIGH Confidence (Official Documentation, Context7)
- [Understanding the Code | MonoGame](https://docs.monogame.net/articles/getting_started/3_understanding_the_code.html) - MonoGame Game class architecture
- [Chapter 17: Scene Management | MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/17_scenes/) - Official scene management tutorial
- [Chapter 12: Collision Detection | MonoGame](https://docs.monogame.net/articles/tutorials/building_2d_games/12_collision_detection/index.html) - Collision primitives
- [What Is Content? | MonoGame](https://docs.monogame.net/articles/getting_to_know/whatis/content_pipeline/CP_Overview.html) - Content Pipeline overview
- [Class Game | MonoGame](https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html) - Game class API
- [Class DrawableGameComponent | MonoGame](https://docs.monogame.net/api/Microsoft.Xna.Framework.DrawableGameComponent.html) - Component API

### MEDIUM Confidence (Community Verified, Multiple Sources)
- [3D Platformer Starter Kit announcement | MonoGame](https://monogame.net/blog/2025-07-16-3d-starter-kit/) - Recent official 3D game example (2025)
- [How best to structure a MonoGame porject? - Community](https://community.monogame.net/t/how-best-to-structure-a-monogame-porject/14437) - Community consensus on architecture
- [3D Engine architecture - Community](https://community.monogame.net/t/3d-engine-architecture/8056) - Deferred rendering discussion
- [Screen Management | MonoGame.Extended](https://www.monogameextended.net/docs/features/screen-management/) - Screen manager pattern
- [GameComponent vs custom class - Community](https://community.monogame.net/t/gamecomponent-vs-custom-class/7606) - When to use components
- [Monogame Components and Services | Gav's Dev Blog](https://gavsdevblog.wordpress.com/2016/09/04/monogame-components-and-services/) - Service locator pattern
- [Monogame and dependency injection – Aventius](https://aventius.co.uk/2024/03/23/monogame-and-dependency-injection/) - DI in MonoGame
- [GitHub - SimonDarksideJ/GameStateManagementSample](https://github.com/SimonDarksideJ/GameStateManagementSample) - Classic screen manager port
- [Fixed and Free 3D Camera Code Example - Community](https://community.monogame.net/t/fixed-and-free-3d-camera-code-example/11476) - Camera implementation
- [January 2025 Update | MonoGame.Extended](https://www.monogameextended.net/blog/update-2025-01/) - Collision system refactor
- [A* based pathfinding using triangular navigation meshes in C# - Community](https://community.monogame.net/t/a-based-pathfinding-using-triangular-navigation-meshes-in-c/11318) - Pathfinding implementation

### MEDIUM-LOW Confidence (Relevant, Single Source)
- [GitHub - Lofionic/MonoGameAnimatedModel](https://github.com/Lofionic/MonoGameAnimatedModel) - Skinned animation example
- [GitHub - jjenber/MonoECS](https://github.com/jjenber/MonoECS) - ECS for MonoGame
- [GitHub - CameronFraser/ecs](https://github.com/CameronFraser/ecs) - Scene + ECS
- [Importing 3D Models into the MonoGame Content Pipeline - RB Whitaker's Wiki](http://rbwhitaker.wikidot.com/forum/t-1673938/importing-3d-models-into-the-monogame-content-pipeline) - FBX import

---
*Architecture research for: MonoGame 3D Action Shooter (Berzerk 3D)*
*Researched: 2026-01-31*
