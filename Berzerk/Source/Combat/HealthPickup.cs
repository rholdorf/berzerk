using Microsoft.Xna.Framework;
using System;

namespace Berzerk.Source.Combat;

/// <summary>
/// Collectable health pickup that floats above ground with bobbing animation.
/// Auto-collects when player enters collection radius.
/// Mirrors AmmoPickup pattern for consistency.
/// </summary>
public class HealthPickup
{
    public Vector3 Position { get; private set; }
    public bool IsActive { get; private set; }
    public int HealAmount { get; private set; } = 25; // Reasonable heal for 10 HP attacks
    public float CollectRadius { get; private set; } = 2f; // Same as AmmoPickup

    private float _bobTime = 0f;
    private const float BOB_SPEED = 3f;
    private const float BOB_HEIGHT = 0.3f;

    public HealthPickup(Vector3 position)
    {
        Position = position;
        IsActive = true;
    }

    /// <summary>
    /// Activate pickup at specified position (for pooling).
    /// </summary>
    public void Activate(Vector3 position)
    {
        Position = position;
        IsActive = true;
        _bobTime = 0f;
    }

    /// <summary>
    /// Update bobbing animation.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        _bobTime += deltaTime * BOB_SPEED;
    }

    /// <summary>
    /// Get display position with bobbing offset.
    /// Floats above ground with sinusoidal motion.
    /// </summary>
    public Vector3 GetDisplayPosition()
    {
        float bobOffset = (float)Math.Sin(_bobTime) * BOB_HEIGHT;
        return Position + Vector3.Up * (0.5f + bobOffset);
    }

    /// <summary>
    /// Check if player is within collection radius.
    /// </summary>
    public bool CheckCollection(Vector3 playerPosition)
    {
        if (!IsActive) return false;

        return Vector3.Distance(Position, playerPosition) <= CollectRadius;
    }

    /// <summary>
    /// Collect the pickup and return heal amount.
    /// Deactivates the pickup.
    /// </summary>
    public int Collect()
    {
        IsActive = false;
        return HealAmount;
    }

    /// <summary>
    /// Get color for rendering (green for health pickups).
    /// </summary>
    public Color GetColor()
    {
        return Color.Green;
    }
}
