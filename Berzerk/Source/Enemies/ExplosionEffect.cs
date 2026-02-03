using Microsoft.Xna.Framework;

namespace Berzerk.Source.Enemies;

/// <summary>
/// Visual effect for enemy death.
/// Expands and fades over duration, creating satisfying destruction feedback.
/// Supports object pooling via Activate/Deactivate pattern.
/// </summary>
public class ExplosionEffect
{
    public Vector3 Position { get; private set; }
    public bool IsActive { get; private set; }

    private float _timer = 0f;
    private const float DURATION = 0.3f; // Total effect duration in seconds
    private const float MAX_RADIUS = 2f; // Maximum sphere radius during expansion

    /// <summary>
    /// Activate effect at specified position (for pooling).
    /// </summary>
    public void Activate(Vector3 position)
    {
        Position = position;
        IsActive = true;
        _timer = 0f;
    }

    /// <summary>
    /// Deactivate effect (for pooling).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Update effect timer. Deactivates when duration completes.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        _timer += deltaTime;

        if (_timer >= DURATION)
        {
            IsActive = false;
        }
    }

    /// <summary>
    /// Get current radius based on animation progression.
    /// Expands over first half (0 to MAX_RADIUS), then shrinks over second half (MAX_RADIUS to 0).
    /// </summary>
    public float GetRadius()
    {
        float progress = _timer / DURATION;

        if (progress < 0.5f)
        {
            // Expand over first half: 0 -> MAX_RADIUS
            return MAX_RADIUS * (progress * 2f);
        }
        else
        {
            // Shrink over second half: MAX_RADIUS -> 0
            return MAX_RADIUS * (2f - progress * 2f);
        }
    }

    /// <summary>
    /// Get current alpha transparency based on lifetime.
    /// Linear fade from 1.0 to 0.0 over duration.
    /// </summary>
    public float GetAlpha()
    {
        return 1.0f - (_timer / DURATION);
    }

    /// <summary>
    /// Get color for rendering with current alpha.
    /// Orange explosion color (matches impact effect decision from 03-02).
    /// </summary>
    public Color GetColor()
    {
        // Orange: (1.0, 0.8, 0.3) * alpha
        return new Color(1.0f, 0.8f, 0.3f) * GetAlpha();
    }
}
