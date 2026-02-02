using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Berzerk.Source.Combat;

/// <summary>
/// Manages all active projectiles using object pooling to prevent GC spikes.
/// Pre-allocates pool of projectiles for reuse during rapid fire.
/// Handles collision detection with walls and spawns impact effects.
/// </summary>
public class ProjectileManager
{
    private List<Projectile> _activeProjectiles;
    private Queue<Projectile> _projectilePool;
    private List<ImpactEffect> _activeEffects;
    private Queue<ImpactEffect> _effectPool;
    private List<BoundingBox> _wallColliders;

    private const int POOL_SIZE = 50;
    private const int EFFECT_POOL_SIZE = 20;

    public int ActiveCount => _activeProjectiles.Count;

    /// <summary>
    /// Initialize projectile manager with pre-allocated pools.
    /// </summary>
    public void Initialize(int poolSize = POOL_SIZE)
    {
        _activeProjectiles = new List<Projectile>();
        _projectilePool = new Queue<Projectile>(poolSize);
        _activeEffects = new List<ImpactEffect>();
        _effectPool = new Queue<ImpactEffect>(EFFECT_POOL_SIZE);
        _wallColliders = new List<BoundingBox>();

        // Pre-allocate projectiles to avoid allocations during gameplay
        for (int i = 0; i < poolSize; i++)
        {
            _projectilePool.Enqueue(new Projectile());
        }

        // Pre-allocate impact effects
        for (int i = 0; i < EFFECT_POOL_SIZE; i++)
        {
            _effectPool.Enqueue(new ImpactEffect());
        }
    }

    /// <summary>
    /// Set wall colliders for projectile collision detection.
    /// </summary>
    public void SetWallColliders(List<BoundingBox> walls)
    {
        _wallColliders = walls;
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
    /// Checks wall collisions and spawns impact effects.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Update projectiles and check wall collisions
        for (int i = _activeProjectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _activeProjectiles[i];
            projectile.Update(deltaTime);

            // Check wall collision if projectile still active after update
            if (projectile.IsActive && _wallColliders != null)
            {
                BoundingSphere projectileSphere = projectile.GetBoundingSphere();

                foreach (var wall in _wallColliders)
                {
                    if (projectileSphere.Intersects(wall))
                    {
                        SpawnImpactEffect(projectile.Transform.Position);
                        projectile.OnHitWall();
                        break; // Only handle one collision per frame
                    }
                }
            }

            // Return inactive projectiles to pool
            if (!projectile.IsActive)
            {
                _projectilePool.Enqueue(projectile);
                _activeProjectiles.RemoveAt(i);
            }
        }

        // Update impact effects
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].Update(deltaTime);

            if (!_activeEffects[i].IsActive)
            {
                _effectPool.Enqueue(_activeEffects[i]);
                _activeEffects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Spawn impact effect at collision point.
    /// </summary>
    private void SpawnImpactEffect(Vector3 position)
    {
        ImpactEffect effect = _effectPool.Count > 0
            ? _effectPool.Dequeue()
            : new ImpactEffect();

        effect.Activate(position);
        _activeEffects.Add(effect);
    }

    /// <summary>
    /// Get read-only list of active projectiles for collision checking and rendering.
    /// </summary>
    public IReadOnlyList<Projectile> GetActiveProjectiles()
    {
        return _activeProjectiles.AsReadOnly();
    }

    /// <summary>
    /// Get read-only list of active impact effects for rendering.
    /// </summary>
    public IReadOnlyList<ImpactEffect> GetActiveEffects()
    {
        return _activeEffects.AsReadOnly();
    }
}
