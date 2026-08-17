# Kart Rental Game

> **Aprenda a pilotar, conquiste sua licença e dispute campeonatos online de kart rental.**

Jogo mobile multiplayer de kart rental com dirigibilidade autêntica (simcade), escola de pilotagem progressiva, progressão esportiva e monetização ética. Android + iOS, Unity 6.3 LTS (`6000.3.22f1`).

---

## Status

🧱 **M1 em andamento** — Fundação Unity, Photon, UGS de desenvolvimento e fluxo Bootstrap → MainMenu criados; M1-T01 a M1-T06 concluídas e Etapa A local da M1-T07 validada parcialmente. Nenhum gameplay.

---

## Estrutura do Repositório

```
Assets/                     ← Projeto Unity URP e assemblies atuais
Packages/                   ← Manifest e lock do Unity Package Manager
ProjectSettings/            ← Configuração versionável do Unity
docs/
  00-index.md              ← Ponto de entrada da documentação
  01-product-vision.md
  02-game-design-document.md
  03-user-flows.md
  04-driving-physics.md
  05-controls-accessibility.md
  06-race-rules-flags.md
  07-ai-bots.md
  08-multiplayer-architecture.md
  09-backend-data-model.md
  10-progression-economy.md
  11-monetization-liveops.md
  12-art-audio-performance.md
  13-analytics-telemetry.md
  14-security-privacy-compliance.md
  15-android-ios-release.md
  16-test-strategy.md
  17-roadmap.md
  18-product-backlog.md     ← Histórias para o Codex
  19-risk-register.md
  20-open-questions.md
  21-unity-foundation.md     ← Evidências da fundação Unity
  22-photon-foundation.md    ← Fundação Photon de desenvolvimento
  23-ugs-foundation.md       ← Authentication e Cloud Save de desenvolvimento
  24-bootstrap-main-menu.md  ← Bootstrap e menu principal placeholder
  25-local-mobile-build-validation.md ← Evidências locais parciais da M1-T07
  adr/
    0001-engine.md
    0002-networking.md
    0003-backend.md
    0004-rendering.md
    0005-build-pipeline.md
    0006-source-of-sporting-rules.md
AGENTS.md                   ← Regras para agentes de IA
KIRO_MASTER_PROMPT.md       ← Prompt original de geração
README.md                   ← Este arquivo
```

---

## Tecnologia

| Camada | Tecnologia |
|---|---|
| Engine | Unity 6.3 LTS (`6000.3.22f1`) |
| Linguagem | C# |
| Render Pipeline | URP |
| Multiplayer | Photon Fusion 2 |
| Backend | Unity Gaming Services |
| IAP | Unity IAP |
| Ads | AdMob |
| CI/CD | Unity Build Automation |
| Versionamento | GitHub + Git LFS |
| Distribuição | TestFlight + Google Play Internal |

---

## Como Começar

1. **Revisar a documentação**: Comece por `docs/00-index.md`.
2. **Resolver questões bloqueantes**: Ver `docs/20-open-questions.md` (itens 🔴).
3. **Iniciar implementação**: Seguir ordem do backlog (`docs/18-product-backlog.md`).
4. **Regras de desenvolvimento**: Ler `AGENTS.md` antes de qualquer código.

---

## Prompt de Handoff para o Codex

> Leia `README.md`, `AGENTS.md`, `docs/00-index.md`, todos os ADRs e a primeira história Ready do backlog. Antes de editar, produza um plano curto, identifique riscos e confirme os testes que provarão os critérios de aceite. Implemente somente essa história em uma branch própria. Execute os testes aplicáveis, revise o diff e atualize a documentação afetada. Não prossiga para a próxima história sem minha aprovação.

---

## Regra Essencial

O Codex pode produzir a maior parte do código e automação, mas **o fundador precisa validar**: dirigibilidade, experiência de jogo, imagens, licenças, contas de loja, pagamentos, testes em aparelhos e decisões comerciais. **Nunca publicar automaticamente em produção sem revisão humana.**
