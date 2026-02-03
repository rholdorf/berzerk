using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Berzerk.UI;

/// <summary>
/// Red screen vignette overlay for damage feedback.
/// Creates texture programmatically with radial gradient.
/// Fades out using exponential decay for frame-rate independence.
/// </summary>
public class DamageVignette
{
    private Texture2D? _vignetteTexture;
    private float _alpha = 0f;
    private float _fadeOutTime = 0.4f;  // Fade out over 0.4 seconds
    private bool _isActive = false;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // Create vignette texture with red gradient from edges
        int width = 256;
        int height = 256;
        _vignetteTexture = new Texture2D(graphicsDevice, width, height);
        Color[] pixels = new Color[width * height];

        float centerX = width / 2f;
        float centerY = height / 2f;
        float maxDist = (float)Math.Sqrt(centerX * centerX + centerY * centerY);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                float intensity = dist / maxDist;
                intensity = intensity * intensity;  // Quadratic falloff for stronger edge
                pixels[y * width + x] = new Color(1f, 0f, 0f, intensity);
            }
        }

        _vignetteTexture.SetData(pixels);
    }

    /// <summary>
    /// Trigger damage flash effect.
    /// </summary>
    public void Trigger()
    {
        _alpha = 1f;  // Instant flash to full intensity
        _isActive = true;
    }

    /// <summary>
    /// Update vignette fade. Uses exponential decay for frame-rate independence.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        // Exponential decay (project pattern from STATE.md)
        float decay = (float)Math.Pow(0.01, deltaTime / _fadeOutTime);
        _alpha *= decay;

        if (_alpha < 0.01f)
        {
            _alpha = 0f;
            _isActive = false;
        }
    }

    /// <summary>
    /// Draw vignette overlay fullscreen.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_vignetteTexture == null || _alpha <= 0f) return;

        spriteBatch.Draw(
            _vignetteTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.White * _alpha
        );
    }
}
