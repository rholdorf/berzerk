using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Berzerk.Source.Input;
using Berzerk.Source.Core;
using System;
using System.Collections.Generic;

namespace Berzerk.Source.Graphics;

/// <summary>
/// Third-person camera with smooth following, scroll wheel zoom, right-click orbit, and collision detection.
/// Implements frame-rate independent smoothing and distance-based angle transitions.
/// </summary>
public class ThirdPersonCamera
{
    private InputManager _inputManager;
    private Transform _target;  // Player's transform to follow

    // Camera state
    private Vector3 _currentPosition;
    private float _currentDistance = 5f;
    private float _currentYaw = 0f;   // Horizontal angle (radians)
    private float _currentPitch = 0f; // Vertical angle (radians)

    // Distance settings
    private const float MinDistance = 2f;
    private const float MaxDistance = 15f;
    private const float ZoomSpeed = 0.01f;  // Units per scroll delta

    // Angle settings (for distance-based angle transition)
    private const float MinPitch = 0f;           // Eye-level when close (radians)
    private const float MaxPitch = MathF.PI / 6; // 30 degrees down when far

    // Orbit settings (right-click drag)
    private const float OrbitSensitivity = 0.005f;
    private const float PitchMin = -MathF.PI / 4;  // Max look up (-45 degrees)
    private const float PitchMax = MathF.PI / 3;   // Max look down (60 degrees)

    // Smoothing
    private const float PositionDamping = 0.01f;  // Lower = more responsive
    private const float DistanceDamping = 0.05f;

    // Collision
    private List<BoundingBox> _collisionGeometry = new();
    private const float CollisionOffset = 0.3f;  // Don't clip exactly at surface

    // Output matrices
    public Matrix ViewMatrix { get; private set; }
    public Matrix ProjectionMatrix { get; private set; }

    public ThirdPersonCamera(InputManager inputManager, Transform target)
    {
        _inputManager = inputManager;
        _target = target;
        _currentPosition = target.Position + GetDesiredOffset();
    }

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            graphicsDevice.Viewport.AspectRatio,
            0.1f,
            1000f
        );
    }

    public void SetCollisionGeometry(List<BoundingBox> geometry)
    {
        _collisionGeometry = geometry;
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // 1. Handle zoom (scroll wheel)
        HandleZoom();

        // 2. Handle orbit (right-click drag)
        HandleOrbit(deltaTime);

        // 3. Calculate automatic pitch based on distance
        UpdateAutoPitch();

        // 4. Calculate desired camera position
        Vector3 desiredPosition = _target.Position + GetDesiredOffset();

        // 5. Apply collision detection
        float finalDistance = CheckCollision(_target.Position, desiredPosition);
        Vector3 collisionAdjustedPosition = _target.Position + GetOffsetAtDistance(finalDistance);

        if (finalDistance != _currentDistance)
        {
            Console.WriteLine($"Update: Collision adjustment applied - finalDistance={finalDistance}, currentDistance={_currentDistance}");
        }

        // 6. Smooth camera position (exponential decay lerp)
        float smoothFactor = 1f - MathF.Pow(PositionDamping, deltaTime);
        _currentPosition = Vector3.Lerp(_currentPosition, collisionAdjustedPosition, smoothFactor);

        // 7. Update view matrix
        Vector3 lookAt = _target.Position + new Vector3(0, 1f, 0); // Look at player chest height
        ViewMatrix = Matrix.CreateLookAt(_currentPosition, lookAt, Vector3.Up);
    }

    private void HandleZoom()
    {
        int scrollDelta = _inputManager.ScrollWheelDelta;
        if (scrollDelta != 0)
        {
            // Scroll up = zoom in (decrease distance), scroll down = zoom out
            _currentDistance -= scrollDelta * ZoomSpeed;
            _currentDistance = MathHelper.Clamp(_currentDistance, MinDistance, MaxDistance);
        }
    }

    private void HandleOrbit(float deltaTime)
    {
        if (_inputManager.IsRightMouseHeld())
        {
            var mouseDelta = _inputManager.MouseDelta;

            // Horizontal: yaw (rotate around Y axis)
            _currentYaw -= mouseDelta.X * OrbitSensitivity;

            // Vertical: pitch (limited up/down)
            _currentPitch += mouseDelta.Y * OrbitSensitivity;
            _currentPitch = MathHelper.Clamp(_currentPitch, PitchMin, PitchMax);
        }
    }

    private void UpdateAutoPitch()
    {
        // When not manually orbiting, blend pitch based on distance
        // Close = more eye-level, Far = more top-down
        if (!_inputManager.IsRightMouseHeld())
        {
            float distanceNormalized = (_currentDistance - MinDistance) / (MaxDistance - MinDistance);
            float autoPitch = MathHelper.Lerp(MinPitch, MaxPitch, distanceNormalized);

            // Blend toward auto pitch slowly when not orbiting
            _currentPitch = MathHelper.Lerp(_currentPitch, autoPitch, 0.05f);
        }
    }

    private Vector3 GetDesiredOffset()
    {
        return GetOffsetAtDistance(_currentDistance);
    }

    private Vector3 GetOffsetAtDistance(float distance)
    {
        // Spherical coordinates to Cartesian
        // Yaw: rotation around Y axis, Pitch: angle from horizontal
        float horizontalDist = distance * MathF.Cos(_currentPitch);
        float verticalDist = distance * MathF.Sin(_currentPitch);

        Vector3 offset = new Vector3(
            horizontalDist * MathF.Sin(_currentYaw),
            verticalDist + 1f,  // +1 for player height offset
            horizontalDist * MathF.Cos(_currentYaw)
        );

        return offset;
    }

    private float CheckCollision(Vector3 playerPos, Vector3 desiredCameraPos)
    {
        if (_collisionGeometry.Count == 0)
            return _currentDistance;

        Vector3 direction = desiredCameraPos - playerPos;
        float desiredDistance = direction.Length();

        if (desiredDistance < 0.001f)
            return _currentDistance;

        direction = Vector3.Normalize(direction);

        // Offset the ray start slightly above player position to avoid ground collision
        Vector3 rayStart = playerPos + new Vector3(0, 1f, 0);
        Ray ray = new Ray(rayStart, direction);

        float closestHit = desiredDistance;
        bool hitDetected = false;

        foreach (var box in _collisionGeometry)
        {
            float? intersection = ray.Intersects(box);
            if (intersection.HasValue && intersection.Value > 0 && intersection.Value < closestHit)
            {
                Console.WriteLine($"Collision detected at distance {intersection.Value}");
                closestHit = intersection.Value;
                hitDetected = true;
            }
        }

        Console.WriteLine($"CheckCollision: hitDetected={hitDetected}, closestHit={closestHit}, desiredDistance={desiredDistance}");

        // If no collision detected, allow camera to move to desired distance
        if (!hitDetected)
        {
            return desiredDistance;
        }

        // Apply collision offset (don't clip exactly at surface)
        float collisionDistance = MathF.Max(closestHit - CollisionOffset, MinDistance);

        // Smooth zoom back out when collision clears
        float distanceSmoothFactor = 1f - MathF.Pow(DistanceDamping, 0.016f); // Assume ~60fps for distance smooth
        return MathHelper.Lerp(_currentDistance, collisionDistance, distanceSmoothFactor);
    }

    /// <summary>
    /// Creates test collision geometry for camera collision testing.
    /// Call this during initialization to set up placeholder walls.
    /// </summary>
    public static List<BoundingBox> CreateTestWalls()
    {
        return new List<BoundingBox>
        {
            // Left wall
            new BoundingBox(new Vector3(-15, 0, -15), new Vector3(-14, 5, 15)),
            // Right wall
            new BoundingBox(new Vector3(14, 0, -15), new Vector3(15, 5, 15)),
            // Back wall
            new BoundingBox(new Vector3(-15, 0, -15), new Vector3(15, 5, -14)),
            // Front wall
            new BoundingBox(new Vector3(-15, 0, 14), new Vector3(15, 5, 15)),
            // Center pillar (for testing collision while moving)
            new BoundingBox(new Vector3(-1, 0, -8), new Vector3(1, 4, -6)),
        };
    }
}
