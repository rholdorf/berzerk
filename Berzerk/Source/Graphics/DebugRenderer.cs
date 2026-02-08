using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Berzerk.Source.Combat;
using Berzerk.Source.Rooms;

namespace Berzerk.Source.Graphics;

/// <summary>
/// Renders simple debug primitives like floors, walls, and bounding boxes.
/// </summary>
public class DebugRenderer
{
    private BasicEffect _effect;
    private GraphicsDevice _graphicsDevice;

    public DebugRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
    }

    /// <summary>
    /// Draws a grid floor at Y=0.
    /// </summary>
    public void DrawFloor(Matrix view, Matrix projection, float size = 30f, int divisions = 30)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.World = Matrix.Identity;

        List<VertexPositionColor> vertices = new List<VertexPositionColor>();
        Color gridColor = new Color(100, 100, 100);

        float step = size / divisions;
        float halfSize = size / 2f;

        // Draw grid lines along X axis
        for (int i = 0; i <= divisions; i++)
        {
            float z = -halfSize + i * step;
            vertices.Add(new VertexPositionColor(new Vector3(-halfSize, 0, z), gridColor));
            vertices.Add(new VertexPositionColor(new Vector3(halfSize, 0, z), gridColor));
        }

        // Draw grid lines along Z axis
        for (int i = 0; i <= divisions; i++)
        {
            float x = -halfSize + i * step;
            vertices.Add(new VertexPositionColor(new Vector3(x, 0, -halfSize), gridColor));
            vertices.Add(new VertexPositionColor(new Vector3(x, 0, halfSize), gridColor));
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices.ToArray(),
                0,
                vertices.Count / 2
            );
        }
    }

    /// <summary>
    /// Draws a wireframe box for a bounding box.
    /// </summary>
    public void DrawBoundingBox(BoundingBox box, Matrix view, Matrix projection, Color color)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.World = Matrix.Identity;

        Vector3[] corners = box.GetCorners();
        VertexPositionColor[] vertices = new VertexPositionColor[24];

        // Define the 12 edges of a box
        int[] indices = new int[]
        {
            // Bottom face
            0, 1,  1, 2,  2, 3,  3, 0,
            // Top face
            4, 5,  5, 6,  6, 7,  7, 4,
            // Vertical edges
            0, 4,  1, 5,  2, 6,  3, 7
        };

        for (int i = 0; i < indices.Length; i++)
        {
            vertices[i] = new VertexPositionColor(corners[indices[i]], color);
        }

        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices,
                0,
                12
            );
        }
    }

    /// <summary>
    /// Draws multiple bounding boxes.
    /// </summary>
    public void DrawBoundingBoxes(List<BoundingBox> boxes, Matrix view, Matrix projection, Color color)
    {
        foreach (var box in boxes)
        {
            DrawBoundingBox(box, view, projection, color);
        }
    }

    /// <summary>
    /// Draw test targets as colored cubes with hit feedback.
    /// </summary>
    public void DrawTargets(IReadOnlyList<TestTarget> targets, Matrix view, Matrix projection)
    {
        foreach (var target in targets)
        {
            if (target.IsActive)
            {
                BoundingBox box = target.GetBoundingBox();
                Color color = target.GetColor();
                DrawBoundingBox(box, view, projection, color);
            }
        }
    }

    /// <summary>
    /// Draw ammo pickups as floating yellow boxes.
    /// </summary>
    public void DrawPickups(IReadOnlyList<AmmoPickup> pickups, Matrix view, Matrix projection)
    {
        foreach (var pickup in pickups)
        {
            if (pickup.IsActive)
            {
                Vector3 displayPos = pickup.GetDisplayPosition();
                // Create small box around pickup position
                float size = 0.2f; // Small pickup box
                BoundingBox box = new BoundingBox(
                    displayPos - new Vector3(size),
                    displayPos + new Vector3(size)
                );
                Color color = pickup.GetColor();
                DrawBoundingBox(box, view, projection, color);
            }
        }
    }

    /// <summary>
    /// Draw health pickups as floating green boxes.
    /// </summary>
    public void DrawHealthPickups(IReadOnlyList<HealthPickup> pickups, Matrix view, Matrix projection)
    {
        foreach (var pickup in pickups)
        {
            if (pickup.IsActive)
            {
                Vector3 displayPos = pickup.GetDisplayPosition();
                // Create small box around pickup position
                float size = 0.3f; // Slightly larger for health (0.3f radius from plan)
                BoundingBox box = new BoundingBox(
                    displayPos - new Vector3(size),
                    displayPos + new Vector3(size)
                );
                Color color = pickup.GetColor();
                DrawBoundingBox(box, view, projection, color);
            }
        }
    }

    /// <summary>
    /// Draw a single door with color based on state.
    /// Red = Closed, Yellow = Opening, Green = Open.
    /// </summary>
    public void DrawDoor(Door door, Matrix view, Matrix projection)
    {
        Color doorColor = door.State switch
        {
            DoorState.Closed => Color.Red,
            DoorState.Opening => Color.Yellow,
            DoorState.Open => Color.Green,
            _ => Color.White
        };

        // Draw the collision box area (shows door frame)
        DrawBoundingBox(door.CollisionBox, view, projection, doorColor);
    }

    /// <summary>
    /// Draw door trigger volume for debugging (semi-transparent).
    /// </summary>
    public void DrawDoorTrigger(Door door, Matrix view, Matrix projection)
    {
        // Draw trigger volume in cyan for visibility
        DrawBoundingBox(door.TriggerVolume, view, projection, Color.Cyan * 0.5f);
    }
}
