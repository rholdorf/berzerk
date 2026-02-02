using Microsoft.Xna.Framework;

namespace Berzerk.Source.Combat;

/// <summary>
/// Manages weapon firing with rate limiting and projectile spawning.
/// Implements frame-rate independent cooldown timing.
/// </summary>
public class WeaponSystem
{
    private float _fireRate = 6.5f; // 6.5 shots per second (within 5-8 range)
    private float _cooldownTimer = 0f;
    private float _projectileSpeed = 50f; // 50 units/sec (within 40-60 range)

    private readonly AmmoSystem _ammoSystem;
    private readonly ProjectileManager _projectileManager;

    public bool CanFire => _cooldownTimer <= 0f && !_ammoSystem.IsEmpty;
    public int CurrentAmmo => _ammoSystem.CurrentMagazine;
    public int ReserveAmmo => _ammoSystem.ReserveAmmo;

    public WeaponSystem(AmmoSystem ammoSystem, ProjectileManager projectileManager)
    {
        _ammoSystem = ammoSystem;
        _projectileManager = projectileManager;
    }

    /// <summary>
    /// Update weapon cooldown and fire if conditions met.
    /// </summary>
    public void Update(GameTime gameTime, bool wantsToFire, Vector3 spawnPosition, Vector3 aimDirection)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Decrease cooldown timer
        if (_cooldownTimer > 0f)
        {
            _cooldownTimer -= deltaTime;
        }

        // Fire if button held, cooldown ready, and ammo available
        if (wantsToFire && _cooldownTimer <= 0f && _ammoSystem.TryConsumeAmmo())
        {
            // Spawn projectile
            _projectileManager.Spawn(spawnPosition, aimDirection, _projectileSpeed);

            // Reset cooldown (fire rate interval)
            _cooldownTimer = 1.0f / _fireRate;
        }
    }
}
