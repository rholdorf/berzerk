using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Berzerk.UI;

public class HealthBar
{
    private Texture2D _pixelTexture;

    // Bar dimensions and position
    private const int BarWidth = 200;
    private const int BarHeight = 20;
    private const int BarX = 20;
    private const int BarY = 20;
    private const int BorderWidth = 2;

    // Flash effect
    private bool _isFlashing;
    private float _flashAlpha;
    private const float FlashDuration = 0.4f;

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // Create 1x1 white pixel texture
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Trigger damage flash effect (coordinated with DamageVignette).
    /// </summary>
    public void Trigger()
    {
        _isFlashing = true;
        _flashAlpha = 1.0f;
    }

    /// <summary>
    /// Update flash effect with exponential decay.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isFlashing) return;

        // Exponential decay
        _flashAlpha *= (float)Math.Pow(0.01, deltaTime / FlashDuration);

        // Stop flashing when alpha is negligible
        if (_flashAlpha < 0.01f)
        {
            _flashAlpha = 0f;
            _isFlashing = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, int currentHealth, int maxHealth)
    {
        // Calculate fill percentage (clamp to 0-1)
        float healthPercent = (float)currentHealth / maxHealth;
        healthPercent = MathHelper.Clamp(healthPercent, 0f, 1f);
        int fillWidth = (int)(BarWidth * healthPercent);

        // Determine fill color based on health percentage
        Color fillColor;
        if (healthPercent > 0.5f)
            fillColor = Color.LimeGreen;
        else if (healthPercent > 0.25f)
            fillColor = Color.Yellow;
        else
            fillColor = Color.Red;

        // Draw border (dark gray)
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(BarX - BorderWidth, BarY - BorderWidth,
                          BarWidth + BorderWidth * 2, BarHeight + BorderWidth * 2),
            Color.DarkGray);

        // Draw background (black)
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(BarX, BarY, BarWidth, BarHeight),
            Color.Black);

        // Draw health fill
        if (fillWidth > 0)
        {
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(BarX, BarY, fillWidth, BarHeight),
                fillColor);
        }

        // Draw red flash overlay when flashing
        if (_isFlashing && _flashAlpha > 0)
        {
            spriteBatch.Draw(_pixelTexture,
                new Rectangle(BarX, BarY, fillWidth, BarHeight),
                Color.Red * _flashAlpha);
        }
    }
}
