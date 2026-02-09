using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Berzerk.UI;

/// <summary>
/// Displays current score centered at top of screen.
/// </summary>
public class ScoreCounter
{
    private SpriteFont _font;

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    /// <summary>
    /// Draw score counter centered at top of screen.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, int score, Viewport viewport)
    {
        string text = $"Score: {score}";
        Vector2 textSize = _font.MeasureString(text);
        Vector2 position = new Vector2(viewport.Width / 2f - textSize.X / 2f, 20);

        spriteBatch.DrawString(_font, text, position, Color.White);
    }
}
