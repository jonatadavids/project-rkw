# ADR-0002: Networking (Multiplayer)

## Status

**Aceito (MVP: Shared Mode)** — Revisão planejada para ranked (Server Mode).

## Contexto

O jogo precisa de multiplayer em tempo real para corridas com até 10 pilotos. O modelo deve suportar salas privadas, matchmaking, reconexão e anti-cheat progressivo. O fundador não opera infraestrutura de game servers.

## Alternativas Consideradas

| Solução | Prós | Contras |
|---|---|---|
| **Photon Fusion 2** | State sync nativo, Shared + Server Mode, SDK Unity maduro, relay no Brasil, matchmaking built-in | Custo por CCU; vendor lock parcial |
| Photon PUN 2 | Simples, barato | Ultrapassado; sem prediction/rollback built-in |
| Mirror (open source) | Grátis, flexível | Requer server hosting manual; sem relay gerenciado |
| Netcode for GameObjects (Unity) | Integrado ao Unity | Menos maduro; sem relay global gerenciado |
| Fish-Net | Performante, open-source | Menor comunidade; hosting manual |

## Decisão

**Photon Fusion 2** em **Shared Mode** (host authority) para o MVP.

Migrar para **Server Mode** (servidor autoritativo) antes de ativar ranked com prêmios.

## Justificativa

1. **Shared Mode:** Zero custo de infraestrutura de servidor dedicado no MVP; suficiente para salas privadas entre amigos.
2. **Server Mode:** Caminho de migração integrado para competitivo sem reescrever netcode.
3. **Relay brasileiro:** Photon tem relay em São Paulo; latência < 50 ms P50.
4. **State sync:** Suporta predição client-side e reconciliação naturalmente.
5. **Matchmaking:** API integrada reduz desenvolvimento custom.
6. **Reconexão e host migration:** Suportados pelo SDK.

## Impacto

- Todo o netcode usa Photon Fusion 2 API (`NetworkObject`, `NetworkRunner`, etc.).
- Tick rate definido em 30 Hz inicialmente (ajustável).
- Bandwidth ~10 KB/s por jogador é aceitável.

## Custo

| Tier Photon | CCU | Preço Estimado |
|---|---|---|
| Free | 20 CCU | $0 |
| Plus | 100 CCU | ~$95/mês |
| Pro | 500 CCU | ~$295/mês |
| Premium | 1000+ CCU | Custom pricing |

> 🧑‍💻 Verificar pricing atualizado antes de commit financeiro.

## Riscos

| Risco | Mitigação |
|---|---|
| Custo escala com CCU | Monitorar; considerar dedicated servers se > 5K CCU |
| Vendor lock-in | Abstraction layer entre game logic e Photon API |
| Shared Mode permite cheating | Aceitável entre amigos no MVP; anti-cheat estatístico; Server Mode para ranked |
| Photon outage | Retry + graceful degradation (offline modes) |

## Plano de Saída

1. Abstraction layer (`INetworkTransport`) encapsula Photon.
2. Mirror + custom server como alternativa open-source.
3. Fish-Net como alternativa leve.
4. Lógica de jogo não depende de Photon-specific annotations em domínio puro.

## Decisões Pendentes

- Q-MP-01: Iniciar com Shared ou já Server Mode? → **Decidido: Shared para MVP.**
- Q-MP-02: Custo exato para 1.000 CCU? → 🧑‍💻 Verificar com Photon sales.

## Referências

- Photon Fusion 2 Documentation
- Photon Pricing Page
- Photon Regions (SA/Brazil)
