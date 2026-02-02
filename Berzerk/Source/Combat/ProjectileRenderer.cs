using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Berzerk.Source.Combat;

/// <summary>
/// Renders projectiles as glowing spheres using BasicEffect emissive color.
/// Uses low-poly sphere mesh for arcade visual style.
/// </summary>
public class ProjectileRenderer
{
    private GraphicsDevice _graphicsDevice;
    private BasicEffect _effect;
    private VertexPositionNormalTexture[] _sphereVertices;
    private short[] _sphereIndices;

    private const int SPHERE_SEGMENTS = 8; // Low poly for arcade style
    private const float SPHERE_RADIUS = 0.2f; // Matches Projectile.Radius

    /// <summary>
    /// Initialize renderer with graphics device.
    /// </summary>
    public ProjectileRenderer(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;
        GenerateSphereMesh();

        // Create BasicEffect with emissive glow
        _effect = new BasicEffect(_graphicsDevice)
        {
            VertexColorEnabled = false,
            LightingEnabled = false, // Emissive only, no shadows
            EmissiveColor = new Vector3(0.3f, 0.9f, 1.0f), // Cyan laser
            DiffuseColor = new Vector3(0.3f, 0.9f, 1.0f)
        };
    }

    /// <summary>
    /// Generate UV sphere mesh with specified segments.
    /// </summary>
    private void GenerateSphereMesh()
    {
        int latSegments = SPHERE_SEGMENTS;
        int lonSegments = SPHERE_SEGMENTS;
        int vertexCount = (latSegments + 1) * (lonSegments + 1);

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
                Vector3 normal = Vector3.Normalize(position); // Normal points outward
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
    /// Draw all active projectiles as glowing spheres.
    /// </summary>
    public void Draw(IReadOnlyList<Projectile> projectiles, Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;

        // Reset to cyan for projectiles
        _effect.EmissiveColor = new Vector3(0.3f, 0.9f, 1.0f);

        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;

            // Create world matrix: scale to radius, then translate to position
            _effect.World = Matrix.CreateScale(SPHERE_RADIUS) *
                           Matrix.CreateTranslation(projectile.Transform.Position);

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
    /// Draw impact effects as fading orange spheres.
    /// </summary>
    public void DrawEffects(IReadOnlyList<ImpactEffect> effects, Matrix view, Matrix projection)
    {
        _effect.View = view;
        _effect.Projection = projection;

        foreach (var effect in effects)
        {
            if (!effect.IsActive) continue;

            // Create world matrix with scaling based on effect lifetime
            _effect.World = Matrix.CreateScale(effect.Scale) *
                           Matrix.CreateTranslation(effect.Position);

            // Orange/yellow fading glow
            _effect.EmissiveColor = new Vector3(1.0f, 0.8f, 0.3f) * effect.Alpha;

            // Draw same sphere mesh
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

        // Reset to cyan for next projectile draw
        _effect.EmissiveColor = new Vector3(0.3f, 0.9f, 1.0f);
    }
}
