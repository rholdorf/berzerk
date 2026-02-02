using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Core;
using Berzerk.Source.Input;
using System;

namespace Berzerk.Controllers;

/// <summary>
/// Player controller with WASD movement and rotation toward movement direction.
/// Implements snappy acceleration for arcade-style responsiveness.
/// </summary>
public class PlayerController
{
    public Transform Transform { get; }

    // Movement settings
    private const float MoveSpeed = 5f;        // Units per second
    private const float Acceleration = 20f;    // How fast to reach full speed
    private const float Deceleration = 15f;    // How fast to stop
    private const float RotationSpeed = 10f;   // Radians per second for rotation blend

    private Vector3 _velocity = Vector3.Zero;
    private InputManager _inputManager;

    public PlayerController(InputManager inputManager)
    {
        Transform = new Transform();
        _inputManager = inputManager;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. Gather input direction (WASD)
        Vector3 inputDir = GetInputDirection();

        // 2. Calculate target velocity
        Vector3 targetVelocity = inputDir * MoveSpeed;

        // 3. Apply snappy acceleration/deceleration
        float accel = inputDir.LengthSquared() > 0 ? Acceleration : Deceleration;
        _velocity = Vector3.Lerp(_velocity, targetVelocity, accel * deltaTime);

        // 4. Apply velocity to position
        Transform.Position += _velocity * deltaTime;

        // 5. Rotate toward movement direction (only if moving)
        if (_velocity.LengthSquared() > 0.01f)
        {
            RotateToward(_velocity, deltaTime);
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector3 dir = Vector3.Zero;

        if (_inputManager.IsKeyHeld(Keys.W)) dir += Vector3.Forward;  // -Z in MonoGame
        if (_inputManager.IsKeyHeld(Keys.S)) dir += Vector3.Backward; // +Z
        if (_inputManager.IsKeyHeld(Keys.A)) dir += Vector3.Left;     // -X
        if (_inputManager.IsKeyHeld(Keys.D)) dir += Vector3.Right;    // +X

        // Normalize to prevent faster diagonal movement
        if (dir.LengthSquared() > 0)
            dir = Vector3.Normalize(dir);

        return dir;
    }

    private void RotateToward(Vector3 direction, float deltaTime)
    {
        // Calculate target rotation from direction
        direction.Y = 0; // Keep rotation on Y axis only
        if (direction.LengthSquared() < 0.001f) return;

        direction = Vector3.Normalize(direction);

        // Create target rotation looking in movement direction
        // Use Atan2 to calculate yaw angle from direction vector
        // Negate both components because model front faces -Z axis
        float targetYaw = MathF.Atan2(-direction.X, -direction.Z);
        Quaternion targetRotation = Quaternion.CreateFromAxisAngle(Vector3.Up, targetYaw);

        // Slerp for smooth rotation blend
        float smoothFactor = 1f - MathF.Pow(0.001f, RotationSpeed * deltaTime);
        Transform.Rotation = Quaternion.Slerp(Transform.Rotation, targetRotation, smoothFactor);
    }
}
