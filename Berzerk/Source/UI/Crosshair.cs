using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Berzerk.UI;

/// <summary>
/// Simple crosshair rendered at screen center.
/// Creates texture programmatically (no external asset needed).
/// </summary>
public class Crosshair
{
    private Texture2D _texture;
    private Vector2 _origin;
    private const int Size = 24;      // Crosshair size in pixels
    private const int Thickness = 2;  // Line thickness
    private const int Gap = 4;        // Gap in center

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        // Create crosshair texture programmatically
        _texture = new Texture2D(graphicsDevice, Size, Size);
        Color[] pixels = new Color[Size * Size];

        int center = Size / 2;

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int dx = Math.Abs(x - center);
                int dy = Math.Abs(y - center);

                bool isHorizontalLine = dy < Thickness && dx >= Gap && dx < Size / 2;
                bool isVerticalLine = dx < Thickness && dy >= Gap && dy < Size / 2;

                if (isHorizontalLine || isVerticalLine)
                {
                    pixels[y * Size + x] = Color.White;
                }
                else
                {
                    pixels[y * Size + x] = Color.Transparent;
                }
            }
        }

        _texture.SetData(pixels);
        _origin = new Vector2(Size / 2f, Size / 2f);
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        Vector2 screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        spriteBatch.Draw(_texture, screenCenter, null, Color.LimeGreen, 0f, _origin, 1f, SpriteEffects.None, 0f);
    }
}
