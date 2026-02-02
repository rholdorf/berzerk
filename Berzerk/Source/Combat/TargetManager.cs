using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Berzerk.Source.Combat;

/// <summary>
/// Manages test targets, ammo pickups, and projectile-target collision detection.
/// Handles target spawning, pickup spawning, and collection.
/// </summary>
public class TargetManager
{
    private List<TestTarget> _targets = new();
    private List<AmmoPickup> _pickups = new();
    private Queue<AmmoPickup> _pickupPool = new();

    private const int POOL_SIZE = 10;

    /// <summary>
    /// Initialize manager and create test targets for Phase 3 validation.
    /// </summary>
    public void Initialize()
    {
        // Pre-populate pickup pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            _pickupPool.Enqueue(new AmmoPickup(Vector3.Zero));
        }

        // Create test targets at fixed positions for validation
        _targets.Add(new TestTarget(new Vector3(-5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(0, 0.5f, -8)));
    }

    /// <summary>
    /// Update all active targets and pickups.
    /// </summary>
    public void Update(float deltaTime)
    {
        // Update all active targets
        foreach (var target in _targets)
        {
            if (target.IsActive)
            {
                target.Update(deltaTime);
            }
        }

        // Update all active pickups
        foreach (var pickup in _pickups)
        {
            if (pickup.IsActive)
            {
                pickup.Update(deltaTime);
            }
        }

        // Return inactive pickups to pool (iterate backwards)
        for (int i = _pickups.Count - 1; i >= 0; i--)
        {
            if (!_pickups[i].IsActive)
            {
                _pickupPool.Enqueue(_pickups[i]);
                _pickups.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Check projectile collisions against all active targets.
    /// Spawns pickup when target is destroyed.
    /// </summary>
    public void CheckProjectileCollisions(IReadOnlyList<Projectile> projectiles)
    {
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;

            BoundingSphere projectileSphere = projectile.GetBoundingSphere();

            foreach (var target in _targets)
            {
                if (!target.IsActive) continue;

                if (projectileSphere.Intersects(target.GetBoundingSphere()))
                {
                    bool stillAlive = target.OnHit();
                    projectile.OnHitTarget(target);

                    if (!stillAlive)
                    {
                        SpawnPickup(target.Position);
                    }

                    break; // Projectile hit something, stop checking
                }
            }
        }
    }

    /// <summary>
    /// Check pickup collection and notify AmmoSystem.
    /// </summary>
    public void CheckPickupCollection(Vector3 playerPosition, AmmoSystem ammoSystem)
    {
        foreach (var pickup in _pickups)
        {
            if (!pickup.IsActive) continue;

            if (pickup.CheckCollection(playerPosition))
            {
                int amount = pickup.Collect();
                ammoSystem.AddAmmo(amount);
                Console.WriteLine($"Collected {amount} ammo!");
            }
        }
    }

    /// <summary>
    /// Spawn ammo pickup at specified position.
    /// </summary>
    private void SpawnPickup(Vector3 position)
    {
        AmmoPickup pickup;

        if (_pickupPool.Count > 0)
        {
            pickup = _pickupPool.Dequeue();
            pickup.Activate(position);
        }
        else
        {
            pickup = new AmmoPickup(position);
        }

        _pickups.Add(pickup);
    }

    /// <summary>
    /// Get read-only list of all targets.
    /// </summary>
    public IReadOnlyList<TestTarget> GetTargets()
    {
        return _targets;
    }

    /// <summary>
    /// Get read-only list of all pickups.
    /// </summary>
    public IReadOnlyList<AmmoPickup> GetPickups()
    {
        return _pickups;
    }

    /// <summary>
    /// Respawn all targets for testing. Clears pickups.
    /// </summary>
    public void RespawnTargets()
    {
        // Clear existing targets
        _targets.Clear();

        // Return all pickups to pool
        foreach (var pickup in _pickups)
        {
            _pickupPool.Enqueue(pickup);
        }
        _pickups.Clear();

        // Re-initialize targets at fixed positions
        _targets.Add(new TestTarget(new Vector3(-5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(0, 0.5f, -8)));
    }
}
