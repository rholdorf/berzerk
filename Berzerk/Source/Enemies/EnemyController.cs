using Microsoft.Xna.Framework;
using Berzerk.Source.Core;
using System;

namespace Berzerk.Source.Enemies;

/// <summary>
/// Individual enemy behavior with FSM-based AI, movement, and combat.
/// Implements detection, chase, melee attack, and death states.
/// </summary>
public class EnemyController
{
    public Transform Transform { get; }
    public EnemyHealth Health { get; }
    public bool IsActive { get; set; }

    // Movement settings (per CONTEXT: 70% of player's 5.0 speed)
    private const float MoveSpeed = 3.5f;
    private const float DetectionRange = 15f;      // Room is ~20 units
    private const float AttackRange = 2.5f;        // Melee distance
    private const float AttackRangeExit = 3.5f;    // Hysteresis to prevent thrashing
    private const float GiveUpRange = 25f;         // Stop chasing if player escapes

    // Combat settings (per CONTEXT)
    private const float AttackCooldown = 1.0f;     // Seconds between attacks
    private const int AttackDamage = 10;           // HP per hit

    // Death settings
    private const float DeathDuration = 0.5f;      // Placeholder animation timer

    // Collision
    private const float CollisionRadius = 0.6f;

    // State machine
    private EnemyState _currentState = EnemyState.Idle;
    private Vector3 _velocity = Vector3.Zero;
    private float _attackTimer = 0f;
    private float _deathTimer = 0f;

    public EnemyController()
    {
        Transform = new Transform();
        Health = new EnemyHealth();

        // Subscribe to death event to trigger dying state
        Health.OnDeath += OnHealthDepleted;
    }

    /// <summary>
    /// Activate enemy at specified position (for pooling).
    /// </summary>
    public void Activate(Vector3 position)
    {
        Transform.Position = position;
        Transform.Rotation = Quaternion.Identity;
        Health.Reset();
        _currentState = EnemyState.Idle;
        _velocity = Vector3.Zero;
        _attackTimer = 0f;
        _deathTimer = 0f;
        IsActive = true;
    }

    /// <summary>
    /// Deactivate enemy (for pooling).
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Get bounding sphere for collision detection.
    /// </summary>
    public BoundingSphere GetBoundingSphere()
    {
        return new BoundingSphere(Transform.Position, CollisionRadius);
    }

    /// <summary>
    /// Update enemy behavior based on current state.
    /// </summary>
    public void Update(GameTime gameTime, Vector3 playerPos)
    {
        if (!IsActive) return;

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Run state-specific update
        switch (_currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState(playerPos);
                break;

            case EnemyState.Chase:
                UpdateChaseState(deltaTime, playerPos);
                break;

            case EnemyState.Attack:
                UpdateAttackState(deltaTime, playerPos);
                break;

            case EnemyState.Dying:
                UpdateDyingState(deltaTime);
                break;
        }
    }

    private void UpdateIdleState(Vector3 playerPos)
    {
        // Check if player within detection range
        float distanceToPlayer = Vector3.Distance(Transform.Position, playerPos);

        if (distanceToPlayer <= DetectionRange)
        {
            TransitionToState(EnemyState.Chase);
        }
    }

    private void UpdateChaseState(float deltaTime, Vector3 playerPos)
    {
        float distance = Vector3.Distance(Transform.Position, playerPos);

        // Check state transitions
        if (distance <= AttackRange)
        {
            TransitionToState(EnemyState.Attack);
            return;
        }

        if (distance > GiveUpRange)
        {
            TransitionToState(EnemyState.Idle);
            return;
        }

        // Move toward player
        Vector3 toPlayer = playerPos - Transform.Position;
        toPlayer.Y = 0; // Keep movement horizontal

        if (toPlayer.LengthSquared() > 0.01f)
        {
            Vector3 direction = Vector3.Normalize(toPlayer);
            _velocity = direction * MoveSpeed;
            Transform.Position += _velocity * deltaTime;

            // Rotate to face player
            Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up,
                (float)Math.Atan2(direction.X, direction.Z));
        }
    }

    private void UpdateAttackState(float deltaTime, Vector3 playerPos)
    {
        float distance = Vector3.Distance(Transform.Position, playerPos);

        // Check if player escaped (using hysteresis)
        if (distance > AttackRangeExit)
        {
            TransitionToState(EnemyState.Chase);
            return;
        }

        // Face player
        Vector3 toPlayer = playerPos - Transform.Position;
        toPlayer.Y = 0;
        if (toPlayer.LengthSquared() > 0.01f)
        {
            Vector3 direction = Vector3.Normalize(toPlayer);
            Transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up,
                (float)Math.Atan2(direction.X, direction.Z));
        }

        // Attack cooldown
        _attackTimer -= deltaTime;

        if (_attackTimer <= 0f && distance <= AttackRange)
        {
            // Calculate knockback direction (from enemy to player)
            Vector3 knockbackDir = playerPos - Transform.Position;
            knockbackDir.Y = 0; // Horizontal only

            // Fire attack event with damage and knockback direction
            OnAttackPlayer?.Invoke(AttackDamage, knockbackDir);

            // Legacy event for compatibility
            OnAttackExecuted?.Invoke(AttackDamage);

            _attackTimer = AttackCooldown;
        }
    }

    private void UpdateDyingState(float deltaTime)
    {
        _deathTimer += deltaTime;

        if (_deathTimer >= DeathDuration)
        {
            IsActive = false;
        }
    }

    private void TransitionToState(EnemyState newState)
    {
        if (_currentState == newState) return;

        OnStateExit(_currentState);
        _currentState = newState;
        OnStateEnter(newState);
    }

    private void OnStateEnter(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
                _attackTimer = 0f; // Reset cooldown on entering attack state
                break;

            case EnemyState.Dying:
                _deathTimer = 0f;
                _velocity = Vector3.Zero;
                break;
        }
    }

    private void OnStateExit(EnemyState state)
    {
        // Clean up when leaving a state if needed
    }

    private void OnHealthDepleted()
    {
        TransitionToState(EnemyState.Dying);
    }

    /// <summary>
    /// Event fired when enemy executes an attack.
    /// Passes damage amount and knockback direction (enemy to player).
    /// </summary>
    public event System.Action<int, Vector3>? OnAttackPlayer;

    /// <summary>
    /// Event fired when enemy executes an attack.
    /// Passes damage amount to be applied by manager.
    /// </summary>
    public event System.Action<int>? OnAttackExecuted;
}
