# Feature Research

**Domain:** 3D Action/Shooter with Procedural Elements (Berzerk 3D)
**Researched:** 2026-01-31
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Responsive shooting** | Core loop foundation - shooter without good shooting feels broken | MEDIUM | Must feel satisfying in first 5 minutes. Includes hit feedback, visual/audio cues, and responsive controls |
| **Player health/death** | Risk-reward foundation - no stakes = no tension | LOW | Single-hit death (Berzerk original) vs health bar (modern). Modern players expect health bar for 3D games |
| **Basic enemy AI** | Enemies must pursue and attack player | MEDIUM | Can be simple (original Berzerk robots moved slowly), but must be readable and fair |
| **Clear victory condition** | Players need to know when room is cleared | LOW | Visual/audio feedback when all enemies defeated, doors unlock |
| **Movement controls** | Third-person shooters require free 3D movement | MEDIUM | WASD + mouse or dual-stick on controller. Camera must be smooth and responsive |
| **Camera system** | Third-person perspective requires over-the-shoulder view | HIGH | Camera is critical for TPS - needs smooth movement, collision handling, aim assist for controllers |
| **Visual feedback** | Player needs to see impact of actions | MEDIUM | Muzzle flash, hit sparks, enemy damage reactions, score popups |
| **Audio feedback** | Shooters rely heavily on audio cues | MEDIUM | Weapon fire, enemy death, hit sounds, ambient danger cues |
| **Scoring system** | Arcade heritage - players expect points for actions | LOW | Kills, room clear, combo bonuses. Connects to original Berzerk's arcade DNA |
| **Level progression** | Clear room → advance to next room | LOW | Door system that opens when room cleared. Core to Berzerk's room-to-room flow |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Evil Otto mechanic** | Creates escalating tension and prevents camping | MEDIUM | Indestructible time-pressure enemy that speeds up as you clear robots. "Greatest video game villain of all time" - differentiates from other room-clear shooters |
| **Voice synthesis taunts** | Personality and arcade nostalgia | LOW | "Intruder alert!", "Chicken, fight like a robot!" Original Berzerk's 30 voice samples were revolutionary. Modern version could expand this |
| **Procedural maze generation** | Infinite replayability | HIGH | 64,000 mazes in original. Modern version needs hybrid approach: handcrafted room prefabs + procedural connection. Prevents repetitiveness |
| **Environmental hazards** | Additional risk layer beyond enemies | MEDIUM | Electrified walls (original Berzerk), explosive barrels, traps. Adds spatial awareness challenge |
| **Risk-reward room clearing** | Evil Otto speeds up when all robots dead | LOW | Creates tension: clear everything for points vs escape quickly for safety. Core to Berzerk's strategic depth |
| **Chain reaction kills** | Robots exploding when hitting walls/each other | MEDIUM | Original Berzerk feature - creates emergent gameplay. In 3D could be enhanced with physics |
| **Run-based progression** | Meta-progression between runs | MEDIUM | Unlock weapons, abilities, or modifiers. Standard for modern roguelike shooters (Enter the Gungeon, Nuclear Throne) |
| **Arcade-style difficulty curve** | Escalating challenge that becomes "impossible" | MEDIUM | Each room harder than last. Creates "one more try" loop and high score chasing |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Full procedural everything** | "More variety = better" | Creates bland, repetitive experiences. Players can't tell the difference after first hour | Hybrid approach: handcrafted prefab rooms connected procedurally. Quality over quantity |
| **Complex weapon crafting** | Popular in modern games | Feature bloat - diverts from core arcade loop. Adds menu complexity | Simple weapon pickups or unlocks between runs. Keep focus on action, not inventory |
| **Narrative/story mode** | Players expect stories now | Berzerk's appeal is pure gameplay loop. Story would slow pacing and require cutscenes/dialogue | Environmental storytelling only. Voice taunts provide personality without breaking flow |
| **Multiplayer** | "Everything needs multiplayer" | Massive scope increase. Netcode complexity. Balancing nightmare for procedural content | Start single-player. Add leaderboards for competition. Multiplayer is v2+ feature |
| **Open world / hub** | Modern game expectation | Destroys arcade pacing. Original's instant-action appeal gets lost in navigation | Menu-based run start. Jump straight into action like original arcade game |
| **Cover mechanics** | Standard for modern TPS | Slows pace, encourages camping - opposite of Evil Otto's design intent | Fast movement and dodging. Keep arcade speed, punish camping with Evil Otto |
| **Realistic graphics** | "It's 3D so it should look real" | Expensive, misses arcade aesthetic. Berzerk was abstract geometric | Stylized geometric aesthetic honoring original's vector-like look. Clarity over realism |

## Feature Dependencies

```
[Responsive Shooting]
    └──requires──> [Visual Feedback]
    └──requires──> [Audio Feedback]
    └──requires──> [Camera System]

[Level Progression]
    └──requires──> [Clear Victory Condition]
    └──requires──> [Procedural Maze Generation]

[Evil Otto Mechanic]
    └──requires──> [Basic Enemy AI]
    └──enhances──> [Risk-Reward Room Clearing]

[Chain Reaction Kills]
    └──requires──> [Environmental Hazards]
    └──enhances──> [Emergent Gameplay]

[Run-based Progression]
    └──requires──> [Scoring System]
    └──conflicts──> [Session-based Arcade Purity]

[Camera System]
    └──required-by──> [Responsive Shooting]
    └──required-by──> [Movement Controls]
```

### Dependency Notes

- **Responsive Shooting is foundational:** Nothing else matters if shooting doesn't feel good within 5 minutes
- **Camera System is critical path:** TPS camera is complex and gates shooting feel, movement, and aiming
- **Evil Otto requires AI foundation:** But adds unique time-pressure layer that defines Berzerk
- **Procedural generation needs quality control:** Hybrid approach (prefabs + connection) prevents bland mazes
- **Run-based progression conflicts with arcade purity:** Decision needed - pure arcade or modern roguelike?

## MVP Definition

### Launch With (v1)

Minimum viable product - what's needed to validate "Berzerk 3D" concept.

- [ ] **Third-person camera and movement** - Foundation for 3D translation. Most complex technical piece
- [ ] **Laser weapon shooting** - Core loop mechanic. Must feel satisfying immediately
- [ ] **Basic robot enemies** - Simple pursuit AI, destructible, trigger room clear
- [ ] **Single handcrafted room** - Prove the core loop before procedural complexity
- [ ] **Room clear victory** - Defeat all robots → doors open
- [ ] **Evil Otto** - The defining Berzerk mechanic. Indestructible, creates time pressure
- [ ] **Health bar** - Modern TPS expectation (not single-hit like original)
- [ ] **Electrified walls** - Environmental hazard from original
- [ ] **Scoring system** - Arcade DNA, creates "one more try" motivation
- [ ] **Voice taunts** - Key differentiator, personality, nostalgia factor

**Rationale:** This is the smallest set that proves "Berzerk in 3D works and feels good." No procedural generation yet - validate core loop first with one perfect room.

### Add After Validation (v1.x)

Features to add once core is proven fun.

- [ ] **5-10 handcrafted rooms** - Expand variety before going procedural (trigger: core loop validated)
- [ ] **Procedural room connection** - Connect rooms randomly for replayability (trigger: room variety sufficient)
- [ ] **Multiple weapon types** - Laser variants with different patterns (trigger: core shooting feels great)
- [ ] **Difficulty progression** - Rooms get harder as you advance (trigger: base difficulty balanced)
- [ ] **Chain reaction explosions** - Robots damage each other/walls (trigger: base collision working)
- [ ] **Visual/audio polish** - Particle effects, impact sounds, screen shake (trigger: mechanics locked)
- [ ] **High score table** - Leaderboard integration (trigger: scoring balanced)

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Run-based unlocks** - Permanent progression between runs (why defer: changes arcade purity - test pure version first)
- [ ] **Additional room prefabs** - Expand to 20-50 rooms for variety (why defer: expensive, need data on what works)
- [ ] **Boss rooms** - Special challenge rooms (why defer: adds complexity, may break pacing)
- [ ] **Multiple player characters** - Different abilities/playstyles (why defer: balancing burden, focus on core first)
- [ ] **Multiplayer co-op** - 2-player support (why defer: massive scope, netcode complexity)
- [ ] **Procedural room generation** - Full procedural rooms vs prefabs (why defer: very high risk of bland content)

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Responsive shooting | HIGH | MEDIUM | P1 |
| Camera system | HIGH | HIGH | P1 |
| Movement controls | HIGH | MEDIUM | P1 |
| Evil Otto mechanic | HIGH | MEDIUM | P1 |
| Basic enemy AI | HIGH | MEDIUM | P1 |
| Health/damage system | HIGH | LOW | P1 |
| Room clear victory | HIGH | LOW | P1 |
| Voice taunts | MEDIUM | LOW | P1 |
| Electrified walls | MEDIUM | LOW | P1 |
| Scoring system | MEDIUM | LOW | P1 |
| Procedural connection | HIGH | HIGH | P2 |
| Multiple weapons | MEDIUM | MEDIUM | P2 |
| Chain reactions | MEDIUM | MEDIUM | P2 |
| Difficulty curve | HIGH | LOW | P2 |
| Visual polish | MEDIUM | MEDIUM | P2 |
| Run-based progression | MEDIUM | HIGH | P3 |
| Multiplayer | MEDIUM | HIGH | P3 |
| Boss rooms | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for MVP - validates core concept
- P2: Should have after validation - adds replayability
- P3: Nice to have - post-PMF expansion

## What Made Original Berzerk Compelling

Research findings on the 1980 arcade classic's appeal:

### Revolutionary for Its Time
- **Speech synthesis:** 30 digitized words ("Intruder alert!", "Chicken, fight like a robot!") - unprecedented in 1980
- **Massive variety:** 64,000 procedurally generated mazes
- **Never got old:** Designer commented kids would get bored with other games in a month but "always came back to Berzerk"

### Core Gameplay Loop
- **Simple but intense:** Navigate maze → shoot robots → escape before Evil Otto arrives
- **Multiple threat vectors:** Robot lasers, electrified walls, robot collisions, Evil Otto
- **Escalating difficulty:** "Quickly rose to the point of being impossible" - arcade coin-eating design
- **No camping:** Evil Otto forced constant movement and risk-taking

### The Evil Otto Factor
- **Indestructible threat:** No way to kill him - revolutionary for the era
- **Speed mechanics:** Moved slowly if robots remained, matched player speed when all robots killed
- **Created tension:** "Greatest video game villain of all time" according to retrospectives
- **Time pressure:** Function was "to quicken the pace of the game"

### Risk-Reward Dynamics
- **Clearing all robots = bonus points BUT faster Evil Otto**
- **Strategic depth:** Clear everything vs escape quickly
- **Chain reactions:** Robots exploding when hitting walls/each other created emergent moments

### Key Insight for 3D Version
The original's brilliance was **extreme simplicity creating intense decisions under pressure**. Don't add complexity - translate the pressure to 3D. Evil Otto is non-negotiable.

## Competitor Feature Analysis

| Feature | Enter the Gungeon | Nuclear Throne | Berzerk 3D Approach |
|---------|-------------------|----------------|----------------------|
| Procedural levels | Handcrafted rooms + procedural connection | Full procedural | Start handcrafted, add hybrid connection |
| Room clear | Kill all enemies → advance | Kill all enemies → warp out | Kill all enemies → doors open (original Berzerk) |
| Time pressure | None - can camp | None - can camp | Evil Otto (unique differentiator) |
| Pacing | Methodical, pattern-learning | Fast, arcadey twitch | Fast arcade with forced urgency (Otto) |
| Meta-progression | Unlock weapons/items | Unlock characters/weapons | TBD - test pure arcade first |
| Perspective | Top-down 2D | Top-down 2D | Third-person 3D (key differentiator) |
| Difficulty | Pattern memorization | Twitch shooting + RNG | Escalating room difficulty + Otto pressure |

**Our positioning:** Fast arcade action (like Nuclear Throne) + time pressure (unique Evil Otto) + 3D perspective (differentiated) - methodical pattern learning (Enter the Gungeon).

## Sources

### 3D Shooter Mechanics
- [Due Process on Steam](https://store.steampowered.com/app/753650/Due_Process/) - Procedural tactical FPS
- [The Creation of Procedural Shooter Sublevel Zero](https://80.lv/articles/the-creation-of-procedural-shooter-sublevel-zero) - Hybrid procedural systems
- [Third-person shooter - Wikipedia](https://en.wikipedia.org/wiki/Third-person_shooter) - TPS mechanics and camera systems
- [Understanding the Mechanics of 3rd Person Shooter Games - Pressversity](https://pressversity.com/blogs/games/2025/06/understanding-the-mechanics-of-3rd-person-shooter-games/) - Camera and aiming essentials
- [Key Gaming Trends of 2026 - AimControllers](https://eu.aimcontrollers.com/blog/key-gaming-trends-of-2026/) - Cross-platform expectations

### Original Berzerk Research
- [Berzerk (video game) - Wikipedia](https://en.wikipedia.org/wiki/Berzerk_(video_game)) - Core mechanics and history
- [Berzerk – Hardcore Gaming 101](http://www.hardcoregaming101.net/berzerk/) - Design analysis
- [Berzerk Arcade - Iconic Maze Shooter (1980)](https://bitvint.com/pages/berzerk) - What made it compelling
- [Berzerk (Video Game) - TV Tropes](https://tvtropes.org/pmwiki/pmwiki.php/VideoGame/Berzerk) - Evil Otto mechanics

### Procedural Generation Best Practices
- [How I Created a Roguelike Map With Procedural Generation | Medium](https://tuliomarks.medium.com/how-i-created-roguelike-map-with-procedural-generation-630043f9a93f) - Hybrid approaches
- [Going Rogue-like: When to use Procedurally Generated Environments in Games](https://www.gamedeveloper.com/design/going-rogue-like-when-to-use-procedurally-generated-environments-in-games) - When procedural works
- [How to effectively use procedural generation in games](https://www.gamedeveloper.com/design/how-to-effectively-use-procedural-generation-in-games) - Prefabs and balance

### Core Loop Design
- [What Is a Gameplay Loop? Types of Core Loops Explained](https://vsquad.art/blog/what-gameplay-loop-types-core-loops-explained) - Shooter core loops
- [Designing an Engaging Gameplay Loop: The Ultimate Guide - Game Dev Essentials](https://gamedevessentials.com/designing-an-engaging-gameplay-loop-the-ultimate-guide/) - Action, feedback, reward
- [Arcade Game Design - Game-Ace](https://game-ace.com/blog/arcade-game-design/) - Fast feedback and replay value

### Procedural Shooter Comparisons
- [Enter the Gungeon - Wikipedia](https://en.wikipedia.org/wiki/Enter_the_Gungeon) - Handcrafted rooms + procedural connection
- [Video game sanctuary- Nuclear Throne & Enter the Gungeon](https://vg-sanctuary.tumblr.com/post/665421218025979904/nuclear-throne-enter-the-gungeon) - Pacing differences

### Anti-Features and Scope Management
- [The Over-scoping Game Designer - The Attack of the Feature Creep](https://www.gamingdebugged.com/2013/01/12/the-over-scoping-game-designer-the-attack-of-the-feature-creep/) - Feature creep dangers
- [Scope Creep in Indie Games: How to Avoid Development Hell - Wayline](https://www.wayline.io/blog/scope-creep-indie-games-avoiding-development-hell) - Focus on core loop
- [How to avoid feature creep in indie game development](https://wardrome.com/how-to-avoid-feature-creep-in-indie-game-development/) - KISS principle

---
*Feature research for: Berzerk 3D - modern 3D version of 1980 arcade classic*
*Researched: 2026-01-31*
