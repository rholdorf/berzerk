namespace Berzerk.Source.Enemies;

/// <summary>
/// Enemy health system with damage tracking and death events.
/// Mirrors HealthSystem pattern for consistency.
/// </summary>
public class EnemyHealth
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; } = 30; // 2-3 hits at 15 HP per laser
    public bool IsDead => CurrentHealth <= 0;

    public event System.Action? OnDamageTaken;
    public event System.Action? OnDeath;

    public EnemyHealth()
    {
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Apply damage to enemy. Fires OnDamageTaken event.
    /// If health reaches zero, fires OnDeath event.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (IsDead) return;  // Ignore damage when already dead

        CurrentHealth = System.Math.Max(0, CurrentHealth - amount);
        OnDamageTaken?.Invoke();

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>
    /// Reset health to maximum (for pooling).
    /// </summary>
    public void Reset()
    {
        CurrentHealth = MaxHealth;
    }
}
