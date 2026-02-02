namespace Berzerk.Source.Combat;

/// <summary>
/// Manages magazine and reserve ammunition with auto-reload functionality.
/// Implements two-tier ammo system: current magazine and reserve pool.
/// </summary>
public class AmmoSystem
{
    public int CurrentMagazine { get; private set; }
    public int MaxMagazineSize { get; private set; } = 25;
    public int ReserveAmmo { get; private set; }
    public int MaxReserveAmmo { get; private set; } = 125;

    public int TotalAmmo => CurrentMagazine + ReserveAmmo;
    public bool IsEmpty => TotalAmmo == 0;

    public AmmoSystem()
    {
        CurrentMagazine = MaxMagazineSize;
        ReserveAmmo = MaxReserveAmmo;
    }

    /// <summary>
    /// Try to consume one ammo. Auto-reloads from reserve if magazine empty.
    /// Returns false only if completely out of ammo.
    /// </summary>
    public bool TryConsumeAmmo()
    {
        // Consume from current magazine if available
        if (CurrentMagazine > 0)
        {
            CurrentMagazine--;
            return true;
        }

        // Auto-reload if reserve available
        if (ReserveAmmo > 0)
        {
            Reload();
            return TryConsumeAmmo(); // Retry after reload
        }

        // Completely out of ammo
        return false;
    }

    /// <summary>
    /// Reload magazine from reserve ammo.
    /// </summary>
    public void Reload()
    {
        int ammoNeeded = MaxMagazineSize - CurrentMagazine;
        int ammoToReload = System.Math.Min(ammoNeeded, ReserveAmmo);

        CurrentMagazine += ammoToReload;
        ReserveAmmo -= ammoToReload;
    }

    /// <summary>
    /// Add ammo to reserve pool, capped at max reserve capacity.
    /// </summary>
    public void AddAmmo(int amount)
    {
        ReserveAmmo = System.Math.Min(ReserveAmmo + amount, MaxReserveAmmo);
    }
}
