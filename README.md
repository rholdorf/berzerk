# Berzerk 3D

A 3D reimagining of the classic 1980 arcade game Berzerk, built with MonoGame. Navigate procedurally generated maze rooms in third-person perspective, battling hostile robots with a laser weapon.

**WARNING**: This is a Work In Progress toy-project. Vibe coding with [Claude Code](https://claude.com/product/claude-code) and [GSD](https://github.com/glittercowboy/get-shit-done). GSD and CC do their thing in the background while I'm doing my things IRL.

## About

This project explorers vibe coding. It uses [MonoGame](https://github.com/MonoGame/MonoGame) just because I'm more comfortable with C# and can evaluate the produced code faster.

Basically, players fight through rooms filled with enemy robots, opening doors to progress deeper into increasingly challenging mazes. That's what I asked GSD/Claude Code to do.

## Features

### Implemented

- Player character movement in 3D space
- Third-person camera with collision detection
- WASD movement controls
- [Mixamo](https://www.mixamo.com/#/) model to MonoGame compatible FBX (hopefully with animations sometime in the future).

## Technology Stack

- **Engine:** MonoGame 3.8 (DesktopGL)
- **Platform:** .NET 8.0
- **Target Platforms:** Windows, Linux, macOS
- **3D Models:** Mixamo (FBX format)
- **Language:** C#

## Requirements

- .NET 8.0 SDK or later
- MonoGame 3.8.x

## Building

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the game
dotnet run --project Berzerk/Berzerk.csproj
```

## Controls

- **W** - Move forward
- **S** - Move backward
- **A** - Rotate left
- **D** - Rotate right
- **Mouse** - Aim (planned)
- **Left Click** - Shoot (planned)
- **Right Click** - Camera orbit

## Project Structure

```text
Berzerk/                    # Main game project
├── Content/                # Game assets (models, textures, etc.)
├── Berzerk.csproj         # Game project file
└── ...

Berzerk.ContentPipeline/   # Custom FBX content processor
├── FbxProcessor.cs        # Mixamo FBX import pipeline
└── ...

.planning/                  # GSD stuff
├── PROJECT.md             # Project overview and requirements
├── ROADMAP.md            # Development roadmap
├── REQUIREMENTS.md       # Detailed requirements
└── phases/               # Individual phase plans
```

## Development Roadmap

The project follows a phased development approach:

1. **Foundation & Content Pipeline** - MonoGame setup with FBX import
2. **Player Movement & Camera** - Controls and camera system
3. **Core Combat System** - Laser weapon and projectiles
4. **Player Health & Survival** - Damage and death mechanics
5. **Enemy AI & Combat** - Robot enemies with combat behavior
6. **Room System & Progression** - Maze rooms with door transitions
7. **UI & HUD** - Player feedback interface
8. **Animation & Visual Polish** - Complete animations and effects

See [.planning/ROADMAP.md](.planning/ROADMAP.md) for detailed phase information.

## Design Philosophy

- **Arcade Intensity:** Fast-paced combat with immediate feedback
- **Procedural Replayability:** Generated mazes for endless variety
- **3D Take:** Third-person perspective on a 2D classic
- **Prototype First:** Solid foundation for future expansion

## Out of Scope (v1)

The following features are deferred to future versions:

- Evil Otto (indestructible enemy)
- Robot collision explosions
- Win condition (currently endless mode)
- Gamepad support
- Multiplayer/co-op
- Save system
- Elaborate menus

## License

This is a personal project prototype.

## Credits

- Original Berzerk (1980) by Stern Electronics
- 3D models and animations from [Mixamo](https://www.mixamo.com/)
- Built with:
  - [.NET](https://dotnet.microsoft.com/)
  - [MonoGame](https://www.monogame.net/)
  - [Claude Code](https://claude.com/product/claude-code)
  - [Get Shit Done](https://github.com/glittercowboy/get-shit-done)
