using Microsoft.Xna.Framework;

namespace Berzerk.Source.Combat;

/// <summary>
/// Destructible test target represented as a colored cube.
/// Changes color when hit and can be destroyed by projectiles.
/// </summary>
public class TestTarget
{
    public Vector3 Position { get; private set; }
    public float Size { get; private set; } = 1f; // 1 unit cube
    public float Radius { get; private set; } = 0.7f; // Collision sphere (slightly smaller than cube diagonal)
    public bool IsActive { get; private set; }
    public bool IsHit { get; private set; }
    public int HitPoints { get; private set; } = 1; // One hit to destroy for arcade feel

    private float _hitFlashTimer = 0f;
    private const float HIT_FLASH_DURATION = 0.1f;

    public TestTarget(Vector3 position)
    {
        Position = position;
        IsActive = true;
        IsHit = false;
    }

    /// <summary>
    /// Get bounding sphere for collision detection.
    /// </summary>
    public BoundingSphere GetBoundingSphere()
    {
        return new BoundingSphere(Position, Radius);
    }

    /// <summary>
    /// Get bounding box for rendering.
    /// </summary>
    public BoundingBox GetBoundingBox()
    {
        Vector3 halfSize = new Vector3(Size / 2f);
        return new BoundingBox(Position - halfSize, Position + halfSize);
    }

    /// <summary>
    /// Called when target is hit by projectile.
    /// Returns true if target is still active, false if destroyed.
    /// </summary>
    public bool OnHit()
    {
        HitPoints--;
        IsHit = true;
        _hitFlashTimer = HIT_FLASH_DURATION;

        if (HitPoints <= 0)
        {
            IsActive = false; // Destroyed
        }

        return IsActive;
    }

    /// <summary>
    /// Update hit flash timer.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (IsHit && _hitFlashTimer > 0)
        {
            _hitFlashTimer -= deltaTime;
            if (_hitFlashTimer <= 0)
            {
                IsHit = false;
            }
        }
    }

    /// <summary>
    /// Get current color based on state.
    /// </summary>
    public Color GetColor()
    {
        if (!IsActive)
            return Color.Transparent;

        if (IsHit)
            return Color.Red; // Flash red on hit

        return Color.Green; // Normal state
    }
}
