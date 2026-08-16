# 19 — Registro de Riscos

## Objetivo e Escopo

Catalogar riscos do projeto com probabilidade, impacto, mitigação e owner, para gestão proativa.

---

## Matriz de Classificação

| Probabilidade \ Impacto | Baixo | Médio | Alto | Crítico |
|---|---|---|---|---|
| **Alta** | 🟡 | 🟠 | 🔴 | 🔴 |
| **Média** | 🟢 | 🟡 | 🟠 | 🔴 |
| **Baixa** | 🟢 | 🟢 | 🟡 | 🟠 |

---

## Riscos Identificados

| ID | Risco | Prob. | Impacto | Classif. | Mitigação | Owner | Status |
|---|---|---|---|---|---|---|---|
| R-01 | Física não parece autêntica para pilotos reais | Média | Alto | 🟠 | Calibração iterativa com telemetria e feedback; NPS gate | Fundador + agente | Aberto |
| R-02 | Controles touch rejeitados pelos jogadores | Média | Alto | 🟠 | 3 modos + assistências + testes A/B precoces | Agente | Aberto |
| R-03 | Multiplayer com latência alta no Brasil | Baixa | Médio | 🟢 | Photon Cloud SP; testes de campo antes de commit | Agente | Aberto |
| R-04 | Receita insuficiente para sustentabilidade | Média | Alto | 🟠 | Validar ARPDAU no soft launch; pivotar modelo se necessário | Fundador | Aberto |
| R-05 | Disponibilidade do fundador insuficiente | Média | Médio | 🟡 | Priorizar decisões bloqueantes; batching de aprovações | Fundador | Aberto |
| R-06 | Unity Build Automation não suporta iOS sem Mac | Baixa | Médio | 🟢 | Verificar early; alternativa: Mac mini + GitHub Actions | Agente | Aberto |
| R-07 | UGS pricing escala acima do orçamento | Baixa | Alto | 🟡 | Monitorar custos; plano de migração para custom backend | Fundador | Aberto |
| R-08 | App Store rejeita na primeira submissão | Média | Baixo | 🟡 | Compliance desde M1; checklist pré-submissão | Agente | Aberto |
| R-09 | Cheaters comprometem experiência no MVP | Baixa | Médio | 🟢 | Host authority + validação backend; ranked pós-MVP com server authority | Agente | Aberto |
| R-10 | Arte/áudio não profissional suficiente | Alta | Médio | 🟠 | Style guide + assets stores de qualidade; orçamento para freelancer se necessário | Fundador | Aberto |
| R-11 | Scope creep — fundador quer features adicionais | Alta | Médio | 🟠 | MVP rígido; features extras vão para backlog pós-MVP | Fundador + agente | Aberto |
| R-12 | Calibração de economia causa inflação/deflação | Média | Médio | 🟡 | Monitorar sink/source ratio semanal; Remote Config para ajustes | Agente | Aberto |
| R-13 | Problemas de privacidade/LGPD descobertos tardiamente | Baixa | Alto | 🟡 | Compliance desde M1; consultor jurídico antes do beta | Fundador (🧑‍💻) | Aberto |
| R-14 | Photon Fusion 2 tem breaking changes ou EoL | Baixa | Crítico | 🟠 | Abstraction layer; ADR com plano de saída | Agente | Aberto |
| R-15 | Tempo de calibração de física excede estimativa | Alta | Médio | 🟠 | Iniciar calibração em M2; iteração contínua; NPS como gate | Agente + Fundador | Aberto |
| R-16 | Bot AI insuficientemente humana | Média | Médio | 🟡 | Perfis com erros parametrizados; feedback de testadores | Agente | Aberto |
| R-17 | Crash rate em dispositivos Android fragmentados | Média | Alto | 🟠 | Device farm testing; crash reporting; staged rollout | Agente | Aberto |
| R-18 | Burnout do fundador (projeto muito longo) | Média | Crítico | 🔴 | Milestones incrementais com valor; celebrar cada alpha | Fundador | Aberto |
| R-19 | Pilotos reais não participam dos testes | Baixa | Médio | 🟢 | Engajar comunidade cedo; incentivos (créditos in-game) | Fundador | Aberto |
| R-20 | Dependência exclusiva de agentes IA | Média | Médio | 🟡 | Documentação completa permite contratar dev se necessário | Fundador | Aberto |

---

## Plano de Monitoramento

| Frequência | Ação |
|---|---|
| Semanal | Revisar riscos abertos com maior classificação |
| Por milestone | Atualizar status; adicionar novos riscos |
| Pós-incident | Post-mortem e atualizar registro |
| Trimestral | Reavaliar probabilidades com dados reais |

---

## Decisões Confirmadas

1. Registro mantido e revisado a cada milestone.
2. Riscos 🔴 bloqueiam avanço para próximo milestone sem plano ativo.
3. Owner definido para cada risco.

## Questões Abertas

- Q-RR-01: Orçamento disponível para mitigação de R-10 (arte/áudio profissional)?
- Q-RR-02: Contratar dev/QA se risco de burnout se materializar?

## Links Relacionados

- [Roadmap](./17-roadmap.md)
- [Questões Abertas](./20-open-questions.md)
- [Product Backlog](./18-product-backlog.md)
