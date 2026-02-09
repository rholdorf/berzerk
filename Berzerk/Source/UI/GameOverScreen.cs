using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Berzerk.Source.Input;
using System;

namespace Berzerk.UI;

public class GameOverScreen
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;

    // Button rectangles and hover states
    private Rectangle _restartButton;
    private Rectangle _quitButton;
    private bool _isHoveringRestart;
    private bool _isHoveringQuit;
    private InputManager _inputManager;

    // Events
    public event Action? OnRestart;
    public event Action? OnQuit;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice, InputManager inputManager)
    {
        _font = content.Load<SpriteFont>("Font");
        _inputManager = inputManager;

        // Create 1x1 pixel for background
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    /// <summary>
    /// Update button hover and click detection.
    /// </summary>
    public void Update()
    {
        // Use InputManager for reliable mouse input on all platforms
        Point mousePos = _inputManager.MousePosition;

        // Update hover states
        _isHoveringRestart = _restartButton.Contains(mousePos);
        _isHoveringQuit = _quitButton.Contains(mousePos);

        // Detect clicks
        if (_inputManager.IsLeftMousePressed())
        {
            if (_isHoveringRestart)
            {
                OnRestart?.Invoke();
            }
            else if (_isHoveringQuit)
            {
                OnQuit?.Invoke();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport, int finalScore)
    {
        // Draw black background
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black);

        float centerX = viewport.Width / 2f;

        // Draw "GAME OVER" in red
        string gameOverText = "GAME OVER";
        Vector2 gameOverSize = _font.MeasureString(gameOverText);
        Vector2 gameOverPos = new Vector2(centerX - gameOverSize.X / 2f, viewport.Height / 2f - 120);
        spriteBatch.DrawString(_font, gameOverText, gameOverPos, Color.Red);

        // Draw final score
        string scoreText = $"Final Score: {finalScore}";
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 scorePos = new Vector2(centerX - scoreSize.X / 2f, viewport.Height / 2f - 50);
        spriteBatch.DrawString(_font, scoreText, scorePos, Color.White);

        // Draw Restart button
        DrawButton(spriteBatch, "Restart", centerX, viewport.Height / 2f + 30, _isHoveringRestart, out _restartButton);

        // Draw Quit button
        DrawButton(spriteBatch, "Quit", centerX, viewport.Height / 2f + 100, _isHoveringQuit, out _quitButton);
    }

    /// <summary>
    /// Draw button with hover effect (same pattern as PauseMenu).
    /// </summary>
    private void DrawButton(SpriteBatch spriteBatch, string text, float centerX, float centerY, bool isHovering, out Rectangle bounds)
    {
        Vector2 textSize = _font.MeasureString(text);
        const int paddingX = 40;
        const int paddingY = 20;

        int buttonWidth = (int)textSize.X + paddingX * 2;
        int buttonHeight = (int)textSize.Y + paddingY * 2;

        bounds = new Rectangle(
            (int)(centerX - buttonWidth / 2),
            (int)(centerY - buttonHeight / 2),
            buttonWidth,
            buttonHeight
        );

        // Background color (darker on hover)
        Color bgColor = isHovering ? Color.DarkGray : Color.Gray;
        spriteBatch.Draw(_pixelTexture, bounds, bgColor);

        // Text (white)
        Vector2 textPos = new Vector2(
            centerX - textSize.X / 2f,
            centerY - textSize.Y / 2f
        );
        spriteBatch.DrawString(_font, text, textPos, Color.White);
    }
}
