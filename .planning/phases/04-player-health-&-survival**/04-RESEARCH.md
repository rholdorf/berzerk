# Phase 4: Player Health & Survival - Research

**Researched:** 2026-02-03
**Domain:** Health system, damage feedback, game state management in MonoGame
**Confidence:** HIGH

## Summary

This phase implements player health tracking, damage visual feedback (red vignette), death sequence (fade to black), and game-over state with restart functionality. The research focuses on MonoGame patterns for screen overlays, state management, and UI rendering within the existing codebase architecture.

The existing codebase demonstrates clear patterns: programmatic texture generation (Crosshair.cs), component-based systems (AmmoSystem.cs), time-based animations (ImpactEffect.cs), and centralized input handling (InputManager.cs). Phase 4 follows these established patterns.

**Primary recommendation:** Use SpriteBatch with 1x1 pixel textures and color multiplication for all screen overlays (vignette, fade to black). This matches the existing programmatic texture approach and avoids external asset dependencies.

## Standard Stack

This phase uses existing MonoGame capabilities - no new libraries needed.

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MonoGame.Framework | 3.8.4.1 | SpriteBatch for overlays, Texture2D for programmatic textures | Already in project |
| .NET 8 | 8.0 | Runtime, Math functions | Already in project |

### Supporting
| Class | Purpose | When to Use |
|-------|---------|-------------|
| SpriteBatch | 2D overlay rendering | Vignette, fade to black, health bar |
| Texture2D.SetData | Create textures at runtime | 1x1 pixel for solid colors, vignette gradient |
| Color.Lerp | Interpolate colors | Fade animations |
| SpriteFont | Text rendering | Game over message |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| SpriteBatch overlay | HLSL shader | More flexible vignette shape but adds complexity; SpriteBatch sufficient for phase scope |
| Programmatic texture | Asset file | Would require content pipeline setup; programmatic matches existing crosshair pattern |
| Simple state enum | Full scene system | Scene system better for larger games; enum sufficient for single game-over screen |

## Architecture Patterns

### Recommended Project Structure
```
Berzerk/Source/
├── Player/
│   └── HealthSystem.cs        # Health tracking, damage/heal, death state
├── UI/
│   ├── Crosshair.cs           # Existing
│   ├── DamageVignette.cs      # Red screen flash on damage
│   └── ScreenFade.cs          # Fade to black for death
└── Core/
    └── GameStateManager.cs    # Playing, Dying, GameOver states
```

### Pattern 1: Health Component (follows AmmoSystem pattern)
**What:** Self-contained health state with public properties and methods
**When to use:** Health tracking with min/max bounds and death detection
**Example:**
```csharp
// Source: Follows existing AmmoSystem.cs pattern
public class HealthSystem
{
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; } = 200;  // Allow overheal
    public int StartingHealth { get; private set; } = 100;
    public bool IsDead => CurrentHealth <= 0;

    // Event for damage feedback (vignette trigger)
    public event Action OnDamageTaken;
    public event Action OnDeath;

    public HealthSystem()
    {
        CurrentHealth = StartingHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;  // Ignore damage when dead

        CurrentHealth = Math.Max(0, CurrentHealth - amount);
        OnDamageTaken?.Invoke();

        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
    }

    public void Reset()
    {
        CurrentHealth = StartingHealth;
    }
}
```

### Pattern 2: Screen Overlay with Alpha Fade (follows ImpactEffect pattern)
**What:** Animated overlay that fades in/out using elapsed time
**When to use:** Damage vignette, fade to black
**Example:**
```csharp
// Source: https://community.monogame.net/t/sharing-an-easy-way-to-fade-in-fade-out-screen/2677
public class DamageVignette
{
    private Texture2D _vignetteTexture;
    private float _alpha = 0f;
    private float _fadeInTime = 0.05f;   // Fast flash in
    private float _fadeOutTime = 0.4f;   // Slower fade out
    private float _elapsed = 0f;
    private bool _isActive = false;

    public void Trigger()
    {
        _alpha = 1f;  // Instant flash
        _elapsed = 0f;
        _isActive = true;
    }

    public void Update(float deltaTime)
    {
        if (!_isActive) return;

        _elapsed += deltaTime;

        // Fade out using exponential decay (matches project pattern)
        float decay = (float)Math.Pow(0.01, deltaTime / _fadeOutTime);
        _alpha *= decay;

        if (_alpha < 0.01f)
        {
            _alpha = 0f;
            _isActive = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Viewport viewport)
    {
        if (_alpha <= 0f) return;

        spriteBatch.Draw(
            _vignetteTexture,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            Color.White * _alpha
        );
    }
}
```

### Pattern 3: Simple Game State Enum
**What:** Enum-based state machine for game flow
**When to use:** Controlling update/draw behavior based on game state
**Example:**
```csharp
// Source: https://docs.monogame.net/articles/tutorials/building_2d_games/17_scenes/index.html (simplified)
public enum GameState
{
    Playing,   // Normal gameplay
    Dying,     // Death sequence (fade to black)
    GameOver   // Show restart prompt
}

// In BerzerkGame:
private GameState _gameState = GameState.Playing;

protected override void Update(GameTime gameTime)
{
    switch (_gameState)
    {
        case GameState.Playing:
            UpdateGameplay(gameTime);
            break;
        case GameState.Dying:
            UpdateDeathSequence(gameTime);
            break;
        case GameState.GameOver:
            UpdateGameOver(gameTime);
            break;
    }
}
```

### Pattern 4: Programmatic Vignette Texture
**What:** Create gradient texture at runtime for edge darkening
**When to use:** Damage vignette with darker edges, lighter center
**Example:**
```csharp
// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Texture2D.html
private Texture2D CreateVignetteTexture(GraphicsDevice graphicsDevice, int width, int height)
{
    var texture = new Texture2D(graphicsDevice, width, height);
    var pixels = new Color[width * height];

    float centerX = width / 2f;
    float centerY = height / 2f;
    float maxDist = (float)Math.Sqrt(centerX * centerX + centerY * centerY);

    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            float dx = x - centerX;
            float dy = y - centerY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // Edge intensity: 0 at center, 1 at corners
            float intensity = dist / maxDist;
            // Apply curve for softer transition
            intensity = intensity * intensity;  // Quadratic falloff

            pixels[y * width + x] = new Color(1f, 0f, 0f, intensity);  // Red vignette
        }
    }

    texture.SetData(pixels);
    return texture;
}
```

### Anti-Patterns to Avoid
- **Creating textures every frame:** Create vignette/fade textures once in LoadContent, not in Draw
- **Polling death state every frame without state machine:** Use events (OnDeath) to trigger state transitions once
- **Restarting by calling LoadContent directly:** Reset component state instead; avoid reloading assets
- **Hardcoding overlay dimensions:** Use Viewport.Width/Height for screen-independent rendering

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Color interpolation | Custom lerp | Color.Lerp(a, b, t) | Built-in, handles all channels correctly |
| Screen dimensions | Hardcoded values | GraphicsDevice.Viewport | Handles window resize automatically |
| Frame-rate timing | Raw deltaTime | Math.Pow(decay, deltaTime) | Frame-rate independent smoothing (project pattern) |
| Text centering | Manual calculation | SpriteFont.MeasureString | Accounts for font metrics, kerning |

**Key insight:** MonoGame provides Color.Lerp, Vector2.Lerp, and MathHelper utilities. The project already uses exponential decay smoothing for frame-rate independence - continue this pattern.

## Common Pitfalls

### Pitfall 1: Texture Creation in Draw Loop
**What goes wrong:** Creating new Texture2D every frame causes massive memory allocation and GC pressure
**Why it happens:** Seems simple to create texture when needed
**How to avoid:** Create all textures in LoadContent, reuse in Draw
**Warning signs:** Frame rate drops over time, GC spikes in profiler

### Pitfall 2: State Transitions Without Guards
**What goes wrong:** Death event fires multiple times, fade restarts mid-animation
**Why it happens:** Damage applies while already dying
**How to avoid:** Check IsDead before applying damage; use state machine with clear transitions
**Warning signs:** Multiple "death" sounds, animation restarts unexpectedly

### Pitfall 3: SpriteBatch Begin/End Mismatch with 3D
**What goes wrong:** 3D rendering corrupted after 2D overlay
**Why it happens:** SpriteBatch changes GraphicsDevice state (depth buffer, blend state)
**How to avoid:** Draw 3D first, then 2D overlays. Reset depth state if needed between.
**Warning signs:** 3D objects disappear or render incorrectly after adding overlay

### Pitfall 4: Fade Alpha Not Reaching Zero
**What goes wrong:** Overlay never fully disappears, permanent tint on screen
**Why it happens:** Exponential decay approaches but never reaches zero
**How to avoid:** Add threshold check: `if (alpha < 0.01f) alpha = 0f`
**Warning signs:** Slight color tint visible after damage fade should complete

### Pitfall 5: Input Handled During Death
**What goes wrong:** Player can still move/shoot during death sequence
**Why it happens:** Update methods still running for all systems
**How to avoid:** Check game state before processing gameplay input
**Warning signs:** Character moves while "dying", shots fired during game over

## Code Examples

### 1x1 Pixel Texture for Solid Color Overlay
```csharp
// Source: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Texture2D.html
// Creates reusable pixel for drawing filled rectangles
private Texture2D CreatePixelTexture(GraphicsDevice graphicsDevice)
{
    var pixel = new Texture2D(graphicsDevice, 1, 1);
    pixel.SetData(new[] { Color.White });
    return pixel;
}

// Usage for fade to black:
spriteBatch.Draw(
    _pixelTexture,
    new Rectangle(0, 0, viewport.Width, viewport.Height),
    Color.Black * _fadeAlpha  // Alpha controls transparency
);
```

### Centered Text Rendering
```csharp
// Source: https://docs.monogame.net/articles/tutorials/building_2d_games/16_working_with_spritefonts/index.html
string message = "Press R to Restart";
Vector2 textSize = _font.MeasureString(message);
Vector2 screenCenter = new Vector2(
    GraphicsDevice.Viewport.Width / 2f,
    GraphicsDevice.Viewport.Height / 2f
);
Vector2 textOrigin = textSize / 2f;

spriteBatch.DrawString(
    _font,
    message,
    screenCenter,
    Color.White,
    0f,              // rotation
    textOrigin,      // origin (center of text)
    1f,              // scale
    SpriteEffects.None,
    0f               // layer depth
);
```

### Frame-Rate Independent Fade (Project Pattern)
```csharp
// Source: STATE.md - exponential decay smoothing pattern
// Fade from 1 to 0 over approximately 1.5 seconds
float fadeSpeed = 1.5f;  // seconds to fade
float decay = (float)Math.Pow(0.001, deltaTime / fadeSpeed);
_fadeAlpha *= decay;

// Snap to zero when close enough
if (_fadeAlpha < 0.01f) _fadeAlpha = 0f;
```

### Health Bar Drawing (Source Rectangle Technique)
```csharp
// Source: https://docs.monogame.net/articles/getting_to_know/howto/graphics/HowTo_Draw_A_Sprite.html
// Draw health bar background (gray)
spriteBatch.Draw(_pixelTexture,
    new Rectangle(barX, barY, barWidth, barHeight),
    Color.DarkGray);

// Draw health fill (green/red based on percentage)
float healthPercent = (float)_healthSystem.CurrentHealth / _healthSystem.MaxHealth;
int fillWidth = (int)(barWidth * healthPercent);
Color fillColor = healthPercent > 0.3f ? Color.Green : Color.Red;

spriteBatch.Draw(_pixelTexture,
    new Rectangle(barX, barY, fillWidth, barHeight),
    fillColor);
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| HLSL vignette shader | SpriteBatch gradient texture | Still valid, both work | Shader more flexible but SpriteBatch simpler for basic vignette |
| Scene manager library | Built-in state enum | For simple games | Enum sufficient for single game-over screen |

**Deprecated/outdated:**
- XNA Game State Management sample is outdated but patterns still apply to MonoGame
- Some old tutorials use `Game.Exit()` for restart - use state reset instead

## Open Questions

1. **Health Bar Visual Design**
   - What we know: Need to show health visually (requirement HEALTH-01)
   - What's unclear: CONTEXT mentions "HP bar" but doesn't specify position/size/style
   - Recommendation: Simple horizontal bar, top-left corner, 200x20 pixels (standard placement)

2. **SpriteFont Asset**
   - What we know: Need text for "Press R to restart"
   - What's unclear: No existing SpriteFont in project
   - Recommendation: Create simple .spritefont file in content pipeline, Arial 24pt is standard

## Sources

### Primary (HIGH confidence)
- MonoGame Texture2D API: https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.Texture2D.html
- MonoGame SpriteFont tutorial: https://docs.monogame.net/articles/tutorials/building_2d_games/16_working_with_spritefonts/index.html
- MonoGame Scene Management: https://docs.monogame.net/articles/tutorials/building_2d_games/17_scenes/index.html
- Existing codebase: AmmoSystem.cs, ImpactEffect.cs, Crosshair.cs patterns

### Secondary (MEDIUM confidence)
- MonoGame Community fade tutorial: https://community.monogame.net/t/sharing-an-easy-way-to-fade-in-fade-out-screen/2677

### Tertiary (LOW confidence)
- None required - all findings verified with official docs or codebase patterns

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Uses only existing MonoGame APIs already in project
- Architecture: HIGH - Follows established patterns from Phase 1-3 code
- Pitfalls: HIGH - Common issues documented in official resources and verified

**Research date:** 2026-02-03
**Valid until:** 60 days (stable MonoGame APIs, established patterns)
