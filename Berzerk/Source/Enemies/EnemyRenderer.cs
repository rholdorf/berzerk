using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Berzerk.Source.Combat;

namespace Berzerk.Source.Enemies;

/// <summary>
/// Renders enemies, explosion effects, and health pickups.
/// Enemies render as colored cubes (placeholder for Phase 5).
/// Explosions render as expanding/shrinking spheres.
/// Health pickups render as green spheres.
/// </summary>
public class EnemyRenderer
{
    private GraphicsDevice _graphicsDevice;
    private BasicEffect _effect;
    private VertexPositionNormalTexture[] _sphereVertices;
    private short[] _sphereIndices;

    private const int SPHERE_SEGMENTS = 8; // Low poly arcade style
    private const float HEALTH_PICKUP_RADIUS = 0.3f; // Per plan specification

    /// <summary>
    /// Initialize renderer with graphics device.
    /// </summary>
    public EnemyRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        GenerateSphereMesh();

        // Create BasicEffect for rendering
        _effect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = false,
            LightingEnabled = false
        };
    }

    /// <summary>
    /// Generate UV sphere mesh with specified segments.
    /// Reuses ProjectileRenderer pattern for consistency.
    /// </summary>
    private void GenerateSphereMesh()
    {
        int latSegments = SPHERE_SEGMENTS;
        int lonSegments = SPHERE_SEGMENTS;

        List<VertexPositionNormalTexture> vertices = new List<VertexPositionNormalTexture>();

        // Generate vertices
        for (int lat = 0; lat <= latSegments; lat++)
        {
            float phi = MathHelper.Pi * lat / latSegments;
            float y = (float)Math.Cos(phi);
            float sinPhi = (float)Math.Sin(phi);

            for (int lon = 0; lon <= lonSegments; lon++)
            {
                float theta = MathHelper.TwoPi * lon / lonSegments;
                float x = sinPhi * (float)Math.Cos(theta);
                float z = sinPhi * (float)Math.Sin(theta);

                Vector3 position = new Vector3(x, y, z);
                Vector3 normal = Vector3.Normalize(position);
                Vector2 texCoord = new Vector2((float)lon / lonSegments, (float)lat / latSegments);

                vertices.Add(new VertexPositionNormalTexture(position, normal, texCoord));
            }
        }

        _sphereVertices = vertices.ToArray();

        // Generate indices for triangle list
        List<short> indices = new List<short>();
        int stride = lonSegments + 1;

        for (int lat = 0; lat < latSegments; lat++)
        {
            for (int lon = 0; lon < lonSegments; lon++)
            {
                int current = lat * stride + lon;
                int next = current + stride;

                // Two triangles per quad
                indices.Add((short)current);
                indices.Add((short)next);
                indices.Add((short)(current + 1));

                indices.Add((short)(current + 1));
                indices.Add((short)next);
                indices.Add((short)(next + 1));
            }
        }

        _sphereIndices = indices.ToArray();
    }

    /// <summary>
    /// Draw enemies as colored cubes (placeholder rendering).
    /// Robot Mixamo models will be loaded in Plan 04 integration.
    /// Color indicates state: red=Chase, orange=Attack, gray=Dying.
    /// </summary>
    public void DrawEnemies(IReadOnlyList<EnemyController> enemies, Matrix view, Matrix projection)
    {
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive) continue;

            // Determine color based on state
            Color stateColor = enemy.Health.IsDead ? Color.Gray :
                              Color.Red; // Simplified: Red for active enemies

            DrawCube(enemy.Transform.Position, 1.0f, stateColor, view, projection);
        }
    }

    /// <summary>
    /// Draw explosion effects as expanding/shrinking spheres with fade.
    /// </summary>
    public void DrawExplosions(IReadOnlyList<ExplosionEffect> effects, Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.LightingEnabled = false; // Emissive only

        foreach (var effect in effects)
        {
            if (!effect.IsActive) continue;

            // Create world matrix with scaling based on effect radius
            float radius = effect.GetRadius();
            _effect.World = Matrix.CreateScale(radius) *
                           Matrix.CreateTranslation(effect.Position);

            // Set emissive color with fade
            Color color = effect.GetColor();
            _effect.EmissiveColor = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
            _effect.DiffuseColor = _effect.EmissiveColor;

            // Draw sphere mesh
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _sphereVertices, 0, _sphereVertices.Length,
                    _sphereIndices, 0, _sphereIndices.Length / 3
                );
            }
        }
    }

    /// <summary>
    /// Draw health pickups as green bobbing spheres.
    /// </summary>
    public void DrawHealthPickups(IReadOnlyList<HealthPickup> pickups, Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.LightingEnabled = false; // Emissive only
        _effect.EmissiveColor = new Vector3(0.0f, 1.0f, 0.0f); // Green
        _effect.DiffuseColor = _effect.EmissiveColor;

        foreach (var pickup in pickups)
        {
            if (!pickup.IsActive) continue;

            Vector3 displayPos = pickup.GetDisplayPosition();

            // Create world matrix with health pickup radius
            _effect.World = Matrix.CreateScale(HEALTH_PICKUP_RADIUS) *
                           Matrix.CreateTranslation(displayPos);

            // Draw sphere mesh
            foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawUserIndexedPrimitives(
                    PrimitiveType.TriangleList,
                    _sphereVertices, 0, _sphereVertices.Length,
                    _sphereIndices, 0, _sphereIndices.Length / 3
                );
            }
        }
    }

    /// <summary>
    /// Helper method to draw a cube at specified position.
    /// Uses DebugRenderer pattern for placeholder rendering.
    /// </summary>
    private void DrawCube(Vector3 position, float size, Color color, Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;
        _effect.World = Matrix.Identity;
        _effect.VertexColorEnabled = true;
        _effect.LightingEnabled = false;

        float halfSize = size * 0.5f;
        Vector3 min = position - new Vector3(halfSize);
        Vector3 max = position + new Vector3(halfSize);

        // Create vertices for cube edges (12 edges = 24 vertices)
        VertexPositionColor[] vertices = new VertexPositionColor[24];

        // Bottom face edges
        vertices[0] = new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color);
        vertices[1] = new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color);
        vertices[2] = new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color);
        vertices[3] = new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color);
        vertices[4] = new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color);
        vertices[5] = new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color);
        vertices[6] = new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color);
        vertices[7] = new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color);

        // Top face edges
        vertices[8] = new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color);
        vertices[9] = new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color);
        vertices[10] = new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color);
        vertices[11] = new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color);
        vertices[12] = new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color);
        vertices[13] = new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color);
        vertices[14] = new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color);
        vertices[15] = new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color);

        // Vertical edges
        vertices[16] = new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), color);
        vertices[17] = new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), color);
        vertices[18] = new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), color);
        vertices[19] = new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), color);
        vertices[20] = new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), color);
        vertices[21] = new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), color);
        vertices[22] = new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), color);
        vertices[23] = new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), color);

        foreach (EffectPass pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList,
                vertices,
                0,
                12
            );
        }

        // Reset to no vertex color for sphere rendering
        _effect.VertexColorEnabled = false;
    }
}
