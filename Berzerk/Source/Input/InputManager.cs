using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Berzerk.Source.Input;

/// <summary>
/// Centralized input handling with state tracking for keyboard and mouse.
/// Follows MonoGame polling pattern from Phase 1 research.
/// </summary>
public class InputManager
{
    private KeyboardState _previousKeyboard;
    private KeyboardState _currentKeyboard;
    private MouseState _previousMouse;
    private MouseState _currentMouse;

    /// <summary>
    /// Update input state. Call once per frame at the start of game Update().
    /// </summary>
    public void Update()
    {
        // Shift current to previous
        _previousKeyboard = _currentKeyboard;
        _previousMouse = _currentMouse;

        // Sample new state (once per frame only - anti-pattern from research)
        _currentKeyboard = Keyboard.GetState();
        _currentMouse = Mouse.GetState();
    }

    /// <summary>
    /// Check if a key was pressed this frame (went from up to down).
    /// </summary>
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }

    /// <summary>
    /// Check if a key is currently held down.
    /// </summary>
    public bool IsKeyHeld(Keys key)
    {
        return _currentKeyboard.IsKeyDown(key);
    }

    /// <summary>
    /// Check if a key was released this frame (went from down to up).
    /// </summary>
    public bool IsKeyReleased(Keys key)
    {
        return _currentKeyboard.IsKeyUp(key) && _previousKeyboard.IsKeyDown(key);
    }

    /// <summary>
    /// Check if left mouse button was pressed this frame.
    /// </summary>
    public bool IsLeftMousePressed()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed &&
               _previousMouse.LeftButton == ButtonState.Released;
    }

    /// <summary>
    /// Check if right mouse button was pressed this frame.
    /// </summary>
    public bool IsRightMousePressed()
    {
        return _currentMouse.RightButton == ButtonState.Pressed &&
               _previousMouse.RightButton == ButtonState.Released;
    }

    /// <summary>
    /// Get current mouse position in window coordinates.
    /// </summary>
    public Point MousePosition => _currentMouse.Position;

    /// <summary>
    /// Get mouse movement delta from previous frame (for camera control).
    /// </summary>
    public Point MouseDelta => new Point(
        _currentMouse.X - _previousMouse.X,
        _currentMouse.Y - _previousMouse.Y
    );

    /// <summary>
    /// Get scroll wheel delta from previous frame.
    /// Positive = scroll up (zoom in), Negative = scroll down (zoom out).
    /// ScrollWheelValue is cumulative, so we calculate per-frame delta.
    /// </summary>
    public int ScrollWheelDelta => _currentMouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;

    /// <summary>
    /// Check if left mouse button is currently held down.
    /// </summary>
    public bool IsLeftMouseHeld()
    {
        return _currentMouse.LeftButton == ButtonState.Pressed;
    }

    /// <summary>
    /// Check if right mouse button is currently held down (for camera orbit).
    /// </summary>
    public bool IsRightMouseHeld()
    {
        return _currentMouse.RightButton == ButtonState.Pressed;
    }
}
