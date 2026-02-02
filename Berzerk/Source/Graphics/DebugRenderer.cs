using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

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
}
