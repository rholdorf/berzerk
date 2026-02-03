namespace Berzerk.Source.Player;

/// <summary>
/// Manages player health with damage, healing, and death tracking.
/// Fires events for damage feedback and state transitions.
/// </summary>
public class HealthSystem
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; } = 200;
    public int StartingHealth { get; private set; } = 100;
    public bool IsDead => CurrentHealth <= 0;

    public event System.Action? OnDamageTaken;
    public event System.Action? OnDeath;

    public HealthSystem()
    {
        CurrentHealth = StartingHealth;
    }

    /// <summary>
    /// Apply damage to health. Fires OnDamageTaken event.
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
    /// Restore health, capped at MaxHealth.
    /// </summary>
    public void Heal(int amount)
    {
        CurrentHealth = System.Math.Min(CurrentHealth + amount, MaxHealth);
    }

    /// <summary>
    /// Reset health to starting value.
    /// </summary>
    public void Reset()
    {
        CurrentHealth = StartingHealth;
    }
}
