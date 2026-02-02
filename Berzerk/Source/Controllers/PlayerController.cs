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
    private const float RotationSpeed = 3f;    // Radians per second for tank rotation

    private Vector3 _velocity = Vector3.Zero;
    private InputManager _inputManager;

    public bool IsMoving => _velocity.LengthSquared() > 0.01f;

    public PlayerController(InputManager inputManager)
    {
        Transform = new Transform();
        _inputManager = inputManager;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. Handle rotation input (A/D keys)
        HandleRotation(deltaTime);

        // 2. Get movement direction (W/S/Q/E keys) relative to character facing
        Vector3 inputDir = GetInputDirection();

        // 3. Calculate target velocity
        Vector3 targetVelocity = inputDir * MoveSpeed;

        // 4. Apply snappy acceleration/deceleration
        float accel = inputDir.LengthSquared() > 0 ? Acceleration : Deceleration;
        _velocity = Vector3.Lerp(_velocity, targetVelocity, accel * deltaTime);

        // 5. Apply velocity to position
        Transform.Position += _velocity * deltaTime;
    }

    private void HandleRotation(float deltaTime)
    {
        float rotationInput = 0f;
        if (_inputManager.IsKeyHeld(Keys.A)) rotationInput += 1f;  // Changed to +1 for counterclockwise (left)
        if (_inputManager.IsKeyHeld(Keys.D)) rotationInput -= 1f;  // Changed to -1 for clockwise (right)

        if (Math.Abs(rotationInput) > 0.01f)
        {
            float rotationAmount = rotationInput * RotationSpeed * deltaTime;
            Quaternion rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, rotationAmount);
            Transform.Rotation = Quaternion.Normalize(Transform.Rotation * rotation);
        }
    }

    private Vector3 GetInputDirection()
    {
        Vector3 localDir = Vector3.Zero;

        // Forward/backward
        if (_inputManager.IsKeyHeld(Keys.W)) localDir.Z -= 1f;
        if (_inputManager.IsKeyHeld(Keys.S)) localDir.Z += 1f;

        // Strafe left/right
        if (_inputManager.IsKeyHeld(Keys.Q)) localDir.X -= 1f;
        if (_inputManager.IsKeyHeld(Keys.E)) localDir.X += 1f;

        // Normalize diagonal movement
        if (localDir.LengthSquared() > 0)
            localDir = Vector3.Normalize(localDir);

        // Transform from local space to world space using character rotation
        return Vector3.Transform(localDir, Transform.Rotation);
    }
}
