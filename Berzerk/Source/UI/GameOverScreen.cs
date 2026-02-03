using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Berzerk.UI;

public class GameOverScreen
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private string _message = "GAME OVER\n\nPress R to Restart";

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Font");

        // Create 1x1 pixel for background
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        // Draw black background
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black);

        // Measure text for centering
        Vector2 textSize = _font.MeasureString(_message);
        Vector2 screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
        Vector2 textPosition = screenCenter - textSize / 2f;

        // Draw text centered
        spriteBatch.DrawString(_font, _message, textPosition, Color.White);
    }
}
