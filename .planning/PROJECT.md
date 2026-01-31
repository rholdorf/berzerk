# Berzerk 3D

## What This Is

Uma versão 3D do clássico arcade Berzerk, construída com MonoGame. O jogador navega por salas com labirintos gerados proceduralmente, combatendo robôs inimigos com uma arma laser em perspectiva de terceira pessoa. Este é um protótipo que serve como base para um jogo maior.

## Core Value

Combate arcade intenso em salas geradas proceduralmente - a essência do Berzerk original transportada para 3D moderno com modelos animados.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Jogador pode mover personagem 3D com WASD
- [ ] Jogador pode mirar com mouse e atirar com arma laser (projéteis visíveis)
- [ ] Câmera terceira pessoa segue o jogador
- [ ] Robôs detectam proximidade do jogador e perseguem
- [ ] Robôs atacam corpo a corpo quando alcançam o jogador
- [ ] Robôs podem ser destruídos pelos projéteis laser
- [ ] Jogador tem barra de vida que diminui ao ser atacado
- [ ] Jogador morre quando vida chega a zero
- [ ] Salas são labirintos 3D gerados proceduralmente
- [ ] Portas se abrem quando todos robôs da sala são eliminados
- [ ] Jogador pode atravessar portas para próxima sala
- [ ] Sistema de munição limitada para arma laser
- [ ] Pickups de munição aparecem nas salas
- [ ] Sistema de pontuação baseado em robôs destruídos
- [ ] HUD mostra: barra de vida, munição atual, pontuação, mira/crosshair
- [ ] Modelos 3D animados do Mixamo para jogador e robôs
- [ ] Salas ficam progressivamente mais difíceis (mais robôs, layouts mais complexos)

### Out of Scope

- Evil Otto (inimigo indestrutível) — deferido para v2, foca no loop básico primeiro
- Robôs explodirem ao colidir com paredes/outros robôs — mecânica extra, não essencial para v1
- Win condition específica — modo infinito por enquanto, adicionar "vitória" depois
- Suporte a gamepad — apenas teclado+mouse na v1, expande controles depois
- Multiplayer ou co-op — jogo single-player focado
- Salvar progresso/save system — sessões são self-contained por enquanto
- Menus elaborados — menu mínimo para começar jogo, expande depois

## Context

**Inspiração:** Berzerk (1980) - arcade clássico onde jogador navega salas cheias de robôs hostis. A versão original era 2D top-down; esta versão moderniza para 3D com perspectiva terceira pessoa.

**Technical approach:** MonoGame para cross-platform 3D, usando pipeline de modelos FBX do Mixamo para animações prontas de qualidade. Geração procedural de labirintos garante replayability.

**Target audience:** Desenvolvedor quer criar base sólida para expandir - este é o foundation para algo maior.

## Constraints

- **Tech stack**: MonoGame (versão mais recente) — engine escolhida pelo desenvolvedor
- **Platform**: Cross-platform (Windows, Linux, macOS) — MonoGame suporta nativamente
- **Assets**: Modelos e animações do Mixamo.com — fonte de assets 3D gratuitos e de qualidade
- **Input**: Teclado + Mouse apenas na v1 — simplifica controles iniciais

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Terceira pessoa vs primeira pessoa | Permite ver modelo do personagem e contexto espacial melhor para navegação em labirintos | — Pending |
| Projéteis visíveis vs raycast instantâneo | Mais fiel ao estilo arcade, adiciona skill de prediction | — Pending |
| Geração procedural desde v1 | Replayability alta desde início, core da experiência | — Pending |
| Mixamo para modelos | Assets prontos de qualidade, acelera desenvolvimento | — Pending |
| Sistema de munição limitada | Adiciona resource management, evita spam infinito | — Pending |

---
*Last updated: 2026-01-31 after initialization*
