namespace Berzerk.Source.Combat;

/// <summary>
/// Tracks player score with point accumulation and event notifications.
/// Fires events on score changes for UI updates.
/// </summary>
public class ScoreSystem
{
    public int CurrentScore { get; private set; }
    public const int PointsPerEnemy = 50;

    public event System.Action<int>? OnScoreChanged;

    /// <summary>
    /// Add points for enemy kill and fire score change event.
    /// </summary>
    public void AddEnemyKill()
    {
        CurrentScore += PointsPerEnemy;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    /// <summary>
    /// Reset score to zero and fire score change event.
    /// </summary>
    public void Reset()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}
