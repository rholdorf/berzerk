using Microsoft.Xna.Framework;
using Berzerk.Source.Combat;
using Berzerk.Graphics;
using System;
using System.Collections.Generic;

namespace Berzerk.Source.Enemies;

/// <summary>
/// Manages enemy spawning, pooling, collision detection, and drop system.
/// Handles enemy lifecycle (spawn, update, death) and projectile-enemy interactions.
/// Implements progressive difficulty through wave scaling.
/// </summary>
public class EnemyManager
{
    private List<EnemyController> _enemies;
    private Queue<EnemyController> _enemyPool;
    private TargetManager? _targetManager;
    private EnemyRenderer? _enemyRenderer;
    private Random _random;

    // Explosion effects
    private List<ExplosionEffect> _activeExplosions;
    private Queue<ExplosionEffect> _explosionPool;

    // Wave progression
    private int _currentWave = 0;
    private const int MAX_ENEMIES_PER_WAVE = 10;
    private bool _allDefeatedFired = false;

    // Spawning settings
    private const float MIN_SPAWN_DISTANCE_FROM_PLAYER = 10f;
    private const float MIN_SPAWN_DISTANCE_BETWEEN_ENEMIES = 3f;
    private const int MAX_SPAWN_ATTEMPTS = 20;

    private const float SPAWN_Y = 0.5f;

    // Drop settings (per CONTEXT: 30-40% chance, using 35%)
    private const float DROP_CHANCE = 0.35f;

    // Damage per projectile hit (per CONTEXT: 15 HP laser damage)
    private const int DAMAGE_PER_HIT = 15;

    public int ActiveCount => _enemies.Count;
    public bool AllEnemiesDefeated => _enemies.Count == 0;

    /// <summary>
    /// Event fired when all enemies in room are defeated.
    /// </summary>
    public event Action OnAllEnemiesDefeated;

    /// <summary>
    /// Initialize enemy manager with pre-allocated pool.
    /// </summary>
    public void Initialize(int poolSize = 20)
    {
        _enemies = new List<EnemyController>();
        _enemyPool = new Queue<EnemyController>(poolSize);
        _random = new Random();

        // Pre-allocate enemy pool to prevent GC spikes
        for (int i = 0; i < poolSize; i++)
        {
            _enemyPool.Enqueue(new EnemyController());
        }

        // Initialize explosion effect pool
        _activeExplosions = new List<ExplosionEffect>();
        _explosionPool = new Queue<ExplosionEffect>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            _explosionPool.Enqueue(new ExplosionEffect());
        }
    }

    /// <summary>
    /// Set target manager for pickup spawning.
    /// </summary>
    public void SetTargetManager(TargetManager targetManager)
    {
        _targetManager = targetManager;
    }

    /// <summary>
    /// Set enemy renderer to access shared animation models.
    /// </summary>
    public void SetEnemyRenderer(EnemyRenderer enemyRenderer)
    {
        _enemyRenderer = enemyRenderer;
    }

    /// <summary>
    /// Spawn wave of enemies at room-defined spawn points.
    /// </summary>
    public void SpawnWave(int enemyCount, Vector3 playerPos, List<Vector3> spawnPoints)
    {
        _allDefeatedFired = false;

        for (int i = 0; i < enemyCount; i++)
        {
            // Cycle through spawn points, skipping those too close to player
            Vector3 spawnPos = Vector3.Zero;
            bool found = false;

            for (int j = 0; j < spawnPoints.Count; j++)
            {
                int idx = (i + j) % spawnPoints.Count;
                Vector3 candidate = spawnPoints[idx];

                if (Vector3.Distance(candidate, playerPos) >= MIN_SPAWN_DISTANCE_FROM_PLAYER)
                {
                    spawnPos = candidate;
                    found = true;
                    break;
                }
            }

            if (!found && spawnPoints.Count > 0)
            {
                // All points too close to player, use the one furthest away
                float maxDist = 0;
                foreach (var pt in spawnPoints)
                {
                    float dist = Vector3.Distance(pt, playerPos);
                    if (dist > maxDist)
                    {
                        maxDist = dist;
                        spawnPos = pt;
                    }
                }
                found = true;
            }

            if (found)
            {
                SpawnEnemy(spawnPos);
            }
        }
    }

    /// <summary>
    /// Start next wave with progressive difficulty.
    /// </summary>
    public void StartNextWave(Vector3 playerPos, List<Vector3> spawnPoints)
    {
        _currentWave++;
        int enemyCount = GetWaveEnemyCount();
        SpawnWave(enemyCount, playerPos, spawnPoints);
    }

    /// <summary>
    /// Calculate enemy count for current wave (2 + wave number, caps at 10).
    /// </summary>
    private int GetWaveEnemyCount()
    {
        return Math.Min(2 + _currentWave, MAX_ENEMIES_PER_WAVE);
    }

    /// <summary>
    /// Spawn single enemy at specified position.
    /// </summary>
    private void SpawnEnemy(Vector3 position)
    {
        EnemyController enemy = _enemyPool.Count > 0
            ? _enemyPool.Dequeue()
            : new EnemyController();

        enemy.Activate(position);

        // Assign shared animation models from renderer
        if (_enemyRenderer != null)
        {
            var (idle, walk, attack) = _enemyRenderer.GetSharedModels();
            enemy.SetAnimatedModels(idle, walk, attack);
        }

        // Subscribe to death event for drop system
        enemy.Health.OnDeath += () => OnEnemyDeath(enemy);

        _enemies.Add(enemy);
    }

    /// <summary>
    /// Set callback for enemy attacks (wires to player damage/knockback).
    /// Call this after spawning to wire all active enemies.
    /// </summary>
    public void SetAttackCallback(Action<int, Vector3> attackCallback)
    {
        foreach (var enemy in _enemies)
        {
            enemy.OnAttackPlayer += attackCallback;
        }
    }

    /// <summary>
    /// Update all active enemies and explosion effects.
    /// Returns inactive enemies to pool.
    /// </summary>
    public void Update(GameTime gameTime, Vector3 playerPos)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Update all enemies
        foreach (var enemy in _enemies)
        {
            enemy.Update(gameTime, playerPos);
        }

        // Return inactive enemies to pool (iterate backwards)
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (!_enemies[i].IsActive)
            {
                _enemyPool.Enqueue(_enemies[i]);
                _enemies.RemoveAt(i);
            }
        }

        // Fire all-defeated event once when enemies are cleared
        if (_enemies.Count == 0 && !_allDefeatedFired && _currentWave > 0)
        {
            _allDefeatedFired = true;
            OnAllEnemiesDefeated?.Invoke();
        }

        // Update explosion effects
        foreach (var explosion in _activeExplosions)
        {
            explosion.Update(deltaTime);
        }

        // Return inactive explosions to pool
        for (int i = _activeExplosions.Count - 1; i >= 0; i--)
        {
            if (!_activeExplosions[i].IsActive)
            {
                _explosionPool.Enqueue(_activeExplosions[i]);
                _activeExplosions.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Check projectile collisions against all active enemies.
    /// Applies damage and deactivates projectile on hit.
    /// </summary>
    public void CheckProjectileCollisions(IReadOnlyList<Projectile> projectiles, int damagePerHit = DAMAGE_PER_HIT)
    {
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;

            BoundingSphere projectileSphere = projectile.GetBoundingSphere();

            foreach (var enemy in _enemies)
            {
                if (!enemy.IsActive) continue;

                if (projectileSphere.Intersects(enemy.GetBoundingSphere()))
                {
                    // Apply damage to enemy
                    enemy.Health.TakeDamage(damagePerHit);

                    // Deactivate projectile on hit
                    projectile.OnHitTarget(null);

                    break; // Projectile can only hit one enemy
                }
            }
        }
    }

    /// <summary>
    /// Handle enemy death - spawn explosion effect and pickup with 35% chance.
    /// </summary>
    private void OnEnemyDeath(EnemyController enemy)
    {
        // Spawn explosion effect
        SpawnExplosion(enemy.Transform.Position);

        if (_targetManager == null) return;

        // 35% chance to drop pickup
        if (_random.NextDouble() < DROP_CHANCE)
        {
            // 50/50 split between ammo and health
            if (_random.NextDouble() < 0.5)
            {
                _targetManager.SpawnAmmoPickup(enemy.Transform.Position);
            }
            else
            {
                _targetManager.SpawnHealthPickup(enemy.Transform.Position);
            }
        }
    }

    /// <summary>
    /// Spawn explosion effect at specified position.
    /// </summary>
    private void SpawnExplosion(Vector3 position)
    {
        ExplosionEffect explosion = _explosionPool.Count > 0
            ? _explosionPool.Dequeue()
            : new ExplosionEffect();

        explosion.Activate(position);
        _activeExplosions.Add(explosion);
    }

    /// <summary>
    /// Get read-only list of active enemies.
    /// </summary>
    public IReadOnlyList<EnemyController> GetEnemies()
    {
        return _enemies.AsReadOnly();
    }

    /// <summary>
    /// Get read-only list of active explosion effects.
    /// </summary>
    public IReadOnlyList<ExplosionEffect> GetActiveExplosions()
    {
        return _activeExplosions.AsReadOnly();
    }

    /// <summary>
    /// Reset enemy manager (for game restart).
    /// </summary>
    public void Reset()
    {
        // Return all enemies to pool
        while (_enemies.Count > 0)
        {
            var enemy = _enemies[0];
            enemy.Deactivate();
            _enemyPool.Enqueue(enemy);
            _enemies.RemoveAt(0);
        }

        // Return all explosions to pool
        while (_activeExplosions.Count > 0)
        {
            var explosion = _activeExplosions[0];
            explosion.Deactivate();
            _explosionPool.Enqueue(explosion);
            _activeExplosions.RemoveAt(0);
        }

        // Reset wave counter
        _currentWave = 0;
        _allDefeatedFired = false;
    }
}
