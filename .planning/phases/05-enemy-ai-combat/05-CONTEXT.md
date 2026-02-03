# Phase 5: Enemy AI & Combat - Context

**Gathered:** 2026-02-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Robot enemies that spawn in the room, detect and pursue the player, attack on melee contact, can be destroyed by laser projectiles, and execute appropriate animations. This phase creates the first real challenge and adversarial dynamics in the game.

</domain>

<decisions>
## Implementation Decisions

### Comportamento de spawn
- Robôs spawnam longe do jogador (distância mínima garantida para dar tempo de reagir)
- Começam com poucos robôs e aumentam progressivamente (primeira sala 2-3, depois mais)
- Não respawnam após destruição (sala limpa = progresso, como Berzerk original)
- Escalonamento de dificuldade: Claude tem discrição sobre como escalar (número, velocidade, tipos)

### Agressividade e movimento
- Robôs se movem mais lentos que jogador (70-80% da velocidade)
- Navegação: Claude decide entre pathfinding básico ou mais simples (balancear simplicidade vs desafio)
- Desistem de perseguir se jogador conseguir fugir para longe (não perseguem infinitamente)
- Raio de detecção: Claude decide baseado no tamanho da sala

### Mecânica de combate corpo-a-corpo
- Dano causado: 10 HP por ataque (consistente com tecla H de teste)
- Alcance de ataque: Claude decide baseado em jogabilidade (entre 1-5 unidades)
- Frequência de ataque: Claude decide baseado em balance (entre 0.5-2s)
- Knockback leve quando jogador é atingido (empurra um pouco para trás, mantém sensação de impacto)

### Destruição e recompensas
- Robôs levam 2-3 hits de laser para serem destruídos (mais satisfatório que 1-hit)
- Dropam munição e health pickups (não score por enquanto)
- Chance média de drop: 30-40% (gerenciamento de recursos importante)
- Efeito visual: Flash e desaparecem (simples, limpo, arcade)

### Claude's Discretion
- Algoritmo exato de pathfinding (linha reta vs navmesh vs A*)
- Raio de detecção preciso (baseado em tamanho da sala)
- Alcance exato de ataque melee
- Frequência exata de ataque
- Escalonamento de dificuldade progressiva
- Quantidade exata de knockback
- Implementação do sistema de health dos robôs
- Animação de morte específica
- Valores exatos de drop chance dentro do range 30-40%

</decisions>

<specifics>
## Specific Ideas

- Como Berzerk original: sala limpa (todos robôs mortos) permite progressão
- Progressão deve ser clara: começa fácil (2-3 robôs), aumenta gradualmente
- Jogador deve poder fugir se necessário (robôs mais lentos + desistem se longe)
- Economia de recursos importa: drops em 30-40% forçam gestão de munição/health
- 2-3 hits para matar robô = sweetspot entre muito fácil (1-hit) e bullet sponge (5+)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope

</deferred>

---

*Phase: 05-enemy-ai-combat*
*Context gathered: 2026-02-03*
