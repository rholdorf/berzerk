using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Berzerk.Source.Rooms;

/// <summary>
/// Room data container with walls, doors, and enemy spawn points.
/// Passive data structure - RoomManager handles logic.
/// </summary>
public class Room
{
    public List<BoundingBox> Walls { get; private set; }
    public Dictionary<Direction, Door> Doors { get; private set; }
    public List<Vector3> EnemySpawnPoints { get; private set; }

    // Room dimensions (matching existing game area)
    private const float ROOM_SIZE = 30f;        // 30x30 room
    private const float HALF_SIZE = ROOM_SIZE / 2f;
    private const float WALL_THICKNESS = 1f;
    private const float WALL_HEIGHT = 5f;
    private const float DOOR_WIDTH = 4f;        // Opening in wall for door

    public Room()
    {
        Walls = CreateMazeWalls();
        Doors = CreateDoors();
        EnemySpawnPoints = CreateSpawnPoints();
    }

    /// <summary>
    /// Create handcrafted maze walls with door openings at cardinal positions.
    /// Based on existing ThirdPersonCamera.CreateTestWalls pattern.
    /// </summary>
    private List<BoundingBox> CreateMazeWalls()
    {
        var walls = new List<BoundingBox>();
        float doorHalf = DOOR_WIDTH / 2f;

        // North wall (-Z edge) with door opening at center
        // Left segment
        walls.Add(new BoundingBox(
            new Vector3(-HALF_SIZE, 0, -HALF_SIZE),
            new Vector3(-doorHalf, WALL_HEIGHT, -HALF_SIZE + WALL_THICKNESS)
        ));
        // Right segment
        walls.Add(new BoundingBox(
            new Vector3(doorHalf, 0, -HALF_SIZE),
            new Vector3(HALF_SIZE, WALL_HEIGHT, -HALF_SIZE + WALL_THICKNESS)
        ));

        // South wall (+Z edge) with door opening at center
        walls.Add(new BoundingBox(
            new Vector3(-HALF_SIZE, 0, HALF_SIZE - WALL_THICKNESS),
            new Vector3(-doorHalf, WALL_HEIGHT, HALF_SIZE)
        ));
        walls.Add(new BoundingBox(
            new Vector3(doorHalf, 0, HALF_SIZE - WALL_THICKNESS),
            new Vector3(HALF_SIZE, WALL_HEIGHT, HALF_SIZE)
        ));

        // East wall (+X edge) with door opening at center
        walls.Add(new BoundingBox(
            new Vector3(HALF_SIZE - WALL_THICKNESS, 0, -HALF_SIZE),
            new Vector3(HALF_SIZE, WALL_HEIGHT, -doorHalf)
        ));
        walls.Add(new BoundingBox(
            new Vector3(HALF_SIZE - WALL_THICKNESS, 0, doorHalf),
            new Vector3(HALF_SIZE, WALL_HEIGHT, HALF_SIZE)
        ));

        // West wall (-X edge) with door opening at center
        walls.Add(new BoundingBox(
            new Vector3(-HALF_SIZE, 0, -HALF_SIZE),
            new Vector3(-HALF_SIZE + WALL_THICKNESS, WALL_HEIGHT, -doorHalf)
        ));
        walls.Add(new BoundingBox(
            new Vector3(-HALF_SIZE, 0, doorHalf),
            new Vector3(-HALF_SIZE + WALL_THICKNESS, WALL_HEIGHT, HALF_SIZE)
        ));

        // Interior maze walls (4 pillars for cover, matching Berzerk style)
        float pillarOffset = 7f;
        float pillarSize = 2f;

        // Northwest pillar
        walls.Add(new BoundingBox(
            new Vector3(-pillarOffset - pillarSize, 0, -pillarOffset - pillarSize),
            new Vector3(-pillarOffset, WALL_HEIGHT - 1, -pillarOffset)
        ));
        // Northeast pillar
        walls.Add(new BoundingBox(
            new Vector3(pillarOffset, 0, -pillarOffset - pillarSize),
            new Vector3(pillarOffset + pillarSize, WALL_HEIGHT - 1, -pillarOffset)
        ));
        // Southwest pillar
        walls.Add(new BoundingBox(
            new Vector3(-pillarOffset - pillarSize, 0, pillarOffset),
            new Vector3(-pillarOffset, WALL_HEIGHT - 1, pillarOffset + pillarSize)
        ));
        // Southeast pillar
        walls.Add(new BoundingBox(
            new Vector3(pillarOffset, 0, pillarOffset),
            new Vector3(pillarOffset + pillarSize, WALL_HEIGHT - 1, pillarOffset + pillarSize)
        ));

        // Center horizontal wall (creates maze feel)
        walls.Add(new BoundingBox(
            new Vector3(-3f, 0, -1f),
            new Vector3(3f, WALL_HEIGHT - 1, 1f)
        ));

        return walls;
    }

    /// <summary>
    /// Create doors at cardinal positions.
    /// </summary>
    private Dictionary<Direction, Door> CreateDoors()
    {
        return new Dictionary<Direction, Door>
        {
            { Direction.North, new Door(Direction.North, new Vector3(0, 0, -HALF_SIZE)) },
            { Direction.South, new Door(Direction.South, new Vector3(0, 0, HALF_SIZE)) },
            { Direction.East, new Door(Direction.East, new Vector3(HALF_SIZE, 0, 0)) },
            { Direction.West, new Door(Direction.West, new Vector3(-HALF_SIZE, 0, 0)) }
        };
    }

    /// <summary>
    /// Create enemy spawn points around room perimeter (away from doors and center).
    /// </summary>
    private List<Vector3> CreateSpawnPoints()
    {
        return new List<Vector3>
        {
            // Corner zones (primary spawn areas, far from doors)
            new Vector3(-10f, 0.5f, -10f),  // NW corner
            new Vector3(10f, 0.5f, -10f),   // NE corner
            new Vector3(-10f, 0.5f, 10f),   // SW corner
            new Vector3(10f, 0.5f, 10f),    // SE corner

            // Mid-wall zones (secondary spawns, between doors and corners)
            new Vector3(-10f, 0.5f, 0),     // West mid
            new Vector3(10f, 0.5f, 0),      // East mid
            new Vector3(0, 0.5f, -10f),     // North mid (near door, use carefully)
            new Vector3(0, 0.5f, 10f),      // South mid (near door, use carefully)
        };
    }

    /// <summary>
    /// Update all doors (called by RoomManager).
    /// </summary>
    public void UpdateDoors(float deltaTime)
    {
        foreach (var door in Doors.Values)
        {
            door.Update(deltaTime);
        }
    }

    /// <summary>
    /// Open all doors (called when room is cleared).
    /// </summary>
    public void OpenAllDoors()
    {
        foreach (var door in Doors.Values)
        {
            door.Open();
        }
    }

    /// <summary>
    /// Close all doors (called on room transition).
    /// </summary>
    public void CloseAllDoors()
    {
        foreach (var door in Doors.Values)
        {
            door.Close();
        }
    }

    /// <summary>
    /// Get all collision geometry including walls and closed door colliders.
    /// </summary>
    public List<BoundingBox> GetCollisionGeometry()
    {
        var colliders = new List<BoundingBox>(Walls);

        foreach (var door in Doors.Values)
        {
            var doorCollision = door.GetActiveCollision();
            if (doorCollision.HasValue)
            {
                colliders.Add(doorCollision.Value);
            }
        }

        return colliders;
    }
}
