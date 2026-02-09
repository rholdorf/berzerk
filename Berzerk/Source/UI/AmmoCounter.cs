using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Berzerk.UI;

/// <summary>
/// Displays current magazine and reserve ammo at top-right corner.
/// Flashes red when magazine is low (< 10 rounds).
/// </summary>
public class AmmoCounter
{
    private SpriteFont _font;
    private float _totalGameTime = 0f;

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    /// <summary>
    /// Update time accumulator for flash animation.
    /// </summary>
    public void Update(float deltaTime)
    {
        _totalGameTime += deltaTime;
    }

    /// <summary>
    /// Draw ammo counter at top-right corner with low ammo flash.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, int currentMag, int reserveAmmo, Viewport viewport)
    {
        string text = $"{currentMag} / {reserveAmmo}";
        Vector2 textSize = _font.MeasureString(text);
        Vector2 position = new Vector2(viewport.Width - textSize.X - 20, 20);

        // Flash red when magazine is low (< 10 rounds)
        Color color;
        if (currentMag < 10)
        {
            // Sinusoidal pulse for smooth flash
            float flash = (float)(0.5 + 0.5 * Math.Sin(_totalGameTime * 8.0));
            color = Color.Lerp(Color.Red, Color.White, flash);
        }
        else
        {
            color = Color.White;
        }

        spriteBatch.DrawString(_font, text, position, color);
    }
}
