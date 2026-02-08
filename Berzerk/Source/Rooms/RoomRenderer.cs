using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Berzerk.Source.Graphics;

namespace Berzerk.Source.Rooms;

/// <summary>
/// Renders room geometry: walls and doors with state-based coloring.
/// Uses DebugRenderer for consistent wireframe rendering.
/// </summary>
public class RoomRenderer
{
    private DebugRenderer _debugRenderer;
    private bool _showTriggers = false;  // Toggle for debug visualization

    public RoomRenderer(GraphicsDevice graphicsDevice)
    {
        _debugRenderer = new DebugRenderer(graphicsDevice);
    }

    /// <summary>
    /// Toggle trigger volume visualization for debugging.
    /// </summary>
    public bool ShowTriggers
    {
        get => _showTriggers;
        set => _showTriggers = value;
    }

    /// <summary>
    /// Draw complete room: walls and doors.
    /// </summary>
    public void Draw(Room room, Matrix view, Matrix projection)
    {
        // Draw walls in white (matches existing test wall rendering)
        _debugRenderer.DrawBoundingBoxes(room.Walls, view, projection, Color.White);

        // Draw each door with state-based color
        foreach (var door in room.Doors.Values)
        {
            _debugRenderer.DrawDoor(door, view, projection);

            // Optionally draw trigger volumes for debugging
            if (_showTriggers)
            {
                _debugRenderer.DrawDoorTrigger(door, view, projection);
            }
        }
    }

    /// <summary>
    /// Draw floor grid for room.
    /// </summary>
    public void DrawFloor(Matrix view, Matrix projection)
    {
        _debugRenderer.DrawFloor(view, projection);
    }
}
