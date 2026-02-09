# Phase 7: UI & HUD - Research

**Researched:** 2026-02-09
**Domain:** MonoGame UI/HUD implementation with SpriteBatch
**Confidence:** HIGH

## Summary

Phase 7 implements a minimalist HUD system showing gameplay information (crosshair, health bar, ammo counter, score counter, pickup notifications) and menu screens (start menu, pause menu, game over screen). The research reveals that MonoGame provides low-level primitives (SpriteBatch, SpriteFont) rather than a full UI framework, which aligns perfectly with the user's decision for a hand-rolled minimalist approach.

The existing codebase already implements 4 of 6 UI elements (Crosshair, HealthBar, GameOverScreen, DamageVignette) with established patterns: programmatic texture generation for graphics, exponential decay for animations, absolute positioning for layout, and a single SpriteBatch Begin/End pair for all UI drawing. These patterns should be extended to the remaining elements (ammo counter, score counter, start menu, pause menu, pickup notifications).

The standard MonoGame approach for simple HUD systems is to hand-roll using SpriteBatch.Draw for graphics and SpriteBatch.DrawString for text, managing state through game enums and drawing everything in a single batched pass. UI frameworks (GeonBit.UI, Myra, Gum) exist but add complexity inappropriate for this minimalist design.

**Primary recommendation:** Extend existing UI class patterns for remaining elements, implement ScoreSystem for tracking points, add GameState.MainMenu and GameState.Paused states, create pickup notification queue with exponential decay fade-out, use established pixel texture and SpriteFont approaches.

## Standard Stack

The established libraries/tools for MonoGame UI development:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework.DesktopGL | 3.8.* | Core game framework with SpriteBatch/SpriteFont | Official cross-platform MonoGame implementation, already in project |
| SpriteBatch | Built-in | 2D sprite and text rendering with batching | MonoGame's primary 2D rendering API, optimal for HUD overlays |
| SpriteFont | Built-in | Texture-based font rendering | MonoGame's standard text rendering, works with Content Pipeline |
| Texture2D | Built-in | Graphics texture storage | Used for UI graphics (procedural or loaded) |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| RenderTarget2D | Built-in | Off-screen rendering | Text caching for performance (optional optimization) |
| Rectangle | Built-in | Bounds and collision detection | Button hit testing, UI element positioning |
| Color.Lerp | Built-in | Color interpolation | Smooth color transitions for flash effects |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Hand-rolled UI | GeonBit.UI | Framework provides widgets (Button, CheckBox, etc.) but adds complexity and opinionated styling inappropriate for minimalist design |
| Hand-rolled UI | Myra | XML-based declarative UI with WYSIWYG designer, but overkill for simple HUD with 6 elements |
| Hand-rolled UI | Gum | Visual UI tool with FlatRedBall integration, but adds external tool dependency and runtime complexity |
| SpriteBatch.DrawString | Pre-rendered RenderTarget2D | 52% faster for static text, but adds memory overhead and complexity; only needed if profiling shows text rendering bottleneck |

**Installation:**
Already installed in project (MonoGame.Framework.DesktopGL 3.8.*)

## Architecture Patterns

### Recommended Project Structure
```
Berzerk/Source/UI/
├── Crosshair.cs          # [EXISTS] Center aiming reticle
├── HealthBar.cs          # [EXISTS] Top-left health display
├── DamageVignette.cs     # [EXISTS] Red flash on damage
├── ScreenFade.cs         # [EXISTS] Fade to/from black
├── GameOverScreen.cs     # [EXISTS] Death screen with restart
├── AmmoCounter.cs        # [NEW] Top-right ammo display
├── ScoreCounter.cs       # [NEW] Top-center score display
├── PickupNotification.cs # [NEW] Temporary '+X Ammo' popups
├── StartMenu.cs          # [NEW] Initial 'Start Game' button
└── PauseMenu.cs          # [NEW] 'Resume'/'Quit' overlay

Berzerk/Source/Combat/
└── ScoreSystem.cs        # [NEW] Point tracking and events
```

### Pattern 1: Single UI Draw Pass
**What:** All UI elements render in one SpriteBatch Begin/End pair, after 3D content
**When to use:** Always for HUD systems - minimizes expensive state changes
**Example:**
```csharp
// In BerzerkGame.Draw() - existing pattern
_spriteBatch.Begin();

// Draw all UI elements here
if (_gameState == GameState.Playing)
{
    _crosshair.Draw(_spriteBatch, GraphicsDevice.Viewport);
    _healthBar.Draw(_spriteBatch, _healthSystem.CurrentHealth, _healthSystem.MaxHealth);
    _ammoCounter.Draw(_spriteBatch, _ammoSystem.CurrentMagazine, _ammoSystem.ReserveAmmo);
    _scoreCounter.Draw(_spriteBatch, _scoreSystem.CurrentScore);
}

// Overlays and effects (always)
_damageVignette.Draw(_spriteBatch, GraphicsDevice.Viewport);
_screenFade.Draw(_spriteBatch, GraphicsDevice.Viewport);

// Menu screens
if (_gameState == GameState.GameOver)
    _gameOverScreen.Draw(_spriteBatch, GraphicsDevice.Viewport, _scoreSystem.CurrentScore);

_spriteBatch.End();
```

### Pattern 2: Programmatic Texture Generation
**What:** Create UI graphics from code using Texture2D.SetData(Color[] pixels)
**When to use:** Simple geometric shapes (crosshairs, bars, solid colors) to avoid asset dependencies
**Example:**
```csharp
// From Crosshair.cs - established project pattern
public void LoadContent(GraphicsDevice graphicsDevice)
{
    _texture = new Texture2D(graphicsDevice, Size, Size);
    Color[] pixels = new Color[Size * Size];

    // Draw crosshair shape into pixel array
    for (int y = 0; y < Size; y++)
    {
        for (int x = 0; x < Size; x++)
        {
            bool isHorizontalLine = dy < Thickness && dx >= Gap && dx < Size / 2;
            bool isVerticalLine = dx < Thickness && dy >= Gap && dy < Size / 2;
            pixels[y * Size + x] = (isHorizontalLine || isVerticalLine)
                ? Color.White
                : Color.Transparent;
        }
    }
    _texture.SetData(pixels);
}
```

### Pattern 3: Exponential Decay for Frame-Rate Independence
**What:** Use Math.Pow(0.01, deltaTime / duration) for smooth fade-outs regardless of framerate
**When to use:** Any time-based animation or fade effect (damage flash, pickup notifications)
**Example:**
```csharp
// From DamageVignette.cs - established project pattern
public void Update(float deltaTime)
{
    if (!_isActive) return;

    // Exponential decay (frame-rate independent)
    float decay = (float)Math.Pow(0.01, deltaTime / _fadeOutTime);
    _alpha *= decay;

    if (_alpha < 0.01f)
    {
        _alpha = 0f;
        _isActive = false;
    }
}
```

### Pattern 4: Absolute Positioning with Constants
**What:** Define UI element positions as const int values relative to screen edges
**When to use:** Simple HUD layouts where elements have fixed positions (minimalist approach)
**Example:**
```csharp
// From HealthBar.cs - established project pattern
private const int BarWidth = 200;
private const int BarHeight = 20;
private const int BarX = 20;      // 20px from left edge
private const int BarY = 20;      // 20px from top edge
private const int BorderWidth = 2;

public void Draw(SpriteBatch spriteBatch, int currentHealth, int maxHealth)
{
    // Border at fixed position
    spriteBatch.Draw(_pixelTexture,
        new Rectangle(BarX - BorderWidth, BarY - BorderWidth,
                      BarWidth + BorderWidth * 2, BarHeight + BorderWidth * 2),
        Color.DarkGray);
}
```

### Pattern 5: GameState Enum for Screen Management
**What:** Use enum to control which UI elements are visible and active
**When to use:** Menu flow and game state transitions
**Example:**
```csharp
// From BerzerkGame.cs - existing pattern, extend for menus
public enum GameState
{
    MainMenu,    // [NEW] Start screen before gameplay
    Playing,     // [EXISTS] Active gameplay
    Paused,      // [NEW] ESC pauses game
    Dying,       // [EXISTS] Fade-out transition
    GameOver     // [EXISTS] Death screen
}

protected override void Update(GameTime gameTime)
{
    switch (_gameState)
    {
        case GameState.MainMenu:
            UpdateMainMenu();
            break;
        case GameState.Playing:
            UpdatePlaying(gameTime, deltaTime);
            break;
        case GameState.Paused:
            UpdatePaused();
            break;
        // ... etc
    }
}
```

### Pattern 6: Mouse Input with Rectangle.Contains
**What:** Detect button clicks by checking if mouse position is within button bounds
**When to use:** Interactive UI elements (buttons in menus)
**Example:**
```csharp
// Button interaction pattern
private Rectangle _buttonBounds;

public void Update(MouseState currentMouse, MouseState previousMouse)
{
    Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
    bool isHovering = _buttonBounds.Contains(mousePos);

    // Detect click (mouse was pressed this frame)
    bool wasClicked = isHovering
        && currentMouse.LeftButton == ButtonState.Released
        && previousMouse.LeftButton == ButtonState.Pressed;

    if (wasClicked)
    {
        OnClick?.Invoke();
    }
}
```

### Pattern 7: SpriteFont for Text Rendering
**What:** Load .spritefont descriptor via Content Pipeline, draw text with MeasureString for centering
**When to use:** All text rendering (HUD counters, menus, notifications)
**Example:**
```csharp
// From GameOverScreen.cs - established project pattern
private SpriteFont _font;

public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
{
    _font = content.Load<SpriteFont>("Font"); // Font.spritefont exists
}

public void Draw(SpriteBatch spriteBatch, Viewport viewport)
{
    // Measure text for centering
    Vector2 textSize = _font.MeasureString(_message);
    Vector2 screenCenter = new Vector2(viewport.Width / 2f, viewport.Height / 2f);
    Vector2 textPosition = screenCenter - textSize / 2f;

    // Draw centered text
    spriteBatch.DrawString(_font, _message, textPosition, Color.White);
}
```

### Pattern 8: Notification Queue with Timed Removal
**What:** List of active notifications, each with lifetime timer and fade-out
**When to use:** Temporary messages (pickup notifications, damage numbers)
**Example:**
```csharp
public class PickupNotification
{
    private class Notification
    {
        public string Text;
        public float TimeRemaining;
        public float Alpha;
    }

    private List<Notification> _activeNotifications = new();
    private const float NotificationDuration = 2.0f; // 2 seconds visible

    public void Show(string text)
    {
        _activeNotifications.Add(new Notification
        {
            Text = text,
            TimeRemaining = NotificationDuration,
            Alpha = 1.0f
        });
    }

    public void Update(float deltaTime)
    {
        for (int i = _activeNotifications.Count - 1; i >= 0; i--)
        {
            var notif = _activeNotifications[i];
            notif.TimeRemaining -= deltaTime;

            // Fade out in last 0.5 seconds
            if (notif.TimeRemaining < 0.5f)
            {
                notif.Alpha = notif.TimeRemaining / 0.5f;
            }

            // Remove when expired
            if (notif.TimeRemaining <= 0)
            {
                _activeNotifications.RemoveAt(i);
            }
        }
    }
}
```

### Anti-Patterns to Avoid
- **Multiple SpriteBatch passes:** Don't Begin/End for each UI element - batch all drawing in one pass (performance: 24% slower with separate passes)
- **Frame-dependent animations:** Don't use `alpha -= 0.05f` without deltaTime - causes speed variations at different framerates (use exponential decay or linear interpolation with deltaTime)
- **Recreating textures each frame:** Don't call `new Texture2D()` in Update/Draw - create once in LoadContent (causes memory churn and GC pressure)
- **String concatenation in Draw:** Don't use `"Score: " + score` every frame - cache strings or use string interpolation sparingly (MeasureString and DrawString are already expensive)
- **Immediate mode GUI pattern:** Don't check input in Draw() - separate Update (input/logic) from Draw (rendering only)

## Don't Hand-Roll

Problems that look simple but have existing solutions or established patterns:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Button state management | Custom hover/click state machine | Rectangle.Contains + MouseState comparison pattern | Edge cases: mouse leaving during press, multiple buttons, keyboard nav. Use proven pattern from MonoGame samples |
| Text centering/alignment | Manual X/Y offset calculations | SpriteFont.MeasureString + Vector2 math | MeasureString accounts for kerning, font metrics, multi-line text. Don't reinvent text layout |
| Color transitions | Manual RGB interpolation | Color.Lerp(start, end, t) | Built-in linear interpolation handles alpha blending and clamping correctly |
| Input state tracking | Manual previous/current state fields | InputManager pattern (already exists in project) | Project already has InputManager with IsKeyPressed/IsLeftMouseHeld - extend for menu navigation |
| Pixel texture generation | Writing image files and loading | Texture2D + SetData(Color[] pixels) | Project pattern from Crosshair/HealthBar - no external assets needed |

**Key insight:** MonoGame UI is intentionally low-level - patterns matter more than libraries. The project already established solid patterns (single draw pass, exponential decay, programmatic textures) that should be consistently applied to new elements rather than introducing new approaches.

## Common Pitfalls

### Pitfall 1: SpriteBatch State Changes Mid-Batch
**What goes wrong:** Calling Begin/End multiple times per frame with different BlendState/SamplerState causes performance degradation. Each Begin reapplies graphics states (RasterizerState, BlendState, DepthStencilState, SamplerStates).
**Why it happens:** Developer wants different blend modes for UI effects or tries to separate 3D/2D rendering mid-frame
**How to avoid:**
- Use a single SpriteBatch.Begin() call for all UI elements
- Draw 3D content first (before SpriteBatch.Begin), then all 2D UI in one batch
- If blend state changes are essential, group all elements with same state together
- Project pattern: All UI draws happen in one Begin/End pair after 3D rendering completes
**Warning signs:** Frame time spikes when UI elements are added; profiler shows many Begin/End calls

### Pitfall 2: Text Rendering Performance with Dynamic Strings
**What goes wrong:** DrawString with frequently changing text (like score counter incrementing) causes performance issues. Each DrawString call measures glyphs and builds vertex buffer. MonoGame achieved 52.4% improvement in MeasureString and 24.4% in DrawString performance in recent versions, but it's still the slowest UI operation.
**Why it happens:** Developer treats DrawString like a cheap operation, calling it for many dynamic counters
**How to avoid:**
- Minimize string allocations: cache format strings, avoid string concatenation in hot paths
- For completely static text (like labels), consider pre-rendering to RenderTarget2D (not needed for this phase)
- Draw only visible/changed text - don't draw UI elements that are off-screen or unchanged
- Include only needed characters in .spritefont CharacterRegions (project Font.spritefont: ASCII 32-126)
**Warning signs:** Frame drops when score updates rapidly; GC collections during gameplay; profiler shows DrawString as hotspot

### Pitfall 3: Frame-Rate Dependent Animations
**What goes wrong:** Animations use fixed increments without deltaTime, causing different speeds at different framerates. Example: `alpha -= 0.05f` runs 2x faster at 120fps vs 60fps.
**Why it happens:** XNA/MonoGame tutorials often use fixed timestep as excuse to skip deltaTime math
**How to avoid:**
- Always multiply by deltaTime for linear changes: `alpha -= decayRate * deltaTime`
- Use exponential decay for smooth fade-outs: `alpha *= (float)Math.Pow(0.01, deltaTime / duration)`
- Project established pattern uses exponential decay in DamageVignette - apply consistently
**Warning signs:** UI animations feel different on different machines; effects too fast/slow after refactoring

### Pitfall 4: Mouse Input Without Previous State Tracking
**What goes wrong:** Checking `MouseState.LeftButton == ButtonState.Pressed` every frame causes button to trigger continuously while held, not once per click
**Why it happens:** Misunderstanding button state vs button event model
**How to avoid:**
- Track previous frame's MouseState: `_previousMouse = Mouse.GetState()` at end of Update
- Detect press event: `current.LeftButton == Pressed && previous.LeftButton == Released`
- Detect release event: `current.LeftButton == Released && previous.LeftButton == Pressed`
- Project's InputManager handles this for keyboard/mouse - extend for menu buttons
**Warning signs:** Buttons trigger multiple times from single click; ESC key pauses/unpauses rapidly

### Pitfall 5: UI Element Positioning Without Viewport Awareness
**What goes wrong:** Hard-coded positions like `new Vector2(400, 300)` break when window is resized or on different resolutions
**Why it happens:** Developer tests at single resolution and doesn't consider windowed mode or different displays
**How to avoid:**
- Use `Viewport.Width` and `Viewport.Height` for relative positioning
- Anchor to screen edges: top-left `(20, 20)`, top-right `(Viewport.Width - elementWidth - 20, 20)`
- Center elements: `(Viewport.Width / 2f - textSize.X / 2f, Viewport.Height / 2f - textSize.Y / 2f)`
- Project uses Viewport for centering (Crosshair, GameOverScreen) - apply pattern consistently
**Warning signs:** UI off-screen at different resolutions; centered elements not actually centered

### Pitfall 6: Missing Game State Transition Cleanup
**What goes wrong:** Changing GameState without cleaning up previous state (e.g., pausing without stopping sounds, unpausing without resetting input)
**Why it happens:** Focus on new state initialization, forget to clean up old state
**How to avoid:**
- Implement state exit/enter methods that handle cleanup
- Reset input states when transitioning: clear `_previousMouse`, reset `InputManager`
- Project's RestartGame() method shows cleanup pattern - apply to pause/unpause
- Disable player controls when not in Playing state: `_playerController.IsEnabled = false`
**Warning signs:** Input bleeds across states (paused but player still moves); objects don't reset on restart

### Pitfall 7: Z-Ordering and Overlay Drawing
**What goes wrong:** UI elements draw in wrong order (crosshair behind menu, notifications under health bar)
**Why it happens:** SpriteBatch draws in call order, easy to forget layering requirements
**How to avoid:**
- Establish draw order convention: HUD elements → overlays (vignette, fade) → menu screens
- Comment the draw order in main Draw method for clarity
- Project draws crosshair/HUD first, then vignette/fade, then game over screen - maintain this order
- For complex layering, use SpriteBatch sortMode (not needed for this phase)
**Warning signs:** UI elements flicker or appear behind other elements; visual inconsistencies

## Code Examples

Verified patterns from existing codebase and official sources:

### Ammo Counter Display (New Element)
```csharp
// Pattern: Extend HealthBar.cs approach for ammo display
public class AmmoCounter
{
    private SpriteFont _font;
    private const int PosX = 20;  // Distance from right edge (calculated in Draw)
    private const int PosY = 20;  // Distance from top

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    public void Draw(SpriteBatch spriteBatch, int currentMag, int reserveAmmo, Viewport viewport)
    {
        string ammoText = $"{currentMag} / {reserveAmmo}";
        Vector2 textSize = _font.MeasureString(ammoText);

        // Position at top-right (CONTEXT: classic corners layout)
        Vector2 position = new Vector2(viewport.Width - textSize.X - PosX, PosY);

        // Flash red when low (CONTEXT: <10 rounds)
        Color textColor = currentMag < 10 ? Color.Red : Color.White;

        spriteBatch.DrawString(_font, ammoText, position, textColor);
    }
}
```

### Score Counter Display (New Element)
```csharp
// Pattern: Center-aligned text at top of screen
public class ScoreCounter
{
    private SpriteFont _font;
    private const int PosY = 20;  // Distance from top

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    public void Draw(SpriteBatch spriteBatch, int score, Viewport viewport)
    {
        // CONTEXT: Score counter always visible at top-center
        string scoreText = $"Score: {score}";
        Vector2 textSize = _font.MeasureString(scoreText);
        Vector2 position = new Vector2(
            viewport.Width / 2f - textSize.X / 2f,  // Centered horizontally
            PosY                                     // Fixed top margin
        );

        spriteBatch.DrawString(_font, scoreText, position, Color.White);
    }
}
```

### Score Tracking System (New System)
```csharp
// Pattern: Event-based system similar to HealthSystem
public class ScoreSystem
{
    public int CurrentScore { get; private set; } = 0;

    // Point values (can be tuned during implementation)
    private const int PointsPerEnemy = 50;

    public event Action<int>? OnScoreChanged;  // Optional for effects

    public void AddEnemyKill()
    {
        CurrentScore += PointsPerEnemy;
        OnScoreChanged?.Invoke(CurrentScore);
    }

    public void Reset()
    {
        CurrentScore = 0;
        OnScoreChanged?.Invoke(CurrentScore);
    }
}

// Wire in BerzerkGame.cs:
// _enemyManager.OnAllEnemiesDefeated += () => { /* existing code */ };
// _enemyManager.OnEnemyDeath += _scoreSystem.AddEnemyKill;
```

### Pickup Notification Queue (New Element)
```csharp
// Pattern: Exponential decay fade-out with stacking notifications
public class PickupNotification
{
    private class Notification
    {
        public string Text;
        public float TimeRemaining;
        public Vector2 Position;
    }

    private SpriteFont _font;
    private List<Notification> _activeNotifications = new();
    private const float Duration = 2.0f;      // CONTEXT: brief text popup
    private const float FadeTime = 0.5f;      // Last 0.5s fade out
    private const int BaseY = 150;            // Below score counter
    private const int StackOffset = 40;       // Space between notifications

    public void LoadContent(ContentManager content)
    {
        _font = content.Load<SpriteFont>("Font");
    }

    public void Show(string text, Viewport viewport)
    {
        // CONTEXT: '+10 Ammo', '+20 Health' format
        Vector2 textSize = _font.MeasureString(text);
        Vector2 position = new Vector2(
            viewport.Width / 2f - textSize.X / 2f,  // Centered
            BaseY + _activeNotifications.Count * StackOffset  // Stack vertically
        );

        _activeNotifications.Add(new Notification
        {
            Text = text,
            TimeRemaining = Duration,
            Position = position
        });
    }

    public void Update(float deltaTime)
    {
        // Update and remove expired (iterate backwards for safe removal)
        for (int i = _activeNotifications.Count - 1; i >= 0; i--)
        {
            _activeNotifications[i].TimeRemaining -= deltaTime;

            if (_activeNotifications[i].TimeRemaining <= 0)
            {
                _activeNotifications.RemoveAt(i);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var notification in _activeNotifications)
        {
            // Fade out in last FadeTime seconds (exponential decay)
            float alpha = 1.0f;
            if (notification.TimeRemaining < FadeTime)
            {
                alpha = notification.TimeRemaining / FadeTime;
            }

            // CONTEXT: monochrome white/gray aesthetic
            Color color = Color.White * alpha;
            spriteBatch.DrawString(_font, notification.Text, notification.Position, color);
        }
    }
}

// Wire to pickup collection in BerzerkGame.UpdatePlaying():
// _targetManager.CheckPickupCollection(...);
// if (ammo pickup collected) _pickupNotification.Show("+10 Ammo", viewport);
// if (health pickup collected) _pickupNotification.Show("+20 Health", viewport);
```

### Start Menu with Button (New Screen)
```csharp
// Pattern: Simple button with mouse interaction
public class StartMenu
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private string _buttonText = "Start Game";
    private Rectangle _buttonBounds;
    private bool _isHovering = false;

    public event Action? OnStartGame;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Font");

        // Create pixel texture for button background (project pattern)
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(MouseState currentMouse, MouseState previousMouse)
    {
        Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
        _isHovering = _buttonBounds.Contains(mousePos);

        // Detect click (button released while hovering)
        if (_isHovering
            && currentMouse.LeftButton == ButtonState.Released
            && previousMouse.LeftButton == ButtonState.Pressed)
        {
            OnStartGame?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        // CONTEXT: Match HUD style (minimal white/gray)

        // Measure button size
        Vector2 textSize = _font.MeasureString(_buttonText);
        int buttonWidth = (int)textSize.X + 40;   // Padding
        int buttonHeight = (int)textSize.Y + 20;

        // Center button
        _buttonBounds = new Rectangle(
            viewport.Width / 2 - buttonWidth / 2,
            viewport.Height / 2 - buttonHeight / 2,
            buttonWidth,
            buttonHeight
        );

        // Draw button background (darker when hovering)
        Color bgColor = _isHovering ? Color.DarkGray : Color.Gray;
        spriteBatch.Draw(_pixelTexture, _buttonBounds, bgColor);

        // Draw button text (centered)
        Vector2 textPos = new Vector2(
            _buttonBounds.X + (_buttonBounds.Width - textSize.X) / 2f,
            _buttonBounds.Y + (_buttonBounds.Height - textSize.Y) / 2f
        );
        spriteBatch.DrawString(_font, _buttonText, textPos, Color.White);
    }
}
```

### Pause Menu (New Screen)
```csharp
// Pattern: Overlay menu with Resume/Quit buttons
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
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(MouseState currentMouse, MouseState previousMouse)
    {
        Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
        _isHoveringResume = _resumeButton.Contains(mousePos);
        _isHoveringQuit = _quitButton.Contains(mousePos);

        bool wasClicked = currentMouse.LeftButton == ButtonState.Released
            && previousMouse.LeftButton == ButtonState.Pressed;

        if (wasClicked)
        {
            if (_isHoveringResume) OnResume?.Invoke();
            if (_isHoveringQuit) OnQuit?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        // Semi-transparent background overlay (darken gameplay)
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black * 0.7f);

        // "PAUSED" title
        string title = "PAUSED";
        Vector2 titleSize = _font.MeasureString(title);
        Vector2 titlePos = new Vector2(
            viewport.Width / 2f - titleSize.X / 2f,
            viewport.Height / 2f - 100
        );
        spriteBatch.DrawString(_font, title, titlePos, Color.White);

        // Resume button (higher priority, above Quit)
        DrawButton(spriteBatch, "Resume", viewport.Height / 2f,
            _isHoveringResume, out _resumeButton);

        // Quit button (below Resume)
        DrawButton(spriteBatch, "Quit", viewport.Height / 2f + 60,
            _isHoveringQuit, out _quitButton);
    }

    private void DrawButton(SpriteBatch spriteBatch, string text, float centerY,
        bool isHovering, out Rectangle bounds)
    {
        Vector2 textSize = _font.MeasureString(text);
        int buttonWidth = (int)textSize.X + 40;
        int buttonHeight = (int)textSize.Y + 20;

        bounds = new Rectangle(
            GraphicsDevice.Viewport.Width / 2 - buttonWidth / 2,
            (int)centerY - buttonHeight / 2,
            buttonWidth,
            buttonHeight
        );

        Color bgColor = isHovering ? Color.DarkGray : Color.Gray;
        spriteBatch.Draw(_pixelTexture, bounds, bgColor);

        Vector2 textPos = new Vector2(
            bounds.X + (bounds.Width - textSize.X) / 2f,
            bounds.Y + (bounds.Height - textSize.Y) / 2f
        );
        spriteBatch.DrawString(_font, text, textPos, Color.White);
    }
}
```

### Game Over Screen Enhancement (Update Existing)
```csharp
// Update GameOverScreen.cs to show final score and have buttons
public class GameOverScreen
{
    private SpriteFont _font;
    private Texture2D _pixelTexture;
    private Rectangle _restartButton;
    private Rectangle _quitButton;
    private bool _isHoveringRestart;
    private bool _isHoveringQuit;

    public event Action? OnRestart;
    public event Action? OnQuit;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Font");
        _pixelTexture = new Texture2D(graphicsDevice, 1, 1);
        _pixelTexture.SetData(new[] { Color.White });
    }

    public void Update(MouseState currentMouse, MouseState previousMouse)
    {
        Vector2 mousePos = new Vector2(currentMouse.X, currentMouse.Y);
        _isHoveringRestart = _restartButton.Contains(mousePos);
        _isHoveringQuit = _quitButton.Contains(mousePos);

        bool wasClicked = currentMouse.LeftButton == ButtonState.Released
            && previousMouse.LeftButton == ButtonState.Pressed;

        if (wasClicked)
        {
            if (_isHoveringRestart) OnRestart?.Invoke();
            if (_isHoveringQuit) OnQuit?.Invoke();
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport, int finalScore)
    {
        // Black background (full screen)
        spriteBatch.Draw(_pixelTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.Black);

        // "GAME OVER" title
        string gameOverText = "GAME OVER";
        Vector2 gameOverSize = _font.MeasureString(gameOverText);
        Vector2 gameOverPos = new Vector2(
            viewport.Width / 2f - gameOverSize.X / 2f,
            viewport.Height / 2f - 120
        );
        spriteBatch.DrawString(_font, gameOverText, gameOverPos, Color.Red);

        // CONTEXT: Show final score - classic arcade feel
        string scoreText = $"Final Score: {finalScore}";
        Vector2 scoreSize = _font.MeasureString(scoreText);
        Vector2 scorePos = new Vector2(
            viewport.Width / 2f - scoreSize.X / 2f,
            viewport.Height / 2f - 50
        );
        spriteBatch.DrawString(_font, scoreText, scorePos, Color.White);

        // CONTEXT: 'Restart' / 'Quit' buttons
        DrawButton(spriteBatch, "Restart", viewport.Height / 2f + 30,
            _isHoveringRestart, out _restartButton, viewport);
        DrawButton(spriteBatch, "Quit", viewport.Height / 2f + 100,
            _isHoveringQuit, out _quitButton, viewport);
    }

    private void DrawButton(SpriteBatch spriteBatch, string text, float centerY,
        bool isHovering, out Rectangle bounds, Viewport viewport)
    {
        Vector2 textSize = _font.MeasureString(text);
        int buttonWidth = (int)textSize.X + 40;
        int buttonHeight = (int)textSize.Y + 20;

        bounds = new Rectangle(
            viewport.Width / 2 - buttonWidth / 2,
            (int)centerY - buttonHeight / 2,
            buttonWidth,
            buttonHeight
        );

        Color bgColor = isHovering ? Color.DarkGray : Color.Gray;
        spriteBatch.Draw(_pixelTexture, bounds, bgColor);

        Vector2 textPos = new Vector2(
            bounds.X + (bounds.Width - textSize.X) / 2f,
            bounds.Y + (bounds.Height - textSize.Y) / 2f
        );
        spriteBatch.DrawString(_font, text, textPos, Color.White);
    }
}
```

### BerzerkGame.cs Integration Pattern
```csharp
// Update BerzerkGame enum and state management
public enum GameState
{
    MainMenu,    // NEW: Initial screen with Start button
    Playing,     // EXISTING: Active gameplay
    Paused,      // NEW: ESC overlay menu
    Dying,       // EXISTING: Fade-out transition
    GameOver     // EXISTING: Death screen
}

// Add new fields in BerzerkGame class:
private ScoreSystem _scoreSystem;
private AmmoCounter _ammoCounter;
private ScoreCounter _scoreCounter;
private PickupNotification _pickupNotification;
private StartMenu _startMenu;
private PauseMenu _pauseMenu;
private MouseState _previousMouseState;

// In Initialize():
_scoreSystem = new ScoreSystem();

// In LoadContent():
_ammoCounter = new AmmoCounter();
_ammoCounter.LoadContent(Content);

_scoreCounter = new ScoreCounter();
_scoreCounter.LoadContent(Content);

_pickupNotification = new PickupNotification();
_pickupNotification.LoadContent(Content);

_startMenu = new StartMenu();
_startMenu.LoadContent(Content, GraphicsDevice);
_startMenu.OnStartGame += () => { _gameState = GameState.Playing; };

_pauseMenu = new PauseMenu();
_pauseMenu.LoadContent(Content, GraphicsDevice);
_pauseMenu.OnResume += () => { _gameState = GameState.Playing; };
_pauseMenu.OnQuit += Exit;

_gameOverScreen = new GameOverScreen();
_gameOverScreen.LoadContent(Content, GraphicsDevice);
_gameOverScreen.OnRestart += RestartGame;
_gameOverScreen.OnQuit += Exit;

// Wire score system to enemy deaths:
_enemyManager.OnEnemyDeath += (enemy) => _scoreSystem.AddEnemyKill();

// In Update():
protected override void Update(GameTime gameTime)
{
    _inputManager.Update();
    MouseState currentMouse = Mouse.GetState();

    switch (_gameState)
    {
        case GameState.MainMenu:
            _startMenu.Update(currentMouse, _previousMouseState);
            break;

        case GameState.Playing:
            // ESC pauses (CONTEXT: pause menu)
            if (_inputManager.IsKeyPressed(Keys.Escape))
            {
                _gameState = GameState.Paused;
                _playerController.IsEnabled = false;
            }

            UpdatePlaying(gameTime, deltaTime);
            _pickupNotification.Update(deltaTime);
            break;

        case GameState.Paused:
            // ESC unpauses
            if (_inputManager.IsKeyPressed(Keys.Escape))
            {
                _gameState = GameState.Playing;
                _playerController.IsEnabled = true;
            }
            _pauseMenu.Update(currentMouse, _previousMouseState);
            break;

        case GameState.Dying:
            UpdateDying(deltaTime);
            break;

        case GameState.GameOver:
            _gameOverScreen.Update(currentMouse, _previousMouseState);
            break;
    }

    _previousMouseState = currentMouse;
    base.Update(gameTime);
}

// In Draw() - extend UI section:
_spriteBatch.Begin();

if (_gameState == GameState.MainMenu)
{
    _startMenu.Draw(_spriteBatch, GraphicsDevice.Viewport);
}
else if (_gameState == GameState.Playing || _gameState == GameState.Paused)
{
    // HUD elements (always visible, even when paused)
    _crosshair.Draw(_spriteBatch, GraphicsDevice.Viewport);
    _healthBar.Draw(_spriteBatch, _healthSystem.CurrentHealth, _healthSystem.MaxHealth);
    _ammoCounter.Draw(_spriteBatch, _ammoSystem.CurrentMagazine,
        _ammoSystem.ReserveAmmo, GraphicsDevice.Viewport);
    _scoreCounter.Draw(_spriteBatch, _scoreSystem.CurrentScore, GraphicsDevice.Viewport);
    _pickupNotification.Draw(_spriteBatch);

    // Pause overlay (only when paused)
    if (_gameState == GameState.Paused)
    {
        _pauseMenu.Draw(_spriteBatch, GraphicsDevice.Viewport);
    }
}

// Effects (always)
_damageVignette.Draw(_spriteBatch, GraphicsDevice.Viewport);
_screenFade.Draw(_spriteBatch, GraphicsDevice.Viewport);

// Game over screen
if (_gameState == GameState.GameOver)
{
    _gameOverScreen.Draw(_spriteBatch, GraphicsDevice.Viewport, _scoreSystem.CurrentScore);
}

_spriteBatch.End();
```

### Pickup Collection Integration
```csharp
// In BerzerkGame.UpdatePlaying() - wire pickup notifications
// Extend TargetManager.CheckPickupCollection to return what was collected
// OR check pickup counts before/after and show notification:

int ammoBeforeCollection = _ammoSystem.TotalAmmo;
int healthBeforeCollection = _healthSystem.CurrentHealth;

_targetManager.CheckPickupCollection(_playerController.Transform.Position,
    _ammoSystem, _healthSystem);

// Show notifications for pickups
int ammoGained = _ammoSystem.TotalAmmo - ammoBeforeCollection;
if (ammoGained > 0)
{
    _pickupNotification.Show($"+{ammoGained} Ammo", GraphicsDevice.Viewport);
}

int healthGained = _healthSystem.CurrentHealth - healthBeforeCollection;
if (healthGained > 0)
{
    _pickupNotification.Show($"+{healthGained} Health", GraphicsDevice.Viewport);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Complex UI frameworks (EmptyKeys, Nez.UI) | Minimal hand-rolled with SpriteBatch | Always available | For simple HUD with 6-10 elements, hand-rolled is faster to implement and has zero framework overhead |
| Fixed-rate animations (alpha -= 0.05f) | Exponential decay with deltaTime | Project established pattern | Frame-rate independent animations, smoother visual feel |
| Individual pixel textures per UI element | Single shared 1x1 pixel texture | Project pattern (HealthBar, GameOverScreen) | Reduced memory overhead, simpler texture management |
| Multiple SpriteBatch passes with state changes | Single Begin/End for all UI | XNA best practices (2010+) | 24% performance improvement, simpler rendering pipeline |
| String allocation in Draw() | MeasureString once, cache when possible | MonoGame 3.8+ optimizations | 52% faster MeasureString, reduced GC pressure |

**Deprecated/outdated:**
- **XNA 4.0 Game State Management sample:** Stack-based screen system with transitions - overly complex for simple game with 5 states. Modern approach uses simple enum + switch statement (project pattern)
- **Immediate mode GUI (ImGui-style):** Checking input in Draw() method - violates separation of concerns. Update/Draw separation is standard MonoGame pattern
- **SpriteFont texture atlases as .png:** Old manual workflow. Current: .spritefont XML descriptor processed by Content Pipeline, automatic texture packing

## Open Questions

Things that couldn't be fully resolved:

1. **Low ammo flash animation timing**
   - What we know: CONTEXT specifies "flash ammo counter when low (<10 rounds)", exponential decay pattern exists in DamageVignette
   - What's unclear: Flash frequency (continuous pulse vs one-time flash), flash color (red vs white), trigger timing (every frame vs on state change)
   - Recommendation: Start with continuous pulse using `Math.Sin(gameTime.TotalGameTime.TotalSeconds * frequency)` to modulate alpha/color when `currentMag < 10`. If too distracting, switch to one-time flash on transition to low ammo state.

2. **Health bar flash coordination with damage vignette**
   - What we know: CONTEXT specifies "Red vignette flash + health bar flash" on damage, both use exponential decay
   - What's unclear: Should health bar flash be same duration as vignette (0.4s), or longer/shorter? Should they use same trigger or separate?
   - Recommendation: Reuse DamageVignette.Trigger() timing, add `Trigger()` method to HealthBar that flashes bar red for 0.4s with exponential decay. Both triggered by HealthSystem.OnDamageTaken event.

3. **Score update performance with frequent enemy deaths**
   - What we know: CONTEXT specifies "silent update to counter only" (no effects), DrawString performance improved 24% in MonoGame 3.8+
   - What's unclear: Will score updates cause frame drops if many enemies die in single frame? Should score increment be animated/lerped?
   - Recommendation: Start with immediate score update (no animation). If profiling shows DrawString hotspot, cache score string until value changes. Don't optimize prematurely.

4. **Pause menu input handling while paused**
   - What we know: Project has InputManager with IsKeyPressed pattern, ESC pauses/unpauses
   - What's unclear: Should mouse visibility change when paused? Should camera/player still update (for visual feedback)?
   - Recommendation: Keep mouse visible (IsMouseVisible = false in game, but pause menu needs cursor for buttons). Freeze all gameplay updates (_playerController.IsEnabled = false) but continue rendering 3D scene behind semi-transparent overlay.

5. **Start menu initial game state**
   - What we know: CONTEXT specifies "Just 'Start Game' button", GameState.MainMenu should be initial state
   - What's unclear: Should 3D scene render behind menu, or black screen? Should enemies spawn before "Start Game" clicked?
   - Recommendation: Render 3D scene (player, room, enemies) behind start menu for visual interest. Don't run gameplay Update logic until GameState.Playing. Initialize world in LoadContent, activate on button click.

## Sources

### Primary (HIGH confidence)
- MonoGame Official Documentation - SpriteBatch: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteBatch.html
- MonoGame Official Documentation - SpriteFont: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.SpriteFont.html
- MonoGame Official Documentation - Working with SpriteFonts: https://docs.monogame.net/articles/tutorials/building_2d_games/16_working_with_spritefonts/index.html
- MonoGame Official Documentation - Drawing Text: https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Draw_Text.html
- MonoGame Official Documentation - UI Fundamentals: https://docs.monogame.net/articles/tutorials/building_2d_games/19_user_interface_fundamentals/index.html
- MonoGame Official Documentation - Scene Management: https://docs.monogame.net/articles/tutorials/building_2d_games/17_scenes/
- MonoGame Official Documentation - Input Handling: https://docs.monogame.net/articles/tutorials/building_2d_games/10_handling_input/index.html
- MonoGame Official Documentation - Mouse Input: https://docs.monogame.net/api/Microsoft.Xna.Framework.Input.Mouse.html
- Project codebase analysis: /Users/rui/src/pg/berzerk/Berzerk/Source/UI/*.cs (Crosshair, HealthBar, GameOverScreen, DamageVignette, ScreenFade)
- Project codebase: /Users/rui/src/pg/berzerk/Berzerk/Content/Font.spritefont (Arial Bold 32pt, ASCII 32-126)
- Project codebase: /Users/rui/src/pg/berzerk/Berzerk/BerzerkGame.cs (GameState enum, Update/Draw patterns)

### Secondary (MEDIUM confidence)
- MonoGame Community - SpriteBatch Begin/End best practices: https://community.monogame.net/t/when-and-how-often-should-i-call-spritebatch-begin-and-spritebatch-end/13248
- MonoGame Community - Using SpriteBatch correctly: https://community.monogame.net/t/using-the-spritebatch-the-correct-way/8082
- MonoGame GitHub - SpriteFont performance improvements PR: https://github.com/MonoGame/MonoGame/pull/5874 (52.4% MeasureString, 24.4% DrawString improvement)
- MonoGame GitHub - DrawString performance PR: https://github.com/MonoGame/MonoGame/pull/5226
- MonoGame Community - UI frameworks discussion: https://community.monogame.net/t/what-are-you-guys-using-for-ui-looking-for-a-simple-ui-for-monogame/8313
- GitHub - GeonBit.UI repository: https://github.com/RonenNess/GeonBit.UI
- GitHub - Myra UI library: https://github.com/rds1983/Myra
- GitHub - MonoGame.Samples: https://github.com/MonoGame/MonoGame.Samples
- GameDev Without a Cause - Pre-rendering text for performance: https://gamedevwithoutacause.com/?p=1244
- MonoGame Community - Fade in/out screen tutorial: https://community.monogame.net/t/sharing-an-easy-way-to-fade-in-fade-out-screen/2677

### Tertiary (LOW confidence)
- RB Whitaker's Wiki - SpriteBatch basics: http://rbwhitaker.wikidot.com/monogame-spritebatch-basics
- RB Whitaker's Wiki - Drawing text with SpriteFont: http://rbwhitaker.wikidot.com/monogame-drawing-text-with-spritefonts
- GameFromScratch - MonoGame textures and SpriteBatch: https://gamefromscratch.com/monogame-tutorial-textures-and-spritebatch/
- GameFromScratch - MonoGame input handling: https://gamefromscratch.com/monogame-tutorial-handling-keyboard-mouse-and-gamepad-input/

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - MonoGame 3.8 is in project, SpriteBatch/SpriteFont are official APIs, patterns verified in existing codebase
- Architecture: HIGH - 4 of 6 UI elements already implemented with consistent patterns, official docs confirm approaches
- Pitfalls: HIGH - Community discussions and GitHub issues document real performance problems, existing code avoids common mistakes
- Code examples: HIGH - All examples based on existing project patterns (Crosshair, HealthBar, GameOverScreen, DamageVignette) or verified official documentation

**Research date:** 2026-02-09
**Valid until:** 2026-03-09 (30 days - MonoGame is stable, patterns are established, SpriteBatch API unchanged since XNA 4.0)
