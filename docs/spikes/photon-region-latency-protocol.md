# Spike M0-T03 — Regiões Photon e protocolo de latência para o Brasil

## Registro da consulta

- Data: **2026-08-16**
- Escopo: pesquisa e protocolo. Nenhuma medição real foi executada em M0.
- Fonte: documentação oficial Photon Fusion/Realtime.

## Regiões candidatas confirmadas

| Prioridade | Região | Código | Datacenter | Motivo |
|---:|---|---|---|---|
| 1 | South America | `sa` | São Paulo | Candidata natural para jogadores no Brasil |
| 2 | USA, South Central | `ussc` | Dallas | Fallback geograficamente razoável |
| 3 | USA, East | `us` | Washington, D.C. | Segundo fallback e comparação |

A região `sa` **existe** e atende Fusion. Não há base oficial para prometer latência específica por cidade; os valores só serão conhecidos por medição real.

Photon recomenda Best Region por menor ping. Em Fusion 2, `NetworkRunner.GetAvailableRegions()` retorna regiões com ping. Fixar uma região no build evita o teste, mas exige atualização para mudar; portanto o MVP deve medir e selecionar em runtime, com allowlist e fallback.

## Protocolo para execução em M1

### Locais e redes

Executar em pelo menos:

1. Brasília/DF;
2. São Paulo/SP;
3. Recife/PE (ou outra capital do Nordeste, documentando substituição).

Em cada cidade, testar Wi‑Fi residencial e 4G/5G quando possível. Registrar operadora, tipo de acesso, data/hora, modelo do aparelho, versão do SO e versão do SDK Photon.

### Procedimento

1. Obter a lista dinâmica com `NetworkRunner.GetAvailableRegions()`; não hardcodear endpoints/IPs.
2. Aplicar allowlist inicial `sa`, `ussc`, `us`.
3. Fazer 5 warm-ups descartados.
4. Fazer **30 rodadas independentes** por rede. Em cada rodada, reinicializar a descoberta de região e registrar o ping informado para cada candidata.
5. Para a melhor região, conectar e manter uma sessão vazia por 60 segundos, enviando heartbeat de aplicação a 10 Hz para medir RTT, jitter e perda.
6. Repetir em três janelas: manhã, noite e fim de semana, se operacionalmente possível.
7. Salvar resultados em CSV/JSON com timestamp UTC e produzir `docs/spikes/photon-region-latency.md` em M1.

### Métricas

| Métrica | Cálculo/registro |
|---|---|
| RTT P50/P95/P99 | percentis das amostras válidas |
| Jitter | média de `abs(RTT[n] - RTT[n-1])` e desvio padrão |
| Packet loss | sequências sem resposta / sequências enviadas |
| Falhas de descoberta/conexão | contagem e código de erro |
| Região escolhida por Best Region | frequência por rodada |
| Tempo até conexão | monotonic clock, início → runner conectado |

### Critérios de recomendação

- Recomendar `sa` se tiver menor P50 na maioria dos locais, P95 aceitável e disponibilidade consistente.
- Usar como alvo inicial: P50 ≤ 100 ms, P95 ≤ 200 ms e perda < 2%, alinhado aos gates posteriores de `tasks.md`.
- Se duas regiões ficarem dentro de 10 ms e sem diferença estatisticamente útil, priorizar estabilidade/P95.
- Se `sa` falhar, deixar Best Region escolher entre `ussc` e `us`; não assumir fallback fixo sem dados.
- Amigos devem entrar na mesma região; regiões Photon são isoladas para matchmaking/salas.

## Estimativa não factual

Espera-se que `sa` vença em Brasília e São Paulo pela proximidade, mas isso é apenas hipótese. Roteamento de operadora pode alterar o resultado; não foi usado relato de comunidade como evidência.

## Decisão preparada

**Protocolo aprovado na revisão humana de 2026-08-16. Candidata padrão:** `sa` (São Paulo), sujeita ao benchmark M1. **Estratégia:** Best Region com allowlist `sa`, `ussc`, `us` e opção futura de seleção manual. A questão de região não bloqueia a fundação técnica do M1; a região definitiva depende da medição.

## Fontes oficiais

- [Photon Cloud Regions](https://doc.photonengine.com/realtime/current/connection-and-authentication/regions)
- [Fusion 2 — Photon Cloud Regions](https://doc.photonengine.com/fusion/v2/manual/connection-and-matchmaking/regions)
- [Photon Realtime FAQ — Best Region](https://doc.photonengine.com/realtime/current/troubleshooting/faq)
