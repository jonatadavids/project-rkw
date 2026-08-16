# 03 — User Flows

## Objetivo e Escopo

Documentar as jornadas principais do jogador desde o primeiro acesso até loops de retenção, com diagramas Mermaid.

---

## Flow 1 — Primeiro Acesso (FTUE)

```mermaid
graph TD
    A[Download + Open] --> B[Splash + Loading]
    B --> C[Login/Guest]
    C --> D[Criar Nome de Piloto]
    D --> E[Briefing Visual - O que é kart rental]
    E --> F[Escola Módulo 1 - Equipamentos]
    F --> G[Escola Módulo 2 - Acelerar e frear]
    G --> H{Continuar Escola?}
    H -- Sim --> I[Módulo 3+]
    H -- Não --> J[Menu Principal]
    J --> K[Treino Livre / Tomada de Tempo]
```

---

## Flow 2 — Corrida Rápida Online

```mermaid
graph TD
    A[Menu Principal] --> B[Partida Rápida]
    B --> C[Matchmaking por categoria + skill + ping]
    C --> D{Sala encontrada?}
    D -- Sim --> E[Lobby com countdown]
    D -- Não em 60s --> F[Criar sala + preencher com bots]
    F --> E
    E --> G[Tomada de Tempo - 3 voltas]
    G --> H[Grid definido]
    H --> I[Corrida - 10 voltas]
    I --> J[Resultado]
    J --> K{Revanche?}
    K -- Sim --> E
    K -- Não --> L[Menu Principal]
```

---

## Flow 3 — Sala Privada

```mermaid
graph TD
    A[Menu Principal] --> B[Criar Sala Privada]
    B --> C[Gerar código 6 chars]
    C --> D[Compartilhar código]
    D --> E[Aguardar jogadores - máx 10]
    E --> F[Host inicia quando pronto]
    F --> G[Tomada de Tempo]
    G --> H[Corrida]
    H --> I[Resultado]
```

---

## Flow 4 — Escola de Pilotagem

```mermaid
graph TD
    A[Menu > Escola] --> B[Lista de Módulos]
    B --> C[Selecionar módulo desbloqueado]
    C --> D[Briefing do módulo]
    D --> E[Exercício prático]
    E --> F{Aprovado?}
    F -- Sim --> G[Desbloquear próximo + XP]
    F -- Não --> H[Feedback específico]
    H --> E
    G --> I{Módulo 10?}
    I -- Sim --> J[Prova de Licença]
    I -- Não --> B
    J --> K{Aprovado?}
    K -- Sim --> L[Licença concedida! 🏆]
    K -- Não --> M[Feedback + sugestão de revisão]
```

---

## Flow 5 — Garagem e Cosméticos

```mermaid
graph TD
    A[Menu > Garagem] --> B[Visualizar Kart atual]
    B --> C[Selecionar slot cosmético]
    C --> D{Tem item?}
    D -- Sim --> E[Aplicar]
    D -- Não --> F[Loja de cosméticos]
    F --> G[Comprar / Usar moeda grátis]
    G --> E
```

---

## Flow 6 — Desconexão e Reconexão

```mermaid
graph TD
    A[Piloto perde conexão] --> B[Bot assume posição]
    B --> C{Reconexão em ≤30s?}
    C -- Sim --> D[Retomar controle na posição do bot]
    C -- Não --> E[Resultado parcial registrado]
    E --> F[Penalidade de abandono se recorrente]
```

---

## Flow 7 — Compra e Restauração

```mermaid
graph TD
    A[Loja] --> B[Selecionar item]
    B --> C[Confirmar compra via IAP nativo]
    C --> D{Sucesso?}
    D -- Sim --> E[Item desbloqueado + receipt validado no backend]
    D -- Não --> F[Exibir erro + retry]
    F --> C
    G[Restaurar Compras] --> H[Consultar store receipts]
    H --> I[Validar no backend]
    I --> J[Restaurar itens]
```

---

## Decisões Confirmadas

1. FTUE direciona para Escola antes do multiplayer.
2. Matchmaking timeout de 60 s antes de preencher com bots.
3. Reconexão permitida em até 30 s.

## Suposições

| ID | Suposição | Validação |
|---|---|---|
| UF-01 | FTUE curta (< 3 min até primeira pilotagem) retém melhor | Teste A/B no soft launch |
| UF-02 | Código de 6 chars é suficiente para salas privadas sem colisão | Cálculo combinatório + monitoramento |

## Questões Abertas

- Q-UF-01: Permitir skip da Escola para pilotos que já jogaram?
- Q-UF-02: Deep link para sala privada via WhatsApp/Telegram?

## Links Relacionados

- [GDD](./02-game-design-document.md)
- [Multiplayer](./08-multiplayer-architecture.md)
- [Progressão](./10-progression-economy.md)
