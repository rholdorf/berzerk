using Microsoft.Xna.Framework;

namespace Berzerk.Source.Rooms;

/// <summary>
/// Door at room boundary with state machine and trigger volumes.
/// Trigger volume detects player entry; collision box blocks when closed.
/// </summary>
public class Door
{
    public Direction Facing { get; }
    public Vector3 Position { get; }
    public DoorState State { get; private set; } = DoorState.Closed;
    public BoundingBox TriggerVolume { get; private set; }
    public BoundingBox CollisionBox { get; private set; }

    private float _openingProgress = 0f;
    private const float OPENING_DURATION = 0.5f;

    // Door dimensions
    private const float DOOR_WIDTH = 4f;     // Width of door opening
    private const float DOOR_HEIGHT = 5f;    // Height of door/wall
    private const float TRIGGER_DEPTH = 2f;  // Trigger extends into room
    private const float WALL_THICKNESS = 1f; // Thickness of door collision

    public Door(Direction facing, Vector3 position)
    {
        Facing = facing;
        Position = position;
        CreateVolumes();
    }

    private void CreateVolumes()
    {
        float halfWidth = DOOR_WIDTH / 2f;

        // Create trigger and collision volumes based on facing direction
        // Trigger extends INTO room, collision is AT door position
        switch (Facing)
        {
            case Direction.North: // Door at -Z edge, trigger extends +Z (south into room)
                TriggerVolume = new BoundingBox(
                    new Vector3(Position.X - halfWidth, 0, Position.Z),
                    new Vector3(Position.X + halfWidth, DOOR_HEIGHT, Position.Z + TRIGGER_DEPTH)
                );
                CollisionBox = new BoundingBox(
                    new Vector3(Position.X - halfWidth, 0, Position.Z - WALL_THICKNESS / 2),
                    new Vector3(Position.X + halfWidth, DOOR_HEIGHT, Position.Z + WALL_THICKNESS / 2)
                );
                break;

            case Direction.South: // Door at +Z edge, trigger extends -Z (north into room)
                TriggerVolume = new BoundingBox(
                    new Vector3(Position.X - halfWidth, 0, Position.Z - TRIGGER_DEPTH),
                    new Vector3(Position.X + halfWidth, DOOR_HEIGHT, Position.Z)
                );
                CollisionBox = new BoundingBox(
                    new Vector3(Position.X - halfWidth, 0, Position.Z - WALL_THICKNESS / 2),
                    new Vector3(Position.X + halfWidth, DOOR_HEIGHT, Position.Z + WALL_THICKNESS / 2)
                );
                break;

            case Direction.East: // Door at +X edge, trigger extends -X (west into room)
                TriggerVolume = new BoundingBox(
                    new Vector3(Position.X - TRIGGER_DEPTH, 0, Position.Z - halfWidth),
                    new Vector3(Position.X, DOOR_HEIGHT, Position.Z + halfWidth)
                );
                CollisionBox = new BoundingBox(
                    new Vector3(Position.X - WALL_THICKNESS / 2, 0, Position.Z - halfWidth),
                    new Vector3(Position.X + WALL_THICKNESS / 2, DOOR_HEIGHT, Position.Z + halfWidth)
                );
                break;

            case Direction.West: // Door at -X edge, trigger extends +X (east into room)
                TriggerVolume = new BoundingBox(
                    new Vector3(Position.X, 0, Position.Z - halfWidth),
                    new Vector3(Position.X + TRIGGER_DEPTH, DOOR_HEIGHT, Position.Z + halfWidth)
                );
                CollisionBox = new BoundingBox(
                    new Vector3(Position.X - WALL_THICKNESS / 2, 0, Position.Z - halfWidth),
                    new Vector3(Position.X + WALL_THICKNESS / 2, DOOR_HEIGHT, Position.Z + halfWidth)
                );
                break;
        }
    }

    /// <summary>
    /// Begin door opening animation. Only valid from Closed state.
    /// </summary>
    public void Open()
    {
        if (State == DoorState.Closed)
        {
            State = DoorState.Opening;
            _openingProgress = 0f;
        }
    }

    /// <summary>
    /// Reset door to closed state (for room transitions).
    /// </summary>
    public void Close()
    {
        State = DoorState.Closed;
        _openingProgress = 0f;
    }

    /// <summary>
    /// Update door state machine. Transitions Opening -> Open after duration.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (State == DoorState.Opening)
        {
            _openingProgress += deltaTime;
            if (_openingProgress >= OPENING_DURATION)
            {
                _openingProgress = OPENING_DURATION;
                State = DoorState.Open;
            }
        }
    }

    /// <summary>
    /// Get opening progress (0-1) for animation purposes.
    /// </summary>
    public float OpeningProgress => _openingProgress / OPENING_DURATION;

    /// <summary>
    /// Check if player can pass through this door.
    /// </summary>
    public bool CanPlayerEnter() => State == DoorState.Open;

    /// <summary>
    /// Check if player position is inside trigger volume.
    /// </summary>
    public bool IsPlayerInTrigger(Vector3 playerPos)
    {
        return TriggerVolume.Contains(playerPos) == ContainmentType.Contains;
    }

    /// <summary>
    /// Get collision box only if door is closed, null otherwise.
    /// Used for dynamic collision list building.
    /// </summary>
    public BoundingBox? GetActiveCollision()
    {
        return State == DoorState.Closed ? CollisionBox : null;
    }
}
