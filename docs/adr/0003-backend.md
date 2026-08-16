# ADR-0003: Backend Services

## Status

**Aceito**

## Contexto

O jogo precisa de autenticação, persistência de dados, economia, leaderboards, configuração remota e validação server-side. O fundador não opera infraestrutura de backend própria e quer custo operacional mínimo.

## Alternativas Consideradas

| Solução | Prós | Contras |
|---|---|---|
| **Unity Gaming Services (UGS)** | SDKs nativos Unity, Auth + Cloud Save + Economy + Leaderboards + Remote Config + Cloud Code integrados | Vendor lock; pricing pode escalar; Cloud Code é JS/C# limitado |
| Firebase + Cloud Functions | Maduro, flexível, free tier generoso | Não integrado nativamente com Unity; mais glue code |
| PlayFab | Game-specific, econômico | Microsoft-owned; menos integração Unity nativa |
| Custom (AWS/GCP + API) | Controle total | Requer expertise; overhead de manutenção; custo dev |
| Supabase | Open-source, Postgres, real-time | Não game-specific; sem economy/leaderboards built-in |

## Decisão

**Unity Gaming Services (UGS)** como plataforma primária de backend.

## Justificativa

1. **Integração nativa:** SDKs Unity sem adapter; reduce friction.
2. **All-in-one:** Auth, Cloud Save, Economy, Leaderboards, Remote Config, Cloud Code em um dashboard.
3. **Zero infra:** Sem servidor para gerenciar; modelo serverless.
4. **Cloud Code:** Validação server-side (resultados, economia) sem server custom.
5. **Custo MVP:** Free tier cobre até escala moderada.
6. **Alinhamento:** Usando Unity como engine; ecossistema unificado.

## Impacto

- Autenticação via UGS Auth (Google, Apple, anonymous).
- Dados de jogador em Cloud Save (JSON, até 5 MB/player).
- Economia (moedas, inventário) gerenciada por UGS Economy.
- Rankings via UGS Leaderboards.
- Feature flags e LiveOps via Remote Config.
- Validação de resultados via Cloud Code.

## Custo

| Serviço | Free Tier | Acima do Free |
|---|---|---|
| Authentication | 50 MAU | $3/1000 MAU |
| Cloud Save | 1 GB storage | $0.03/GB |
| Economy | 50K transactions/mês | Verificar pricing |
| Leaderboards | 10M scores | Verificar |
| Remote Config | 10K configs | Verificar |
| Cloud Code | 1M invocations | $1/1M |

> ⚠️ Verificar pricing atualizado do UGS antes de soft launch.

## Riscos

| Risco | Mitigação |
|---|---|
| UGS descontinuado por Unity | Plano de saída para Firebase; dados exportáveis |
| Limites de Cloud Save (5 MB/player) | Monitorar; comprimir JSON; escalar se necessário |
| Cloud Code limitado para lógica complexa | Manter lógica simples; custom server se necessário pós-MVP |
| Latência de Cloud Code | Benchmark P95; otimizar ou cache |

## Plano de Saída

1. Exportar dados de Cloud Save via API.
2. Migrar auth para Firebase Auth (suporta mesmos providers).
3. Economia: reimplementar em Firebase + Firestore.
4. Leaderboards: Redis ou Firestore.
5. Remote Config: Firebase Remote Config (equivalente direto).
6. Migração estimada: 2–4 semanas de agente.

## Referências

- UGS Documentation
- UGS Pricing
- Unity Cloud Code Reference
