using Microsoft.Xna.Framework;
using System;

namespace Berzerk.Source.Combat;

/// <summary>
/// Collectable ammo pickup that floats above ground with bobbing animation.
/// Auto-collects when player enters collection radius.
/// </summary>
public class AmmoPickup
{
    public Vector3 Position { get; private set; }
    public bool IsActive { get; private set; }
    public int AmmoAmount { get; private set; } = 40; // Within CONTEXT.md 30-50 range
    public float CollectRadius { get; private set; } = 2f; // Generous auto-collect radius

    private float _bobTime = 0f;
    private const float BOB_SPEED = 3f;
    private const float BOB_HEIGHT = 0.3f;

    public AmmoPickup(Vector3 position)
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
    /// Collect the pickup and return ammo amount.
    /// Deactivates the pickup.
    /// </summary>
    public int Collect()
    {
        IsActive = false;
        return AmmoAmount;
    }

    /// <summary>
    /// Get color for rendering (yellow for ammo pickups).
    /// </summary>
    public Color GetColor()
    {
        return Color.Yellow;
    }
}
