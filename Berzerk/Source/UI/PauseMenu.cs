using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Berzerk.UI;

public class PauseMenu
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _resumeButton;
    private Rectangle _quitButton;
    private bool _isHoveringResume;
    private bool _isHoveringQuit;

    public event Action? OnResume;
    public event Action? OnQuit;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Font");

        // Create 1x1 pixel texture for backgrounds and buttons
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(MouseState currentMouse, MouseState previousMouse)
    {
        // Check hover state for both buttons
        _isHoveringResume = _resumeButton.Contains(currentMouse.Position);
        _isHoveringQuit = _quitButton.Contains(currentMouse.Position);

        // Detect click on Resume button
        if (_isHoveringResume &&
            currentMouse.LeftButton == ButtonState.Released &&
            previousMouse.LeftButton == ButtonState.Pressed)
        {
            OnResume?.Invoke();
        }

        // Detect click on Quit button
        if (_isHoveringQuit &&
            currentMouse.LeftButton == ButtonState.Released &&
            previousMouse.LeftButton == ButtonState.Pressed)
        {
            OnQuit?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        // Draw semi-transparent black overlay over gameplay
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // Draw "PAUSED" title centered
        string titleText = "PAUSED";
        Vector2 titleSize = _font.MeasureString(titleText);
        Vector2 titlePosition = new Vector2(
            viewport.Width / 2f - titleSize.X / 2f,
            viewport.Height / 2f - 100
        );
        spriteBatch.DrawString(_font, titleText, titlePosition, Color.White);

        // Draw "Resume" button
        float resumeCenterY = viewport.Height / 2f;
        DrawButton(spriteBatch, "Resume", resumeCenterY, _isHoveringResume, out _resumeButton, viewport);

        // Draw "Quit" button
        float quitCenterY = viewport.Height / 2f + 60;
        DrawButton(spriteBatch, "Quit", quitCenterY, _isHoveringQuit, out _quitButton, viewport);
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
