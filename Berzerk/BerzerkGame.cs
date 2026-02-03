using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Berzerk.Source.Input;
using Berzerk.Graphics;
using Berzerk.Source.Graphics;
using Berzerk.Controllers;
using Berzerk.UI;
using Berzerk.Source.Combat;
using Berzerk.Source.Player;
using Berzerk.Source.Enemies;
using System;
using System.Collections.Generic;

namespace Berzerk;

public enum GameState
{
    Playing,
    Dying,
    GameOver
}

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

    // Health and survival
    private HealthSystem _healthSystem;
    private DamageVignette _damageVignette;
    private ScreenFade _screenFade;
    private HealthBar _healthBar;
    private GameOverScreen _gameOverScreen;
    private GameState _gameState = GameState.Playing;

    // Enemy system
    private EnemyManager _enemyManager;
    private EnemyRenderer _enemyRenderer;

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

        // Initialize health system
        _healthSystem = new HealthSystem();
        _healthSystem.OnDamageTaken += () => _damageVignette.Trigger();
        _healthSystem.OnDeath += () =>
        {
            _gameState = GameState.Dying;
            _screenFade.FadeToBlack(1.5f);  // 1.5 second fade
            _playerController.IsEnabled = false;
            Console.WriteLine("Player died!");
        };

        // Initialize enemy system
        _enemyManager = new EnemyManager();
        _enemyManager.Initialize(20);
        _enemyManager.SetTargetManager(_targetManager);

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

        // Initialize enemy renderer
        _enemyRenderer = new EnemyRenderer(GraphicsDevice);

        // Spawn initial enemy wave (3 enemies per CONTEXT)
        _enemyManager.SpawnWave(3, _playerController.Transform.Position);
        Console.WriteLine("Spawned initial wave: 3 enemies");

        // Wire enemy attacks to player damage and knockback
        _enemyManager.SetAttackCallback((damage, direction) =>
        {
            if (_gameState != GameState.Playing) return;
            _healthSystem.TakeDamage(damage);
            _playerController.ApplyKnockback(direction, 8f); // 8 units/sec knockback force
            Console.WriteLine($"Enemy attacked! -{damage} HP");
        });

        // Load health UI
        _damageVignette = new DamageVignette();
        _damageVignette.LoadContent(GraphicsDevice);

        _screenFade = new ScreenFade();
        _screenFade.LoadContent(GraphicsDevice);

        _healthBar = new HealthBar();
        _healthBar.LoadContent(GraphicsDevice);

        _gameOverScreen = new GameOverScreen();
        _gameOverScreen.LoadContent(Content, GraphicsDevice);

        Console.WriteLine("\n=== Controls ===");
        Console.WriteLine("WASD/QE: Move player (tank controls)");
        Console.WriteLine("Mouse: Aim (crosshair)");
        Console.WriteLine("Left Mouse: Fire (hold for auto-fire)");
        Console.WriteLine("Right-click + drag: Orbit camera");
        Console.WriteLine("Scroll wheel: Zoom camera");
        Console.WriteLine("H: Test damage (10 HP)");
        Console.WriteLine("R: Respawn targets / Restart (game over)");
        Console.WriteLine("G: Spawn new enemy wave");
        Console.WriteLine("1/2/3: Switch animations");
        Console.WriteLine("Escape: Exit");
        Console.WriteLine("================\n");
    }

    protected override void Update(GameTime gameTime)
    {
        _inputManager.Update();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_gameState)
        {
            case GameState.Playing:
                UpdatePlaying(gameTime, deltaTime);
                break;
            case GameState.Dying:
                UpdateDying(deltaTime);
                break;
            case GameState.GameOver:
                UpdateGameOver();
                break;
        }

        base.Update(gameTime);
    }

    private void UpdatePlaying(GameTime gameTime, float deltaTime)
    {
        // Test damage: H key deals 10 HP
        if (_inputManager.IsKeyPressed(Keys.H))
        {
            _healthSystem.TakeDamage(10);
            Console.WriteLine($"Damage! Health: {_healthSystem.CurrentHealth}/{_healthSystem.MaxHealth}");
        }

        // Existing player/camera/combat updates
        _playerController.Update(gameTime);
        _camera.Update(gameTime);

        // Combat update
        bool isFiring = _inputManager.IsLeftMouseHeld();
        Vector3 spawnPos = _playerController.Transform.Position + Vector3.Up * 1.5f;
        Vector3 aimDir = _camera.Forward;
        _weaponSystem.Update(gameTime, isFiring, spawnPos, aimDir);

        _projectileManager.Update(deltaTime);
        _targetManager.Update(deltaTime);
        _targetManager.CheckProjectileCollisions(_projectileManager.GetActiveProjectiles());
        _targetManager.CheckPickupCollection(_playerController.Transform.Position, _ammoSystem, _healthSystem);

        // Update enemy system
        _enemyManager.Update(gameTime, _playerController.Transform.Position);
        _enemyManager.CheckProjectileCollisions(_projectileManager.GetActiveProjectiles());

        // R key respawns targets (existing)
        if (_inputManager.IsKeyPressed(Keys.R))
        {
            _targetManager.RespawnTargets();
            Console.WriteLine("Targets respawned!");
        }

        // G key spawns new enemy wave (test)
        if (_inputManager.IsKeyPressed(Keys.G))
        {
            int waveSize = 3;
            _enemyManager.SpawnWave(waveSize, _playerController.Transform.Position);
            _enemyManager.SetAttackCallback((damage, direction) =>
            {
                if (_gameState != GameState.Playing) return;
                _healthSystem.TakeDamage(damage);
                _playerController.ApplyKnockback(direction, 8f);
                Console.WriteLine($"Enemy attacked! -{damage} HP");
            });
            Console.WriteLine($"Spawned test wave: {waveSize} enemies");
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

        // Update animations and effects
        _currentModel?.Update(gameTime);
        _damageVignette.Update(deltaTime);

        // Exit handling
        if (_inputManager.IsKeyPressed(Keys.Escape))
            Exit();
    }

    private void UpdateDying(float deltaTime)
    {
        _screenFade.Update(deltaTime);
        _damageVignette.Update(deltaTime);  // Continue fading any active vignette

        if (_screenFade.IsComplete)
        {
            _gameState = GameState.GameOver;
            Console.WriteLine("Game Over - Press R to restart");
        }
    }

    private void UpdateGameOver()
    {
        if (_inputManager.IsKeyPressed(Keys.R))
        {
            RestartGame();
        }

        if (_inputManager.IsKeyPressed(Keys.Escape))
            Exit();
    }

    private void RestartGame()
    {
        _healthSystem.Reset();
        _screenFade.Reset();
        _playerController.IsEnabled = true;
        _playerController.Transform.Position = Vector3.Zero;  // Reset position
        _gameState = GameState.Playing;
        _targetManager.RespawnTargets();
        _ammoSystem = new AmmoSystem();  // Reset ammo
        _weaponSystem = new WeaponSystem(_ammoSystem, _projectileManager);

        // Reset and respawn enemies
        _enemyManager.Reset();
        _enemyManager.SpawnWave(3, _playerController.Transform.Position);
        _enemyManager.SetAttackCallback((damage, direction) =>
        {
            if (_gameState != GameState.Playing) return;
            _healthSystem.TakeDamage(damage);
            _playerController.ApplyKnockback(direction, 8f);
            Console.WriteLine($"Enemy attacked! -{damage} HP");
        });

        Console.WriteLine("Game restarted!");
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Draw 3D content (only when not fully faded to black)
        if (_screenFade.Alpha < 0.99f)
        {
            // Draw floor grid
            _debugRenderer.DrawFloor(_camera.ViewMatrix, _camera.ProjectionMatrix);

            // Draw collision walls
            _debugRenderer.DrawBoundingBoxes(_testWalls, _camera.ViewMatrix, _camera.ProjectionMatrix, Color.White);

            // Draw combat elements
            _projectileRenderer.Draw(_projectileManager.GetActiveProjectiles(), _camera.ViewMatrix, _camera.ProjectionMatrix);
            _projectileRenderer.DrawEffects(_projectileManager.GetActiveEffects(), _camera.ViewMatrix, _camera.ProjectionMatrix);
            _debugRenderer.DrawTargets(_targetManager.GetTargets(), _camera.ViewMatrix, _camera.ProjectionMatrix);
            _debugRenderer.DrawPickups(_targetManager.GetAmmoPickups(), _camera.ViewMatrix, _camera.ProjectionMatrix);

            // Draw enemy system
            _enemyRenderer.DrawEnemies(_enemyManager.GetEnemies(), _camera.ViewMatrix, _camera.ProjectionMatrix);
            _enemyRenderer.DrawExplosions(_enemyManager.GetActiveExplosions(), _camera.ViewMatrix, _camera.ProjectionMatrix);
            _enemyRenderer.DrawHealthPickups(_targetManager.GetHealthPickups(), _camera.ViewMatrix, _camera.ProjectionMatrix);

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
        }

        // Draw 2D UI
        _spriteBatch.Begin();

        // Always draw crosshair during gameplay
        if (_gameState == GameState.Playing)
        {
            _crosshair.Draw(_spriteBatch, GraphicsDevice.Viewport);
            _healthBar.Draw(_spriteBatch, _healthSystem.CurrentHealth, _healthSystem.MaxHealth);
        }

        // Draw damage vignette (always, handles own alpha)
        _damageVignette.Draw(_spriteBatch, GraphicsDevice.Viewport);

        // Draw screen fade (always, handles own alpha)
        _screenFade.Draw(_spriteBatch, GraphicsDevice.Viewport);

        // Draw game over screen when in GameOver state
        if (_gameState == GameState.GameOver)
        {
            _gameOverScreen.Draw(_spriteBatch, GraphicsDevice.Viewport);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
