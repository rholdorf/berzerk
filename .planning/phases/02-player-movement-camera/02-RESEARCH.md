# Phase 2: Player Movement & Camera - Research

**Researched:** 2026-02-01
**Domain:** MonoGame 3D character controller and third-person camera system
**Confidence:** MEDIUM

## Summary

Third-person character controllers in MonoGame require implementing custom camera and movement systems, as MonoGame provides only the foundational 3D math primitives (Vector3, Matrix, Quaternion) without high-level character controller components. The standard approach uses Matrix-based cameras with View/Projection separation, Vector3.Lerp or spring-damper systems for smooth following, and Ray/BoundingFrustum intersection tests for collision detection.

MonoGame's input system already exists in the project via InputManager (Phase 1), providing keyboard and mouse polling. Character movement combines WASD input with Vector3 transformations, while camera systems maintain position/rotation state and generate view matrices using Matrix.CreateLookAt(). Smooth camera following uses interpolation (Lerp, SmoothStep) or spring-damper algorithms for frame-rate independent motion. Camera collision detection uses Viewport.Unproject to create rays for raycasting against scene geometry.

The primary challenge is that MonoGame lacks built-in character controller or camera components—everything must be hand-rolled using math primitives. However, the framework provides excellent support for the underlying operations: quaternion rotation (avoiding gimbal lock), matrix transformations, bounding volume intersections, and viewport unprojection for screen-to-world ray casting.

**Primary recommendation:** Implement separate Camera and PlayerController classes using Matrix.CreateLookAt for view generation, Quaternion.Slerp for smooth rotation, Vector3.Lerp or spring-damper for position smoothing, and Ray.Intersects with scene bounding volumes for collision detection.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework | 3.8.4.1 | 3D math primitives and input | Project foundation from Phase 1 |
| System.Numerics | .NET 8 | Supplemental math (if needed) | .NET standard library |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| InputManager | Custom | Keyboard/mouse state tracking | Already implemented in Phase 1 |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom camera | MonoGame.Extended Camera | Extended only provides 2D orthographic camera, not 3D perspective |
| Hand-rolled spring damper | Unity-style SmoothDamp port | MonoGame has no built-in SmoothDamp; must implement or use simpler Lerp |
| Custom character controller | Physics engine (e.g., Jitter) | Overkill for arcade-style movement; adds complexity for simple WASD |

**Installation:**
No additional packages needed beyond Phase 1 setup (MonoGame.Framework 3.8.4.1).

## Architecture Patterns

### Recommended Project Structure
```
Berzerk/Source/
├── Camera/              # Camera system
│   └── Camera3D.cs      # View/projection matrix management
├── Player/              # Player character controller
│   ├── PlayerController.cs   # Movement and rotation logic
│   └── PlayerCamera.cs       # Camera following player with collision
└── Input/               # Already exists from Phase 1
    └── InputManager.cs  # Keyboard/mouse polling
```

### Pattern 1: Matrix-Based Camera with View/Projection Separation
**What:** Camera maintains Position, Target (LookAt point), and Up vector. Generates View matrix via Matrix.CreateLookAt() and Projection matrix via Matrix.CreatePerspectiveFieldOfView(). These matrices are passed to rendering code.

**When to use:** All 3D MonoGame rendering requires view and projection matrices. This is the standard pattern.

**Example:**
```csharp
// Source: MonoGame community - https://community.monogame.net/t/fixed-and-free-3d-camera-code-example/11476
public class Camera3D
{
    public Vector3 Position { get; set; }
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; } = Vector3.Up;

    public Matrix View => Matrix.CreateLookAt(Position, Target, Up);
    public Matrix Projection { get; private set; }

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        // Cast to float to avoid integer truncation
        float aspectRatio = (float)graphicsDevice.Viewport.Width / graphicsDevice.Viewport.Height;
        Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,   // 45-degree FOV
            aspectRatio,
            0.1f,                  // Near plane
            1000f                  // Far plane
        );
    }

    public void Update(Vector3 newPosition, Vector3 newTarget)
    {
        Position = newPosition;
        Target = newTarget;
    }
}
```

### Pattern 2: Smooth Camera Following with Interpolation
**What:** Camera position interpolates toward desired position each frame using Vector3.Lerp or spring-damper algorithm. Prevents jarring camera snaps.

**When to use:** Third-person cameras that follow player. User requested "light spring smoothing" for camera follow.

**Example:**
```csharp
// Source: MonoGame API docs - https://docs.monogame.net/api/Microsoft.Xna.Framework.Vector3.html
public class PlayerCamera
{
    private Vector3 _currentPosition;
    private Vector3 _currentTarget;

    // Lerp-based smoothing (ease-out motion)
    public void UpdateSmooth(Vector3 desiredPosition, Vector3 desiredTarget, float deltaTime)
    {
        // Smoothing factor 0.1 = camera moves 10% of remaining distance per frame
        // For frame-rate independence, use: 1 - MathF.Pow(0.1f, deltaTime)
        float smoothFactor = 1 - MathF.Pow(0.1f, deltaTime);

        _currentPosition = Vector3.Lerp(_currentPosition, desiredPosition, smoothFactor);
        _currentTarget = Vector3.Lerp(_currentTarget, desiredTarget, smoothFactor);
    }

    // Alternative: SmoothStep for cubic interpolation (smoother than Lerp)
    public void UpdateSmoothStep(Vector3 desiredPosition, Vector3 desiredTarget, float amount)
    {
        _currentPosition = Vector3.SmoothStep(_currentPosition, desiredPosition, amount);
        _currentTarget = Vector3.SmoothStep(_currentTarget, desiredTarget, amount);
    }
}
```

### Pattern 3: Character Rotation Toward Movement Direction
**What:** Player character rotates to face movement direction using Quaternion.Slerp for smooth rotation. Avoids gimbal lock and instant snapping.

**When to use:** Character controllers where character faces the direction they're moving. User specified "player character faces movement direction."

**Example:**
```csharp
// Source: MonoGame Quaternion docs - https://docs.monogame.net/api/Microsoft.Xna.Framework.Quaternion.html
// Combined with community pattern - https://community.monogame.net/t/solved-3d-rotation-slerp-towards-target/10997
public class PlayerController
{
    private Quaternion _currentRotation = Quaternion.Identity;
    private Vector3 _position;

    public void UpdateRotation(Vector3 movementDirection, float deltaTime)
    {
        if (movementDirection.LengthSquared() > 0.001f)
        {
            // Create target rotation from movement direction
            movementDirection.Normalize();
            Matrix rotationMatrix = Matrix.CreateWorld(_position, movementDirection, Vector3.Up);
            Quaternion targetRotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);

            // Smooth rotation using Slerp
            float rotationSpeed = 8.0f; // Adjust for desired rotation speed
            float slerpAmount = MathHelper.Clamp(deltaTime * rotationSpeed, 0f, 1f);
            _currentRotation = Quaternion.Slerp(_currentRotation, targetRotation, slerpAmount);
        }
    }
}
```

### Pattern 4: Camera Collision Detection via Raycasting
**What:** Cast a ray from player position to desired camera position. If collision detected, move camera to collision point minus offset.

**When to use:** Preventing camera clipping through walls. User requires "camera collision detection (no clipping through walls)."

**Example:**
```csharp
// Source: MonoGame collision docs - https://docs.monogame.net/articles/getting_to_know/howto/HowTo_CollisionDetectionOverview.html
public Vector3 CalculateCameraPosition(Vector3 playerPosition, Vector3 desiredOffset, List<BoundingBox> sceneGeometry)
{
    Vector3 desiredPosition = playerPosition + desiredOffset;

    // Create ray from player to desired camera position
    Vector3 direction = desiredPosition - playerPosition;
    float distance = direction.Length();
    direction.Normalize();
    Ray ray = new Ray(playerPosition, direction);

    // Test against all scene geometry
    float? closestCollision = null;
    foreach (var geometry in sceneGeometry)
    {
        float? intersection = ray.Intersects(geometry);
        if (intersection.HasValue && intersection.Value < distance)
        {
            if (!closestCollision.HasValue || intersection.Value < closestCollision.Value)
            {
                closestCollision = intersection.Value;
            }
        }
    }

    // If collision, place camera at collision point minus small offset
    if (closestCollision.HasValue)
    {
        float offset = 0.3f; // Prevent clipping into wall
        return playerPosition + direction * MathHelper.Max(closestCollision.Value - offset, 0.5f);
    }

    return desiredPosition;
}
```

### Pattern 5: Screen-to-World Ray Casting for Mouse Aiming
**What:** Convert mouse cursor screen coordinates to 3D ray using Viewport.Unproject. Used for aiming in 3D space.

**When to use:** Mouse-based aiming where cursor controls shooting direction. User requires "free vertical aiming (mouse Y-axis controls pitch for aiming up/down)."

**Example:**
```csharp
// Source: MonoGame docs - https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Select_an_Object_with_a_Mouse.html
public Ray GetMouseRay(MouseState mouseState, Viewport viewport, Matrix view, Matrix projection)
{
    // Screen-space points at near and far clipping planes
    Vector3 nearSource = new Vector3(mouseState.X, mouseState.Y, 0f);
    Vector3 farSource = new Vector3(mouseState.X, mouseState.Y, 1f);

    Matrix world = Matrix.Identity;

    // Unproject to world space
    Vector3 nearPoint = viewport.Unproject(nearSource, projection, view, world);
    Vector3 farPoint = viewport.Unproject(farSource, projection, view, world);

    // Create normalized ray
    Vector3 direction = farPoint - nearPoint;
    direction.Normalize();

    return new Ray(nearPoint, direction);
}
```

### Anti-Patterns to Avoid
- **Calling GetState() multiple times per frame:** InputManager already handles this correctly (Phase 1). Never bypass it.
- **Using Euler angles for camera rotation:** Causes gimbal lock when looking straight up/down. Use Quaternion or Matrix.CreateLookAt instead.
- **Lerp with constant amount (0.1f) every frame:** Not frame-rate independent. Use `1 - MathF.Pow(smoothFactor, deltaTime)` for consistent motion at any framerate.
- **Creating new matrices every frame without caching:** View/Projection should be properties that create on access, not stored fields (memory vs. computation tradeoff, but matrices are lightweight to recompute).

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Frame-rate independent interpolation | Manual deltaTime multiplication | `1 - MathF.Pow(factor, deltaTime)` pattern | Lerp with constant factor is frame-rate dependent; exponential decay is correct |
| Spring-damper smoothing from scratch | Custom physics simulation | Simplified exponential smoothing or Vector3.SmoothStep | Full spring-mass-damper requires second-order differential equations; overkill for camera smoothing |
| Scroll wheel delta tracking | Manual subtraction each frame | InputManager pattern (store previous, compare current) | Already established in Phase 1; extend InputManager with ScrollWheelDelta property |
| Angle wrapping for rotations | Manual modulo arithmetic | MathHelper.WrapAngle() | Built-in MonoGame utility handles edge cases correctly |
| Screen-to-world conversion | Manual matrix inversion | Viewport.Unproject() | MonoGame's built-in method handles all matrix math correctly |

**Key insight:** MonoGame provides excellent math primitives but no high-level components. The "don't hand-roll" items are low-level math utilities that MonoGame already provides (WrapAngle, Unproject) or well-established patterns (exponential decay for frame-rate independence). Don't try to build physics engines or advanced spring systems—use simpler interpolation for this arcade-style game.

## Common Pitfalls

### Pitfall 1: Gimbal Lock in Camera Rotation
**What goes wrong:** When using Euler angles (pitch/yaw/roll) for camera rotation, looking straight up or down causes two rotation axes to align, losing one degree of freedom. Camera rotation becomes unpredictable or "locks" at 90-degree angles.

**Why it happens:** Euler angle rotations are applied sequentially (rotate X, then Y, then Z). At extreme angles, axes overlap.

**How to avoid:** Use Matrix.CreateLookAt() for cameras instead of Euler angles. The LookAt pattern uses position, target, and up vectors—no gimbal lock possible. For character rotation, use Quaternion.Slerp.

**Warning signs:** Camera rotation becomes jerky or unresponsive when looking straight up/down. Rotation suddenly "flips" 180 degrees.

### Pitfall 2: Frame-Rate Dependent Smoothing
**What goes wrong:** Using constant Lerp amount (e.g., `Vector3.Lerp(current, target, 0.1f)`) produces different motion speeds at different framerates. At 60 FPS, camera moves slowly; at 30 FPS, camera moves faster.

**Why it happens:** Lerp with constant amount closes 10% of the gap each frame. More frames = more lerp steps = slower overall motion.

**How to avoid:** Use exponential decay formula: `float t = 1 - MathF.Pow(smoothFactor, deltaTime)`. This produces consistent motion regardless of framerate.

**Warning signs:** Camera feels sluggish on high-refresh monitors, too fast on slower machines. Motion isn't smooth across different hardware.

### Pitfall 3: Scroll Wheel Value Misunderstanding
**What goes wrong:** Treating MouseState.ScrollWheelValue as a delta when it's actually cumulative since game start. Reading it directly produces incorrect zoom behavior.

**Why it happens:** MonoGame's ScrollWheelValue is the total rotation since launch, not the change since last frame. Must calculate delta manually.

**How to avoid:** Store previous frame's scroll value, subtract from current: `int scrollDelta = currentMouse.ScrollWheelValue - previousMouse.ScrollWheelValue`. Extend InputManager with ScrollWheelDelta property.

**Warning signs:** Scroll wheel zoom accelerates over time instead of producing consistent zoom increments. Zoom behavior is erratic.

### Pitfall 4: Integer Truncation in Aspect Ratio
**What goes wrong:** Creating projection matrix with `viewport.Width / viewport.Height` produces aspect ratio of 1.0 regardless of actual window size, causing stretched/squashed rendering.

**Why it happens:** Integer division in C# truncates to integer result. 1920 / 1080 = 1 (not 1.777).

**How to avoid:** Cast to float before division: `float aspectRatio = (float)viewport.Width / viewport.Height`.

**Warning signs:** Scene appears stretched horizontally or vertically. Circles render as ellipses. 3D objects look squashed.

### Pitfall 5: Camera-Player Collision (Camera Inside Player Model)
**What goes wrong:** When camera is very close to player, it clips inside the player's model, showing backface culling artifacts or rendering the inside of the mesh.

**Why it happens:** Camera collision detection only checks world geometry, not the player's own bounding volume.

**How to avoid:** Enforce minimum camera distance from player position. Clamp camera distance to never go below threshold (e.g., 1.5 units).

**Warning signs:** Player model disappears when zooming in close. See inside of character mesh. Weird triangle flickering at close zoom.

### Pitfall 6: Camera Collision Ray Direction Normalization
**What goes wrong:** Forgot to normalize ray direction after calculating farPoint - nearPoint, causing intersection distances to be incorrect.

**Why it happens:** Ray.Intersects returns distance along ray direction. If direction isn't unit length, distances are scaled incorrectly.

**How to avoid:** Always call `direction.Normalize()` after creating direction vectors for rays.

**Warning signs:** Camera collision stops working at certain distances. Collision detection is inconsistent.

## Code Examples

Verified patterns from official sources:

### Extending InputManager for Scroll Wheel Delta
```csharp
// Source: MonoGame MouseState API - https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.MouseState.html
// Add to existing InputManager class from Phase 1
public class InputManager
{
    // ... existing code from Phase 1 ...

    /// <summary>
    /// Get scroll wheel delta this frame (positive = scrolled up, negative = scrolled down).
    /// Typically changes in increments of 120.
    /// </summary>
    public int ScrollWheelDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

    /// <summary>
    /// Check if mouse left button is held down (useful for camera drag).
    /// </summary>
    public bool IsLeftMouseHeld() => _currentMouse.LeftButton == ButtonState.Pressed;
}
```

### Complete Third-Person Camera with Collision and Smoothing
```csharp
// Synthesis of MonoGame patterns
public class ThirdPersonCamera
{
    private Vector3 _currentPosition;
    private Vector3 _currentTarget;
    private float _currentDistance = 5.0f;
    private float _currentAngle = MathHelper.PiOver4; // 45 degrees

    public Matrix View { get; private set; }
    public Matrix Projection { get; private set; }

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        float aspectRatio = (float)graphicsDevice.Viewport.Width / graphicsDevice.Viewport.Height;
        Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4, aspectRatio, 0.1f, 1000f);
    }

    public void Update(Vector3 playerPosition, float scrollDelta, float deltaTime, List<BoundingBox> sceneGeometry)
    {
        // Update camera distance from scroll wheel
        float zoomSpeed = 0.01f;
        _currentDistance -= scrollDelta * zoomSpeed;
        _currentDistance = MathHelper.Clamp(_currentDistance, 2.0f, 10.0f);

        // Camera angle transitions based on distance (eye-level close, high angle far)
        float targetAngle = MathHelper.Lerp(0.2f, MathHelper.PiOver4,
            (_currentDistance - 2.0f) / 8.0f);
        _currentAngle = MathHelper.Lerp(_currentAngle, targetAngle, deltaTime * 5.0f);

        // Calculate desired camera offset
        Vector3 offset = new Vector3(0, _currentDistance * MathF.Sin(_currentAngle),
            -_currentDistance * MathF.Cos(_currentAngle));

        // Apply collision detection
        Vector3 desiredPosition = playerPosition + offset;
        Vector3 collisionSafePosition = CalculateCameraPosition(playerPosition, offset, sceneGeometry);

        // Smooth interpolation
        float smoothFactor = 1 - MathF.Pow(0.1f, deltaTime);
        _currentPosition = Vector3.Lerp(_currentPosition, collisionSafePosition, smoothFactor);
        _currentTarget = Vector3.Lerp(_currentTarget, playerPosition + Vector3.Up, smoothFactor);

        // Generate view matrix
        View = Matrix.CreateLookAt(_currentPosition, _currentTarget, Vector3.Up);
    }

    private Vector3 CalculateCameraPosition(Vector3 playerPosition, Vector3 desiredOffset,
        List<BoundingBox> sceneGeometry)
    {
        Vector3 desiredPosition = playerPosition + desiredOffset;
        Vector3 direction = desiredPosition - playerPosition;
        float distance = direction.Length();
        direction.Normalize();
        Ray ray = new Ray(playerPosition, direction);

        float? closestCollision = null;
        foreach (var geometry in sceneGeometry)
        {
            float? intersection = ray.Intersects(geometry);
            if (intersection.HasValue && intersection.Value < distance)
            {
                if (!closestCollision.HasValue || intersection.Value < closestCollision.Value)
                    closestCollision = intersection.Value;
            }
        }

        if (closestCollision.HasValue)
        {
            return playerPosition + direction * MathHelper.Max(closestCollision.Value - 0.3f, 1.5f);
        }

        return desiredPosition;
    }
}
```

### WASD Character Movement with Smooth Rotation
```csharp
// MonoGame community pattern adapted
public class PlayerController
{
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.Identity;
    private float _moveSpeed = 5.0f;

    public Vector3 Position => _position;
    public Matrix World => Matrix.CreateFromQuaternion(_rotation) * Matrix.CreateTranslation(_position);

    public void Update(InputManager input, float deltaTime)
    {
        // Gather WASD input
        Vector3 moveDirection = Vector3.Zero;

        if (input.IsKeyHeld(Keys.W)) moveDirection += Vector3.Forward;
        if (input.IsKeyHeld(Keys.S)) moveDirection += Vector3.Backward;
        if (input.IsKeyHeld(Keys.A)) moveDirection += Vector3.Left;
        if (input.IsKeyHeld(Keys.D)) moveDirection += Vector3.Right;

        // Apply movement
        if (moveDirection.LengthSquared() > 0)
        {
            moveDirection.Normalize();

            // Snappy acceleration - instant full speed (user requested snappy, not gradual ramp)
            _position += moveDirection * _moveSpeed * deltaTime;

            // Smooth rotation toward movement direction
            Matrix rotationMatrix = Matrix.CreateWorld(_position, moveDirection, Vector3.Up);
            Quaternion targetRotation = Quaternion.CreateFromRotationMatrix(rotationMatrix);
            float rotationSpeed = 10.0f; // Quick rotation blend
            float slerpAmount = MathHelper.Clamp(deltaTime * rotationSpeed, 0f, 1f);
            _rotation = Quaternion.Slerp(_rotation, targetRotation, slerpAmount);
        }
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Fixed camera | Smooth interpolated following | Ongoing trend | User expects smooth modern camera motion |
| Euler angle rotation | Quaternion/Matrix-based rotation | Established best practice | Avoids gimbal lock, smoother interpolation |
| Frame-locked lerp | Exponential decay with deltaTime | Modern game dev standard | Consistent motion across different framerates |
| Simple Lerp | Spring-damper systems | 2020s advanced cameras | More natural physics-based motion (but overkill for arcade game) |

**Deprecated/outdated:**
- XNA Camera tutorials using Euler angles: Modern approach uses Matrix.CreateLookAt or Quaternion to avoid gimbal lock
- MonoGame.Extended Camera: Only supports 2D orthographic cameras, not 3D perspective cameras needed for this project

## Open Questions

Things that couldn't be fully resolved:

1. **Optimal spring-damper vs. simple Lerp for camera smoothing**
   - What we know: User requested "light spring smoothing," spring-damper is more realistic but complex
   - What's unclear: Whether simple exponential decay (Lerp-based) provides acceptable "spring-like" feel, or if full spring-mass-damper system is needed
   - Recommendation: Start with exponential decay Lerp (simpler, frame-rate independent). Only implement spring-damper if motion feels too linear. Full spring system is likely overkill for arcade game.

2. **Camera collision with complex geometry**
   - What we know: Ray.Intersects works with BoundingBox, BoundingSphere, BoundingFrustum
   - What's unclear: How to efficiently get scene geometry bounding volumes for collision tests in Phase 2 (no full level geometry yet)
   - Recommendation: Start with placeholder BoundingBox list representing walls. Phase 4 (level geometry) will provide actual collision geometry. For now, manually create test boxes.

3. **Mouse cursor visibility during aiming**
   - What we know: User wants "crosshair on screen for aiming visual"
   - What's unclear: Whether to hide OS cursor and draw custom crosshair, or leave OS cursor visible with crosshair overlay
   - Recommendation: Hide OS cursor (`Mouse.SetCursor(MouseCursor.FromTexture2D(...))` or `IsMouseVisible = false`) and draw custom crosshair sprite. More professional appearance.

4. **Camera rotation control via mouse**
   - What we know: User specified "free camera rotation controlled by mouse or keys"
   - What's unclear: Whether mouse rotation is right-click drag, always-on mouse delta, or specific key modifier
   - Recommendation: Right-click drag for camera orbit (common third-person pattern). Mouse movement without right-click controls aiming direction only.

## Sources

### Primary (HIGH confidence)
- MonoGame API Documentation - Vector3 methods: https://docs.monogame.net/api/Microsoft.Xna.Framework.Vector3.html
- MonoGame API Documentation - Quaternion methods: https://docs.monogame.net/api/Microsoft.Xna.Framework.Quaternion.html
- MonoGame API Documentation - MathHelper utilities: https://docs.monogame.net/api/Microsoft.Xna.Framework.MathHelper.html
- MonoGame API Documentation - BoundingFrustum collision: https://docs.monogame.net/api/Microsoft.Xna.Framework.BoundingFrustum.html
- MonoGame API Documentation - MouseState input: https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.MouseState.html
- MonoGame Documentation - Collision Detection Overview: https://docs.monogame.net/articles/getting_to_know/howto/HowTo_CollisionDetectionOverview.html
- MonoGame Documentation - Mouse Object Selection: https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Select_an_Object_with_a_Mouse.html

### Secondary (MEDIUM confidence)
- MonoGame Community - Fixed and Free 3D Camera Code Example: https://community.monogame.net/t/fixed-and-free-3d-camera-code-example/11476
- MonoGame Community - 3D Rotation Slerp Toward Target: https://community.monogame.net/t/solved-3d-rotation-slerp-towards-target/10997
- Game Developer - Accurate Collision Zoom for Cameras: https://www.gamedeveloper.com/programming/accurate-collision-zoom-for-cameras
- The BitBull Blog - Lerp Smoothing Tutorial: https://blog.bitbull.com/2019/11/13/a-lerp-smoothing-tutorial-and-example-code/
- Ryan Juckett - Damped Springs: https://www.ryanjuckett.com/damped-springs/
- Generalist Programmer - Game Camera Systems Guide 2025: https://generalistprogrammer.com/tutorials/game-camera-systems-complete-programming-guide-2025

### Tertiary (LOW confidence - flagged for validation)
- WebSearch results on spring-damper smoothing implementation (multiple sources agree on exponential decay pattern, but specific coefficients vary)
- WebSearch results on third-person camera design patterns (architectural patterns not specific to MonoGame, but general game dev wisdom)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - MonoGame 3.8.4.1 is established from Phase 1, no additional packages needed
- Architecture patterns: HIGH - Matrix.CreateLookAt, Quaternion.Slerp, Vector3.Lerp are verified in official MonoGame API docs
- Common pitfalls: HIGH - Gimbal lock, frame-rate dependence, scroll wheel delta are well-documented issues with verified solutions
- Spring damper vs. Lerp: LOW - Unclear which approach provides requested "light spring smoothing" without prototyping
- Camera collision complexity: MEDIUM - Ray.Intersects pattern is verified, but integration with scene geometry depends on Phase 4 level work

**Research date:** 2026-02-01
**Valid until:** 2026-03-01 (30 days - MonoGame is stable, API unlikely to change)
