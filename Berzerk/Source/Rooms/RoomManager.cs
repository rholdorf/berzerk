using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Berzerk.Source.Rooms;

/// <summary>
/// Manages room lifecycle: current room state, door opening on clear, and room transitions.
/// Wires to EnemyManager for room-clear detection.
/// </summary>
public class RoomManager
{
    private Room _currentRoom;
    private bool _roomCleared = false;
    private float _doorOpenDelay = 0f;
    private bool _doorsOpening = false;

    // Delay before doors open after room clear (for visual clarity)
    private const float DOOR_OPEN_DELAY = 0.5f;

    // Spawn offset from door (units inside room)
    private const float SPAWN_OFFSET_FROM_DOOR = 3f;

    /// <summary>
    /// Current room instance.
    /// </summary>
    public Room CurrentRoom => _currentRoom;

    /// <summary>
    /// Whether current room has been cleared of all enemies.
    /// </summary>
    public bool IsRoomCleared => _roomCleared;

    /// <summary>
    /// Event fired when room is cleared (all enemies defeated).
    /// </summary>
    public event Action OnRoomCleared;

    /// <summary>
    /// Event fired when player enters an open door.
    /// Provides exit direction for spawn position calculation.
    /// </summary>
    public event Action<Direction> OnRoomTransition;

    /// <summary>
    /// Initialize room manager with starting room.
    /// </summary>
    public void Initialize()
    {
        _currentRoom = new Room();
        _roomCleared = false;
        _doorsOpening = false;
        _doorOpenDelay = 0f;
    }

    /// <summary>
    /// Handle notification that all enemies in room are defeated.
    /// Call this from EnemyManager.OnAllEnemiesDefeated event.
    /// </summary>
    public void HandleAllEnemiesDefeated()
    {
        if (!_roomCleared)
        {
            _roomCleared = true;
            _doorsOpening = true;
            _doorOpenDelay = DOOR_OPEN_DELAY;
            Console.WriteLine("Room cleared! Doors opening in 0.5s...");
        }
    }

    /// <summary>
    /// Update room state, door animations, and check for player transition.
    /// </summary>
    public void Update(float deltaTime, Vector3 playerPos)
    {
        // Handle delayed door opening after room clear
        if (_doorsOpening)
        {
            _doorOpenDelay -= deltaTime;
            if (_doorOpenDelay <= 0)
            {
                _currentRoom.OpenAllDoors();
                _doorsOpening = false;
                OnRoomCleared?.Invoke();
                Console.WriteLine("Doors are now open!");
            }
        }

        // Update door animations
        _currentRoom.UpdateDoors(deltaTime);

        // Check for player entering open doors
        if (_roomCleared)
        {
            CheckDoorTransitions(playerPos);
        }
    }

    /// <summary>
    /// Check if player has entered any open door trigger volume.
    /// </summary>
    private void CheckDoorTransitions(Vector3 playerPos)
    {
        foreach (var kvp in _currentRoom.Doors)
        {
            Direction direction = kvp.Key;
            Door door = kvp.Value;

            if (door.CanPlayerEnter() && door.IsPlayerInTrigger(playerPos))
            {
                Console.WriteLine($"Player entered {direction} door! Transitioning...");
                OnRoomTransition?.Invoke(direction);
                break; // Only process one transition per frame
            }
        }
    }

    /// <summary>
    /// Perform room transition: reset doors, clear room state.
    /// Call this when handling OnRoomTransition event.
    /// </summary>
    public void TransitionToNewRoom()
    {
        // Close all doors for fresh challenge
        _currentRoom.CloseAllDoors();

        // Reset room state
        _roomCleared = false;
        _doorsOpening = false;
        _doorOpenDelay = 0f;

        Console.WriteLine("Room transitioned - fresh challenge begins!");
    }

    /// <summary>
    /// Calculate player spawn position based on entry direction.
    /// Exit North -> spawn at South door inside room, etc.
    /// </summary>
    public Vector3 GetSpawnPositionForEntry(Direction entryDirection)
    {
        // Entry direction is OPPOSITE of exit direction
        // If player exited North, they enter new room from South
        Direction entryDoor = GetOppositeDirection(entryDirection);

        if (_currentRoom.Doors.TryGetValue(entryDoor, out Door door))
        {
            // Spawn offset from door toward room center
            Vector3 offset = GetDirectionOffset(entryDoor) * SPAWN_OFFSET_FROM_DOOR;
            return door.Position + offset + new Vector3(0, 0.5f, 0); // Slight Y offset
        }

        // Fallback to room center
        return new Vector3(0, 0.5f, 0);
    }

    /// <summary>
    /// Get opposite cardinal direction.
    /// </summary>
    private Direction GetOppositeDirection(Direction direction)
    {
        return direction switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            Direction.West => Direction.East,
            _ => Direction.South
        };
    }

    /// <summary>
    /// Get Vector3 offset for direction (pointing INTO room).
    /// </summary>
    private Vector3 GetDirectionOffset(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector3(0, 0, 1),   // North door, offset south into room
            Direction.South => new Vector3(0, 0, -1),  // South door, offset north into room
            Direction.East => new Vector3(-1, 0, 0),   // East door, offset west into room
            Direction.West => new Vector3(1, 0, 0),    // West door, offset east into room
            _ => Vector3.Zero
        };
    }

    /// <summary>
    /// Get current collision geometry (walls + closed doors).
    /// </summary>
    public List<BoundingBox> GetCollisionGeometry()
    {
        return _currentRoom.GetCollisionGeometry();
    }

    /// <summary>
    /// Get spawn points for enemy placement.
    /// </summary>
    public List<Vector3> GetEnemySpawnPoints()
    {
        return _currentRoom.EnemySpawnPoints;
    }

    /// <summary>
    /// Reset room manager (for game restart).
    /// </summary>
    public void Reset()
    {
        _currentRoom.CloseAllDoors();
        _roomCleared = false;
        _doorsOpening = false;
        _doorOpenDelay = 0f;
    }
}
