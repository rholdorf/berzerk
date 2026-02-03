using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Berzerk.UI;

public class ScreenFade
{
    private Texture2D _pixelTexture;
    private float _alpha = 0f;
    private float _targetAlpha = 0f;
    private float _duration = 1.5f;  // Default 1.5 seconds (CONTEXT: 1-2 seconds)
    private bool _isActive = false;

    public bool IsComplete => _isActive && Math.Abs(_alpha - _targetAlpha) < 0.01f;
    public float Alpha => _alpha;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // Create 1x1 white pixel texture
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Start fade to target alpha over duration.
    /// </summary>
    public void Start(float targetAlpha, float duration)
    {
        _targetAlpha = targetAlpha;
        _duration = duration;
        _isActive = true;
    }

    /// <summary>
    /// Convenience method: fade to black.
    /// </summary>
    public void FadeToBlack(float duration = 1.5f)
    {
        Start(1f, duration);
    }

    /// <summary>
    /// Convenience method: fade from black (clear).
    /// </summary>
    public void FadeFromBlack(float duration = 0.5f)
    {
        Start(0f, duration);
    }

    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        // Linear interpolation toward target
        float step = deltaTime / _duration;

        if (_alpha < _targetAlpha)
        {
            _alpha = Math.Min(_alpha + step, _targetAlpha);
        }
        else if (_alpha > _targetAlpha)
        {
            _alpha = Math.Max(_alpha - step, _targetAlpha);
        }

        // Snap when close enough
        if (Math.Abs(_alpha - _targetAlpha) < 0.01f)
        {
            _alpha = _targetAlpha;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_alpha <= 0f) return;

        spriteBatch.Draw(
            _pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * _alpha
        );
    }

    public void Reset()
    {
        _alpha = 0f;
        _targetAlpha = 0f;
        _isActive = false;
    }
}
