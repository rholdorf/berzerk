using Microsoft.Xna.Framework;
using Berzerk.Source.Core;

namespace Berzerk.Source.Combat;

/// <summary>
/// Represents a projectile in 3D space with position, velocity, and lifetime tracking.
/// Designed for object pooling to prevent GC spikes during rapid fire.
/// </summary>
public class Projectile
{
    public Transform Transform { get; private set; }
    public bool IsActive { get; private set; }
    public float Speed { get; private set; }
    public float Radius { get; private set; } = 0.2f;
    public float MaxDistance { get; private set; } = 75f;

    private Vector3 _velocity;
    private float _distanceTraveled;

    public Projectile()
    {
        Transform = new Transform();
        IsActive = false;
    }

    /// <summary>
    /// Activate projectile from pool with position, direction, and speed.
    /// </summary>
    public void Activate(Vector3 position, Vector3 direction, float speed)
    {
        Transform.Position = position;
        _velocity = Vector3.Normalize(direction) * speed;
        Speed = speed;
        _distanceTraveled = 0f;
        IsActive = true;
    }

    /// <summary>
    /// Update projectile position and distance traveled. Deactivates when max distance reached.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        // Move projectile
        Vector3 movement = _velocity * deltaTime;
        Transform.Position += movement;
        _distanceTraveled += movement.Length();

        // Deactivate after max distance
        if (_distanceTraveled >= MaxDistance)
        {
            Deactivate();
        }
    }

    /// <summary>
    /// Deactivate projectile and return to pool.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Get bounding sphere for collision detection.
    /// </summary>
    public BoundingSphere GetBoundingSphere()
    {
        return new BoundingSphere(Transform.Position, Radius);
    }

    /// <summary>
    /// Called when projectile hits a wall. Deactivates projectile.
    /// </summary>
    public void OnHitWall()
    {
        // TODO: Spawn impact effect in Plan 02
        Deactivate();
    }

    /// <summary>
    /// Called when projectile hits a target. Deactivates projectile.
    /// </summary>
    public void OnHitTarget(object target)
    {
        // TODO: Spawn impact effect and notify target in Plan 02/03
        Deactivate();
    }
}
