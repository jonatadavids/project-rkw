# 01 — Visão do Produto

## Objetivo e Escopo

Definir a proposta de valor, público-alvo, diferenciação e sucesso mensurável do jogo mobile de kart rental.

---

## Proposta de Valor

> **Aprenda a pilotar, conquiste sua licença e dispute campeonatos online de kart rental.**

---

## Público-Alvo

| Segmento | Descrição | Motivação |
|---|---|---|
| Pilotos de kart rental | Pessoas que correm regularmente em kartódromos brasileiros | Competição séria fora da pista |
| Aspirantes | Jovens/adultos curiosos mas sem acesso frequente a kartódromo | Aprender e praticar |
| Fãs de corrida mobile | Jogadores de jogos de corrida mobile que querem realismo sem armas | Experiência autêntica |

---

## Diferenciação

| Aspecto | Kart Rental Game | Mario Kart | Real Racing / Asphalt |
|---|---|---|---|
| Armas/poderes | ❌ Sem armas | ✅ Power-ups | Parcial (nitro, etc.) |
| Vantagem competitiva | Técnica de pilotagem | RNG + itens | Upgrades P2W |
| Categoria | Kart rental exclusivo | Fantasia | Carros/supercarros |
| Escola | Currículo completo | ❌ | ❌ |
| Monetização | Cosméticos + passe | Pago + IAP | Gacha + energy |
| Comunidade | Pilotos reais BR | Casual global | Casual global |

---

## Contexto do Fundador

- **Perfil:** Fundador solo; dados pessoais não são necessários na documentação técnica
- **Mercado inicial:** Brasil, com medições de rede previstas em Brasília-DF e outras cidades
- **Background:** Infraestrutura/cloud; piloto de kart rental em campeonato real
- **Equipamentos:** Mac, Android e iPhone disponíveis para testes
- **Ferramentas de desenvolvimento:** Codex, Kiro, Gemini
- **Disponibilidade:** 8–12 h/semana para validação, testes e decisões

---

## Decisões Confirmadas

1. Jogo mobile (Android + iOS) com base única Unity 6.3 LTS (`6000.3.22f1`).
2. Sem armas, poderes ou turbo mágico — vantagem exclusiva de pilotagem.
3. Monetização ética: cosméticos, passe de temporada, ads não-intrusivos. Sem pay-to-win.
4. Visual 3D semi-realista inspirado em kartódromos brasileiros.
5. Pistas fictícias no MVP para evitar questões de marca.
6. Multiplayer com salas privadas no MVP; ranked pós-MVP.

---

## Suposições

| ID | Suposição | Plano de Validação |
|---|---|---|
| SV-01 | Comunidade de kart rental BR (estimada 500 mil+) se interessa por um jogo mobile sério | Alpha com amigos pilotos do fundador |
| SV-02 | O modelo simcade em touch é acessível o suficiente sem auto-acelerar | Teste com 10 pilotos reais + 10 não pilotos |
| SV-03 | Receita de cosméticos + ads é suficiente para sustentabilidade | Análise D30 de ARPDAU no soft launch |
| SV-04 | Unity 6.3 LTS (`6000.3.22f1`) + URP atinge 30 FPS em dispositivos modestos | Benchmark no milestone 2 |

---

## Métricas de Sucesso (MVP)

| Métrica | Meta | Prazo |
|---|---|---|
| Retenção D1 | ≥ 40% | Soft launch |
| Retenção D7 | ≥ 20% | Soft launch |
| Sessão média | ≥ 12 min | Soft launch |
| Corridas/dia por user | ≥ 3 | Soft launch |
| NPS pilotos reais | ≥ 50 | Alpha |
| Crash rate | < 1% | Beta |
| FPS P10 Android modesto | ≥ 30 | Beta |

---

## Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|
| Física não parece autêntica para pilotos | Média | Alto | Calibração com telemetria real + feedback iterativo |
| Controles touch rejeitados | Média | Alto | 3 modos + assistências progressivas |
| Receita insuficiente | Média | Alto | Validar ARPDAU no soft launch antes de escalar |
| Multiplayer latência no BR | Baixa | Médio | Servidores São Paulo + fallback relay |

---

## Questões Abertas

- Q-PV-01: Qual parceiro de kartódromo real abordar primeiro para pista licenciada pós-MVP?
- Q-PV-02: Idade mínima de cadastro? Definir política de controle parental.
- Q-PV-03: Nome definitivo do jogo e marca registrada.

---

## Links Relacionados

- [GDD](./02-game-design-document.md)
- [Roadmap](./17-roadmap.md)
- [Product Backlog](./18-product-backlog.md)
- [Registro de Riscos](./19-risk-register.md)
