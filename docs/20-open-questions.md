# 20 — Questões Abertas

## Objetivo e Escopo

Centralizar todas as questões que exigem decisão humana, validação externa ou informação adicional antes de implementação.

---

## Legenda

| Prioridade | Descrição |
|---|---|
| 🔴 Bloqueante | Impede início ou continuação de milestone |
| 🟠 Alta | Deve ser resolvida durante o milestone corrente |
| 🟡 Média | Deve ser resolvida antes do beta |
| 🟢 Baixa | Pode ser resolvida pós-MVP |

---

## Questões

### Produto e Design

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-PV-01 | Qual kartódromo real abordar primeiro para pista licenciada? | 🟢 | Pós-MVP | Fundador | Parcerias comerciais |
| Q-PV-02 | Idade mínima de cadastro? Política de controle parental? | 🟠 | M1 | Fundador + jurídico | **Não bloqueia a fundação técnica do M1. Bloqueia:** fluxo definitivo de idade e conta infantil, anúncios reais, IAP real, Alpha externo e publicação nas lojas. Não implementar checkbox, ano de nascimento ou autodeclaração de idade sem parecer jurídico. Checklist preliminar aprovado: [`docs/spikes/legal-checklist.md`](./spikes/legal-checklist.md). |
| Q-PV-03 | Nome comercial provisório `KARTGRID` — confirmar marca, domínio e disponibilidade nas lojas | 🟠 | M1 | Fundador (🧑‍💻) | `KARTGRID` é somente nome comercial provisório do protótipo. Pesquisa de marca, domínio e disponibilidade nas lojas permanece obrigatória. O placeholder técnico provisório `br.com.suitedigital.rentalkartworld` não foi alterado. |
| Q-GDD-01 | Voltas de corrida configuráveis por sala privada? | 🟡 | M5 | Fundador | Flexibilidade social |
| Q-GDD-02 | Ghost compartilhável entre amigos? | 🟢 | Pós-MVP | Fundador | Social feature |
| Q-GDD-03 | Replay completo ou apenas highlight reel? | 🟡 | M7 | Fundador | Custo de storage/CPU |
| Q-UF-01 | Permitir skip da Escola para pilotos experientes? | 🟡 | M6 | Fundador | UX vs onboarding |
| Q-UF-02 | Deep link para sala privada (WhatsApp/Telegram)? | 🟡 | M5 | Agente | Viralidade |

### Controles e Física

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-CT-01 | Suportar controllers Bluetooth (gamepad)? | 🟢 | Pós-MVP | Fundador | Nicho; Input System suporta |
| Q-CT-02 | Feedback visual de intensidade do acelerador? | 🟡 | M3 | Agente | UX |
| Q-CT-03 | Tutorial específico para cada modo de controle? | 🟡 | M6 | Agente | Custo de conteúdo |
| ~~Q-PH-01~~ | ~~PhysX interno ou física custom para determinismo?~~ | ✅ RESOLVIDO | M2 | Agente | **Decisão: PhysX + camada custom C# de dinâmica de kart. Detalhes em `docs/04-driving-physics.md`.** |
| Q-PH-02 | Quanto de assistência de frenagem mínima? | 🟡 | M3 | Agente + pilotos | Calibração |
| Q-PH-03 | Peso do piloto considerado? Equalização justa? | 🟢 | Pós-MVP | Fundador | Sensibilidade |

### Regras e Competitivo

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-RF-01 | Sistema de apelação para campeonatos? | 🟢 | Pós-MVP | Fundador | Custo operacional |
| Q-RF-02 | Azul obrigatória ou informativa no MVP? | 🟡 | M4 | Fundador | Regulamento base |
| Q-RF-03 | Penalidade de abandono afeta ranking ou só cooldown? | 🟡 | M7 | Fundador | Game design |

### IA e Bots

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-AI-01 | Bots com nomes/avatares persistentes? | 🟡 | M4 | Agente | Imersão |
| Q-AI-02 | ML para perfis avançados pós-MVP? | 🟢 | Pós-MVP | Agente | Custo vs benefício |
| Q-AI-03 | Bot comunica via emotes? | 🟢 | Pós-MVP | Fundador | Social |

### Multiplayer

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| ~~Q-MP-01~~ | ~~Photon Shared ou Server Mode desde o início?~~ | ✅ RESOLVIDO | M5 | Agente | **Decisão: Shared Mode para protótipo/alpha/casual. Migração obrigatória para server authority antes de ranked/prêmios. Detalhes em `docs/08-multiplayer-architecture.md`.** |
| ~~Q-MP-02~~ | ~~Custo mensal Photon para 1.000 CCU?~~ | ✅ PESQUISADO | M0 | Fundador (🧑‍💻) | **Photon Development 20 CCU aprovado. Nenhuma contratação de Photon 1.000 CCU foi autorizada.** A referência pesquisada para 1.000 CCU é US$ 250/mês + possível excedente de tráfego, consulta oficial em 2026-08-16. Ver [`docs/spikes/photon-cost-estimate.md`](./spikes/photon-cost-estimate.md). |
| Q-MP-03 | Custom server vs Photon Server para ranked? | 🟡 | M10 | Fundador + Agente | Custo vs controle |
| Q-MP-04 | Voice chat futuro? SDK? | 🟢 | Pós-MVP | Fundador | UX/custo |
| Q-MP-05 | Qual região Photon usar para audiência brasileira? | 🟠 MEDIÇÃO PARCIAL | M1 | Agente + Fundador | Protocolo aprovado. Em 2026-08-18, o snapshot de Brasília indicou `sa` ≈ 21 ms, `ussc` ≈ 171 ms e `us` ≈ 172 ms; `sa` é a recomendação provisória. O cache de 10 segundos do Fusion impede tratar chamadas sucessivas como série temporal. Faltam heartbeat de sessão e ao menos duas localidades BR reais, sem VPN, para decisão definitiva. Ver [`protocolo`](./spikes/photon-region-latency-protocol.md) e [`resultados`](./spikes/photon-region-latency.md). |

### Backend e Economia

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| ~~Q-BD-01~~ | ~~Limite de storage Cloud Save por jogador?~~ | ✅ RESOLVIDO E APROVADO | M0 | Agente | 5 MiB e 2.000 chaves por jogador/access class; estratégia de 32 KiB/jogador aprovada. Ver [`docs/spikes/cloud-save-limits.md`](./spikes/cloud-save-limits.md). |
| Q-BD-02 | Plano de migração se trocar UGS? | 🟡 | M5 | Agente | Exit strategy |
| Q-BD-03 | Procedimento GDPR/LGPD para exclusão no UGS? | 🟠 PROTOCOLO PRELIMINAR APROVADO | M1 | Agente + jurídico | Cascade aprovado somente como protocolo preliminar; Economy exige solicitação à Unity, e implementação, retenções e texto ao usuário ainda exigem validação técnica/jurídica. Ver [`docs/spikes/data-deletion-procedure.md`](./spikes/data-deletion-procedure.md). |
| Q-PE-01 | Cap de Coins por dia (anti-farm)? | 🟡 | M7 | Fundador | Economia |
| Q-PE-02 | Reset de ELO por temporada ou decay gradual? | 🟢 | Pós-MVP | Fundador | Game design |
| Q-PE-03 | Recompensas de temporada exclusivas ou retornáveis? | 🟡 | M9 | Fundador | FOMO vs inclusão |

### Monetização

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-MN-01 | Patrocínios in-game com marcas de kart? | 🟢 | Pós-MVP | Fundador (🧑‍💻) | Receita |
| Q-MN-02 | Bundle de boas-vindas? Preço? | 🟡 | M9 | Fundador | Monetização |
| Q-MN-03 | Preço da remoção de ads? | 🟡 | M9 | Fundador | Pricing |
| Q-MN-04 | Passe catch-up ou comprar níveis? | 🟡 | M9 | Fundador | UX de passe |

### Arte e Áudio

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-AA-01 | Gravar motor real ou síntese? | 🟡 | M3 | Fundador (🧑‍💻) | Custo vs autenticidade |
| ~~Q-AA-02~~ | ~~Wwise/FMOD ou Unity Audio nativo?~~ | ✅ APROVADO COM CONDIÇÃO | M0 | Agente | Unity Audio nativo aprovado para MVP, condicionado ao teste prático em M1. Ver [`docs/spikes/unity-audio-requirements.md`](./spikes/unity-audio-requirements.md). |
| Q-AA-03 | Budget de animações de pilotos? | 🟡 | M3 | Agente | Arte |

### Release e Operação

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-RL-01 | Nome definitivo do pacote/bundle ID? | 🟠 REABERTA | M1 | Fundador (🧑‍💻) | `br.com.suitedigital.rentalkartworld` permanece **PLACEHOLDER PROVISÓRIO**, não definitivamente aprovado. Não criar aplicativos definitivos nas lojas até decisão humana explícita. |
| Q-RL-02 | Quem produz screenshots e assets de loja? | 🟠 | M9 | Fundador (🧑‍💻) | Marketing |
| Q-RL-03 | Localização (pt-BR + en-US) desde MVP? | 🟡 | M9 | Fundador | Alcance |
| Q-RL-04 | Game Center integration? | 🟢 | Pós-MVP | Agente | iOS |

### Segurança e Legal

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-SP-01 | Consultor jurídico para Política de Privacidade? | 🟠 PROTOCOLO PRELIMINAR APROVADO | M1 | Fundador (🧑‍💻) | Checklist aprovado somente como protocolo preliminar. Consultor continua obrigatório antes de fluxo definitivo de idade/conta infantil, anúncios ou IAP reais, Alpha externo e publicação. Ver [`docs/spikes/legal-checklist.md`](./spikes/legal-checklist.md). |
| Q-SP-02 | Age gate: birth year ou checkbox? | 🟡 | M7 | Fundador | UX/legal |
| Q-SP-03 | Plano de resposta a incidentes? | 🟡 | M9 | Fundador + Agente | Operação |
| Q-SP-04 | Seguro contra vazamento de dados? | 🟢 | Pós-MVP | Fundador (🧑‍💻) | Financeiro |

### Testes

| ID | Questão | Prioridade | Milestone | Owner | Contexto |
|---|---|---|---|---|---|
| Q-TS-01 | Dispositivos exatos da matriz de testes? | 🟠 MATRIZ PARCIALMENTE CONFIRMADA | M3 | Fundador (🧑‍💻) | High-tier disponível: Samsung Galaxy S25 (Android) e iPhone 17 (iOS). Android low/mid pendentes por empréstimo ou aparelho de piloto/testador para o gate de performance do M3. Ver [`docs/spikes/device-matrix.md`](./spikes/device-matrix.md). |
| Q-TS-02 | Firebase Test Lab para device farm? | 🟡 | M8 | Agente | Automação |
| Q-TS-03 | Frequência de sessões com pilotos? | 🟡 | M8 | Fundador | Disponibilidade |

---

## Questões Bloqueantes (🔴) — Resumo

| ID | Questão | Milestone | Owner | Status |
|---|---|---|---|---|
| ~~Q-PH-01~~ | PhysX ou custom physics? | M2 | Agente | ✅ RESOLVIDO — PhysX + custom C# layer |
| ~~Q-MP-01~~ | Photon Shared ou Server Mode? | M5 | Agente | ✅ RESOLVIDO — Shared para MVP; migração obrigatória antes de ranked |
| Q-RL-01 | Bundle ID definitivo | M1 | Fundador (🧑‍💻) | 🟠 REABERTA — `br.com.suitedigital.rentalkartworld` é somente placeholder; não criar apps definitivos nas lojas |

**Nenhum bloqueio 🔴 impede a fundação técnica do M1.** Q-RL-01 permanece aberta e bloqueia a criação de aplicativos definitivos nas lojas. Q-PV-02 permanece aberta com os bloqueios específicos registrados acima.

---

## Decisões humanas — revisão dos spikes M0 (2026-08-16)

- Estratégia do Cloud Save aprovada.
- Photon Development 20 CCU aprovado.
- Nenhuma contratação de Photon 1.000 CCU autorizada.
- Protocolo de latência Photon aprovado; região definitiva depende da execução em M1.
- Ghost local em 30 Hz aprovado; limite de 50 KiB vale para o arquivo completo.
- Unity Audio nativo aprovado, condicionado ao teste prático em M1.
- Procedimento de exclusão e checklist jurídico aprovados somente como protocolos preliminares.
- Matriz de dispositivos parcialmente confirmada: high-tier Android e iOS disponíveis; Android low/mid pendentes para M3.

## Exit Gate M0 — APROVADO (2026-08-16)

- M0-T01 a M0-T08 concluídas e M0-T09 aprovado por validação humana.
- Cloud Save, Photon Development 20 CCU, ghost local em 30 Hz e Unity Audio nativo aprovados nos limites documentados.
- Photon 1.000 CCU não autorizado.
- Bundle ID permanece provisório.
- Matriz parcialmente confirmada com Samsung Galaxy S25 e iPhone 17; Android low/mid pendentes para M3.
- Questões jurídicas e de idade não bloqueiam a fundação técnica, mas bloqueiam anúncios reais, IAP real, Alpha externo e publicação nas lojas.
- Protocolos de Photon e áudio serão executados em M1.
- Nenhuma questão vermelha bloqueia a fundação do projeto.
- Aprovação deste gate não inicia nem autoriza tarefas M1.

## Decisão humana — entrada condicional no M2 (2026-08-17)

- O Exit Gate M1 integral permanece aberto; M1-T07, M1-T12, M1-T13 e M1-T14 continuam `[ ]` sem redução de seus critérios.
- Está autorizada a entrada condicional somente em M2-T01, pois as pendências restantes não bloqueiam física, controles locais ou pista greybox.
- M1-T07 mantém UBA, lojas, triggers e distribuição adiados, sem cobrança; M1-T12 mantém heartbeat e duas localidades pendentes; M1-T13 mantém profiling Android low/mid pendente para M3.
- Esta decisão não inicia M2-T01 nem autoriza M2-T02+, produção ou publicação. Detalhes e evidências: [`docs/28-m1-conditional-gate.md`](./28-m1-conditional-gate.md).

---

## Links Relacionados

- [Registro de Riscos](./19-risk-register.md)
- [Roadmap](./17-roadmap.md)
- [ADRs](./adr/)
