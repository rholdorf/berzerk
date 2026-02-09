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
using Berzerk.Source.Rooms;
using System;
using System.Collections.Generic;

namespace Berzerk;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
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
    private DebugRenderer _debugRenderer;

    // Combat systems
    private ProjectileManager _projectileManager;
    private ProjectileRenderer _projectileRenderer;
    private AmmoSystem _ammoSystem;
    private WeaponSystem _weaponSystem;
    private TargetManager _targetManager;
    private ScoreSystem _scoreSystem;

    // Health and survival
    private HealthSystem _healthSystem;
    private DamageVignette _damageVignette;
    private ScreenFade _screenFade;
    private HealthBar _healthBar;
    private GameOverScreen _gameOverScreen;
    private GameState _gameState = GameState.MainMenu;

    // HUD elements
    private AmmoCounter _ammoCounter;
    private ScoreCounter _scoreCounter;
    private PickupNotification _pickupNotification;

    // Menu screens
    private StartMenu _startMenu;
    private PauseMenu _pauseMenu;

    // Enemy system
    private EnemyManager _enemyManager;
    private EnemyRenderer _enemyRenderer;

    // Room system
    private RoomManager _roomManager;
    private RoomRenderer _roomRenderer;
    private int _roomsCleared = 0;

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
        IsMouseVisible = true;  // Show cursor for main menu

        // On macOS, allow user to resize window (helps with mouse coordinate tracking)
        Window.AllowUserResizing = false;
        Window.Title = "BERZERK";  // Set window title explicitly
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
        _scoreSystem = new ScoreSystem();

        // Initialize health system
        _healthSystem = new HealthSystem();
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

        // Initialize room system
        _roomManager = new RoomManager();
        _roomManager.Initialize();

        // Wire room clear event
        _roomManager.OnRoomCleared += () =>
        {
            Console.WriteLine("All doors are open! Find an exit.");
        };

        // Wire room transition event
        _roomManager.OnRoomTransition += HandleRoomTransition;

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

        // Initialize projectile renderer after graphics device is ready
        _projectileRenderer = new ProjectileRenderer(GraphicsDevice);

        // Initialize enemy renderer
        _enemyRenderer = new EnemyRenderer(GraphicsDevice);

        // Load shared robot animation models
        _enemyRenderer.LoadRobotModels(Content);
        _enemyManager.SetEnemyRenderer(_enemyRenderer);

        // Initialize room renderer
        _roomRenderer = new RoomRenderer(GraphicsDevice);

        // Set up room collision geometry (replaces test walls)
        var roomColliders = _roomManager.GetCollisionGeometry();
        _camera.SetCollisionGeometry(roomColliders);
        _projectileManager.SetWallColliders(roomColliders);

        // Spawn initial enemy wave (3 enemies per CONTEXT)
        _enemyManager.SpawnWave(3, _playerController.Transform.Position, _roomManager.GetEnemySpawnPoints());
        Console.WriteLine("Spawned initial wave: 3 enemies");

        // Wire enemy attacks to player damage and knockback
        _enemyManager.SetAttackCallback((damage, direction) =>
        {
            if (_gameState != GameState.Playing) return;
            _healthSystem.TakeDamage(damage);
            _playerController.ApplyKnockback(direction, 8f); // 8 units/sec knockback force
            Console.WriteLine($"Enemy attacked! -{damage} HP");
        });

        // Wire enemy manager to room manager for room clear detection
        _enemyManager.OnAllEnemiesDefeated += _roomManager.HandleAllEnemiesDefeated;

        // Load health UI
        _damageVignette = new DamageVignette();
        _damageVignette.LoadContent(GraphicsDevice);

        _screenFade = new ScreenFade();
        _screenFade.LoadContent(GraphicsDevice);

        _healthBar = new HealthBar();
        _healthBar.LoadContent(GraphicsDevice);

        _gameOverScreen = new GameOverScreen();
        _gameOverScreen.LoadContent(Content, GraphicsDevice, _inputManager);

        // Load HUD elements
        _ammoCounter = new AmmoCounter();
        _ammoCounter.LoadContent(Content);

        _scoreCounter = new ScoreCounter();
        _scoreCounter.LoadContent(Content);

        _pickupNotification = new PickupNotification();
        _pickupNotification.LoadContent(Content);

        // Load menu screens
        _startMenu = new StartMenu();
        _startMenu.LoadContent(Content, GraphicsDevice, _inputManager);

        _pauseMenu = new PauseMenu();
        _pauseMenu.LoadContent(Content, GraphicsDevice, _inputManager);

        // On macOS, initialize mouse position to center of window to "wake up" mouse tracking
        Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

        // Wire menu events
        _startMenu.OnStartGame += () =>
        {
            _gameState = GameState.Playing;
            IsMouseVisible = false;
        };

        _pauseMenu.OnResume += () =>
        {
            _gameState = GameState.Playing;
            _playerController.IsEnabled = true;
            IsMouseVisible = false;
        };

        _pauseMenu.OnQuit += Exit;

        _gameOverScreen.OnRestart += () =>
        {
            RestartGame();
            IsMouseVisible = false;
        };

        _gameOverScreen.OnQuit += Exit;

        // Wire score tracking to enemy kills
        _enemyManager.OnEnemyKilled += _scoreSystem.AddEnemyKill;

        // Wire health bar flash to damage
        _healthSystem.OnDamageTaken += () =>
        {
            _damageVignette.Trigger();
            _healthBar.Trigger();
        };

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
            case GameState.MainMenu:
                _startMenu.Update(GraphicsDevice.Viewport, deltaTime);
                break;

            case GameState.Playing:
                // Check ESC for pause BEFORE processing gameplay
                if (_inputManager.IsKeyPressed(Keys.Escape))
                {
                    _gameState = GameState.Paused;
                    _playerController.IsEnabled = false;
                    IsMouseVisible = true;
                    break;
                }
                UpdatePlaying(gameTime, deltaTime);
                break;

            case GameState.Paused:
                // No ESC to unpause - only Resume button unpauses (per user decision)
                _pauseMenu.Update();
                break;

            case GameState.Dying:
                UpdateDying(deltaTime);
                break;

            case GameState.GameOver:
                IsMouseVisible = true;
                _gameOverScreen.Update();
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

        // Check pickup collection with before/after comparison for notifications
        int ammoBefore = _ammoSystem.TotalAmmo;
        int healthBefore = _healthSystem.CurrentHealth;
        _targetManager.CheckPickupCollection(_playerController.Transform.Position, _ammoSystem, _healthSystem);
        int ammoGained = _ammoSystem.TotalAmmo - ammoBefore;
        if (ammoGained > 0)
            _pickupNotification.Show($"+{ammoGained} Ammo", GraphicsDevice.Viewport);
        int healthGained = _healthSystem.CurrentHealth - healthBefore;
        if (healthGained > 0)
            _pickupNotification.Show($"+{healthGained} Health", GraphicsDevice.Viewport);

        // Update enemy system
        _enemyManager.Update(gameTime, _playerController.Transform.Position);
        _enemyManager.CheckProjectileCollisions(_projectileManager.GetActiveProjectiles());

        // Update room system (door animations, transition detection)
        _roomManager.Update(deltaTime, _playerController.Transform.Position);

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
            _enemyManager.SpawnWave(waveSize, _playerController.Transform.Position, _roomManager.GetEnemySpawnPoints());
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
        _healthBar.Update(deltaTime);
        _pickupNotification.Update(deltaTime);
        _ammoCounter.Update(deltaTime);
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
        // Game over screen now handled by Update switch case (mouse interaction)
        // No keyboard shortcuts - use buttons only
    }

    private void RestartGame()
    {
        _healthSystem.Reset();
        _screenFade.Reset();
        _scoreSystem.Reset();
        _playerController.IsEnabled = true;
        _playerController.Transform.Position = Vector3.Zero;  // Reset position
        _gameState = GameState.Playing;
        _targetManager.RespawnTargets();
        _ammoSystem = new AmmoSystem();  // Reset ammo
        _weaponSystem = new WeaponSystem(_ammoSystem, _projectileManager);

        // Reset room system
        _roomManager.Reset();
        _roomsCleared = 0;

        // Update collision geometry
        var roomColliders = _roomManager.GetCollisionGeometry();
        _camera.SetCollisionGeometry(roomColliders);
        _projectileManager.SetWallColliders(roomColliders);

        // Reset and respawn enemies
        _enemyManager.Reset();
        _enemyManager.SpawnWave(3, _playerController.Transform.Position, _roomManager.GetEnemySpawnPoints());
        _enemyManager.SetAttackCallback((damage, direction) =>
        {
            if (_gameState != GameState.Playing) return;
            _healthSystem.TakeDamage(damage);
            _playerController.ApplyKnockback(direction, 8f);
            Console.WriteLine($"Enemy attacked! -{damage} HP");
        });

        Console.WriteLine("Game restarted!");
    }

    private void HandleRoomTransition(Direction exitDirection)
    {
        _roomsCleared++;
        Console.WriteLine($"Room {_roomsCleared} cleared! Entering new room...");

        // Reset room state
        _roomManager.TransitionToNewRoom();

        // Clear projectiles (don't carry across rooms)
        _projectileManager.DeactivateAll();

        // Reset and respawn enemies with progressive difficulty
        _enemyManager.Reset();
        int enemyCount = Math.Min(3 + _roomsCleared, 10); // 3 base + 1 per room, max 10
        _enemyManager.SpawnWave(enemyCount, _playerController.Transform.Position, _roomManager.GetEnemySpawnPoints());

        // Re-wire attack callback for new enemies
        _enemyManager.SetAttackCallback((damage, direction) =>
        {
            if (_gameState != GameState.Playing) return;
            _healthSystem.TakeDamage(damage);
            _playerController.ApplyKnockback(direction, 8f);
            Console.WriteLine($"Enemy attacked! -{damage} HP");
        });

        // Re-wire room clear event for new wave
        // (Already wired to _roomManager.HandleAllEnemiesDefeated)

        // Update collision geometry (door states changed)
        var roomColliders = _roomManager.GetCollisionGeometry();
        _camera.SetCollisionGeometry(roomColliders);
        _projectileManager.SetWallColliders(roomColliders);

        // Move player to entry position
        Vector3 spawnPos = _roomManager.GetSpawnPositionForEntry(exitDirection);
        _playerController.Transform.Position = spawnPos;

        Console.WriteLine($"Spawned {enemyCount} enemies. Player at {spawnPos}");
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        // Draw 3D content (only when not fully faded to black)
        if (_screenFade.Alpha < 0.99f)
        {
            // Draw floor grid
            _debugRenderer.DrawFloor(_camera.ViewMatrix, _camera.ProjectionMatrix);

            // Draw room (walls and doors)
            _roomRenderer.Draw(_roomManager.CurrentRoom, _camera.ViewMatrix, _camera.ProjectionMatrix);

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

        if (_gameState == GameState.MainMenu)
        {
            _startMenu.Draw(_spriteBatch, GraphicsDevice.Viewport);
        }
        else if (_gameState == GameState.Playing || _gameState == GameState.Paused)
        {
            // HUD elements (visible during gameplay and when paused)
            _crosshair.Draw(_spriteBatch, GraphicsDevice.Viewport);
            _healthBar.Draw(_spriteBatch, _healthSystem.CurrentHealth, _healthSystem.MaxHealth);
            _ammoCounter.Draw(_spriteBatch, _ammoSystem.CurrentMagazine, _ammoSystem.ReserveAmmo, GraphicsDevice.Viewport);
            _scoreCounter.Draw(_spriteBatch, _scoreSystem.CurrentScore, GraphicsDevice.Viewport);
            _pickupNotification.Draw(_spriteBatch);

            // Pause overlay on top of HUD
            if (_gameState == GameState.Paused)
            {
                _pauseMenu.Draw(_spriteBatch, GraphicsDevice.Viewport);
            }
        }

        // Effects (always, they handle own alpha)
        _damageVignette.Draw(_spriteBatch, GraphicsDevice.Viewport);
        _screenFade.Draw(_spriteBatch, GraphicsDevice.Viewport);

        // Game over screen (on top of everything)
        if (_gameState == GameState.GameOver)
        {
            _gameOverScreen.Draw(_spriteBatch, GraphicsDevice.Viewport, _scoreSystem.CurrentScore);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
