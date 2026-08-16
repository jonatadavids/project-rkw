# Índice da Documentação — Kart Rental Game

## Visão Geral

Este repositório contém a documentação de engenharia e produto e a fundação Unity do jogo mobile de kart rental. Gameplay ainda não foi iniciado.

---

## Estrutura de Documentos

| # | Documento | Descrição |
|---|---|---|
| 00 | [Índice](./00-index.md) | Este documento |
| 01 | [Visão do Produto](./01-product-vision.md) | Proposta de valor, público-alvo, posicionamento |
| 02 | [Game Design Document](./02-game-design-document.md) | Mecânicas, modos, progressão e loops |
| 03 | [User Flows](./03-user-flows.md) | Jornadas do jogador e diagramas de fluxo |
| 04 | [Física de Pilotagem](./04-driving-physics.md) | Modelo simcade, parâmetros e calibração |
| 05 | [Controles e Acessibilidade](./05-controls-accessibility.md) | Inputs, layouts, assistências e inclusão |
| 06 | [Regras, Bandeiras e Fiscais](./06-race-rules-flags.md) | Regulamento esportivo e penalidades |
| 07 | [Bots (IA)](./07-ai-bots.md) | Perfis, comportamento e dificuldade |
| 08 | [Arquitetura Multiplayer](./08-multiplayer-architecture.md) | Networking, autoridade, anti-cheat |
| 09 | [Backend e Data Model](./09-backend-data-model.md) | Serviços, esquemas e integrações |
| 10 | [Progressão e Economia](./10-progression-economy.md) | Licenças, XP, moedas e equilíbrio |
| 11 | [Monetização e LiveOps](./11-monetization-liveops.md) | Receita, passes, ads e ética |
| 12 | [Arte, Áudio e Performance](./12-art-audio-performance.md) | Orçamentos, LODs, som e háptica |
| 13 | [Analytics e Telemetria](./13-analytics-telemetry.md) | Eventos, métricas e privacidade |
| 14 | [Segurança, Privacidade e Compliance](./14-security-privacy-compliance.md) | LGPD, GDPR, anti-cheat, políticas |
| 15 | [Android/iOS Release](./15-android-ios-release.md) | Build, CI/CD, lojas e publicação |
| 16 | [Estratégia de Testes](./16-test-strategy.md) | Unitários, integração, performance, humanos |
| 17 | [Roadmap](./17-roadmap.md) | Milestones com estimativas e dependências |
| 18 | [Product Backlog](./18-product-backlog.md) | Épicos e histórias priorizadas |
| 19 | [Registro de Riscos](./19-risk-register.md) | Riscos, probabilidade, impacto e mitigação |
| 20 | [Questões Abertas](./20-open-questions.md) | Perguntas que exigem decisão humana |
| 21 | [Fundação Unity](./21-unity-foundation.md) | Configuração e decisões de M1-T01 a M1-T04 |
| 22 | [Fundação Photon](./22-photon-foundation.md) | SDK e conexão mínima de desenvolvimento de M1-T04 |

---

## ADRs (Architecture Decision Records)

| # | ADR | Tema |
|---|---|---|
| 0001 | [Engine](./adr/0001-engine.md) | Escolha de Unity 6.3 LTS (`6000.3.22f1`) |
| 0002 | [Networking](./adr/0002-networking.md) | Photon Fusion 2 |
| 0003 | [Backend](./adr/0003-backend.md) | Unity Gaming Services |
| 0004 | [Rendering](./adr/0004-rendering.md) | URP e pipeline gráfico |
| 0005 | [Build Pipeline](./adr/0005-build-pipeline.md) | Unity Build Automation |
| 0006 | [Regras Esportivas](./adr/0006-source-of-sporting-rules.md) | Fonte de verdade regulamentar |

---

## Spikes M0 (consultas em 2026-08-16)

| Tarefa | Documento |
|---|---|
| M0-T01 | [Limites e custos do Cloud Save](./spikes/cloud-save-limits.md) |
| M0-T02 | [Estimativa de custos Photon](./spikes/photon-cost-estimate.md) |
| M0-T03 | [Protocolo de latência por região Photon](./spikes/photon-region-latency-protocol.md) |
| M0-T04 | [Procedimento de exclusão de dados](./spikes/data-deletion-procedure.md) |
| M0-T05 | [Matriz de dispositivos](./spikes/device-matrix.md) |
| M0-T06 | [Dimensionamento de amostra do ghost](./spikes/ghost-sample-sizing.md) |
| M0-T07 | [Requisitos de áudio Unity](./spikes/unity-audio-requirements.md) |
| M0-T08 | [Checklist jurídico e de políticas](./spikes/legal-checklist.md) |

---

## Documentos Transversais

| Documento | Localização |
|---|---|
| [AGENTS.md](../AGENTS.md) | Regras duráveis para agentes de IA (Codex) |
| [README.md](../README.md) | Ponto de entrada do repositório |

---

## Convenções

- **Linguagem:** Português do Brasil. Termos técnicos mantidos em inglês quando padrão da indústria.
- **Valores numéricos de física:** Marcados como "hipótese de calibração" até validação com telemetria.
- **Itens TBD:** Identificados explicitamente com plano de resolução.
- **Ações humanas obrigatórias:** Marcadas com 🧑‍💻.
- **Diagramas:** Mermaid quando melhoram clareza de arquitetura ou fluxo.
- **Rastreabilidade:** Visão → Requisitos → Backlog → Testes → Telemetria.
