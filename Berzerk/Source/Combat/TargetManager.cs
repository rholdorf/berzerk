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
    private List<AmmoPickup> _ammoPickups = new();
    private Queue<AmmoPickup> _ammoPickupPool = new();
    private List<HealthPickup> _healthPickups = new();
    private Queue<HealthPickup> _healthPickupPool = new();

    private const int POOL_SIZE = 10;

    /// <summary>
    /// Initialize manager and create test targets for Phase 3 validation.
    /// </summary>
    public void Initialize()
    {
        // Pre-populate ammo pickup pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            _ammoPickupPool.Enqueue(new AmmoPickup(Vector3.Zero));
        }

        // Pre-populate health pickup pool
        for (int i = 0; i < POOL_SIZE; i++)
        {
            _healthPickupPool.Enqueue(new HealthPickup(Vector3.Zero));
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

        // Update all active ammo pickups
        foreach (var pickup in _ammoPickups)
        {
            if (pickup.IsActive)
            {
                pickup.Update(deltaTime);
            }
        }

        // Update all active health pickups
        foreach (var pickup in _healthPickups)
        {
            if (pickup.IsActive)
            {
                pickup.Update(deltaTime);
            }
        }

        // Return inactive ammo pickups to pool (iterate backwards)
        for (int i = _ammoPickups.Count - 1; i >= 0; i--)
        {
            if (!_ammoPickups[i].IsActive)
            {
                _ammoPickupPool.Enqueue(_ammoPickups[i]);
                _ammoPickups.RemoveAt(i);
            }
        }

        // Return inactive health pickups to pool (iterate backwards)
        for (int i = _healthPickups.Count - 1; i >= 0; i--)
        {
            if (!_healthPickups[i].IsActive)
            {
                _healthPickupPool.Enqueue(_healthPickups[i]);
                _healthPickups.RemoveAt(i);
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
                        SpawnAmmoPickup(target.Position);
                    }

                    break; // Projectile hit something, stop checking
                }
            }
        }
    }

    /// <summary>
    /// Check pickup collection and notify AmmoSystem and HealthSystem.
    /// </summary>
    public void CheckPickupCollection(Vector3 playerPosition, AmmoSystem ammoSystem, Player.HealthSystem healthSystem)
    {
        // Check ammo pickup collection
        foreach (var pickup in _ammoPickups)
        {
            if (!pickup.IsActive) continue;

            if (pickup.CheckCollection(playerPosition))
            {
                int amount = pickup.Collect();
                ammoSystem.AddAmmo(amount);
                Console.WriteLine($"Collected {amount} ammo!");
            }
        }

        // Check health pickup collection
        foreach (var pickup in _healthPickups)
        {
            if (!pickup.IsActive) continue;

            if (pickup.CheckCollection(playerPosition))
            {
                int amount = pickup.Collect();
                healthSystem.Heal(amount);
                Console.WriteLine($"Healed {amount} HP!");
            }
        }
    }

    /// <summary>
    /// Spawn ammo pickup at specified position.
    /// </summary>
    public void SpawnAmmoPickup(Vector3 position)
    {
        AmmoPickup pickup;

        if (_ammoPickupPool.Count > 0)
        {
            pickup = _ammoPickupPool.Dequeue();
            pickup.Activate(position);
        }
        else
        {
            pickup = new AmmoPickup(position);
        }

        _ammoPickups.Add(pickup);
    }

    /// <summary>
    /// Spawn health pickup at specified position.
    /// </summary>
    public void SpawnHealthPickup(Vector3 position)
    {
        HealthPickup pickup;

        if (_healthPickupPool.Count > 0)
        {
            pickup = _healthPickupPool.Dequeue();
            pickup.Activate(position);
        }
        else
        {
            pickup = new HealthPickup(position);
        }

        _healthPickups.Add(pickup);
    }

    /// <summary>
    /// Get read-only list of all targets.
    /// </summary>
    public IReadOnlyList<TestTarget> GetTargets()
    {
        return _targets;
    }

    /// <summary>
    /// Get read-only list of all ammo pickups.
    /// </summary>
    public IReadOnlyList<AmmoPickup> GetAmmoPickups()
    {
        return _ammoPickups;
    }

    /// <summary>
    /// Get read-only list of all health pickups.
    /// </summary>
    public IReadOnlyList<HealthPickup> GetHealthPickups()
    {
        return _healthPickups;
    }

    /// <summary>
    /// Respawn all targets for testing. Clears pickups.
    /// </summary>
    public void RespawnTargets()
    {
        // Clear existing targets
        _targets.Clear();

        // Return all ammo pickups to pool
        foreach (var pickup in _ammoPickups)
        {
            _ammoPickupPool.Enqueue(pickup);
        }
        _ammoPickups.Clear();

        // Return all health pickups to pool
        foreach (var pickup in _healthPickups)
        {
            _healthPickupPool.Enqueue(pickup);
        }
        _healthPickups.Clear();

        // Re-initialize targets at fixed positions
        _targets.Add(new TestTarget(new Vector3(-5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(5, 0.5f, -5)));
        _targets.Add(new TestTarget(new Vector3(0, 0.5f, -8)));
    }
}
