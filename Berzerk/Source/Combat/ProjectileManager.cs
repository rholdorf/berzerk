using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Berzerk.Source.Combat;

/// <summary>
/// Manages all active projectiles using object pooling to prevent GC spikes.
/// Pre-allocates pool of projectiles for reuse during rapid fire.
/// </summary>
public class ProjectileManager
{
    private List<Projectile> _activeProjectiles;
    private Queue<Projectile> _projectilePool;
    private const int POOL_SIZE = 50;

    public int ActiveCount => _activeProjectiles.Count;

    /// <summary>
    /// Initialize projectile manager with pre-allocated pool.
    /// </summary>
    public void Initialize(int poolSize = POOL_SIZE)
    {
        _activeProjectiles = new List<Projectile>();
        _projectilePool = new Queue<Projectile>(poolSize);

        // Pre-allocate projectiles to avoid allocations during gameplay
        for (int i = 0; i < poolSize; i++)
        {
            _projectilePool.Enqueue(new Projectile());
        }
    }

    /// <summary>
    /// Spawn a projectile from the pool with specified position, direction, and speed.
    /// </summary>
    public Projectile Spawn(Vector3 position, Vector3 direction, float speed)
    {
        // Get projectile from pool or create new if pool exhausted
        Projectile projectile = _projectilePool.Count > 0
            ? _projectilePool.Dequeue()
            : new Projectile();

        // Normalize direction before activation
        projectile.Activate(position, Vector3.Normalize(direction), speed);
        _activeProjectiles.Add(projectile);

        return projectile;
    }

    /// <summary>
    /// Update all active projectiles. Returns inactive projectiles to pool.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Iterate backwards for safe removal during iteration
        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            _activeProjectiles[i].Update(deltaTime);

            // Return inactive projectiles to pool
            if (!_activeProjectiles[i].IsActive)
            {
                _projectilePool.Enqueue(_activeProjectiles[i]);
                _activeProjectiles.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Get read-only list of active projectiles for collision checking and rendering.
    /// </summary>
    public IReadOnlyList<Projectile> GetActiveProjectiles()
    {
        return _activeProjectiles.AsReadOnly();
    }
}
