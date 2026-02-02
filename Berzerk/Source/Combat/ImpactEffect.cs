using Microsoft.Xna.Framework;

namespace Berzerk.Source.Combat;

/// <summary>
/// Visual feedback effect for projectile impacts.
/// Fades out and shrinks over lifetime at collision points.
/// </summary>
public class ImpactEffect
{
    public Vector3 Position { get; private set; }
    public bool IsActive { get; private set; }

    private float _lifetime = 0.2f; // Seconds
    private float _elapsed = 0f;
    private float _initialScale = 0.3f;

    /// <summary>
    /// Get current scale factor based on lifetime progression.
    /// </summary>
    public float Scale => _initialScale * (1.0f - _elapsed / _lifetime);

    /// <summary>
    /// Get current alpha transparency based on lifetime progression.
    /// </summary>
    public float Alpha => 1.0f - _elapsed / _lifetime;

    /// <summary>
    /// Activate effect at specified position.
    /// </summary>
    public void Activate(Vector3 position)
    {
        Position = position;
        _elapsed = 0f;
        IsActive = true;
    }

    /// <summary>
    /// Update effect lifetime. Deactivates when lifetime expires.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        _elapsed += deltaTime;

        if (_elapsed >= _lifetime)
        {
            IsActive = false;
        }
    }
}
