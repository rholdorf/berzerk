using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Input;
using Berzerk.Graphics;
using Berzerk.Source.Graphics;
using Berzerk.Controllers;
using Berzerk.UI;
using Berzerk.Source.Combat;
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
    private DebugRenderer _debugRenderer;

    // Combat systems
    private ProjectileManager _projectileManager;
    private ProjectileRenderer _projectileRenderer;
    private AmmoSystem _ammoSystem;
    private WeaponSystem _weaponSystem;
    private TargetManager _targetManager;

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
        _camera = new ThirdPersonCamera(_inputManager, _playerController.Transform, _playerController);
        _crosshair = new Crosshair();

        // Initialize combat systems
        _projectileManager = new ProjectileManager();
        _projectileManager.Initialize(50);
        _ammoSystem = new AmmoSystem();
        _weaponSystem = new WeaponSystem(_ammoSystem, _projectileManager);
        _targetManager = new TargetManager();
        _targetManager.Initialize();

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

        // Initialize camera and debug renderer
        _camera.Initialize(GraphicsDevice);
        _crosshair.LoadContent(GraphicsDevice);
        _debugRenderer = new DebugRenderer(GraphicsDevice);

        // Set up test collision walls
        _testWalls = ThirdPersonCamera.CreateTestWalls();
        _camera.SetCollisionGeometry(_testWalls);
        _projectileManager.SetWallColliders(_testWalls);
        Console.WriteLine($"Camera collision geometry initialized with {_testWalls.Count} boxes");

        // Initialize projectile renderer after graphics device is ready
        _projectileRenderer = new ProjectileRenderer(GraphicsDevice);

        Console.WriteLine("\n=== Controls ===");
        Console.WriteLine("WASD/QE: Move player (tank controls)");
        Console.WriteLine("Mouse: Aim (crosshair)");
        Console.WriteLine("Left Mouse: Fire (hold for auto-fire)");
        Console.WriteLine("Right-click + drag: Orbit camera");
        Console.WriteLine("Scroll wheel: Zoom camera");
        Console.WriteLine("R: Respawn targets");
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

        // Combat update
        bool isFiring = _inputManager.IsLeftMouseHeld();
        Vector3 spawnPos = _playerController.Transform.Position + Vector3.Up * 1.5f; // Shoulder height
        Vector3 aimDir = _camera.Forward;
        _weaponSystem.Update(gameTime, isFiring, spawnPos, aimDir);

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _projectileManager.Update(deltaTime);
        _targetManager.Update(deltaTime);
        _targetManager.CheckProjectileCollisions(_projectileManager.GetActiveProjectiles());
        _targetManager.CheckPickupCollection(_playerController.Transform.Position, _ammoSystem);

        // Test: Escape key exits game
        if (_inputManager.IsKeyPressed(Keys.Escape))
            Exit();

        // R key respawns targets for testing
        if (_inputManager.IsKeyPressed(Keys.R))
        {
            _targetManager.RespawnTargets();
            Console.WriteLine("Targets respawned!");
        }

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

        // Draw floor grid
        _debugRenderer.DrawFloor(_camera.ViewMatrix, _camera.ProjectionMatrix);

        // Draw collision walls
        _debugRenderer.DrawBoundingBoxes(_testWalls, _camera.ViewMatrix, _camera.ProjectionMatrix, Color.White);

        // Draw combat elements
        _projectileRenderer.Draw(_projectileManager.GetActiveProjectiles(), _camera.ViewMatrix, _camera.ProjectionMatrix);
        _projectileRenderer.DrawEffects(_projectileManager.GetActiveEffects(), _camera.ViewMatrix, _camera.ProjectionMatrix);
        _debugRenderer.DrawTargets(_targetManager.GetTargets(), _camera.ViewMatrix, _camera.ProjectionMatrix);
        _debugRenderer.DrawPickups(_targetManager.GetPickups(), _camera.ViewMatrix, _camera.ProjectionMatrix);

        // Draw 3D content with camera matrices
        // Scale down model by 0.01x - Mixamo models are typically 100x too large
        // Mixamo models face +Z, but we want them to face -Z (forward in MonoGame)
        Matrix modelScale = Matrix.CreateScale(0.01f);
        Matrix modelRotationCorrection = Matrix.CreateRotationY(MathHelper.Pi); // 180 degree turn
        Matrix worldMatrix = modelScale * modelRotationCorrection * _playerController.Transform.WorldMatrix;

        _currentModel?.Draw(
            GraphicsDevice,
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
