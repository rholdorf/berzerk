using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Input;
using Berzerk.Graphics;
using System;

namespace Berzerk;

public class BerzerkGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private InputManager _inputManager;

    // Test animated model and animations
    private AnimatedModel _testCharacter;
    private AnimatedModel _idleAnimation;
    private AnimatedModel _walkAnimation;
    private AnimatedModel _runAnimation;
    private AnimatedModel _currentModel;

    // Camera matrices
    private Matrix _viewMatrix;
    private Matrix _projectionMatrix;

    public BerzerkGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // Initialize input manager
        _inputManager = new InputManager();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load test character and separate animation files
        _testCharacter = new AnimatedModel();
        _testCharacter.LoadContent(Content, "Models/test-character");

        _idleAnimation = new AnimatedModel();
        _idleAnimation.LoadContent(Content, "Models/idle");

        _walkAnimation = new AnimatedModel();
        _walkAnimation.LoadContent(Content, "Models/walk");

        _runAnimation = new AnimatedModel();
        _runAnimation.LoadContent(Content, "Models/run");

        // Start with idle animation
        _currentModel = _idleAnimation;
        var animNames = _currentModel.GetAnimationNames();
        if (animNames.Count > 0)
        {
            _currentModel.PlayAnimation(animNames[0]);
        }

        // Set up camera
        _viewMatrix = Matrix.CreateLookAt(
            new Vector3(0, 100, 200),  // Camera position (moved back and up for better view)
            new Vector3(0, 50, 0),     // Look at point (center of character)
            Vector3.Up                 // Up direction
        );

        _projectionMatrix = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,                          // 45 degree field of view
            GraphicsDevice.Viewport.AspectRatio,         // Aspect ratio
            0.1f,                                        // Near plane
            1000f                                        // Far plane
        );

        Console.WriteLine("\n=== Animation Test Controls ===");
        Console.WriteLine("Press 1: Play idle animation");
        Console.WriteLine("Press 2: Play walk animation");
        Console.WriteLine("Press 3: Play run animation");
        Console.WriteLine("Press Escape: Exit");
        Console.WriteLine("================================\n");
    }

    protected override void Update(GameTime gameTime)
    {
        // Update input state at start of frame
        _inputManager.Update();

        // Test: Escape key exits game
        if (_inputManager.IsKeyPressed(Keys.Escape))
            Exit();

        // Animation switching using keyboard input
        if (_inputManager.IsKeyPressed(Keys.D1))
        {
            _currentModel = _idleAnimation;
            var animNames = _currentModel.GetAnimationNames();
            if (animNames.Count > 0)
            {
                _currentModel.PlayAnimation(animNames[0]);
                Console.WriteLine("Switched to idle animation");
            }
        }
        else if (_inputManager.IsKeyPressed(Keys.D2))
        {
            _currentModel = _walkAnimation;
            var animNames = _currentModel.GetAnimationNames();
            if (animNames.Count > 0)
            {
                _currentModel.PlayAnimation(animNames[0]);
                Console.WriteLine("Switched to walk animation");
            }
        }
        else if (_inputManager.IsKeyPressed(Keys.D3))
        {
            _currentModel = _runAnimation;
            var animNames = _currentModel.GetAnimationNames();
            if (animNames.Count > 0)
            {
                _currentModel.PlayAnimation(animNames[0]);
                Console.WriteLine("Switched to run animation");
            }
        }

        // Update current animation
        _currentModel?.Update(gameTime);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Draw the current animated model
        _currentModel?.Draw(Matrix.Identity, _viewMatrix, _projectionMatrix);

        base.Draw(gameTime);
    }
}
