using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Input;
using System;

namespace Berzerk.UI;

public class StartMenu
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _buttonBounds;
    private bool _isHovering;
    private InputManager _inputManager;
    private float _timeElapsed = 0f;
    private bool _hasStarted = false;

    public event Action? OnStartGame;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice, InputManager inputManager)
    {
        _font = content.Load<SpriteFont>("Font");
        _inputManager = inputManager;

        // Create 1x1 pixel texture for button backgrounds
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(Viewport viewport, float deltaTime)
    {
        if (_hasStarted) return;

        // WORKAROUND for macOS MonoGame input bug: Auto-start after 3 seconds
        // macOS MonoGame has known issues with Keyboard/Mouse.GetState() not working on initial window load
        // This ensures the game is playable even if input doesn't work
        _timeElapsed += deltaTime;
        if (_timeElapsed >= 3f)
        {
            Console.WriteLine("Auto-starting game (macOS input workaround)");
            OnStartGame?.Invoke();
            _hasStarted = true;
            return;
        }

        // Try input detection (works after window gains focus on some macOS configurations)
        if (_inputManager.IsKeyPressed(Keys.Enter) || _inputManager.IsKeyPressed(Keys.Space))
        {
            OnStartGame?.Invoke();
            _hasStarted = true;
            return;
        }

        // Try mouse click detection
        Point mousePos = _inputManager.MousePosition;
        _isHovering = _buttonBounds.Contains(mousePos);

        if (_isHovering && _inputManager.IsLeftMousePressed())
        {
            OnStartGame?.Invoke();
            _hasStarted = true;
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
            viewport.Height / 2f - 120
        );
        spriteBatch.DrawString(_font, titleText, titlePosition, Color.White);

        // Draw instruction text with countdown
        int countdown = (int)Math.Ceiling(3f - _timeElapsed);
        string instructionText = $"Starting in {countdown}... (or press ENTER/SPACE/click)";
        Vector2 instructionSize = _font.MeasureString(instructionText);
        Vector2 instructionPosition = new Vector2(
            viewport.Width / 2f - instructionSize.X / 2f,
            viewport.Height / 2f - 40
        );
        spriteBatch.DrawString(_font, instructionText, instructionPosition, Color.Gray);

        // Draw "Start Game" button (kept for mouse users)
        string buttonText = "Start Game";
        float buttonCenterY = viewport.Height / 2f + 40;
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
