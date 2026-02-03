namespace Berzerk.Source.Enemies;

/// <summary>
/// AI states for enemy finite state machine.
/// Defines behavior modes for robot enemies.
/// </summary>
public enum EnemyState
{
    /// <summary>
    /// Waiting state - player not detected or too far away.
    /// </summary>
    Idle,

    /// <summary>
    /// Moving toward player position.
    /// </summary>
    Chase,

    /// <summary>
    /// In melee range, dealing damage to player.
    /// </summary>
    Attack,

    /// <summary>
    /// Playing death animation, about to despawn.
    /// </summary>
    Dying
}
