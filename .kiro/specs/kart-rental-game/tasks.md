# Implementation Plan: Project RKW (Kart Rental Game)

## Overview

Plano de implementação incremental para o jogo mobile multiplayer de kart rental. Organizado em 11 milestones (M0–M10), cada um com exit gate mensurável. A estratégia privilegia vertical slice (kart dirigível → cronometragem → multiplayer) e validação precoce com pilotos reais ANTES de expandir conteúdo.

**Linguagem:** C# (Unity 6.3 LTS (`6000.3.22f1`))  
**Framework de testes:** Unity Test Framework/NUnit; properties com geradores determinísticos próprios  
**Convenções:** AGENTS.md, ADRs existentes, ScriptableObjects para dados

---

## Premissas das Estimativas

| Premissa | Valor / Descrição |
|---|---|
| **Horas de dedicação (Fundador)** | 8–12 h/semana (fundador solo, valida, testa, decide) |
| **Execução por agentes (Codex/Kiro)** | Execução assistida por Codex/Kiro conforme capacidade disponível. Prazo depende de limites de uso, ciclos de correção, disponibilidade das ferramentas e validação humana. Estimativas são hipóteses de planejamento, não compromissos de prazo. |
| **Tempo de espera por validação humana** | 1–3 dias úteis por gate que exige Fundador |
| **Disponibilidade de pilotos** | 2–4 pilotos contactáveis; agendar com 1 semana de antecedência |
| **Dispositivos de teste** | Fundador possui ao menos 1 Android mid + 1 iOS; low-tier emprestado/adquirido conforme M0-T05 |
| **Assets** | Placeholder próprios para MVP; marketplace assets opcionais (budget permitting); arte final pós-MVP |
| **Aprovação externa (jurídico)** | Excluída do timeline de dev; risco aceito (placeholder até consultor) |
| **Complexidade escola** | 10 módulos com conteúdo textual/visual simples; sem áudio narrado no MVP |
| **Trabalho jurídico** | Separado do timeline técnico; blocos legais marcados como dependência externa |

---

## Orçamento por Fase (limite inicial: R$ 500)

| Categoria | Custo | Fase | Obrigatório? | Notas |
|---|---|---|---|---|
| **Unity 6.3 LTS (`6000.3.22f1`)** | Grátis (Personal) | M0–M10 | ✅ | Até 100K USD/ano de receita |
| **GitHub (repositório)** | Grátis (free tier) | M0–M10 | ✅ | Repos privados ilimitados |
| **Photon Fusion 2** | Grátis (20 CCU dev) | M5+ | ✅ | Plano de lançamento de 100 CCU grátis (1 app/cliente) ou 100 CCU por US$ 95 uma vez/12 meses; consulta em 2026-08-16 |
| **UGS (Auth, Cloud Save, Economy)** | Grátis (free tier) | M1+ | ✅ | Limites generosos para alpha/beta |
| **Firebase Crashlytics** | Grátis | M3+ | ✅ | Sem custo |
| **Google Play Console** | ~R$ 130 (USD 25, taxa única) | Pré-Alpha | ✅ Android | Pode adiar até distribuição |
| **Apple Developer Program** | ~R$ 530 (USD 99/ano) | Pré-Alpha | ✅ iOS | Pode adiar; dev continua cross-platform |
| **Unity Build Automation** | Grátis (starter) | M1+ | ✅ | 200 min/mês; suficiente para alpha |
| **Assets marketplace** | R$ 0–200 | M3+ | ❌ Opcional | Greybox primeiro; comprar se necessário |
| **Photon 100 CCU** | US$ 0 no plano de lançamento ou US$ 95 uma vez/12 meses | Beta+ | ❌ Adiar | Confirmar elegibilidade ao plano grátis antes do beta; preços consultados em 2026-08-16 |
| **Domínio web** | ~R$ 50/ano | Pré-lançamento | ❌ Adiar | Para política de privacidade |

**Resumo de investimento inicial (até closed alpha):**

| Cenário | Custo Total |
|---|---|
| Apenas Android (MVP alpha) | ~R$ 130 |
| Android + iOS simultâneo | ~R$ 660 (excede R$ 500) |
| **Recomendação** | Iniciar apenas Android; iOS adiar até beta ou quando budget permitir |

> ⚠️ Desenvolvimento continua cross-platform (builds compilam para ambos). Apenas o pagamento/registro nas lojas é adiado para iOS. Quando budget permitir, registrar Apple Developer Program e publicar.

**Custos recorrentes (pós-lançamento):**
- Photon: 100 CCU sem recorrência no plano de lançamento ou US$ 95 uma vez/12 meses; recorrência começa em US$ 125/mês para 500 CCU
- Apple Developer: $99/ano
- UGS: grátis até limites do free tier (suficiente para 10K MAU)
- AdMob/Firebase: grátis (Google absorve)

---

## Questões acompanhadas após os spikes M0 (nenhuma bloqueia tarefas internas)

| ID | Questão | Status | Impacto em Tasks |
|---|---|---|---|
| Q-PV-02 | Idade mínima e controle parental | 🟠 Checklist preliminar aprovado; parecer jurídico pendente | Não bloqueia fundação técnica; bloqueia fluxo definitivo de idade/conta infantil, ads e IAP reais, Alpha externo e lojas |
| Q-PV-03 | Nome comercial + registro de marca | 🟠 Pendente | Bundle ID atual é placeholder técnico provisório |
| Q-RL-01 | Bundle ID definitivo | 🟠 Reaberta | Não criar aplicativos definitivos nas lojas; placeholder não equivale a aprovação |
| Q-MP-02 | Custo Photon 1.000 CCU | ✅ Pesquisado | Development 20 CCU aprovado; nenhuma contratação de 1.000 CCU autorizada |
| Q-BD-01 | Limite Cloud Save por jogador | ✅ Resolvido e aprovado | Estratégia de 32 KiB/jogador aprovada |
| Q-BD-03 | Procedimento exclusão LGPD/GDPR no UGS | 🟠 Protocolo preliminar aprovado | Implementação/retenções ainda exigem validação técnica e jurídica |
| Q-SP-01 | Consultor jurídico para Política de Privacidade | 🟠 Checklist preliminar aprovado; contratação pendente | Obrigatório antes dos gates legais/comerciais definidos em Q-PV-02 |
| Q-TS-01 | Dispositivos da matriz de testes | 🟠 Matriz parcialmente confirmada | Galaxy S25 e iPhone 17 disponíveis; Android low/mid pendentes para M3 |
| Q-MP-05 | Região Photon validada para BR | 🟠 Protocolo aprovado | Benchmark real em M1 define a região |

**Q-AA-02 APROVADO COM CONDIÇÃO:** Unity Audio nativo para MVP, condicionado ao teste prático em M1. Wwise/FMOD apenas se profiling ou necessidade avançada justificar migração.

---

## Tasks

---

### M0 — Preparação e Decisões (Pesquisa apenas — sem projetos Unity)

> **Objetivo:** Resolver spikes técnicos via pesquisa e documentação, definir matriz de dispositivos, estimar custos, preparar checklist legal e garantir que nenhuma decisão pendente bloqueie M1+. NENHUM projeto Unity é criado nesta fase — apenas protocolos de teste são documentados para execução em M1.

- [x] M0-T01 Spike: Verificar limite e custo de Cloud Save (UGS) por jogador
  - Consultar documentação UGS sobre limites de Cloud Save (storage por jogador, rate limits, pricing tiers)
  - Documentar resultado em `docs/20-open-questions.md` (Q-BD-01)
  - Estimar consumo para: perfil JSON, settings, progresso escola, ghost local metadata
  - Criar arquivo `docs/spikes/cloud-save-limits.md` com findings
  - _Requirements: R10.5, R13_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Documento com limites, custos estimados para 1K/10K/100K jogadores, e recomendação de estratégia de armazenamento_
  - _Testes: N/A (pesquisa)_
  - _Evidência: Markdown com dados da documentação oficial_
  - _Validação humana: 🧑‍💻 Fundador aprova estratégia_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: Limites podem forçar arquitetura diferente de persistência_

- [x] M0-T02 Spike: Estimar custo Photon para 20, 100 e 1.000 CCU
  - Consultar pricing page e calculadora Photon Fusion 2
  - Documentar tiers, limites free, custo mensal por faixa
  - Mapear para cenários: alpha privado (20 CCU), beta (100 CCU), lançamento (1.000 CCU)
  - Atualizar `docs/20-open-questions.md` (Q-MP-02)
  - Criar arquivo `docs/spikes/photon-cost-estimate.md`
  - _Requirements: R9.1_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Tabela de custos por faixa com recomendação de tier para cada fase_
  - _Testes: N/A (pesquisa)_
  - _Evidência: Markdown com pricing documentado_
  - _Validação humana: 🧑‍💻 Fundador aprova budget_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: Pricing pode mudar; documentar data de consulta_

- [x] M0-T03 Spike: Pesquisar regiões Photon e documentar protocolo de latência para BR
  - Pesquisar regiões Photon disponíveis (us, sa, asia, eu, etc.) e documentar quais servem Brasil
  - Documentar protocolo de teste de latência: script de ping a implementar em M1, medir de Brasília + 2 cidades
  - Estimar latência esperada com base na documentação Photon e relatos da comunidade
  - Criar `docs/spikes/photon-region-latency-protocol.md` com o plano de teste
  - Atualizar questão NOVA em `docs/20-open-questions.md`
  - _Requirements: R9.9_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Protocolo documentado com regiões candidatas e plano de medição para M1_
  - _Testes: N/A (pesquisa — execução em M1)_
  - _Evidência: Markdown com protocolo_
  - _Validação humana: 🧑‍💻 Fundador valida se plano faz sentido_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: Região "sa" pode não existir; documentar alternativas_

- [x] M0-T04 Spike: Documentar procedimento de exclusão de dados (LGPD/GDPR) no UGS
  - Pesquisar APIs de UGS para deletion de player data (Cloud Save, Economy, Leaderboards, Auth)
  - Documentar fluxo completo: request → verificação → exclusão → confirmação
  - Identificar dados que podem requerer retenção legal (anti-fraude, obrigações fiscais)
  - Criar `docs/spikes/data-deletion-procedure.md`
  - Atualizar Q-BD-03 em `docs/20-open-questions.md`
  - _Requirements: R13.5, R13.6_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Documento com fluxo de exclusão, APIs necessárias, exceções legais_
  - _Testes: N/A (pesquisa)_
  - _Evidência: Markdown com procedimento_
  - _Validação humana: 🧑‍💻 Fundador + consultor jurídico revisam_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: UGS pode não ter API de bulk delete; mitigação documentada_

- [x] M0-T05 Spike: Definir matriz inicial de dispositivos (Android + iOS)
  - Propor dispositivos por tier: low (Android), mid (Android/iOS), high (iOS)
  - Incluir SoC, RAM, GPU, versão de OS para cada dispositivo
  - Documentar em `docs/spikes/device-matrix.md`
  - Atualizar Q-TS-01 em `docs/20-open-questions.md`
  - _Requirements: R12.1, R12.2_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Tabela com no mínimo 3 Android + 2 iOS com specs_
  - _Testes: N/A_
  - _Evidência: Markdown com tabela_
  - _Validação humana: 🧑‍💻 Fundador confirma dispositivos disponíveis para teste_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: Fundador pode não ter todos os dispositivos; priorizar os disponíveis_

- [x] M0-T06 Spike: Estimar tamanho e frequência de amostras de ghost
  - Calcular tamanho por amostra (position Vector3 quantized + rotation compressed quaternion)
  - Estimar amostras por segundo (30 Hz = 30 samples/s)
  - Calcular tamanho total para volta de ~45s, ~60s, ~90s
  - Propor estratégia de compressão (delta encoding, quantization)
  - Documentar em `docs/spikes/ghost-sample-sizing.md`
  - _Requirements: R24.1, R24.4_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Documento com estimativa de ~50KB target validada ou revisada_
  - _Testes: N/A_
  - _Evidência: Cálculos documentados_
  - _Validação humana: não_
  - _Ambiente: Desktop_
  - _Risco: Baixo — ajuste de frequência/compressão resolve_

- [x] M0-T07 Spike: Pesquisar requisitos de Unity Audio nativo para mobile
  - Pesquisar documentação Unity Audio sobre: latência, CPU usage, mixers simultâneos, spatial audio no mobile
  - Documentar requisitos e limitações conhecidas em Android/iOS
  - Criar protocolo de teste para execução em M1 (após projeto existir): quais métricas medir, em quais dispositivos
  - Criar `docs/spikes/unity-audio-requirements.md` com findings e protocolo
  - Confirmar Q-AA-02 RESOLVIDO: Unity Audio nativo para MVP (com validação prática em M1)
  - _Requirements: R12_
  - _Dependências: nenhuma (pesquisa independente de device matrix)_
  - _Critério de conclusão: Documento com requisitos, limitações conhecidas e protocolo de validação para M1_
  - _Testes: N/A (pesquisa — execução prática em M1)_
  - _Evidência: Markdown com protocolo_
  - _Validação humana: não_
  - _Ambiente: Desktop (pesquisa)_
  - _Risco: Baixo — informação disponível na documentação oficial_

- [x] M0-T08 Spike: Preparar checklist legal (sem substituir consultor jurídico)
  - Listar itens legais necessários: Política de Privacidade, Termos de Uso, LGPD/GDPR compliance, idade mínima, controle parental, consentimento por base legal
  - Documentar em `docs/spikes/legal-checklist.md`
  - Marcar itens que EXIGEM consultor jurídico (Q-SP-01, Q-PV-02)
  - NÃO redigir textos legais definitivos
  - _Requirements: R11.8, R13.4, R13.5_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Checklist com itens, responsável, e status (pendente consultor / pode fazer internamente)_
  - _Testes: N/A_
  - _Evidência: Markdown_
  - _Validação humana: 🧑‍💻 Fundador contrata consultor_
  - _Ambiente: Desktop_
  - _Risco: Sem consultor, não pode publicar; risco aceito para dev interno_

- [x] M0-T09 Exit Gate M0
  - **Status: APROVADO em 2026-08-16 por validação humana**
  - M0-T01 a M0-T08 concluídas; 8/8 spikes documentados com resultados
  - Estratégias aprovadas nos limites documentados: Cloud Save, Photon Development 20 CCU, ghost local em 30 Hz e Unity Audio nativo
  - Photon 1.000 CCU não autorizado
  - Bundle ID permanece placeholder provisório; não criar aplicativos definitivos nas lojas
  - Matriz parcialmente confirmada: Samsung Galaxy S25 e iPhone 17 disponíveis como high-tier; Android low/mid pendentes para o gate de performance do M3
  - Decisões jurídicas e de idade não bloqueiam a fundação técnica; bloqueiam anúncios reais, IAP real, Alpha externo e publicação nas lojas
  - Protocolos de latência Photon e áudio prontos e aprovados para execução em M1
  - Nenhuma questão vermelha bloqueia a fundação do projeto
  - Fundador revisou e aprovou os spikes dentro das condições registradas
  - _Critério de conclusão: 8/8 spikes com documento + aprovação onde necessário_
  - _Validação humana: 🧑‍💻 Exit Gate M0 confirmado e aprovado_

---

### M1 — Fundação do Projeto Unity

> **Objetivo:** Criar projeto Unity 6.3 LTS (`6000.3.22f1`) com estrutura de assemblies mínima, configuração de plataformas, integração de Photon Fusion 2, UGS base, CI/CD mínimo, framework de testes e execução dos testes de validação preparados em M0. Kart NÃO precisa dirigir ainda.

- [x] M1-T01 Criar projeto Unity 6.3 LTS (`6000.3.22f1`) com configuração base
  - Criar projeto com URP template
  - Configurar Player Settings: Company Name "Suite Digital", Product Name "Project RKW"
  - Configurar Scripting Backend: IL2CPP
  - Configurar platforms: Android (min API 26, target 34+, arm64+armv7) + iOS (min 15.0, arm64)
  - Configurar Time.fixedDeltaTime = 0.02f (50 Hz)
  - Configurar .gitignore para Unity
  - _Requirements: R12, R14.7_
  - _Dependências: nenhuma_
  - _Critério de conclusão: Projeto compila para Android e iOS sem erros_
  - _Testes: Build compile check_
  - _Evidência: Console sem erros + build logs_
  - _Validação humana: não_
  - _Ambiente: Desktop (Unity Editor)_
  - _Risco: Baixo_

- [x] M1-T02 Criar estrutura de assemblies mínima (apenas módulos necessários até M2)
  - Criar APENAS os assembly definitions com consumidor neste bloco:
    - RKW.Core (identidade técnica compartilhada)
    - RKW.Core.EditMode.Tests (sanidade EditMode)
    - RKW.PlayMode.Tests (sanidade PlayMode)
  - Não antecipar RKW.Physics, RKW.Physics.Tests ou RKW.Controls antes do primeiro consumidor
  - Outros assemblies serão criados no milestone onde seu primeiro consumidor aparece:
    - M4: RKW.Timing, RKW.Timing.Tests, RKW.Bots, RKW.Bots.Tests, RKW.Telemetry
    - M5: RKW.Network, RKW.Network.Tests
    - M6: RKW.Race, RKW.Race.Tests
    - M7: RKW.School
    - M8: RKW.Backend, RKW.Backend.Tests, RKW.Championship, RKW.Championship.Tests
    - M9: RKW.UI
    - Quando necessário: RKW.Track, RKW.Track.Tests, RKW.Editor
  - _Requirements: R14 (build structure)_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Todos os asmdef iniciais compilam sem circular reference_
  - _Testes: Compilação incremental verifica isolamento_
  - _Evidência: Projeto compila sem warnings de assembly_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo — assemblies adicionais são criados incrementalmente_

- [x] M1-T03 Configurar framework de testes (NUnit + avaliação de FsCheck)
  - Configurar Unity Test Framework/NUnit para EditMode e PlayMode
  - Avaliar FsCheck antes de incorporar; não adicionar NuGet/DLL sem compatibilidade Unity 6.3 LTS (`6000.3.22f1`) + IL2CPP comprovada
  - Criar test assembly RKW.Core.EditMode.Tests com um teste EditMode de sanidade
  - Criar test assembly RKW.PlayMode.Tests com um teste PlayMode placeholder
  - Manter as 34 properties obrigatórias; até FsCheck ser validado, usar NUnit com geradores determinísticos, seed registrada, mínimo de 100 casos e seed exibida em toda falha
  - FsCheck pode ser reavaliado futuramente sem bloquear M1
  - Validar que `Test Runner` executa ambos os modos
  - _Requirements: R14.2, AGENTS.md Rule 7_
  - _Dependências: M1-T02_
  - _Critério de conclusão: Testes placeholder passam em EditMode e PlayMode_
  - _Testes: Placeholder tests green_
  - _Evidência: Screenshot Test Runner com 2 tests passing_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: FsCheck pode ter conflito com IL2CPP; testar em build se necessário_

- [x] M1-T04 Integrar Photon Fusion 2 SDK
  - Adicionar Photon Fusion 2 pelo pacote oficial versionado (ADR-0002 aprovado)
  - Configurar App ID no Photon Dashboard (usar App ID de desenvolvimento)
  - Criar script de conexão básica (ConnectToPhoton → log sucesso/falha)
  - Não implementar gameplay networking ainda
  - _Requirements: R9.1_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Log "Connected to Photon" no console_
  - _Testes: EditMode mock + PlayMode local para conexão real, timeout, cancelamento e repetição sem runner órfão_
  - _Evidência: `Connected to Photon` em duas conexões Shared Mode; EditMode 8/8 e PlayMode 4/4_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + internet_
  - _Risco: Médio — SDK pode ter breaking changes; pin version_

- [x] M1-T05 Integrar UGS base (Authentication + Cloud Save)
  - Configurar Unity Gaming Services no projeto (Project Settings → Services)
  - Implementar AuthenticationService: SignInAnonymously() como default
  - Implementar Cloud Save read/write básico (salvar/carregar JSON simples)
  - Criar interface ICloudPersistence para abstração
  - _Requirements: R10.5, R9.7 (backend)_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Login anônimo funciona + JSON salva/carrega do Cloud Save_
  - _Testes: EditMode 32/32; PlayMode local 6/6 com 3 integrações ignoradas; integração UGS real 7/7 executados com 2 integrações Photon ignoradas_
  - _Evidência: Auth anônima e round-trip da chave `rkw_m1_t05_smoke_v1` confirmados no ambiente `development`; dado verificado no UGS Dashboard em 2026-08-16_
  - _Robustez: timeouts finitos/configuráveis para init, auth, save e load; cancelamento best-effort com observação segura da tarefa SDK; JSON validado antes do envio_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + internet_
  - _Risco: Médio — requer conta UGS configurada_

- [x] M1-T06 Criar cenas base (Bootstrap + MainMenu placeholder)
  - Criar cena Bootstrap com inicialização de Authentication no UGS `development`; Remote Config e DI foram explicitamente adiados por decisão humana
  - Criar cena MainMenu placeholder com botões stub (Play, School, Garage)
  - Configurar scene loading (Bootstrap → MainMenu additive)
  - _Requirements: R2 (modos de jogo — estrutura)_
  - _Dependências: M1-T01, M1-T05_
  - _Critério de conclusão: App inicia em Bootstrap, carrega MainMenu, botões visíveis_
  - _Testes: EditMode 32/32; PlayMode local 13/13 com 5 integrações/captura condicionais ignoradas; inclui Retry sem concorrência, destruição durante autenticação pendente e câmera única após carga/repetição; integração real do Bootstrap com UGS 1/1_
  - _Evidência: captura Unity 2340×1080 sem dados sensíveis em `/tmp/rkw-m1-t06-main-menu.png`; Bootstrap e MainMenu carregadas additive com exatamente uma câmera ativa_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M1-T07 Configurar CI/CD básico (Unity Build Automation)
  - **Etapa A local parcial concluída em 2026-08-16:** suítes locais, APK Development ARM64/IL2CPP com debug signing, smoke físico no Galaxy S25, exportação Xcode e compile check iOS sem assinatura
  - **M1-T07 permanece incompleta:** nenhum AAB, Unity Build Automation, trigger, distribuição, conta de loja, signing de produção ou upload foi configurado
  - _Evidência parcial sanitizada: `docs/25-local-mobile-build-validation.md`_
  - Configurar Unity Build Automation para projeto
  - Criar build configs: Android (.aab) e iOS (IPA)
  - Configurar trigger em branch `release/*`
  - Configurar execução de testes EditMode antes de build
  - **Obrigatório mesmo sem Apple Developer Program:**
    - Build Android (.aab) completo
    - Compilação do código para target iOS (verifica erros de plataforma)
    - Exportação do projeto Xcode (valida que código compila para iOS)
  - **Condicional à conta Apple ativa:**
    - Assinatura do IPA
    - Instalação em iPhone real
    - TestFlight Internal
    - Distribuição automática iOS
  - Configurar distribuição para Google Play Internal Track — AUTO
  - A ausência temporária da conta Apple NÃO bloqueia M1 nem o Alpha Android
  - Criar gate explícito: "iOS Real Device Gate" — exigido antes do primeiro teste real em iPhone e antes de qualquer TestFlight
  - _Requirements: R14.1, R14.2, R14.3, R14.4, R14.5_
  - _Dependências: M1-T01, M1-T03_
  - _Critério de conclusão: Build Android funciona end-to-end; código compila para iOS sem erros; distribuição Android Internal Track automática_
  - _Testes: Trigger manual de build verifica pipeline Android; compilação iOS verifica ausência de erros_
  - _Evidência: Build log verde Android + Xcode export sem erros_
  - _Validação humana: 🧑‍💻 Fundador configura conta Google Developer (obrigatório); Apple Developer Program quando budget permitir_
  - _Ambiente: Unity Build Automation cloud_
  - _Risco: Médio — iOS signing adiado não bloqueia dev; requer conta Apple antes de TestFlight/iPhone test_

- [x] M1-T08 Criar tipos core em RKW.Core
  - Definir somente os enums consumidos neste bloco: GameMode e AssistClass
  - Definir SessionContextKey e LeaderboardKey imutáveis (com IEquatable, igualdade ordinal e GetHashCode apenas para coleções em memória)
  - Direction permanece propriedade canônica de TrackConfigurationId, sem campo duplicado
  - Adiar interfaces, constantes e ID persistente/canônico até existir consumidor
  - _Requirements: R19.1, R19.2_
  - _Dependências: M1-T02_
  - _Critério de conclusão: Types compilam, são referenciáveis por outros assemblies_
  - _Testes: EditMode test verifying LeaderboardKey equality_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [x] M1-T09 Property test: LeaderboardKey strict equality (Property 28)
  - **Property 28: LeaderboardKey Strict Equality**
  - Para quaisquer dois LeaderboardKey, igualdade sse TODOS os campos idênticos
  - Mudar qualquer campo único deve produzir desigualdade
  - Gerador determinístico NUnit com seed fixa/configurável gera LeaderboardKeys e verifica reflexividade, simetria, transitividade e alteração individual de cada campo
  - **Validates: Requirements 19.2, 19.5**
  - _Dependências: M1-T08_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [x] M1-T10 Configurar Unity Localization e String Tables
  - Unity Localization `1.5.12` fixado; samples não importados
  - Criada somente a String Table Collection `UI`, pois é a única com consumidores atuais; `Instructor`, `Penalties` e `HUD` foram adiadas até seus consumidores existirem
  - pt-BR configurado como Project Locale e locale inicial
  - en preparado sem traduções inventadas, com fallback seguro para pt-BR e sem seletor visível
  - Sete textos consumidos pelo Bootstrap/MainMenu migrados; apenas a mensagem emergencial mínima de infraestrutura permanece hardcoded
  - Inicialização e preload locais têm timeout configurável de 10 segundos por etapa; falha/timeout continua com fallback seguro e observa conclusões tardias
  - _Requirements: R28.4_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Localization funciona; strings carregadas localmente da table UI sem texto vazio_
  - _Testes: EditMode valida package/settings/table, timeout e conclusão tardia; PlayMode valida fluxo, fallback e chave ausente_
  - _Evidência: String exibida via Localization API, captura sanitizada e builds Android/iOS_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M1-T11 Configurar Remote Config (UGS) com feature flags base
  - Conectar UGS Remote Config
  - Criar flags iniciais: `enable_multiplayer`, `enable_championship`, `enable_school`, `enable_ads`
  - Implementar RemoteConfigManager que carrega flags no Bootstrap
  - _Requirements: R12.5 (auto-adjust params), R29.3 (Remote Config ativa templates)_
  - _Dependências: M1-T05_
  - _Critério de conclusão: Flags carregam do UGS e são acessíveis em código_
  - _Testes: Integration test com fallback para defaults_
  - _Evidência: Log de flags carregados_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + internet_
  - _Risco: Baixo_

- [ ] M1-T12 Executar teste de latência Photon (protocolo M0-T03)
  - Implementar script de ping para regiões Photon disponíveis conforme protocolo documentado em M0-T03
  - Medir latência de Brasília e ao menos 2 outras localizações BR (SP, RJ ou NE)
  - Documentar resultados em `docs/spikes/photon-region-latency.md` (atualizar protocolo com resultados)
  - _Requirements: R9.9_
  - _Dependências: M1-T04 (Photon SDK integrado), M0-T03 (protocolo definido)_
  - _Critério de conclusão: Tabela com ping médio por região, recomendação de região padrão_
  - _Testes: Script de medição executável_
  - _Evidência: Output de ping com timestamps_
  - _Validação humana: 🧑‍💻 Fundador valida se latência é aceitável_
  - _Ambiente: Unity Editor + rede BR real_
  - _Risco: Região "sa" pode não existir ou ter latência alta; plano B documentado_

- [ ] M1-T13 Executar validação de Unity Audio em dispositivo real (protocolo M0-T07)
  - Implementar cena de teste com audio sources, mixers e spatial audio conforme protocolo M0-T07
  - Testar em ao menos 1 Android real (e iOS se disponível)
  - Medir: latência de trigger, CPU usage, suporte a mixers simultâneos (motor + zebra + colisão + ambiente)
  - Atualizar `docs/spikes/unity-audio-requirements.md` com resultados reais
  - _Requirements: R12_
  - _Dependências: M1-T01, M0-T07 (protocolo), M0-T05 (dispositivos conhecidos)_
  - _Critério de conclusão: Audio funciona sem glitches audíveis, CPU < 2ms em dispositivo low_
  - _Testes: Manual em dispositivo real_
  - _Evidência: Screenshot de profiler + anotação "sem glitches"_
  - _Validação humana: 🧑‍💻 Fundador testa em dispositivo real_
  - _Ambiente: Dispositivo Android real_
  - _Risco: Latência pode ser alta em Android antigo; documentar workaround_

- [ ] M1-T14 Exit Gate M1
  - Projeto compila para Android e iOS sem erros (iOS via Xcode export — assinatura condicional)
  - Assemblies iniciais isolados e compilação incremental funciona
  - Property test NUnit com gerador determinístico executa e passa
  - Photon conecta (log de sucesso)
  - UGS auth + Cloud Save funciona
  - CI/CD dispara build Android em push para release/* (iOS build condicional à conta Apple)
  - Localization carrega strings
  - Remote Config carrega flags
  - Latência Photon medida e região recomendada
  - Unity Audio validado em dispositivo real
  - **Gate iOS Real Device:** NÃO exigido para passar M1. Exigido separadamente antes do primeiro teste real em iPhone e antes de qualquer TestFlight. Ausência de Apple Developer Program NÃO bloqueia Alpha Android.
  - _Critério de conclusão: Todos os itens obrigatórios verificados; iOS compilação sem erros (signing pendente se conta indisponível)_
  - _Validação humana: 🧑‍💻 Fundador verifica build Android funcional_

---

### M2 — Protótipo de Dirigibilidade (Vertical Slice)

> **Objetivo:** Vertical slice completo: kart dirigível em pista greybox com controles touch, start/finish line, checkpoints mínimos, TimingManagerLite (cronometragem básica + detecção de volta válida + display de tempo), validação com pilotos reais. O piloto COMPLETA uma volta cronometrada ANTES de M2 terminar.

- [ ] M2-T01 Implementar KartDynamics core (Custom Physics Layer)
  - Criar MonoBehaviour KartDynamics em RKW.Physics
  - Implementar: modelo de pneu simplificado (grip curve vs slip angle)
  - Implementar: eixo traseiro rígido com transferência de peso lateral
  - Implementar: lift-off da roda interna em curva
  - Implementar: forças longitudinais e laterais por eixo
  - Implementar: drag + coasting (desaceleração natural)
  - Toda a física em FixedUpdate (50 Hz)
  - Parâmetros lidos de KartCategorySO (ScriptableObject)
  - _Requirements: R4.1, R4.2, R4.3, R4.12, R4.13_
  - _Dependências: M1-T02, M1-T08_
  - _Critério de conclusão: Kart acelera, freia, curva com transferência de peso visível_
  - _Testes: EditMode tests para weight transfer, grip curve_
  - _Evidência: Vídeo de kart curvando com comportamento realístico_
  - _Validação humana: 🧑‍💻 Fundador + pilotos avaliam feel_
  - _Ambiente: Unity Editor (PlayMode)_
  - _Risco: Alto — calibração pode levar várias iterações; ScriptableObjects permitem ajuste rápido_

- [ ] M2-T02 Property test: Weight Transfer Monotonicity (Property 5)
  - **Property 5: Weight Transfer Monotonicity**
  - Para qualquer kart acima de threshold de velocidade, aumentar ângulo de esterço SHALL aumentar peso na roda externa traseira monotonicamente
  - Gerador determinístico NUnit gera: speed ∈ [threshold, maxSpeed], steer ∈ [0, maxSteer]
  - **Validates: Requirements 4.2**
  - _Dependências: M2-T01_

- [ ] M2-T03 Property test: Steering Speed Loss (Property 6)
  - **Property 6: Steering Speed Loss**
  - Para qualquer kart em velocidade elevada, aumentar ângulo de esterço SHALL aumentar perda de velocidade monotonicamente
  - **Validates: Requirements 4.3**
  - _Dependências: M2-T01_

- [ ] M2-T04 Implementar modelo de frenagem
  - Distribuição 70% rear / 30% front (parametrizável via SO)
  - Frenagem em reta: distância mínima
  - Frenagem com esterço: sobre-esterço proporcional
  - Bloqueio de pneu quando força > aderência
  - _Requirements: R4.4, R4.5_
  - _Dependências: M2-T01_
  - _Critério de conclusão: Frenagem em reta < frenagem com esterço (mensurável)_
  - _Testes: EditMode tests comparando distâncias_
  - _Evidência: Test output com distâncias comparadas_
  - _Validação humana: 🧑‍💻 Pilotos avaliam feedback de frenagem_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — calibração de bloqueio é sensível_

- [ ] M2-T05 Property test: Straight Braking Superiority (Property 7)
  - **Property 7: Straight Braking Superiority**
  - Para qualquer velocidade acima de threshold, stopping distance em reta < stopping distance com steer ≠ 0
  - **Validates: Requirements 4.4**
  - _Dependências: M2-T04_

- [ ] M2-T06 Property test: Brake-Steer Oversteer (Property 8)
  - **Property 8: Brake-Steer Oversteer**
  - Para qualquer velocidade e steer ≠ 0, adicionar frenagem SHALL aumentar lateral slip vs mesmo steer sem frenagem
  - **Validates: Requirements 4.5**
  - _Dependências: M2-T04_

- [ ] M2-T07 Implementar superfícies (grip modifiers)
  - Criar SurfaceDataSO com coeficientes por tipo (asphalt, grass, dirt, curb/zebra)
  - Implementar surface triggers que modificam grip no KartDynamics
  - Zebra desestabiliza proporcional a velocidade/ângulo
  - Grama/sujeira reduz aderência em ≥ 40%
  - _Requirements: R4.6, R4.7_
  - _Dependências: M2-T01_
  - _Critério de conclusão: Kart perde aderência visivelmente em grama, desestabiliza em zebra_
  - _Testes: EditMode test com grip multiplier assertions_
  - _Evidência: Test green + vídeo de kart em superfícies_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M2-T08 Property test: Surface Grip Reduction (Property 9)
  - **Property 9: Surface Grip Reduction**
  - Para qualquer kart state, grip em grama/dirt ≤ 60% do grip em asfalto seco
  - **Validates: Requirements 4.7**
  - _Dependências: M2-T07_

- [ ] M2-T09 Implementar colisões com perda de velocidade proporcional
  - Usar PhysX OnCollisionEnter para detectar contato
  - Calcular severidade contínua: f(velocidade_relativa, ângulo, massa)
  - Aplicar perda de velocidade = severidade * fator_categoria
  - NÃO acionar recovery por colisão (apenas registrar evento)
  - _Requirements: R4.10, R4.14_
  - _Dependências: M2-T01_
  - _Critério de conclusão: Colisão reduz velocidade proporcionalmente; nenhuma recovery acionada_
  - _Testes: EditMode test verifica escala contínua; PlayMode test verifica não-recovery_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M2-T10 Implementar recuperação segura (recovery)
  - Monitorar: imóvel > 4s, invertido > 85°, fora do perímetro, risco
  - Ao detectar: reposicionar kart no ponto de recovery mais próximo
  - Tornar não-colidível por 3 segundos após recovery
  - _Requirements: R4.11, R7.6, R7.7_
  - _Dependências: M2-T09_
  - _Critério de conclusão: Kart preso > 4s é reposicionado; colisão forte NÃO aciona_
  - _Testes: PlayMode test com cenários de stuck e colisão_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor (PlayMode)_
  - _Risco: Baixo_

- [ ] M2-T11 Property test: Recovery Trigger Conditions (Property 11)
  - **Property 11: Recovery Trigger Conditions**
  - Para qualquer evento de colisão (qualquer severidade), sistema NÃO aciona recovery
  - Recovery APENAS quando: stuck > 4s, invertido > 85°, fora do perímetro, risco
  - **Validates: Requirements 4.10, 4.11**
  - _Dependências: M2-T10 (recovery deve existir antes de testar)_

- [ ] M2-T12 Criar KartCategorySO para Escola (6.5 HP) e Rental Sport (13 HP)
  - Criar ScriptableObjects com parâmetros conforme tabela do design
  - Escola: 55 km/h max, accel 8s, aderência 1.0g
  - Rental Sport: 85 km/h max, accel 5s, aderência 1.2g
  - _Requirements: R5.1, R5.3_
  - _Dependências: M2-T01_
  - _Critério de conclusão: SOs criados, KartDynamics lê parâmetros corretamente_
  - _Testes: EditMode test verifica parâmetros carregados de SO_
  - _Evidência: Test green_
  - _Validação humana: 🧑‍💻 Pilotos validam feel por categoria_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo — valores são hipóteses, ajustáveis_

- [ ] M2-T13 Property test: Category Differentiation (Property 12)
  - **Property 12: Category Differentiation**
  - Para qualquer par de categorias distintas, ao menos maxSpeed, acceleration e lateralGrip diferem
  - **Validates: Requirements 5.3**
  - _Dependências: M2-T12_

- [ ] M2-T14 Implementar controles touch (joystick virtual + pedais)
  - Criar InputController em RKW.Controls com Unity Input System
  - Implementar joystick virtual no lado esquerdo (steering [-1,1])
  - Implementar pedal de acelerador no lado direito (throttle [0,1])
  - Implementar pedal de freio no lado direito (brake [0,1])
  - Implementar rampa de acelerador (≥ 150 ms para full throttle)
  - Soltar pedais → coasting (desaceleração natural)
  - _Requirements: R3.1, R3.3, R3.4, R3.5_
  - _Dependências: M2-T01_
  - _Critério de conclusão: Kart controlável por touch; acelerador progressivo_
  - _Testes: EditMode test para rampa de throttle_
  - _Evidência: Vídeo de controle touch funcional_
  - _Validação humana: 🧑‍💻 Fundador testa usabilidade_
  - _Ambiente: Dispositivo Android real_
  - _Risco: Médio — UX de touch precisa iteração_

- [ ] M2-T15 Property test: Throttle Ramp Rate Limit (Property 4)
  - **Property 4: Throttle Ramp Rate Limit**
  - Para qualquer sequência de inputs de throttle, output nunca aumenta mais rápido que 1/0.15 por segundo
  - **Validates: Requirements 3.5**
  - _Dependências: M2-T14_

- [ ] M2-T16 Implementar slipstream (vácuo)
  - Detectar kart à frente dentro de 1.5 comprimentos por ≥ 1 segundo
  - Reduzir drag progressivamente até 8% (parametrizável)
  - Drag reduction maior quanto mais próximo
  - _Requirements: R4.8_
  - _Dependências: M2-T01_
  - _Critério de conclusão: Drag reduz quando seguindo outro kart_
  - _Testes: EditMode test com distâncias variadas_
  - _Evidência: Test green mostrando redução progressiva_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M2-T17 Property test: Slipstream Drag Reduction Monotonicity (Property 10)
  - **Property 10: Slipstream Drag Reduction Monotonicity**
  - Para quaisquer d1 < d2 ambas dentro do range de ativação, drag reduction em d1 ≥ drag reduction em d2
  - **Validates: Requirements 4.8**
  - _Dependências: M2-T16_

- [ ] M2-T18 Criar pista greybox com start/finish line e checkpoints
  - Criar cena com geometria simples: circuito fechado com curvas variadas (lentas, médias, rápidas)
  - Adicionar colliders, surface triggers (asfalto, zebras, grama)
  - Incluir ao menos 1 seção de reta e 1 chicane
  - **Incluir: start/finish line trigger + mínimo 3 checkpoints (triggers)**
  - **Incluir: recovery points (posições de reposicionamento)**
  - NÃO precisa de arte final — apenas primitivas com materiais de cor
  - _Requirements: R16 (track base)_
  - _Dependências: M2-T07_
  - _Critério de conclusão: Circuito navegável com start/finish + checkpoints posicionados_
  - _Testes: PlayMode test: kart completa volta sem cair/glitches_
  - _Evidência: Screenshot da pista greybox com checkpoints visíveis_
  - _Validação humana: 🧑‍💻 Fundador faz voltas para sentir layout_
  - _Ambiente: Unity Editor + Android (build de teste)_
  - _Risco: Baixo — greybox é rápido de criar_

- [ ] M2-T19 Implementar TimingManagerLite (cronometragem básica para vertical slice)
  - Criar TimingManagerLite em RKW.Physics (ou RKW.Core temporariamente até M4 criar RKW.Timing)
  - Detectar passagem por start/finish line trigger
  - Detectar passagem por checkpoints (validar ordem)
  - Calcular tempo de volta (precisão: milissegundos)
  - Registrar: volta atual em curso, última volta completada, melhor volta da sessão
  - Validar volta: passou por todos checkpoints em ordem → volta válida
  - Invalidar volta: missed checkpoint → volta inválida (sem tempo registrado)
  - **Exibir tempo no HUD** (texto simples: último tempo + melhor tempo)
  - _Requirements: R20.1, R20.2_
  - _Dependências: M2-T18 (pista com checkpoints)_
  - _Critério de conclusão: Kart completa volta, tempo exibido, volta válida/inválida funciona_
  - _Testes: PlayMode test: kart cruza checkpoints em ordem → tempo registrado; skip checkpoint → inválida_
  - _Evidência: Test green + screenshot com tempo exibido_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo — lógica simples; evolui para TimingManager completo em M4_

- [ ] M2-T20 Checkpoint - Validar vertical slice com pilotos reais
  - Gerar build Android de teste com: kart + pista greybox + controles touch + cronometragem
  - **Vertical slice completo**: piloto dirige, completa voltas cronometradas, vê tempo na tela
  - Distribuir para ao menos 2 pilotos reais de kart
  - Coletar feedback: "parece kart?", responsividade, frenagem, curvas, esterço
  - Coletar avaliação numérica 0-10 por critério: realismo, responsividade, diversão, controles
  - Documentar feedback em `docs/playtests/M2-playtest-01.md`
  - Iterar calibração conforme feedback (ajustar ScriptableObjects)
  - _Critério de conclusão: Média ≥ 6/10, nenhum critério crítico abaixo de 5, aprovação qualitativa de ao menos 2 pilotos_
  - _Validação humana: 🧑‍💻 Fundador + pilotos reais obrigatório_
  - _Ambiente: Dispositivo Android real_
  - _Risco: Alto — se feel não convence, iterar mais em M2 antes de prosseguir_

- [ ] M2-T21 Exit Gate M2
  - Kart dirigível com física autêntica validada por pilotos
  - Controles touch funcionais em dispositivo real
  - Ao menos 8 property tests de física passando
  - 2 categorias (Escola + Rental Sport) diferenciáveis
  - Superfícies (asfalto, grama, zebra) alterando grip
  - Frenagem com/sem esterço diferenciada
  - Slipstream funcional
  - Recovery funcional (apenas por stuck/invertido)
  - **Kart completa volta cronometrada (TimingManagerLite)**
  - **Tempo de volta exibido na tela**
  - **Volta válida/inválida detectada (checkpoints)**
  - **Start/finish line funcional**
  - Playtest com pilotos: média ≥ 6/10, nenhum critério abaixo de 5, aprovação qualitativa de ao menos 2 pilotos
  - _Validação humana: 🧑‍💻 Fundador confirma exit gate baseado em playtest_

---

### M3 — Pista Fictícia e Performance Mobile

> **Objetivo:** Pista fictícia com arte mínima jogável, 3 perfis de qualidade, auto-adjust funcionando, performance validada em dispositivo Android real (30 FPS sustentados).

- [ ] M3-T01 Criar pista fictícia MVP com arte mínima
  - Substituir greybox por geometria low-poly com texturas básicas
  - Layout: ~1 km, curvas variadas, 3 setores, grid de 10 posições
  - Incluir: zebras, escape areas, grama, muros, postos de fiscal (placeholder)
  - Incluir: start/finish line, pit entry/exit (placeholder)
  - Configurar iluminação baked (preset "Dia" único)
  - Manter dentro de budget: ≤ 100K triângulos, ≤ 100 draw calls (tier low)
  - _Requirements: R16.1, R16.2, R16.7, R17.6, R12.6_
  - _Dependências: M2-T18 (evolui greybox)_
  - _Critério de conclusão: Pista jogável com visual minimamente aceitável_
  - _Testes: PlayMode test: kart completa volta; performance profiler_
  - _Evidência: Screenshot + profiler stats_
  - _Validação humana: 🧑‍💻 Fundador avalia visual/layout_
  - _Ambiente: Unity Editor + Android_
  - _Risco: Médio — arte pode levar mais tempo que esperado; priorizar gameplay sobre estética_

- [ ] M3-T02 Criar TrackConfigurationSO para a pista MVP
  - Criar assembly RKW.Track + RKW.Track.Tests (primeiro consumidor)
  - Criar ScriptableObject com: trackConfigurationId, trackId, direction (clockwise)
  - Definir: racing spline, ideal line, bot path, braking points
  - Definir: grid positions (10), start/finish line IDs, pit entry/exit IDs
  - Definir: checkpoints, 3 timing sectors, track limits, escape areas, recovery points
  - Usar IDs estáveis (não Transform direto) — bindings resolvidos em runtime
  - _Requirements: R16.2, R16.4, R16.5, R16.6_
  - _Dependências: M3-T01_
  - _Critério de conclusão: SO criado e carregado pelo sistema em runtime_
  - _Testes: EditMode test valida campos obrigatórios preenchidos_
  - _Evidência: SO inspecionável no editor + test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M3-T03 Criar EnvironmentPresetSO "Day" e TrackConditionSO "Dry"
  - EnvironmentPreset "Day": iluminação baked, skybox diurno, sem spotlights, crowd placeholder
  - TrackCondition "Dry": todos multipliers = 1.0 (baseline)
  - Integrar com sistema de loading de cena
  - _Requirements: R17.1, R17.6, R18.1, R18.5_
  - _Dependências: M3-T01_
  - _Critério de conclusão: Preset e condition carregados, multipliers aplicados_
  - _Testes: EditMode test: dry condition não altera grip (multiplier = 1.0)_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M3-T04 Implementar 3 perfis de qualidade (Low/Medium/High) + detecção automática
  - Criar QualityManager em RKW.Core
  - Definir perfis: Low (30 FPS target, ≤100 draw calls), Medium (60 FPS, ≤200), High (60 FPS, ≤350)
  - Implementar detecção automática baseada em SystemInfo (GPU, RAM)
  - Permitir override manual pelo jogador
  - _Requirements: R12.3_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Perfis alternam visual quality (shadow resolution, LOD, etc.)_
  - _Testes: EditMode test: perfil Low aplica settings corretos_
  - _Evidência: Test green + screenshot comparativo Low vs High_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M3-T05 Implementar auto-adjust de qualidade com histerese
  - WHEN média FPS em 3s < 28 → reduzir 1 nível
  - Upgrade ONLY WHEN: média 10s > 55 FPS AND cooldown 30s AND margem 5 FPS
  - Dynamic resolution scale: 70%–100%
  - _Requirements: R12.5_
  - _Dependências: M3-T04_
  - _Critério de conclusão: Sistema reduz qualidade quando FPS baixo, não oscila_
  - _Testes: EditMode test com dados simulados de FPS_
  - _Evidência: Test green demonstrando histerese_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — tuning de thresholds pode precisar ajuste_

- [ ] M3-T06 Property test: Quality Auto-Adjust with Hysteresis (Property 24)
  - **Property 24: Quality Auto-Adjust with Hysteresis**
  - Para qualquer histórico de FPS: downgrade quando 3s avg < 28; upgrade somente quando 10s avg > 55 AND cooldown 30s AND margem 5 FPS
  - **Validates: Requirements 12.5**
  - _Dependências: M3-T05_

- [ ] M3-T07 Implementar telemetria de performance (FPS, memória, thermal)
  - Coletar FPS a cada frame (rolling average)
  - Coletar memória usada (Profiler.GetTotalAllocatedMemoryLong)
  - Integrar Thermal Status API quando disponível (categorias: nominal/light/moderate/severe/critical)
  - Enviar amostras periódicas via Unity Analytics
  - _Requirements: R12.4_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Dados de FPS/memória/thermal visíveis no profiler e enviados_
  - _Testes: EditMode test: telemetria collector produz samples válidos_
  - _Evidência: Unity Analytics dashboard com dados_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + Android real_
  - _Risco: Baixo — Thermal API pode não estar disponível em todos dispositivos_

- [ ] M3-T08 Performance test em dispositivo Android real
  - Gerar build para dispositivo low-tier da matriz (M0-T05)
  - Rodar 30 minutos de gameplay (voltas contínuas com 10 karts)
  - Medir: FPS médio, FPS mínimo, temperatura, memória
  - Comparar com budget: ≥ 30 FPS sustentados
  - Documentar em `docs/playtests/M3-performance-01.md`
  - _Requirements: R12.1_
  - _Dependências: M3-T01, M3-T04, M2-T01_
  - _Critério de conclusão: 30 FPS sustentados por 30 min no dispositivo low_
  - _Testes: Profiling session de 30 min_
  - _Evidência: Profiler data + screenshot com FPS graph_
  - _Validação humana: 🧑‍💻 Fundador executa no dispositivo real_
  - _Ambiente: Dispositivo Android real (low tier)_
  - _Risco: Alto — se não atingir 30 FPS, otimizar antes de avançar_

- [ ] M3-T09 Integrar Firebase Crashlytics
  - Adicionar Firebase SDK (Crashlytics only)
  - Configurar crash reporting automático
  - Testar: forçar crash → verificar no Firebase console
  - _Requirements: R12.4 (telemetria)_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Crash aparece no Firebase Console_
  - _Testes: Forçar exception → verify crash report_
  - _Evidência: Screenshot do Firebase console_
  - _Validação humana: não_
  - _Ambiente: Android real + Firebase Console_
  - _Risco: Baixo_

- [ ] M3-T10 Exit Gate M3
  - Pista fictícia jogável com arte mínima
  - 3 perfis de qualidade funcionando
  - Auto-adjust com histerese funcionando
  - 30 FPS sustentados por 30 min em Android low-tier
  - Crashlytics integrado
  - Telemetria de performance coletando dados
  - TrackConfigurationSO + EnvironmentPresetSO + TrackConditionSO criados
  - _Validação humana: 🧑‍💻 Fundador valida performance em dispositivo real_

---

### M4 — Cronometragem Completa, Setores, Ghost e Bots

> **Objetivo:** Evoluir TimingManagerLite (M2) para TimingManager completo com setores, delta, volta ideal, ghost pessoal. 5 perfis de bot navegando a pista com a mesma física. A evolução é incremental — NÃO duplicar o sistema de M2.

- [ ] M4-T01 Evoluir TimingManagerLite → TimingManager completo
  - Criar assembly RKW.Timing + RKW.Timing.Tests (primeiro consumidor)
  - Migrar/evoluir TimingManagerLite para TimingManager em RKW.Timing
  - Adicionar: cronometragem por setor (3 setores conforme TrackConfigurationSO)
  - Adicionar: precisão interna em microsegundos
  - Registrar: volta atual, última volta, melhor volta pessoal, melhor volta da sessão
  - Manter validação de voltas (checkpoints em ordem) já implementada em M2
  - Invalidar voltas com motivo expandido (track limits, missed checkpoint)
  - _Requirements: R20.1, R20.2, R20.3_
  - _Dependências: M2-T19 (TimingManagerLite), M3-T02 (setores definidos na TrackConfiguration)_
  - _Critério de conclusão: Tempos por setor registrados corretamente com validação expandida_
  - _Testes: EditMode tests para timing calculations; PlayMode test para fluxo completo_
  - _Evidência: Tests green + log de tempos por setor_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo — evolução incremental de M2_

- [ ] M4-T02 Implementar DeltaCalculator (comparação de setores)
  - Criar DeltaCalculator em RKW.Timing
  - Após cada setor: calcular delta vs referência (melhor volta pessoal)
  - Convenção: negativo = mais rápido (verde), positivo = mais lento (vermelho)
  - Exibir no HUD com sinal, ícone direcional E cor (acessibilidade)
  - _Requirements: R21.1, R21.2, R21.3, R21.4_
  - _Dependências: M4-T01_
  - _Critério de conclusão: Delta exibido corretamente após cada setor_
  - _Testes: EditMode test para cálculo de delta_
  - _Evidência: Test green + screenshot do HUD com delta_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T03 Property test: Sector Delta Calculation (Property 15)
  - **Property 15: Sector Delta Calculation**
  - Para qualquer conjunto de tempos de referência e tempos atuais, delta = actual - reference, corretamente sinalizado
  - **Validates: Requirements 6.5**
  - _Dependências: M4-T02_

- [ ] M4-T04 Implementar IdealLapCalculator (volta ideal teórica)
  - Criar IdealLapCalculator em RKW.Timing
  - Calcular: soma dos melhores setores válidos pessoais para o MESMO LeaderboardKey
  - NÃO combinar setores de keys diferentes ou voltas inválidas
  - Exibir pós-corrida: tempo ideal + quais setores compõem
  - _Requirements: R22.1, R22.2, R22.3_
  - _Dependências: M4-T01, M1-T08 (LeaderboardKey)_
  - _Critério de conclusão: Volta ideal calculada corretamente_
  - _Testes: EditMode test com dados variados_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T05 Property test: Theoretical Ideal Lap Correctness (Property 30)
  - **Property 30: Theoretical Ideal Lap Correctness**
  - Para qualquer conjunto de lap records com mesmo LeaderboardKey, ideal = Σ min(sector_i_valid)
  - Setores de keys diferentes ou voltas inválidas NUNCA contribuem
  - **Validates: Requirements 22.1, 22.3**
  - _Dependências: M4-T04_

- [ ] M4-T06 Implementar GhostRecorder e GhostPlayer
  - Criar assembly RKW.Telemetry (primeiro consumidor)
  - Criar GhostRecorder em RKW.Telemetry: gravar amostras (position + rotation) a 30 Hz
  - Associar ghost à LeaderboardKey
  - Criar GhostPlayer: reproduzir amostras com interpolação
  - Ghost SEM colisão, SEM interferência física
  - Armazenar localmente (1 ghost por LeaderboardKey)
  - Limite de tamanho: ~50KB comprimido por ghost
  - _Requirements: R24.1, R24.3, R24.4, R24.5_
  - _Dependências: M4-T01, M1-T08_
  - _Critério de conclusão: Ghost grava e reproduz volta corretamente_
  - _Testes: PlayMode test: ghost não interfere na física; EditMode test: tamanho ≤ 50KB_
  - _Evidência: Vídeo de ghost visível + test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T07 Property test: Ghost Zero Physics Interference (Property 31)
  - **Property 31: Ghost Zero Physics Interference**
  - Presença/ausência de ghost produz zero diferença no estado físico de qualquer kart
  - **Validates: Requirements 24.3**
  - _Dependências: M4-T06_

- [ ] M4-T08 Implementar sistema de Bots (AI)
  - Criar assembly RKW.Bots + RKW.Bots.Tests (primeiro consumidor)
  - Criar BotNavigator em RKW.Bots: segue waypoint spline com variação
  - Criar BotProfileSO com 5 perfis: iniciante, cauteloso, equilibrado, agressivo limpo, rápido
  - Bots usam a MESMA KartDynamics (mesma física do humano)
  - Implementar ErrorInjector: erros dentro de tolerâncias por perfil
  - Decision layer: brake point, overtake evaluator (básico)
  - Budget: < 0.5 ms/frame por bot, total < 5 ms para 9 bots
  - _Requirements: R8.1, R8.2, R8.3, R8.4_
  - _Dependências: M2-T01, M3-T02 (bot path na TrackConfiguration)_
  - _Critério de conclusão: 9 bots completam voltas sem colisões absurdas, tempos variados por perfil_
  - _Testes: PlayMode test: bot completa N voltas; performance profiler < 5ms_
  - _Evidência: Vídeo de bots correndo + profiler stats_
  - _Validação humana: 🧑‍💻 Fundador avalia: "bots parecem humanos?"_
  - _Ambiente: Unity Editor + Android (performance)_
  - _Risco: Alto — calibração de bots convincentes é iterativa_

- [ ] M4-T09 Property test: Bot Error Within Tolerance (Property 19)
  - **Property 19: Bot Error Within Tolerance**
  - Para qualquer perfil de bot e erro intencional, magnitude ∈ [min, max] do perfil
  - **Validates: Requirements 8.3**
  - _Dependências: M4-T08_

- [ ] M4-T10 Implementar ConsistencyCalculator
  - Criar ConsistencyCalculator em RKW.Timing
  - Calcular: melhor volta, média, desvio padrão, range (max-min), voltas dentro de margem
  - Apenas para voltas válidas com mesmo LeaderboardKey
  - Exibir básico pós-corrida
  - _Requirements: R26.1, R26.2, R26.3_
  - _Dependências: M4-T01_
  - _Critério de conclusão: Métricas corretas para N voltas simuladas_
  - _Testes: EditMode test com dados conhecidos_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T11 Property test: Consistency Calculation Correctness (Property 33)
  - **Property 33: Consistency Calculation Correctness**
  - Para N tempos válidos com mesmo LeaderboardKey: mean = sum/N, stddev = sqrt(Σ(t-mean)²/(N-1)), range = max-min
  - **Validates: Requirements 26.1, 26.2**
  - _Dependências: M4-T10_

- [ ] M4-T12 Implementar LapValidator com track limits
  - Detectar 4 rodas fora do traçado (track limit triggers)
  - Invalidar volta quando track limits excedidos conforme regra
  - Registrar motivo de invalidação
  - Registrar penalidade de tempo se aplicável (3s)
  - _Requirements: R7.4, R20.2_
  - _Dependências: M4-T01, M3-T02_
  - _Critério de conclusão: Volta corretamente invalidada ao exceder limites_
  - _Testes: PlayMode test: kart corta → volta inválida_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T13 Implementar persistência de ghost (integração com TimingManager)
  - Salvar ghost da melhor volta automaticamente ao bater PB
  - Carregar ghost salvo ao iniciar sessão de Tomada de Tempo
  - Integrar GhostRecorder com TimingManager (start/stop recording por volta)
  - _Requirements: R24.1, R24.4_
  - _Dependências: M4-T06, M4-T01_
  - _Critério de conclusão: Ghost persiste entre sessões e é reproduzido_
  - _Testes: PlayMode test: PB → ghost salvo → reload → ghost plays_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M4-T14 Checkpoint - Validar vertical slice com timing completo
  - Teste integrado: kart humano + 9 bots completam 10 voltas
  - Timing registra todas voltas/setores
  - Ghost grava melhor volta
  - Bots variam por perfil
  - Delta funciona corretamente
  - Performance dentro do budget (30 FPS em Android low)
  - _Critério de conclusão: Vertical slice jogável end-to-end com timing completo_
  - _Validação humana: 🧑‍💻 Fundador faz corrida completa contra bots_

- [ ] M4-T15 Exit Gate M4
  - TimingManager completo com 3 setores funcional e preciso (evoluído do M2 TimingManagerLite)
  - Delta exibido após cada setor (verde/vermelho + sinal + ícone)
  - Volta ideal teórica calculada pós-corrida
  - Ghost pessoal grava/reproduz sem interferência física, com persistência
  - 5 perfis de bot completam voltas convincentemente
  - Performance: 10 karts + timing + ghost dentro de budget
  - Volta validada/invalidada por track limits
  - Consistência calculada pós-corrida
  - _Validação humana: 🧑‍💻 Fundador valida vertical slice completo_

---

### M5 — Multiplayer Shared Mode

> **Objetivo:** 2-4 humanos + bots jogam juntos via Photon Fusion 2 (Shared Mode). State sync funcional, interpolação suave, reconexão básica. Rankings marcados NÃO-OFICIAIS. Multiplayer INICIA somente após física, timing e vertical slice estáveis (depende de Exit Gate M4).

- [ ] M5-T01 Implementar NetworkTransport com Photon Fusion 2 (Shared Mode)
  - Criar assembly RKW.Network + RKW.Network.Tests (primeiro consumidor)
  - Implementar INetworkTransport com Photon Fusion 2
  - Cada client tem State Authority sobre seu próprio kart
  - Sync de estado: position (Vector3 quantized), rotation (compressed quaternion), velocity, input
  - Tick rate conforme ADR de networking (30 Hz)
  - Separar reliable (flags, penalidades) e unreliable (posição, rotação)
  - _Requirements: R9.1, R9.2, R9.4_
  - _Dependências: M1-T04, M2-T01, **Exit Gate M4 (física, timing e vertical slice estáveis)**_
  - _Critério de conclusão: 2 clientes veem karts mutuamente em posições corretas_
  - _Testes: PlayMode test com 2 runners locais (Photon Shared Mode)_
  - _Evidência: Vídeo de 2 karts sincronizados_
  - _Validação humana: não_
  - _Ambiente: Unity Editor (2 instâncias) + LAN_
  - _Risco: Alto — sync de física customizada pode ter jitter_

- [ ] M5-T02 Implementar interpolação e predição para karts remotos
  - Interpolar posição/rotação de karts não-locais entre ticks recebidos
  - Suavizar teleportes quando pacotes atrasam
  - Testar com latência simulada (50ms, 100ms, 200ms)
  - _Requirements: R9.4_
  - _Dependências: M5-T01_
  - _Critério de conclusão: Karts remotos se movem suavemente sem teleporte visível_
  - _Testes: PlayMode test com latência artificial_
  - _Evidência: Vídeo sem teleportes com 100ms latência_
  - _Validação humana: não_
  - _Ambiente: Unity Editor (2 instâncias) + Clumsy para simular latência_
  - _Risco: Alto — tuning de interpolação é sensível_

- [ ] M5-T03 Implementar matchmaking e salas (lobby) com room codes seguros
  - Implementar criação de sala com:
    - **Identificador interno forte (UUID)** para referência do sistema
    - **Código amigável de 6 caracteres alfanuméricos** para compartilhamento
    - **Verificação de colisão**: ao gerar código, verificar se já existe sala ativa com esse código
    - **Retry com novo código** se colisão detectada (NÃO assumir unicidade por aleatoriedade)
  - Implementar join por código amigável (resolve para UUID internamente)
  - Implementar matchmaking rápido (encontrar sala compatível ou criar)
  - Fill de bots quando timeout 60s sem mínimo de jogadores
  - Máximo 10 participantes (4 humanos + 6 bots no MVP)
  - _Requirements: R1.1, R2.4, R2.5, R9.10_
  - _Dependências: M5-T01_
  - _Critério de conclusão: 2+ jogadores entram na mesma sala via código; colisão de código tratada_
  - _Testes: Integration test: create room → join → verify participants; generate 10K codes → verify uniqueness after collision check_
  - _Evidência: Log de 2 jogadores na mesma sala + test de colisão passando_
  - _Validação humana: não_
  - _Ambiente: Unity Editor (2 instâncias) + internet_
  - _Risco: Médio — Photon room management_

- [ ] M5-T04 Property test: Room Code Collision Safety (Property 27)
  - **Property 27: Room Code Collision Safety**
  - Para qualquer batch de N códigos gerados com verificação de colisão, todos têm 6 chars alfanuméricos e são únicos dentro do conjunto de salas ativas
  - Geração com colisão detectada → retry → código final não colide com salas ativas
  - NÃO afirmar unicidade global por aleatoriedade — verificar contra salas existentes
  - **Validates: Requirements 2.4**
  - _Dependências: M5-T03_

- [ ] M5-T05 Property test: Session Participant Invariant (Property 2)
  - **Property 2: Session Participant Invariant**
  - Para qualquer sequência de join/leave, participantes (humanos + bots) NUNCA > 10
  - **Validates: Requirements 1.2**
  - _Dependências: M5-T03_

- [ ] M5-T06 Implementar reconexão básica (< 30s → retoma bot)
  - Ao desconectar: substituir por bot com habilidade compatível
  - Ao reconectar (< 30s): reintegrar piloto na posição do bot
  - Após 30s: resultado parcial registrado
  - _Requirements: R1.8, R8.5, R8.6, R9.8_
  - _Dependências: M5-T01, M4-T08 (bots)_
  - _Critério de conclusão: Desconexão → bot assume → reconexão retoma controle_
  - _Testes: PlayMode test: simulate disconnect → reconnect_
  - _Evidência: Log de substituição e retomada_
  - _Validação humana: não_
  - _Ambiente: Unity Editor (com kill de conexão simulado)_
  - _Risco: Médio_

- [ ] M5-T07 Testar multiplayer com latência real e métricas objetivas
  - Usar região Photon selecionada em M1-T12
  - 2-4 jogadores em locais diferentes jogam sessão completa
  - **Métricas objetivas obrigatórias:**
    - Mediana de latência e P95
    - Jitter (variação de latência)
    - Packet loss (%)
    - Bandwidth por jogador (KB/s)
    - Correção máxima de posição (threshold de teleporte)
    - Desconexões por sessão
    - Resultados em Wi-Fi vs 4G (quando possível)
  - Avaliação visual: teleporte visível sim/não, suavidade de interpolação
  - Documentar em `docs/playtests/M5-network-test-01.md`
  - _Requirements: R9.9_
  - _Dependências: M5-T02, M1-T12_
  - _Critério de conclusão: Sessão jogável; mediana latência ≤ 100ms; P95 ≤ 200ms; packet loss < 2%; sem teleporte visível_
  - _Testes: Manual com 2+ jogadores remotos + coleta automatizada de métricas_
  - _Evidência: Documento com medições numéricas + avaliação visual_
  - _Validação humana: 🧑‍💻 Fundador + ao menos 1 jogador remoto_
  - _Ambiente: Dispositivos reais + internet BR_
  - _Risco: Alto — latência pode exigir tuning_

- [ ] M5-T08 Exit Gate M5
  - 2-4 humanos + bots jogam juntos sem crash
  - Interpolação suave (sem teleporte com ≤ 150ms)
  - Reconexão funcional em < 30s
  - Salas privadas com código funcionam (com verificação de colisão)
  - Bandwidth ≤ 10 KB/s por jogador
  - Sessão completa (10 voltas) sem desync fatal
  - **Métricas de rede documentadas:** mediana, P95, jitter, packet loss, bandwidth
  - **Depende de Exit Gate M4 cumprido**
  - _Validação humana: 🧑‍💻 Fundador joga sessão remota com amigo_

---

### M6 — Fluxo de Corrida e Direção de Prova

> **Objetivo:** Implementar a máquina de estados completa da corrida (Lobby→Qualifying→Grid→Starting→Racing→Finished→Results), tomada de tempo com 3 tentativas, largada parada com semáforo configurável, sistema de bandeiras, engine de penalidades explicável, timeout de fim de corrida (60s), tela de resultados e HUD modo Essencial. A corrida completa deve funcionar end-to-end com regras esportivas.

- [ ] M6-T01 Implementar Race State Machine completa
  - Criar assembly RKW.Race + RKW.Race.Tests (primeiro consumidor)
  - Implementar estados: Lobby → Qualifying → GridFormation → Starting → Racing → Finished → Results
  - Transições conforme design: min players/timeout → Qualifying, 3 tentativas consumidas → Grid, grid ready → Starting, lights out → Racing, líder completa volta 10 → Finished, all finish/timeout → Results
  - Integrar com TimingManager (M4) e NetworkTransport (M5)
  - Cada estado com duração e ações conforme design document
  - _Requirements: R1.1, R1.3, R1.4, R1.5, R6_
  - _Dependências: M4-T01 (TimingManager), M5-T01 (NetworkTransport), M4-T08 (Bots)_
  - _Critério de conclusão: State machine executa fluxo completo sem erros em PlayMode_
  - _Testes: PlayMode test: fluxo Lobby→Results sem crash; EditMode test: transições válidas_
  - _Evidência: Test green + log de transições_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Alto — integração de múltiplos subsistemas_

- [ ] M6-T02 Implementar Qualifying (1 out-lap + 3 tentativas cronometradas)
  - Volta de saída não cronometrada (out-lap)
  - 3 tentativas cronometradas; voltas inválidas CONTAM como tentativa mas NÃO registram tempo
  - Grid ordenado por melhor tempo válido; pilotos sem tempo válido → final do grid
  - Pilotos desconectados: grid baseado no melhor tempo já registrado
  - Integrar com TimingManager para validação de volta
  - _Requirements: R1.3, R1.11, R1.12_
  - _Dependências: M6-T01, M4-T01, M4-T12_
  - _Critério de conclusão: 10 pilotos (humanos+bots) completam qualifying e grid é ordenado corretamente_
  - _Testes: EditMode test: ordenação de grid com dados variados; PlayMode test: fluxo completo_
  - _Evidência: Test green + log de grid formado_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — lógica de invalidação precisa estar alinhada com M4_

- [ ] M6-T03 Property test: Grid Ordering by Best Qualifying Time (Property 3)
  - **Property 3: Grid Ordering by Best Qualifying Time**
  - Para qualquer conjunto de tempos de classificação de N pilotos, o grid resultante SHALL ser ordenado ascendentemente pelo melhor tempo válido de cada piloto
  - Pilotos sem tempo válido SHALL estar no final do grid
  - Gerador determinístico NUnit gera conjuntos de tempos variados (incluindo pilotos sem tempo válido)
  - **Validates: Requirements 1.3**
  - _Dependências: M6-T02_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M6-T04 Implementar Standing Start (semáforo configurável)
  - Sequência de luzes configurável pelo Ruleset/StartProcedure (hipótese inicial: 5 luzes)
  - Detecção de queima de largada (false start): movimento antes de lights out
  - Penalidade por false start: drive-through ou tempo (configurável)
  - Número de luzes parametrizável via ScriptableObject (StartProcedureSO)
  - _Requirements: R34.1_
  - _Dependências: M6-T01_
  - _Critério de conclusão: Semáforo exibe luzes sequenciais, detecta false start, penaliza_
  - _Testes: PlayMode test: largada normal funciona; false start detectada_
  - _Evidência: Test green + vídeo de largada_
  - _Validação humana: 🧑‍💻 Fundador avalia timing de luzes_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M6-T05 Implementar Flag System (bandeiras)
  - Implementar detecção e sinalização: verde, amarela local, vermelha, azul, branca, preta, quadriculada
  - Amarela local: por setor, restrições conforme RaceRuleset (sem limite fixo de velocidade)
  - Vermelha: interrupção de corrida por incidente crítico
  - Azul: informar retardatário sobre líder se aproximando
  - Preta: infração grave, retorno ao box em até 1 volta
  - Quadriculada: líder completou 10ª volta
  - _Requirements: R7.1, R7.2, R7.5_
  - _Dependências: M6-T01, M4-T12_
  - _Critério de conclusão: Todas as bandeiras funcionam conforme regras_
  - _Testes: PlayMode test: cenários de cada bandeira; EditMode test: lógica de ativação_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — integração com penalty engine_

- [ ] M6-T06 Property test: Yellow Flag Penalty Minimum (Property 17)
  - **Property 17: Yellow Flag Penalty Minimum**
  - Para qualquer evento de ultrapassagem detectado durante bandeira amarela ativa no setor, a penalidade de tempo aplicada SHALL ser ≥ 3 segundos
  - Gerador determinístico NUnit gera: eventos de ultrapassagem com amarela ativa, varia posição/setor/velocidade
  - **Validates: Requirements 7.3**
  - _Dependências: M6-T05_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M6-T07 Implementar Penalty Engine (Direção de Prova explicável)
  - Separar: detecção automática de infrações → decisão → feedback ao piloto
  - Tipos: track limits (4 rodas fora), ultrapassagem sob amarela, false start, contato com culpa, cortar pista
  - Cada penalidade registrada com: tipo, momento (lap+setor+timestamp), regra, evidência, punição, consequência, origem
  - Colisões ambíguas → NÃO aplicar penalidade automática, registrar para investigação
  - _Requirements: R7.3, R7.4, R7.8, R7.9, R31.1, R31.2_
  - _Dependências: M6-T05, M4-T12_
  - _Critério de conclusão: Penalidades aplicadas corretamente com metadata completa_
  - _Testes: EditMode test: metadata completeness; PlayMode test: cenários de penalidade_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — classificação de colisões ambíguas requer tuning_

- [ ] M6-T08 Property test: Ambiguous Collision Non-Penalty (Property 18)
  - **Property 18: Ambiguous Collision Non-Penalty**
  - Para qualquer colisão classificada como ambígua conforme critério da Direção de Prova (incidente de corrida com trajetórias convergentes onde culpa é unclear), NENHUMA penalidade automática SHALL ser aplicada; evento SHALL ser registrado para investigação
  - Gerador determinístico NUnit gera: cenários de colisão com parâmetros variados (ângulo, velocidade, trajetórias)
  - **Validates: Requirements 7.9**
  - _Dependências: M6-T07_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M6-T09 Property test: Penalty Metadata Completeness (Property 34)
  - **Property 34: Penalty Metadata Completeness**
  - Para qualquer evento de penalidade gerado pela Direção de Prova, o registro SHALL conter TODOS os campos obrigatórios: tipo, momento (lap+setor+timestamp), regra aplicada, valor de punição, descrição de consequência e origem (automática/manual). Nenhum campo obrigatório SHALL ser null ou vazio
  - Gerador determinístico NUnit gera: penalidades aleatórias com todos os tipos possíveis
  - **Validates: Requirements 31.1**
  - _Dependências: M6-T07_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M6-T10 Implementar Race End com timeout 60s
  - Líder cruza linha na volta 10 → bandeira quadriculada
  - Cada piloto restante recebe quadriculada ao cruzar a linha pela próxima vez
  - Timeout de 60 segundos (configurável via Remote Config) após quadriculada
  - Pilotos que não cruzarem no timeout → classificados por: voltas(desc) + tempo(asc)
  - Se humanos < 1 → encerrar sessão, registrar resultado parcial
  - _Requirements: R1.4, R1.5, R1.6, R1.9_
  - _Dependências: M6-T01, M6-T05_
  - _Critério de conclusão: Corrida termina corretamente com stragglers classificados_
  - _Testes: PlayMode test: líder termina → timeout → classificação; EditMode test: lógica de classificação_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M6-T11 Implementar Result Screen
  - Exibir: posição final, melhor volta, penalidades acumuladas, delta XP, evolução de licença
  - Exibir: setores, volta ideal teórica, consistência
  - Preparar área para envio de resultado ao backend (stub até M8)
  - _Requirements: R1.7, R35.3_
  - _Dependências: M6-T10, M4-T02, M4-T04, M4-T10_
  - _Critério de conclusão: Tela de resultado exibe todos os dados necessários_
  - _Testes: PlayMode test: tela exibe dados após corrida_
  - _Evidência: Screenshot da tela de resultado_
  - _Validação humana: 🧑‍💻 Fundador avalia layout/clareza_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M6-T12 Implementar HUD Modo Essencial
  - Durante corrida exibir APENAS: posição, volta, tempo atual, delta simples, bandeira ativa, piloto próximo, indicadores de pedal/volante
  - Pós-volta: tempo, delta vs melhor, melhor volta, status válida/inválida
  - NÃO exibir toda telemetria durante pilotagem
  - _Requirements: R35.1, R35.2, R35.5_
  - _Dependências: M6-T01, M4-T02_
  - _Critério de conclusão: HUD Essencial funcional com informações mínimas corretas_
  - _Testes: PlayMode test: HUD exibe dados corretos por estado_
  - _Evidência: Screenshot do HUD durante corrida_
  - _Validação humana: 🧑‍💻 Fundador avalia legibilidade_
  - _Ambiente: Unity Editor + Android_
  - _Risco: Baixo_

- [ ] M6-T13 Checkpoint — Corrida completa end-to-end com regras
  - Teste integrado: 10 participantes (humanos+bots) completam qualifying + corrida 10 voltas
  - Bandeiras, penalidades e estado machine funcionam end-to-end
  - Resultado exibido corretamente
  - HUD Essencial legível durante corrida
  - Performance dentro do budget
  - _Critério de conclusão: Corrida jogável end-to-end com regras esportivas_
  - _Validação humana: 🧑‍💻 Fundador joga corrida completa com regras_

- [ ] M6-T14 Exit Gate M6
  - State machine completa funcional (Lobby→Results)
  - Qualifying com 3 tentativas e grid ordenado
  - Standing start com semáforo configurável e detecção de false start
  - Flag system completo (verde, amarela, vermelha, azul, branca, preta, quadriculada)
  - Penalty engine explicável com metadata completa
  - Race end com timeout 60s e classificação de stragglers
  - Result screen com dados completos
  - HUD Essencial funcional
  - Property tests 3, 17, 18, 34 passando
  - _Validação humana: 🧑‍💻 Fundador confirma corrida completa funcional_

---

### M7 — Escola, Instrutor e Progressão

> **Objetivo:** Implementar os 10 módulos da escola de pilotagem com desbloqueio progressivo, ideal line com fade, instrutor de pilotagem (texto+visual, pt-BR via String Tables), feedback por setor, prova de licença, calculador de XP, índice de pilotagem limpa, caderno do piloto (versão simples) e persistência de perfil. O piloto completa a escola e obtém licença.

- [ ] M7-T01 Implementar SchoolManager com 10 módulos e desbloqueio progressivo
  - Criar assembly RKW.School (primeiro consumidor)
  - Implementar SchoolManager: gerencia 10 módulos com desbloqueio sequencial
  - Módulos definidos em ScriptableObjects (SchoolModuleSO)
  - Desbloqueio: módulo N requer módulo N-1 completo
  - Cada módulo com: briefing, objetivo, critérios de aprovação, feedback
  - Persistir progresso via Cloud Save (schoolProgress)
  - _Requirements: R6.1_
  - _Dependências: M1-T05 (Cloud Save), M2-T01 (KartDynamics), M4-T01 (TimingManager)_
  - _Critério de conclusão: 10 módulos navegáveis com desbloqueio sequencial_
  - _Testes: EditMode test: desbloqueio sequencial; PlayMode test: completar módulo avança_
  - _Evidência: Test green + screenshot de progressão_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — conteúdo dos módulos requer design pedagógico_

- [ ] M7-T02 Implementar Ideal Line com fade progressivo
  - Exibir linha ideal 100% visível no início
  - Após 50% dos módulos anteriores completados: reduzir opacidade em 50%
  - Após todos os módulos anteriores completados: ocultar completamente
  - Implementar via shader/material com alpha parametrizável
  - _Requirements: R6.2, R6.3, R6.4_
  - _Dependências: M7-T01, M3-T02 (ideal line definida na TrackConfiguration)_
  - _Critério de conclusão: Linha ideal faz fade conforme progressão_
  - _Testes: EditMode test: cálculo de opacidade vs progresso_
  - _Evidência: Test green + screenshots com diferentes níveis de fade_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M7-T03 Implementar DrivingInstructor (texto + visual, pt-BR, String Tables)
  - Criar sistema de instrutor conforme design: Performance Analyzer → Rules Engine → Priority Queue → Cooldown Filter → Output
  - Mensagens: "Freie em linha reta", "Você freiou tarde", "Solte o freio antes do ápice", etc.
  - Regras de UX: cooldown 5s, max 4 mensagens/volta, prioridade (segurança > técnica > informação)
  - Supressão durante comunicação da direção de prova
  - TODOS os textos via Unity Localization / String Tables (zero hardcoded)
  - Locale pt-BR como único idioma MVP
  - Opção de desabilitar nas settings
  - _Requirements: R28.1, R28.2, R28.3, R28.4_
  - _Dependências: M1-T10 (Localization), M4-T01 (setor data para feedback)_
  - _Critério de conclusão: Instrutor exibe mensagens contextuais com cooldown e prioridade_
  - _Testes: EditMode test: cooldown respeitado, prioridade correta; PlayMode test: mensagens aparecem_
  - _Evidência: Test green + screenshot de mensagem do instrutor_
  - _Validação humana: 🧑‍💻 Fundador avalia utilidade das mensagens_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — calibração de trigger conditions_

- [ ] M7-T04 Implementar Sector Feedback (delta + diagnóstico específico)
  - Após cada setor: exibir delta vs referência
  - Diagnóstico específico: frenagem antecipada/tardia, entrada excessiva, ápice perdido, aceleração antecipada/tardia
  - Integrar com DeltaCalculator (M4) e DrivingInstructor
  - _Requirements: R6.5_
  - _Dependências: M4-T02 (DeltaCalculator), M7-T03_
  - _Critério de conclusão: Feedback específico exibido após cada setor na escola_
  - _Testes: EditMode test: diagnóstico correto para cenários conhecidos_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Médio — mapeamento de erros de pilotagem para diagnóstico_

- [ ] M7-T05 Implementar prova de licença
  - Exame final por categoria: tempo válido ≤ threshold + todas voltas válidas → licença concedida
  - Se não atingir: exibir setores com maior déficit e sugerir módulos de revisão
  - Licença persiste no perfil (Cloud Save: licenses)
  - _Requirements: R6.6, R6.7_
  - _Dependências: M7-T01, M4-T01_
  - _Critério de conclusão: Licença concedida/negada corretamente com feedback_
  - _Testes: EditMode test: lógica de concessão; PlayMode test: fluxo completo_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M7-T06 Property test: License Granting Logic (Property 16)
  - **Property 16: License Granting Logic**
  - Para qualquer resultado de exame de licença, a licença SHALL ser concedida se e somente se o tempo de volta ≤ threshold E todas as voltas são válidas
  - Gerador determinístico NUnit gera: tempos variados, status de volta (válida/inválida), thresholds
  - **Validates: Requirements 6.6**
  - _Dependências: M7-T05_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M7-T07 Property test: License Gating (Property 13)
  - **Property 13: License Gating**
  - Para qualquer estado de licença do jogador, acesso à categoria N requer posse da licença para categoria N-1. Jogador sem licença N-1 NÃO pode entrar em sessão de categoria N
  - Gerador determinístico NUnit gera: estados de licença variados, tentativas de acesso a categorias
  - **Validates: Requirements 5.4**
  - _Dependências: M7-T05_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M7-T08 Implementar XPCalculator
  - Calcular XP após corrida: base + position_bonus + clean_bonus - penalty_reduction
  - Determinístico: mesmos inputs → mesmo resultado
  - XP sempre ≥ 0 (non-negative)
  - Conceder XP via backend (stub local até M8 integrar Cloud Code)
  - _Requirements: R10.1, R10.2_
  - _Dependências: M6-T10 (resultado de corrida)_
  - _Critério de conclusão: XP calculado corretamente para cenários variados_
  - _Testes: EditMode test: cálculos com dados conhecidos_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M7-T09 Property test: XP Calculation Determinism (Property 20)
  - **Property 20: XP Calculation Determinism**
  - Para qualquer resultado de corrida válido (posição 1-10, voltas 0-10, lista de penalidades, flag limpa), o cálculo de XP SHALL ser determinístico e produzir um inteiro não-negativo conforme fórmula: base + position_bonus + clean_bonus - penalty_reduction
  - Gerador determinístico NUnit gera: posições, voltas, penalidades e clean flags aleatórios
  - **Validates: Requirements 10.1**
  - _Dependências: M7-T08_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M7-T10 Implementar Clean Driving Index
  - Calcular índice baseado em: infrações, contatos, abandonos, respeito a bandeiras
  - Índice inicia em 80, varia entre [0, 100] inclusive
  - Aplicar delta após cada corrida (positivo por corrida limpa, negativo por infrações)
  - Persistir no perfil (Cloud Save: cleanDrivingIndex)
  - _Requirements: R10.3_
  - _Dependências: M6-T07 (penalidades), M7-T08_
  - _Critério de conclusão: Índice atualiza corretamente e respeita bounds_
  - _Testes: EditMode test: cálculo com sequências variadas_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M7-T11 Property test: Clean Driving Index Bounds (Property 21)
  - **Property 21: Clean Driving Index Bounds**
  - Para qualquer sequência de eventos de corrida aplicados a um índice de pilotagem limpa iniciando em 80, o índice resultante SHALL permanecer dentro de [0, 100] inclusive
  - Gerador determinístico NUnit gera: sequências longas de eventos (infrações, corridas limpas, abandonos)
  - **Validates: Requirements 10.3**
  - _Dependências: M7-T10_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M7-T12 Implementar Pilot Notebook (versão simples)
  - Exibir: melhores tempos por pista/configuração, posição média, vitórias, pódios, poles, voltas mais rápidas
  - Dados lidos do perfil persistido (Cloud Save)
  - Layout simples (lista/tabela) — sem gráficos no MVP
  - _Requirements: R27.1, R27.2_
  - _Dependências: M7-T08, M1-T05_
  - _Critério de conclusão: Caderno exibe dados do piloto corretamente_
  - _Testes: PlayMode test: dados exibidos após corridas_
  - _Evidência: Screenshot do caderno_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M7-T13 Implementar persistência de perfil completa
  - Salvar/carregar perfil completo via Cloud Save: licenses, xp, level, cleanDrivingIndex, schoolProgress, stats, settings
  - Reconciliação ao reconectar (LWW para settings, server-wins para economia/progressão)
  - Alerta se progressão local não sincronizada > 24h
  - _Requirements: R10.5, R10.6_
  - _Dependências: M1-T05, M7-T08, M7-T10, M7-T12_
  - _Critério de conclusão: Perfil persiste entre sessões e reconecta corretamente_
  - _Testes: Integration test: salvar → fechar → reabrir → dados íntegros_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + internet_
  - _Risco: Médio — conflitos de reconciliação_

- [ ] M7-T14 Property test: Serialization Round-Trip (Property 1)
  - **Property 1: Serialization Round-Trip**
  - Para qualquer objeto de dados do jogo válido (player profile, control layout, ghost recording, race result), serializar e deserializar SHALL produzir um objeto equivalente ao original
  - Gerador determinístico NUnit gera: profiles, layouts, ghosts e results com dados aleatórios
  - **Validates: Requirements 2.3, 3.7, 10.5**
  - _Dependências: M7-T13_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M7-T15 Exit Gate M7
  - 10 módulos da escola navegáveis com desbloqueio progressivo
  - Ideal line com fade funcional (100% → 50% → 0%)
  - Instrutor de pilotagem funcional (texto+visual, pt-BR, String Tables)
  - Feedback por setor com diagnóstico específico
  - Prova de licença concede/nega corretamente
  - XP calculado deterministicamente
  - Clean Driving Index respeitando bounds [0,100]
  - Caderno do piloto com dados básicos
  - Perfil persiste via Cloud Save com reconciliação
  - Property tests 16, 13, 20, 21, 1 passando
  - _Validação humana: 🧑‍💻 Fundador completa escola e obtém licença_

---

### M8 — Campeonato Privado e Backend

> **Objetivo:** Implementar validação de Cloud Code (plausibility checks), economia server-authoritative (Coins+Gems via UGS Economy), Leaderboards NÃO-OFICIAIS, ChampionshipManager (preset Standard 10, sem bônus/descarte), telemetria mínima, autenticação completa (Guest+Google+Apple), e modos offline (Treino Livre, Time Attack, Race vs Bots). Backend validando resultados end-to-end.

- [ ] M8-T01 Implementar Cloud Code validation (plausibility checks)
  - Criar assembly RKW.Backend + RKW.Backend.Tests (primeiro consumidor)
  - Implementar Cloud Code script: validateRaceResult(raceData)
  - Validações: tempo vs máximo teórico da categoria, velocidade plausível, posição consistente
  - Rejeitar resultados implausíveis (tempo < mínimo teórico, velocidades impossíveis)
  - Calcular XP e rewards server-side
  - _Requirements: R1.10, R9.6, R9.7, R15.3_
  - _Dependências: M7-T08 (XP Calculator), M6-T10 (resultados)_
  - _Critério de conclusão: Cloud Code valida/rejeita resultados corretamente_
  - _Testes: Integration test: resultado válido aceito; resultado implausível rejeitado_
  - _Evidência: Test green + UGS Dashboard mostra execuções_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + UGS Cloud Code_
  - _Risco: Médio — definir thresholds de plausibilidade requer dados reais_

- [ ] M8-T02 Implementar UGS Economy (Coins + Gems, server-authoritative)
  - Configurar currencies no UGS Economy: Coins (grátis) e Gems (premium)
  - Toda operação de moeda via Cloud Code (client NUNCA modifica diretamente)
  - Conceder Coins por: corrida, escola, ads recompensados, nível
  - Gastar Coins/Gems por: cosméticos (stub até M9)
  - Invariante: saldo ≥ 0 SEMPRE; transação que resulta em negativo → rejeitada
  - _Requirements: R10.1, R11.1, AGENTS.md Rule 12_
  - _Dependências: M8-T01, M1-T05_
  - _Critério de conclusão: Economia funciona server-authoritative; client não pode manipular_
  - _Testes: Integration test: earn → spend → verify balance; attempt negative → rejected_
  - _Evidência: Test green + UGS Economy dashboard_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + UGS Economy_
  - _Risco: Médio — configuração de UGS Economy_

- [ ] M8-T03 Property test: Economy Non-Negative Balance Invariant (Property 26)
  - **Property 26: Economy Non-Negative Balance Invariant**
  - Para qualquer sequência de transações econômicas (earn, spend), o saldo resultante de qualquer moeda SHALL nunca ser negativo. Transação que resultaria em saldo negativo SHALL ser rejeitada
  - Gerador determinístico NUnit gera: sequências de transações com valores variados
  - **Validates: Requirements 10 (implied), AGENTS.md Rule 12**
  - _Dependências: M8-T02_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M8-T04 Implementar Leaderboards (NÃO-OFICIAIS)
  - Configurar UGS Leaderboards por LeaderboardKey
  - Submeter melhor volta quando validada por Cloud Code
  - Marcar rankings como NÃO-OFICIAIS (Shared Mode)
  - Respeitar LeaderboardKey estrita: tempos de keys diferentes NUNCA no mesmo ranking
  - Consultar: pessoal, sessão, all-time (versão atual)
  - _Requirements: R23.1, R23.2, R9.11_
  - _Dependências: M8-T01, M1-T08 (LeaderboardKey)_
  - _Critério de conclusão: Rankings exibem tempos corretos respeitando LeaderboardKey_
  - _Testes: Integration test: submeter tempo → consultar → verificar posição_
  - _Evidência: Test green + UGS Leaderboards dashboard_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + UGS Leaderboards_
  - _Risco: Baixo_

- [ ] M8-T05 Property test: Rankings Respect LeaderboardKey (Property 29)
  - **Property 29: Rankings Respect LeaderboardKey**
  - Para qualquer consulta de ranking filtrada por uma LeaderboardKey específica, TODAS as entradas retornadas SHALL ter LeaderboardKey idêntica. Nenhuma entrada com key diferente SHALL aparecer nos resultados
  - Gerador determinístico NUnit gera: múltiplas keys e tempos, consultas com filtro específico
  - **Validates: Requirements 16.3, 19.5, 23.2**
  - _Dependências: M8-T04_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M8-T06 Implementar ChampionshipManager (Standard 10, sem bônus/descarte)
  - Criar assembly RKW.Championship + RKW.Championship.Tests (primeiro consumidor)
  - Implementar: criar campeonato, adicionar participantes, calendário de etapas
  - Scoring: preset "Standard 10" (25-18-15-12-10-8-6-4-2-1)
  - NÃO implementar bônus (pole, volta rápida, etc.) nem descarte — Alpha/Beta
  - Standings: soma de pontos; desempate por mais vitórias
  - Persistir via Cloud Save
  - _Requirements: R25.1, R25.2, R25.4_
  - _Dependências: M6-T10 (resultados), M8-T01_
  - _Critério de conclusão: Campeonato funciona com múltiplas etapas e standings corretos_
  - _Testes: EditMode test: scoring com dados conhecidos; Integration test: fluxo completo_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M8-T07 Property test: Championship Scoring Determinism (Property 32)
  - **Property 32: Championship Scoring Determinism**
  - Para qualquer conjunto válido de resultados de corrida e configuração de scoring (preset de pontos, bônus, regra de descarte), o cálculo de standings SHALL ser determinístico e produzir o mesmo ranking para os mesmos inputs. Pontos SHALL ser soma dos resultados scored menos worst descartados
  - Gerador determinístico NUnit gera: resultados de corrida, configurações de scoring variadas
  - **Validates: Requirements 25.1, 25.2**
  - _Dependências: M8-T06_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M8-T08 Implementar telemetria mínima por corrida
  - Registrar por piloto/sessão: melhor tempo por setor, tempo total, penalidades, desconexões, colisões com severidade, controle/assistências
  - Persistir junto ao resultado no backend
  - Formato extensível (campos opcionais sem quebrar schema)
  - _Requirements: R15.1, R15.2, R15.4_
  - _Dependências: M8-T01, M6-T07_
  - _Critério de conclusão: Telemetria persiste com resultado e é consultável_
  - _Testes: Integration test: corrida → telemetria persistida → consulta_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + UGS_
  - _Risco: Baixo_

- [ ] M8-T09 Implementar autenticação completa (Guest + Google + Apple)
  - Guest (anônimo): já funcional (M1-T05)
  - Google Play Games: implementar vinculação
  - Sign in with Apple: implementar (obrigatório por Apple policy se oferecemos login social)
  - Upgrade de guest: vincular conta depois sem perder progresso
  - _Requirements: R2 (auth), R9.7_
  - _Dependências: M1-T05_
  - _Critério de conclusão: Login funciona com 3 providers; upgrade de guest preserva dados_
  - _Testes: Integration test: cada provider autentica corretamente_
  - _Evidência: Test green + UGS Auth dashboard mostra 3 providers_
  - _Validação humana: 🧑‍💻 Fundador testa em dispositivo real (Google + Apple)_
  - _Ambiente: Dispositivos reais (Android + iOS)_
  - _Risco: Alto — Sign in with Apple requer Apple Developer Program ativo_

- [ ] M8-T10 Implementar modos offline (Treino Livre, Time Attack, Race vs Bots)
  - Treino Livre: sem limite de voltas, sem ranking, sem penalidades de abandono
  - Time Attack (Tomada de Tempo): ghost pessoal, registra melhor tempo
  - Race vs Bots: corrida completa offline contra 9 bots (state machine M6)
  - Todos usam a mesma física, timing e regras
  - _Requirements: R2.1, R2.2, R2.3_
  - _Dependências: M6-T01 (state machine), M4-T08 (bots), M4-T06 (ghost)_
  - _Critério de conclusão: 3 modos offline jogáveis end-to-end_
  - _Testes: PlayMode test: cada modo funciona sem rede_
  - _Evidência: Test green + screenshot de cada modo_
  - _Validação humana: 🧑‍💻 Fundador joga cada modo_
  - _Ambiente: Unity Editor + Android (modo avião)_
  - _Risco: Baixo_

- [ ] M8-T11 Exit Gate M8
  - Cloud Code valida resultados (plausibility checks)
  - Economia server-authoritative funcionando (Coins+Gems)
  - Leaderboards NÃO-OFICIAIS funcionando com LeaderboardKey
  - Campeonato privado com Standard 10 funcional
  - Telemetria mínima persistida com resultados
  - Autenticação completa (Guest+Google+Apple)
  - 3 modos offline jogáveis
  - Property tests 26, 29, 32 passando
  - _Validação humana: 🧑‍💻 Fundador valida economia e campeonato end-to-end_

---

### M9 — Monetização Cosmética e Preparação para Alpha

> **Objetivo:** Implementar garagem com preview de cosméticos, IAP (Unity IAP + Cloud Code validation), AdMob (APENAS fora de sessões, intervalo 5min), compra de remoção de ads, eventos de analytics, aviso de privacidade (primeiro boot), customização completa de controles (reposição, resize, canhoto, sensibilidade, háptica), modos de controle adicionais (volante+tilt), template de desafio onboarding, e build alpha (distribuição interna, sufixo .dev).

- [ ] M9-T01 Implementar Garage com preview de cosméticos
  - Criar assembly RKW.UI (primeiro consumidor)
  - Implementar tela de garagem: visualização do kart com cosméticos equipados
  - Preview de itens antes da compra
  - Categorias: capacetes, balaclavas, luvas, macacões, pinturas, adesivos, comemorações, molduras
  - Cosméticos NÃO alteram performance (zero gameplay effect)
  - _Requirements: R11.1, R11.2_
  - _Dependências: M8-T02 (Economy)_
  - _Critério de conclusão: Garagem exibe kart com cosméticos equipáveis_
  - _Testes: PlayMode test: equip/desequip funciona; performance inalterada_
  - _Evidência: Screenshot da garagem_
  - _Validação humana: 🧑‍💻 Fundador avalia UX da garagem_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M9-T02 Property test: Cosmetics Have Zero Gameplay Effect (Property 22)
  - **Property 22: Cosmetics Have Zero Gameplay Effect**
  - Para qualquer item cosmético ou combinação de itens equipados em um kart, TODOS os parâmetros relevantes de física (maxSpeed, acceleration, grip, braking, inertia) SHALL permanecer idênticos aos valores base da categoria
  - Gerador determinístico NUnit gera: combinações aleatórias de cosméticos
  - **Validates: Requirements 11.1, 11.2**
  - _Dependências: M9-T01_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M9-T03 Property test: Category Equalization in Online Sessions (Property 14)
  - **Property 14: Category Equalization in Online Sessions**
  - Para qualquer sessão de corrida online, TODOS os participantes (humanos e bots) SHALL usar o mesmo KartCategorySO
  - Gerador determinístico NUnit gera: sessões com participantes variados
  - **Validates: Requirements 5.5**
  - _Dependências: M6-T01, M9-T01_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M9-T04 Implementar IAP (Unity IAP + Cloud Code validation)
  - Integrar Unity IAP package
  - Implementar fluxo: select item → InitiatePurchase → Receipt → Cloud Code validateReceipt → conceder item
  - Validação server-to-server (Cloud Code verifica receipt com Google/Apple)
  - Produtos: Gems (packs variados), Remove Ads (one-time)
  - Restauração de compras conforme políticas Android/iOS
  - _Requirements: R11.7_
  - _Dependências: M8-T01 (Cloud Code), M8-T02 (Economy)_
  - _Critério de conclusão: Compra funciona end-to-end com validação no backend_
  - _Testes: Integration test: purchase flow com sandbox/test mode_
  - _Evidência: Test green + UGS Economy mostra items concedidos_
  - _Validação humana: 🧑‍💻 Fundador testa compra real (sandbox) em dispositivo_
  - _Ambiente: Dispositivos reais + sandbox stores_
  - _Risco: Alto — requer contas developer ativas e configuração de produtos_

- [ ] M9-T05 Implementar AdMob (APENAS fora de sessões, intervalo 5min)
  - Integrar AdMob SDK
  - Intersticiais: APENAS em menus, lobby, resultados, garagem, entre sessões
  - NUNCA durante pilotagem (zero banners durante corrida)
  - Intervalo mínimo: 5 minutos entre exibições
  - Rewarded ads: opcional para moedas (Coins bonus)
  - _Requirements: R11.4, R11.5_
  - _Dependências: M1-T01_
  - _Critério de conclusão: Ads exibem fora de sessão com intervalo respeitado_
  - _Testes: EditMode test: lógica de intervalo; PlayMode test: ad não aparece durante corrida_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + Android real (test ads)_
  - _Risco: Médio — AdMob SDK pode ter conflitos_

- [ ] M9-T06 Property test: Ad Interval Minimum (Property 23)
  - **Property 23: Ad Interval Minimum**
  - Para qualquer sequência de eventos de exibição de anúncio intersticial, o tempo entre exibições consecutivas SHALL ser ≥ 5 minutos (300 segundos)
  - Gerador determinístico NUnit gera: sequências de timestamps de request de ad
  - **Validates: Requirements 11.4**
  - _Dependências: M9-T05_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M9-T07 Implementar Remove Ads purchase
  - Compra one-time que desabilita todos intersticiais e banners permanentemente
  - Persistir flag via Cloud Save (account-level)
  - Verificar flag antes de exibir qualquer ad
  - _Requirements: R11.6_
  - _Dependências: M9-T04, M9-T05_
  - _Critério de conclusão: Após compra, nenhum ad é exibido_
  - _Testes: Integration test: purchase → no more ads_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M9-T08 Implementar analytics events
  - Configurar Unity Analytics com eventos críticos MVP:
    - race_completed, lap_completed, penalty_applied, iap_completed, fps_sample
    - tutorial_completed, disconnect, control_mode_set, ad_impression
  - Anonimizar dados antes de envio (sem PII direta)
  - Player IDs pseudonimizados
  - _Requirements: R13.1, R13.2, R13.3_
  - _Dependências: M3-T07 (telemetria performance), M8-T08 (telemetria corrida)_
  - _Critério de conclusão: Eventos aparecem no Unity Analytics dashboard_
  - _Testes: Integration test: eventos enviados e recebidos_
  - _Evidência: Screenshot do Analytics dashboard_
  - _Validação humana: não_
  - _Ambiente: Unity Editor + internet_
  - _Risco: Baixo_

- [ ] M9-T09 Property test: Analytics Event Anonymization (Property 25)
  - **Property 25: Analytics Event Anonymization**
  - Para qualquer evento de analytics gerado pelo sistema de telemetria, o payload SHALL não conter informação pessoal identificável (sem email, nome real, telefone, localização precisa ou device identifiers que possam identificar um indivíduo)
  - Gerador determinístico NUnit gera: eventos com payloads variados
  - **Validates: Requirements 13.3**
  - _Dependências: M9-T08_
  - _Critério de conclusão: 100+ iterações passam_
  - _Evidência: Test Runner output_

- [ ] M9-T10 Implementar Privacy Notice (primeiro boot)
  - Exibir aviso de privacidade claro no primeiro boot
  - Solicitar consentimento explícito para finalidades baseadas em consentimento
  - Disponibilizar: política de privacidade, termos de uso
  - Opt-out disponível nas configurações para finalidades baseadas em consentimento
  - Crianças < 13: ads personalizados desabilitados, analytics limitados
  - _Requirements: R11.8, R13.4, R13.5_
  - _Dependências: M1-T05 (Cloud Save para persistir choice)_
  - _Critério de conclusão: Aviso exibido no primeiro boot, escolha persistida_
  - _Testes: PlayMode test: primeiro boot mostra aviso; subsequente não_
  - _Evidência: Screenshot do aviso_
  - _Validação humana: 🧑‍💻 Fundador + consultor jurídico revisam texto_
  - _Ambiente: Unity Editor + Android_
  - _Risco: Alto — texto final depende de consultor jurídico_

- [ ] M9-T11 Implementar customização completa de controles
  - Reposicionamento de todos elementos de controle (drag)
  - Redimensionamento (pinch ou slider)
  - Modo canhoto (espelhar layout)
  - Sensibilidade configurável por tipo de controle
  - Zona morta configurável
  - Assistência de direção (levels: none, light, medium)
  - Háptica configurável (on/off + intensidade)
  - Persistir layout via Cloud Save
  - _Requirements: R3.6, R3.7, R3.8, R3.9, R3.10_
  - _Dependências: M2-T14 (controles base)_
  - _Critério de conclusão: Todos os aspectos de controle são configuráveis e persistem_
  - _Testes: PlayMode test: mudar layout → persistir → recarregar → layout mantido_
  - _Evidência: Screenshot de tela de customização_
  - _Validação humana: 🧑‍💻 Fundador testa em dispositivo real_
  - _Ambiente: Dispositivo Android real_
  - _Risco: Médio — UX de customização requer iteração_

- [ ] M9-T12 Implementar modos de controle adicionais (Volante Virtual + Tilt)
  - Volante Virtual: giro rotacional com feedback visual
  - Inclinação (Tilt): giroscópio com sensibilidade e zona morta configuráveis
  - Ambos integrados com o mesmo pipeline de InputController
  - _Requirements: R3.1, R3.2_
  - _Dependências: M2-T14, M9-T11_
  - _Critério de conclusão: 3 modos de controle funcionais (joystick, volante, tilt)_
  - _Testes: PlayMode test: cada modo controla kart corretamente_
  - _Evidência: Vídeo dos 3 modos_
  - _Validação humana: 🧑‍💻 Fundador testa cada modo em dispositivo real_
  - _Ambiente: Dispositivo Android + iOS real_
  - _Risco: Médio — tilt requer calibração de sensibilidade_

- [ ] M9-T13 Implementar Onboarding Challenge Template
  - Criar ChallengeTemplateSO de onboarding estático (opcional)
  - Desafio: completar primeiro módulo da escola, fazer primeira volta válida
  - Ativar/desativar via Remote Config
  - Recompensa: XP + cosmético básico
  - _Requirements: R29.4, R29.5_
  - _Dependências: M7-T01 (escola), M1-T11 (Remote Config)_
  - _Critério de conclusão: Desafio de onboarding funciona quando ativado_
  - _Testes: Integration test: ativar flag → desafio disponível → completar → reward_
  - _Evidência: Test green_
  - _Validação humana: não_
  - _Ambiente: Unity Editor_
  - _Risco: Baixo_

- [ ] M9-T14 Gerar Alpha Build (distribuição interna, sufixo .dev)
  - Bundle ID placeholder atual: `br.com.suitedigital.rentalkartworld.dev`; não criar aplicativo definitivo nas lojas sem resolver Q-RL-01
  - Build Android (.aab) para Google Play Internal Track
  - Build iOS (IPA) para TestFlight Internal (se Apple Developer Program ativo)
  - Versionamento semântico: 0.1.0+buildNumber
  - Verificar: todos os sistemas integrados funcional end-to-end
  - _Requirements: R14.1, R14.6, R14.7_
  - _Dependências: M1-T07 (CI/CD), todas tasks M9 anteriores, Q-RL-01 resolvida antes de criar aplicativo nas lojas_
  - _Critério de conclusão: Build distribuído internamente, jogável end-to-end_
  - _Testes: Smoke test manual: login → escola → corrida → resultado → economia_
  - _Evidência: Build log verde + app instalável_
  - _Validação humana: 🧑‍💻 Fundador instala e joga no dispositivo real_
  - _Ambiente: Dispositivos reais + stores (internal track)_
  - _Risco: Alto — integração de todos os sistemas pode revelar bugs_

- [ ] M9-T15 Exit Gate M9
  - Garagem com cosméticos (zero gameplay effect) funcional
  - IAP funciona end-to-end com validação backend
  - AdMob exibe APENAS fora de sessão com intervalo 5min
  - Remove Ads funciona permanentemente
  - Analytics events coletados e anonimizados
  - Privacy Notice exibido no primeiro boot
  - Controles completamente customizáveis e persistentes
  - 3 modos de controle funcionais
  - Onboarding challenge funcional (via Remote Config)
  - Alpha build (.dev) distribuído internamente
  - Property tests 22, 14, 23, 25 passando
  - _Validação humana: 🧑‍💻 Fundador joga alpha build end-to-end em dispositivo real_

---

### M10 — Estabilização, Testes Físicos e Distribuição Interna

> **Objetivo:** Estabilizar o MVP com suite de testes completa em CI, teste de performance na matriz de dispositivos, teste de multiplayer sustentado, bug bash, validação com pilotos reais (avaliação ≥ 7/10 com 5-10 pilotos), versionamento semântico e preparação para closed beta (gate humano obrigatório). Declarar MVP Ready Candidate ao cumprir exit criteria.

- [ ] M10-T01 Executar suite de testes completa em CI
  - Garantir que TODOS os property tests (35) executam em CI
  - Garantir que TODOS os EditMode e PlayMode tests executam
  - Configurar test report com cobertura
  - Fix any flaky tests
  - CI bloqueia build se qualquer teste falhar
  - _Requirements: R14.2, R14.3_
  - _Dependências: M1-T07 (CI/CD), todas property tests anteriores_
  - _Critério de conclusão: 34 property tests obrigatórios no MVP + todos unit/integration tests passando em CI (Property 35 obrigatória antes de habilitar condições não secas no Alpha/Beta)_
  - _Testes: CI pipeline green_
  - _Evidência: CI report com 100% pass rate_
  - _Validação humana: não_
  - _Ambiente: Unity Build Automation_
  - _Risco: Médio — flaky tests podem requerer investigação_

- [ ] M10-T02 Performance testing na matriz de dispositivos
  - Testar em TODOS os dispositivos da matriz (M0-T05): Android low, mid, high + iOS
  - Medir por dispositivo: FPS médio, FPS P1, memória máxima, temperatura, battery drain
  - Cenário: corrida completa (10 voltas, 10 karts) + qualifying
  - Critério obrigatório: ≥ 30 FPS sustentados em Android low-tier
  - Meta: 60 FPS em mid/high-tier
  - Documentar em `docs/playtests/M10-performance-matrix.md`
  - _Requirements: R12.1, R12.2_
  - _Dependências: M9-T14 (alpha build)_
  - _Critério de conclusão: Todos dispositivos atendem requisitos mínimos de performance_
  - _Testes: Profiling sessions por dispositivo_
  - _Evidência: Tabela com métricas por dispositivo_
  - _Validação humana: 🧑‍💻 Fundador executa nos dispositivos disponíveis_
  - _Ambiente: Dispositivos reais (matriz completa)_
  - _Risco: Alto — otimização pode ser necessária para low-tier_

- [ ] M10-T03 Teste de multiplayer sustentado
  - 4 jogadores humanos + 6 bots jogam ao menos 5 sessões consecutivas
  - Medir: estabilidade (zero crashes), latência sustentada, packet loss, desync
  - Verificar: reconexão funciona, bots substituem corretamente, resultados persistem
  - Cenário mínimo: 2 horas de jogo contínuo
  - Documentar em `docs/playtests/M10-multiplayer-sustained.md`
  - _Requirements: R9_
  - _Dependências: M9-T14 (alpha build), M5 (multiplayer)_
  - _Critério de conclusão: Zero crashes em 2h, mediana latência ≤ 100ms, packet loss < 2%_
  - _Testes: Manual com 4 jogadores remotos_
  - _Evidência: Documento com métricas + zero crashes_
  - _Validação humana: 🧑‍💻 Fundador + 3 jogadores participam_
  - _Ambiente: Dispositivos reais + internet BR_
  - _Risco: Alto — problemas de estabilidade podem requerer fixes_

- [ ] M10-T04 Bug Bash
  - Sessão dedicada de bug finding (Fundador + pilotos)
  - Testar: todos modos de jogo, edge cases, fluxos interrompidos
  - Classificar bugs: P0 (blocker), P1 (critical), P2 (major), P3 (minor)
  - Corrigir todos P0 e P1 antes de avançar
  - Documentar bugs encontrados/corrigidos
  - _Requirements: (qualidade geral)_
  - _Dependências: M9-T14_
  - _Critério de conclusão: Zero P0, zero P1 abertos_
  - _Testes: Regression tests para bugs corrigidos_
  - _Evidência: Bug tracker limpo (P0/P1)_
  - _Validação humana: 🧑‍💻 Fundador + pilotos participam do bug bash_
  - _Ambiente: Dispositivos reais_
  - _Risco: Médio — volume de bugs desconhecido_

- [ ] M10-T05 Checkpoint — Validação com pilotos reais (avaliação ≥ 7/10)
  - Distribuir alpha build para 5-10 pilotos reais de kart
  - Cada piloto joga: escola (ao menos 3 módulos), 3+ corridas online, treino livre
  - Coletar avaliação numérica 0-10 por critério: realismo, diversão, controles, multiplayer, progressão, UI
  - Coletar feedback qualitativo: o que gostou, o que não gostou, sugestões
  - Documentar em `docs/playtests/M10-pilot-validation.md`
  - _Critério de conclusão: Média geral ≥ 7/10, nenhum critério abaixo de 5, aprovação qualitativa_
  - _Validação humana: 🧑‍💻 Fundador coordena com 5-10 pilotos reais — GATE OBRIGATÓRIO_
  - _Ambiente: Dispositivos reais dos pilotos_
  - _Risco: Alto — se avaliação média < 7/10, iterar antes de beta_

- [ ] M10-T06 Configurar versionamento semântico
  - Implementar esquema MAJOR.MINOR.PATCH
  - Build number incremental automático
  - Configurar em Player Settings + CI/CD
  - Version: 1.0.0 para MVP release candidate
  - _Requirements: R14.6_
  - _Dependências: M1-T07_
  - _Critério de conclusão: Versão exibida corretamente no app e builds_
  - _Testes: Verificar version string no build output_
  - _Evidência: Build com version 1.0.0-rc.1_
  - _Validação humana: não_
  - _Ambiente: CI/CD_
  - _Risco: Baixo_

- [ ] M10-T07 Preparação para Closed Beta (gate humano obrigatório)
  - Preparar distribuição para Google Play Closed Beta + TestFlight External
  - Configurar: gate humano para publicação (NUNCA automático para beta externo)
  - **iOS:** Validação completa (assinatura, TestFlight, dispositivo real) obrigatória antes do lançamento iOS, mas NÃO bloqueia um Alpha exclusivamente Android. Se Apple Developer Program estiver ativo neste ponto, incluir TestFlight External. Caso contrário, closed beta apenas Android.
  - Preparar: release notes, known issues, feedback form
  - Verificar: privacy policy URL, terms of use URL
  - NÃO publicar ainda — apenas preparar
  - _Requirements: R14.4, R14.5_
  - _Dependências: M10-T05 (pilotos aprovaram), M10-T04 (bugs corrigidos)_
  - _Critério de conclusão: Tudo preparado para publicação, aguardando gate humano_
  - _Testes: Dry-run do processo de publicação_
  - _Evidência: Checklist de publicação completo_
  - _Validação humana: 🧑‍💻 Fundador autoriza publicação para closed beta — GATE OBRIGATÓRIO_
  - _Ambiente: Google Play Console + App Store Connect_
  - _Risco: Médio — aprovação de loja pode requerer ajustes_

- [ ] M10-T08 Exit Gate M10 — MVP Ready Candidate
  - 34 property tests obrigatórios no MVP passando em CI (Property 35 — Track Condition Alters Grip — obrigatória no Alpha/Beta antes de habilitar condições não secas)
  - Performance validada na matriz de dispositivos (≥ 30 FPS low-tier)
  - Multiplayer sustentado estável (2h sem crash)
  - Zero bugs P0/P1 abertos
  - Pilotos reais aprovaram com média ≥ 7/10
  - Versionamento semântico configurado
  - Closed beta preparado (gate humano pendente)
  - TODOS os exit gates M0-M9 cumpridos
  - **Declaração: MVP Ready Candidate**
  - _Validação humana: 🧑‍💻 Fundador declara MVP Ready Candidate_

---

## Caminho Crítico (Critical Path)

```
M0 → M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8 → M9 → M10
```

**Gargalos identificados:**

| Milestone | Gargalo | Impacto | Mitigação |
|---|---|---|---|
| M2 | Validação com pilotos reais (M2-T20) | Depende de agenda de 2+ pilotos | Agendar com 1 semana de antecedência |
| M3 | Performance em Android low-tier (M3-T08) | Se < 30 FPS, otimizar antes de avançar | Profiling contínuo desde M2 |
| M5 | Qualidade de networking (M5-T07) | Latência/jitter podem ser inaceitáveis | Tuning de interpolação + região Photon |
| M10 | Avaliação média pilotos ≥ 7/10 (M10-T05) | Se < 7/10, iterar até satisfazer | Validação contínua desde M2 |

---

## Tarefas Paralelizáveis

| Milestone | Tasks em Paralelo | Condição |
|---|---|---|
| M0 | T01–T08 (todos spikes) | Independentes entre si |
| M1 | T01+T04+T05+T10 (setup paralelo) | T01 é pré-req de T02 e T03; T04/T05/T10 independentes entre si após T01 |
| M2 | T01+T14 (physics + controls), T07+T09+T16 (superfícies, colisões, slipstream) | Após T01, features de physics são paralelizáveis |
| M3 | T04+T07+T09 (quality, telemetria, crashlytics) | Independentes após M3-T01 |
| M4 | T06+T08 (ghost + bots) | Independentes; ambos dependem de M2-T01 |
| M5 | T03+T06 (matchmaking + reconexão) | Ambos dependem de M5-T01 |
| M6 | T05+T07 (flags + penalties) | Ambos dependem de M6-T01 |
| M7 | T01+T03+T08+T10 (escola, instrutor, XP, clean index) | Parcialmente paralelizáveis |
| M8 | T04+T06+T08+T09+T10 (leaderboards, championship, telemetria, auth, offline) | Paralelizáveis após M8-T01 |
| M9 | T01+T05+T08+T10+T11 (garagem, ads, analytics, privacy, controles) | Independentes entre si |
| M10 | T01+T02+T03 (CI, performance, multiplayer) | Paralelizáveis |

---

## Tarefas que Requerem Fundador ou Pilotos Reais

| Task | Quem | Tipo |
|---|---|---|
| M0-T01 a T08 | 🧑‍💻 Fundador (aprovação) | Revisão de spikes |
| M1-T07 | 🧑‍💻 Fundador | Configurar conta Google Developer (obrigatório); Apple quando budget permitir |
| M1-T12 | 🧑‍💻 Fundador | Validar latência aceitável |
| M1-T13 | 🧑‍💻 Fundador | Teste audio em dispositivo |
| M2-T01 | 🧑‍💻 Fundador + 🏎️ Pilotos | Avaliar feel da física |
| M2-T14 | 🧑‍💻 Fundador | Testar usabilidade de controles |
| M2-T20 | 🧑‍💻 Fundador + 🏎️ Pilotos (2+) | **GATE: Playtest vertical slice** |
| M3-T01 | 🧑‍💻 Fundador | Avaliar visual/layout da pista |
| M3-T08 | 🧑‍💻 Fundador | Executar teste em dispositivo real |
| M4-T08 | 🧑‍💻 Fundador | Avaliar comportamento de bots |
| M4-T14 | 🧑‍💻 Fundador | Corrida completa contra bots |
| M5-T07 | 🧑‍💻 Fundador + jogador remoto | Teste multiplayer com latência real |
| M6-T04 | 🧑‍💻 Fundador | Avaliar timing de luzes |
| M6-T11 | 🧑‍💻 Fundador | Avaliar layout de resultados |
| M6-T12 | 🧑‍💻 Fundador | Avaliar legibilidade do HUD |
| M6-T13 | 🧑‍💻 Fundador | Corrida completa com regras |
| M7-T03 | 🧑‍💻 Fundador | Avaliar utilidade do instrutor |
| M8-T09 | 🧑‍💻 Fundador | Testar auth em dispositivo |
| M8-T10 | 🧑‍💻 Fundador | Jogar modos offline |
| M9-T01 | 🧑‍💻 Fundador | Avaliar UX garagem |
| M9-T04 | 🧑‍💻 Fundador | Testar compra IAP (sandbox) |
| M9-T10 | 🧑‍💻 Fundador + consultor jurídico | Revisar texto de privacidade |
| M9-T11 | 🧑‍💻 Fundador | Testar customização em dispositivo |
| M9-T12 | 🧑‍💻 Fundador | Testar modos de controle |
| M9-T14 | 🧑‍💻 Fundador | Instalar/jogar alpha build |
| M10-T02 | 🧑‍💻 Fundador | Performance nos dispositivos |
| M10-T03 | 🧑‍💻 Fundador + 3 jogadores | Multiplayer sustentado |
| M10-T04 | 🧑‍💻 Fundador + 🏎️ Pilotos | Bug bash |
| M10-T05 | 🧑‍💻 Fundador + 🏎️ Pilotos (5-10) | **GATE: Pilot validation ≥ 7/10** |
| M10-T07 | 🧑‍💻 Fundador | **GATE: Autorizar closed beta** |

---

## Estimativas com Premissas

> **Premissas:** Fundador 8-12h/semana; execução assistida por Codex/Kiro conforme capacidade disponível (prazo depende de limites de uso, ciclos de correção e disponibilidade das ferramentas); pilotos com 1 semana de antecedência; 1 Android mid + 1 iOS disponíveis. Estimativas são hipóteses de planejamento, não compromissos de prazo.

| Milestone | Optimista (semanas) | Base (semanas) | Pessimista (semanas) | Gargalo Principal |
|---|---|---|---|---|
| M0 | 1 | 2 | 3 | Pesquisa + aprovação Fundador |
| M1 | 2 | 3 | 5 | Setup Unity + CI/CD + contas |
| M2 | 4 | 6 | 10 | Calibração física + playtest pilotos |
| M3 | 2 | 3 | 5 | Performance optimization |
| M4 | 3 | 4 | 6 | Bots convincentes + timing integrado |
| M5 | 3 | 5 | 8 | Networking tuning + latência |
| M6 | 3 | 4 | 6 | Integração state machine + penalty engine |
| M7 | 2 | 3 | 5 | Conteúdo escola + instrutor |
| M8 | 3 | 4 | 6 | Cloud Code + economia + auth |
| M9 | 3 | 4 | 6 | IAP + AdMob + alpha build |
| M10 | 3 | 4 | 6 | Bug fixes + pilot validation |
| **TOTAL** | **29** | **42** | **66** | — |

**Nota:** Estimativas assumem execução por agentes com revisão humana. Tempo real depende de disponibilidade de Fundador para gates e da velocidade de iteração com pilotos.

---

## Orçamento por Fase

> Já documentado na seção "Orçamento por Fase" no início deste arquivo. Resumo: investimento inicial até closed alpha ~R$ 130 (Android only) ou ~R$ 660 (Android + iOS). Recomendação: iniciar apenas Android; iOS quando budget permitir.

---

## MVP Ready Criteria

Para declarar MVP Ready Candidate, TODOS os seguintes critérios devem ser satisfeitos:

1. Kart dirigível com física simcade autêntica validada por pilotos reais (média ≥ 7/10)
2. 10 módulos de escola funcionais com desbloqueio progressivo e prova de licença
3. Corrida online funcional: 2-4 humanos + bots via Photon Shared Mode
4. State machine de corrida completa (Lobby → Results) com regras esportivas
5. Economia server-authoritative (Coins + Gems) com zero possibilidade de manipulação client
6. IAP funcional com validação backend (Unity IAP + Cloud Code)
7. AdMob funcional APENAS fora de sessão com intervalo 5min respeitado
8. 34 property tests obrigatórios passando em CI (Property 35 obrigatória no Alpha/Beta antes de habilitar condições não secas)
9. Performance: ≥ 30 FPS sustentados em Android low-tier por 30 min
10. Multiplayer: mediana latência ≤ 100ms, zero crashes em 2h de jogo sustentado
11. Autenticação completa: Guest + Google + Apple
12. Rankings NÃO-OFICIAIS funcionando (Shared Mode)
13. Privacidade: aviso de primeiro boot, exclusão de dados implementada
14. CI/CD: builds automáticos com testes gate, distribuição interna automática
15. Pilotos reais (5-10) aprovaram com avaliação ≥ 7/10 — GATE HUMANO OBRIGATÓRIO

---

## Fora do MVP (Out of MVP Scope)

| Item | Fase Planejada | Justificativa |
|---|---|---|
| Direção anti-horária (2ª configuração) | Alpha/Beta | Arquitetura preparada; 1 config suficiente para validar |
| Preset noturno (EnvironmentPreset) | Alpha/Beta | Requer spotlights + teste de visibilidade |
| Condição úmida/chuva (TrackCondition) | Alpha/Beta | SOs preparados; multipliers = 1.0 no MVP |
| Rankings diário/semanal/mensal | Alpha/Beta | All-time suficiente para MVP |
| Desafios diários/semanais (rotação) | Alpha/Beta | Onboarding estático suficiente |
| Ghost de amigo (cloud) | Alpha/Beta | Ghost pessoal local suficiente |
| Cartão compartilhável | Alpha/Beta | Tela de resultado funciona como screenshot |
| Consistência avançada (gráficos) | Alpha/Beta | Estatísticas básicas pós-corrida suficientes |
| Campeonato com bônus/descarte | Alpha/Beta | Standard 10 sem extras suficiente |
| Instrutor com áudio | Alpha/Beta | Texto+visual suficiente |
| Idioma inglês | Alpha/Beta | pt-BR via String Tables; estrutura pronta |
| Ranked competitivo (divisões) | Pós-lançamento | Requer server authority |
| Server Authority (Host/Dedicated) | Pós-lançamento | Shared Mode aceitável para alpha/casual |
| Real Track Partner Platform | Pós-lançamento | Requer licenciamento + pipeline 3D |
| Pistas reais licenciadas | Pós-lançamento | Requer parceiros + fotogrametria |
| Evolução de pista (grip dinâmico) | Pós-lançamento | Apenas extension points documentados |
| Lastro e categorias de peso | Pós-lançamento | Registrado no roadmap |
| Largada lançada (rolling start) | Pós-lançamento | Standing start suficiente |
| Replay de incidentes | Pós-lançamento | Penalidade explicável por texto suficiente |
| Campeonatos híbridos (virtual+real) | Pós-lançamento | Requer parceiros reais |
| Coach adaptativo (ML) | Pós-lançamento | Instrutor baseado em regras suficiente |
| Categoria Rental Pro (18 HP) | Alpha/Beta | Feature flag preparada; 2 categorias no MVP |
| Voice Chat | Pós-lançamento | Fora do escopo inicial |
| Clãs | Pós-lançamento | Fora do escopo inicial |
| Modo Endurance | Pós-lançamento | 10 voltas suficiente |

---

## Matriz de Rastreabilidade (Tasks → Requirements)

| Requisito | Tasks Implementadoras | Milestone(s) |
|---|---|---|
| R1 (Corrida) | M6-T01, M6-T02, M6-T03, M6-T04, M6-T05, M6-T07, M6-T10, M6-T11 | M6 |
| R2 (Modos) | M8-T10, M5-T03, M6-T01 | M5, M6, M8 |
| R3 (Controles) | M2-T14, M2-T15, M9-T11, M9-T12 | M2, M9 |
| R4 (Física) | M2-T01 a M2-T11, M2-T16, M2-T17 | M2 |
| R5 (Categorias) | M2-T12, M2-T13, M7-T06, M7-T07, M9-T03 | M2, M7, M9 |
| R6 (Escola) | M7-T01 a M7-T07 | M7 |
| R7 (Bandeiras/Penalidades) | M6-T05 a M6-T09 | M6 |
| R8 (Bots) | M4-T08, M4-T09 | M4 |
| R9 (Multiplayer) | M5-T01 a M5-T08, M8-T01 | M5, M8 |
| R10 (Progressão) | M7-T08 a M7-T13 | M7 |
| R11 (Monetização) | M9-T01 a M9-T07, M9-T10 | M9 |
| R12 (Performance) | M3-T04 a M3-T08, M10-T02 | M3, M10 |
| R13 (Analytics/Privacidade) | M9-T08, M9-T09, M9-T10 | M9 |
| R14 (Build/CI) | M1-T07, M9-T14, M10-T01, M10-T06, M10-T07 | M1, M9, M10 |
| R15 (Telemetria) | M8-T08 | M8 |
| R16 (Track Config) | M3-T02 | M3 |
| R17 (Env Presets) | M3-T03 | M3 |
| R18 (Conditions) | M3-T03 | M3 |
| R19 (Session/Leaderboard Keys) | M1-T08, M1-T09 | M1 |
| R20 (Timing) | M2-T19, M4-T01, M4-T12 | M2, M4 |
| R21 (Sector Comparison) | M4-T02, M4-T03 | M4 |
| R22 (Ideal Lap) | M4-T04, M4-T05 | M4 |
| R23 (Rankings) | M8-T04, M8-T05 | M8 |
| R24 (Ghost) | M4-T06, M4-T07, M4-T13 | M4 |
| R25 (Championship) | M8-T06, M8-T07 | M8 |
| R26 (Consistency) | M4-T10, M4-T11 | M4 |
| R27 (Notebook) | M7-T12 | M7 |
| R28 (Instructor) | M7-T03, M7-T04 | M7 |
| R29 (Challenges) | M9-T13 | M9 |
| R30 (Card) | — (Alpha/Beta) | — |
| R31 (Race Direction) | M6-T07, M6-T09 | M6 |
| R32 (Track Evolution) | — (Pós-MVP, apenas docs) | — |
| R33 (Ballast) | — (Pós-MVP) | — |
| R34 (Start Procedures) | M6-T04 | M6 |
| R35 (Interface) | M6-T11, M6-T12 | M6 |
| R36 (Post-MVP) | — (documentação existente) | — |

---

## Notas Importantes

1. **34 property tests são OBRIGATÓRIOS no MVP** (Properties 1–34). Property 35 (Track Condition Alters Grip) é adiada para Alpha/Beta e se torna obrigatória antes de habilitar qualquer condição diferente de Dry. Falha em qualquer property test obrigatório bloqueia o exit gate do milestone correspondente.
2. **Performance em dispositivo real** é medida a partir de M2/M3. Otimização contínua, não apenas em M10.
3. **Multiplayer (Photon Shared Mode)** somente após Exit Gate M4. Nunca iniciar networking sem física e timing estáveis.
4. **Economia é server-authoritative** desde M8. Client NUNCA modifica moeda, inventário ou resultado diretamente.
5. **Rankings são NÃO-OFICIAIS** em Shared Mode. Nenhum prêmio financeiro ou resultado oficial até migração para server authority.
6. **Codename "Project RKW"** — nenhuma app nas lojas com nome comercial provisório. Bundle ID é placeholder com sufixo de ambiente (.dev, .staging).
7. **Validação com pilotos é GATE obrigatório** em M2-T20 (vertical slice, ≥ 6/10) e M10-T05 (alpha completo, ≥ 7/10). NÃO avançar sem aprovação de pilotos reais.
