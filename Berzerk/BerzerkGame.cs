using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Input;
using Berzerk.Graphics;
using Berzerk.Source.Graphics;
using Berzerk.Controllers;
using Berzerk.UI;
using System;
using System.Collections.Generic;

namespace Berzerk;

public class BerzerkGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private InputManager _inputManager;
    private PlayerController _playerController;
    private ThirdPersonCamera _camera;
    private Crosshair _crosshair;
    private List<BoundingBox> _testWalls;

    // Test animated model and animations
    private AnimatedModel _testCharacter;
    private AnimatedModel _idleAnimation;
    private AnimatedModel _walkAnimation;
    private AnimatedModel _runAnimation;
    private AnimatedModel _currentModel;

    public BerzerkGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = false;  // Hide OS cursor (we draw our own crosshair)
    }

    protected override void Initialize()
    {
        // Initialize input manager
        _inputManager = new InputManager();

        // Initialize player controller
        _playerController = new PlayerController(_inputManager);

        // Initialize camera and crosshair
        _camera = new ThirdPersonCamera(_inputManager, _playerController.Transform);
        _crosshair = new Crosshair();

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

        // Initialize camera
        _camera.Initialize(GraphicsDevice);
        _crosshair.LoadContent(GraphicsDevice);

        // Set up test collision walls
        _testWalls = ThirdPersonCamera.CreateTestWalls();
        _camera.SetCollisionGeometry(_testWalls);

        Console.WriteLine("\n=== Controls ===");
        Console.WriteLine("WASD: Move player");
        Console.WriteLine("Mouse: Aim (crosshair)");
        Console.WriteLine("Right-click + drag: Orbit camera");
        Console.WriteLine("Scroll wheel: Zoom camera");
        Console.WriteLine("1/2/3: Switch animations");
        Console.WriteLine("Escape: Exit");
        Console.WriteLine("================\n");
    }

    protected override void Update(GameTime gameTime)
    {
        // Update input state at start of frame
        _inputManager.Update();

        // Update player controller
        _playerController.Update(gameTime);

        // Update camera
        _camera.Update(gameTime);

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

        // Draw 3D content with camera matrices
        // Scale down model by 0.01x - Mixamo models are typically 100x too large
        Matrix modelScale = Matrix.CreateScale(0.01f);
        Matrix worldMatrix = modelScale * _playerController.Transform.WorldMatrix;

        _currentModel?.Draw(
            worldMatrix,
            _camera.ViewMatrix,
            _camera.ProjectionMatrix
        );

        // Draw 2D UI (crosshair)
        _spriteBatch.Begin();
        _crosshair.Draw(_spriteBatch, GraphicsDevice.Viewport);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
