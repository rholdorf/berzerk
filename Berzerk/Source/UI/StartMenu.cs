using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Berzerk.UI;

public class StartMenu
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _buttonBounds;
    private bool _isHovering;

    public event Action? OnStartGame;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Font");

        // Create 1x1 pixel texture for button backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(MouseState currentMouse, MouseState previousMouse)
    {
        // Check if mouse is hovering over button
        _isHovering = _buttonBounds.Contains(currentMouse.Position);

        // Detect click: released after being pressed while hovering
        if (_isHovering &&
            currentMouse.LeftButton == ButtonState.Released &&
            previousMouse.LeftButton == ButtonState.Pressed)
        {
            OnStartGame?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        // Draw black background
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black);

        // Draw game title "BERZERK" centered above center
        string titleText = "BERZERK";
        Vector2 titleSize = _font.MeasureString(titleText);
        Vector2 titlePosition = new Vector2(
            viewport.Width / 2f - titleSize.X / 2f,
            viewport.Height / 2f - 80
        );
        spriteBatch.DrawString(_font, titleText, titlePosition, Color.White);

        // Draw "Start Game" button
        string buttonText = "Start Game";
        float buttonCenterY = viewport.Height / 2f + 20;
        DrawButton(spriteBatch, buttonText, buttonCenterY, _isHovering, out _buttonBounds, viewport);
    }

    private void DrawButton(SpriteBatch spriteBatch, string text, float centerY, bool isHovering,
        out Rectangle bounds, Viewport viewport)
    {
        // Measure text and calculate button dimensions
        Vector2 textSize = _font.MeasureString(text);
        int buttonWidth = (int)(textSize.X + 40);
        int buttonHeight = (int)(textSize.Y + 20);

        // Center button horizontally
        int buttonX = viewport.Width / 2 - buttonWidth / 2;
        int buttonY = (int)(centerY - buttonHeight / 2);

        bounds = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

        // Draw button background (darker gray on hover)
        Color buttonColor = isHovering ? Color.DarkGray : Color.Gray * 0.5f;
        spriteBatch.Draw(_pixelTexture, bounds, buttonColor);

        // Draw button text centered
        Vector2 textPosition = new Vector2(
            buttonX + (buttonWidth - textSize.X) / 2f,
            buttonY + (buttonHeight - textSize.Y) / 2f
        );
        spriteBatch.DrawString(_font, text, textPosition, Color.White);
    }
}
