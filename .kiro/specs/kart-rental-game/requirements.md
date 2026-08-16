# Requirements Document — Kart Rental Game (Codinome: Project RKW)

## Introduction

Jogo mobile multiplayer de kart rental com dirigibilidade autêntica (simcade), escola de pilotagem progressiva, progressão esportiva sem pay-to-win e monetização ética. Publicado para Android e iOS a partir de uma única base Unity 6.3 LTS (`6000.3.22f1`). Voltado à comunidade brasileira de kart rental, com potencial de expansão global.

> ⚠️ **"Rental Kart World" NÃO é nome aprovado.** Utilizar codinome interno **"Project RKW"** durante desenvolvimento. Nome comercial definitivo pendente verificação de marca e decisão do fundador. O Bundle ID `br.com.suitedigital.rentalkartworld` é placeholder técnico PROVISÓRIO — NÃO criar apps definitivos nas lojas até nome ser escolhido e verificado.

---

## Glossary

| Termo | Definição |
|---|---|
| **Sistema** | O jogo mobile de kart rental em sua totalidade |
| **Piloto** | Usuário humano que participa de uma corrida |
| **Bot** | Agente de IA que substitui ou completa vagas de pilotos humanos |
| **Kart** | Veículo controlado pelo Piloto ou Bot durante a corrida |
| **Física_Engine** | Subsistema Unity responsável pela simulação física do Kart |
| **Escola** | Modo offline de aprendizado de pilotagem |
| **Sessão** | Uma instância de corrida, treino livre ou tomada de tempo |
| **Volta** | Uma volta completa ao redor do traçado da Pista |
| **Pista** | Kartódromo virtual com traçado, setores e superfícies definidos |
| **Linha_Ideal** | Trajetória de referência visual que indica o traçado ótimo |
| **Fiscal** | Representação visual de juiz de pista em posto protegido |
| **Direção_de_Prova** | Sistema que monitora infrações e aplica penalidades |
| **Matchmaking** | Subsistema que agrupa Pilotos em Sessões por critérios definidos |
| **UGS** | Unity Gaming Services — plataforma de backend |
| **ADR** | Architecture Decision Record — registro de decisão arquitetural |
| **MVP** | Minimum Viable Product — escopo mínimo para lançamento |
| **XP** | Pontos de experiência do Piloto |
| **Licença** | Progressão conquistada por categoria de kart |
| **Vácuo** | Redução de arrasto aerodinâmico ao seguir próximo outro Kart |
| **Simcade** | Estilo de gameplay que combina precisão de simulação com acessibilidade de arcade |
| **Tick_Rate** | Frequência de atualização de estado do multiplayer (Hz) |
| **Interpolação** | Técnica de suavização de movimento entre estados de rede recebidos |
| **Predição** | Estimativa local de estado futuro antes da confirmação da rede |
| **CCU** | Concurrent Users — usuários simultâneos |
| **DAU** | Daily Active Users — usuários ativos diários |
| **MAU** | Monthly Active Users — usuários ativos mensais |
| **D1/D7/D30** | Retenção no dia 1, 7 e 30 após primeiro acesso |
| **Apex** | Ponto interno mais próximo da curva no traçado ideal |
| **Slipstream** | Sinônimo de Vácuo no contexto automobilístico |
| **LTS** | Long-Term Support — versão Unity com suporte estendido |
| **URP** | Universal Render Pipeline — pipeline gráfico Unity para mobile |
| **ScriptableObject** | Asset Unity que armazena dados de configuração sem código |
| **Feature_Flag** | Chave remota que ativa/desativa funcionalidades em produção |
| **Índice_de_Pilotagem_Limpa** | Métrica calculada com base em infrações, contatos e fair play |
| **Passe_de_Temporada** | Produto cosmético por tempo limitado sem pay-to-win |
| **State_Authority** | Em Photon Fusion Shared Mode, autoridade sobre um NetworkObject específico — cada cliente tem State Authority sobre seu próprio kart; a autoridade é DISTRIBUÍDA entre os clientes (não centralizada em um host) |
| **Shared_Mode** | Modo do Photon Fusion 2 onde State Authority é DISTRIBUÍDA entre clientes — cada cliente simula e tem autoridade sobre seu próprio NetworkObject; NÃO há host autoritativo central nem servidor dedicado validando estado em tempo real |
| **Plausibility_Check** | Verificação de backend que valida se resultados são fisicamente plausíveis para a categoria, sem reconstrução completa do estado (validação de plausibilidade, não re-simulação) |
| **Volta_de_Saída** | (Out Lap) Volta não cronometrada em que o piloto sai do grid/box e entra na pista para iniciar tentativas |
| **Tentativa_Cronometrada** | Volta cronometrada durante a tomada de tempo; cada piloto tem exatamente 3 tentativas; voltas inválidas CONTAM como tentativa utilizada |
| **Recuperação_Segura** | Reposicionamento do kart na pista, acionado APENAS quando o kart está preso, imóvel, invertido, fora do perímetro recuperável ou criando risco; NUNCA acionado apenas por colisão |
| **SessionContextKey** | Chave de contexto completo de uma sessão: TrackId + TrackConfigurationId + KartCategory + TrackCondition + EnvironmentPreset + GameMode + PhysicsVersion + TrackVersion + RulesetVersion + AssistClass + metadados de telemetria. Utilizada para auditoria, telemetria e reprodução de incidentes. Direction é propriedade canônica de TrackConfiguration (sem duplicação) |
| **LeaderboardKey** | Subconjunto da SessionContextKey contendo APENAS as dimensões que determinam comparabilidade competitiva: TrackConfigurationId + KartCategory + TrackCondition + EnvironmentPreset + GameMode + PhysicsVersion + TrackVersion + RulesetVersion + AssistClass. Utilizada para rankings, volta ideal, consistência e ghost. Tempos com LeaderboardKeys diferentes NUNCA são comparados |
| **TrackConfiguration** | Configuração independente de um traçado (full, short, técnico, horário, anti-horário etc.) com ID estável, spline, grid, checkpoints e dados próprios |
| **EnvironmentPreset** | Preset de ambiente (manhã, entardecer, noite) que configura iluminação, skybox, sombras, pós-processamento e orçamento de performance |
| **TrackCondition** | Condição da pista (seco, úmido, chuva leve, chuva forte) que altera grip, frenagem, tração, visibilidade e comportamento de superfícies |
| **Setor** | Subdivisão do traçado para cronometragem parcial; cada volta é composta de N setores consecutivos |
| **Delta** | Diferença de tempo entre a volta/setor atual e uma referência (negativo = mais rápido, positivo = mais lento) |
| **Volta_Ideal_Teórica** | Soma dos melhores setores válidos pessoais para o mesmo LeaderboardKey |
| **Ghost** | Gravação comprimida de posição/rotação de uma volta, utilizada para visualização assíncrona sem colisão nem interferência física |
| **RaceRuleset** | Conjunto versionado de regras esportivas (penalidades, bandeiras, limites) aplicável a uma sessão |

---

## Requirements

### Requirement 1 — Experiência Principal de Corrida

**User Story:** Como Piloto, eu quero disputar corridas online com formato de kart rental autêntico, para que eu possa competir de forma justa e divertida com amigos e desconhecidos.

#### Acceptance Criteria

1. WHEN o Piloto solicita partida rápida, THE Matchmaking SHALL encontrar uma Sessão compatível em até 60 segundos ou criar uma nova com Bots completando vagas.
2. THE Sistema SHALL suportar até 10 participantes por Sessão (humanos + Bots combinados).
3. WHEN a fase de tomada de tempo se inicia, THE Sistema SHALL permitir 1 volta de saída (não cronometrada) seguida de exatamente 3 tentativas cronometradas por Piloto; voltas inválidas (ex: limites de pista excedidos) CONTAM como tentativa utilizada (consomem 1 das 3 tentativas) mas NÃO registram tempo válido; o grid SHALL ser ordenado pelo melhor tempo válido de cada Piloto; Pilotos sem NENHUM tempo válido SHALL largar no final do grid.
4. WHEN a corrida se inicia, THE Sistema SHALL executar corrida de 10 voltas; o líder VENCE ao cruzar a linha de chegada após completar a 10ª volta válida.
5. WHEN o líder cruza a linha após a 10ª volta (bandeira quadriculada), THE Sistema SHALL sinalizar bandeira quadriculada para os demais pilotos; cada piloto restante SHALL receber a quadriculada ao cruzar a linha pela próxima vez (NÃO precisam completar todas as 10 voltas — retardatários encerram sua corrida ao cruzar a linha após a bandeira).
6. WHEN a bandeira quadriculada é acionada, THE Sistema SHALL iniciar timeout parametrizável (hipótese: 60 segundos — configurável via Remote Config); pilotos que não cruzarem a linha dentro do timeout SHALL ser classificados por: (a) voltas completadas (decrescente), depois (b) tempo total (crescente).
7. WHEN a corrida termina, THE Sistema SHALL exibir resultado com posição final, melhor Volta, penalidades acumuladas, delta de XP e evolução de Licença.
8. WHEN o Piloto desconecta durante uma Sessão, THE Sistema SHALL substituí-lo por um Bot com desempenho compatível com a posição atual.
9. IF o número de humanos cair abaixo de 1, THEN THE Sistema SHALL encerrar a Sessão de forma segura e registrar resultado parcial.
10. THE Sistema SHALL validar resultados, posições e recompensas via plausibility checks em Cloud Code antes de persistir, independente do modelo de autoridade da rede.
11. WHEN Piloto desconecta durante tomada de tempo, THE Sistema SHALL atribuir posição de grid com base no melhor tempo válido já registrado; se nenhum tempo válido foi registrado, o Piloto SHALL largar no final do grid.
12. WHEN sessão de tomada de tempo é interrompida (ex: todos desconectam), THE Sistema SHALL usar os melhores tempos válidos já registrados; Pilotos sem tempo válido SHALL largar no final do grid.

---

### Requirement 2 — Modos de Jogo

**User Story:** Como Piloto, eu quero escolher entre diferentes modos de jogo, para que eu possa praticar offline ou competir online conforme minha disponibilidade.

#### Acceptance Criteria

1. THE Sistema SHALL disponibilizar os modos: Escola offline, Treino Livre, Tomada de Tempo com ghost, Corrida offline contra Bots, Sala Privada online, Partida Rápida online e Campeonato Privado.
2. WHEN o Piloto seleciona Treino Livre, THE Sistema SHALL iniciar a Sessão sem limite de Voltas, sem ranking competitivo e sem penalidades de abandono.
3. WHEN o Piloto seleciona Tomada de Tempo, THE Sistema SHALL exibir o ghost da melhor Volta pessoal do Piloto e registrar o novo melhor tempo ao término.
4. WHEN o Piloto cria uma Sala Privada, THE Sistema SHALL gerar um código alfanumérico único de 6 caracteres e exibir para compartilhamento.
5. WHEN outro Piloto insere o código correto, THE Sistema SHALL adicionar o Piloto à Sala Privada correspondente.
6. WHERE a funcionalidade de Ranking Competitivo estiver ativa via Feature_Flag, THE Sistema SHALL registrar resultados em divisões sazonais.

---

### Requirement 3 — Controles Mobile

**User Story:** Como Piloto usando tela touch, eu quero controles configuráveis e responsivos, para que eu possa pilotar o Kart com precisão sem depender de aceleração automática.

#### Acceptance Criteria

1. THE Sistema SHALL oferecer joystick virtual e volante virtual como opções de controle de direção no lado esquerdo da tela.
2. WHERE a opção de inclinação estiver habilitada, THE Sistema SHALL usar os dados do giroscópio do dispositivo como entrada de direção.
3. THE Sistema SHALL posicionar pedal de acelerador e pedal de freio no lado direito da tela como controle padrão.
4. WHEN o Piloto solta ambos os pedais, THE Sistema SHALL aplicar desaceleração natural (coasting) proporcional à velocidade atual do Kart.
5. THE Sistema SHALL aplicar o acelerador de forma progressiva por rampa temporal de no mínimo 150 ms para evitar spin de pneus na saída.
6. THE Sistema SHALL permitir reposicionamento e redimensionamento de todos os elementos de controle na tela.
7. THE Sistema SHALL persistir a configuração de layout de controles por conta de Piloto via Cloud Save.
8. WHERE a opção de modo canhoto estiver habilitada, THE Sistema SHALL espelhar a disposição padrão de controles.
9. THE Sistema SHALL oferecer configuração de sensibilidade, zona morta e assistência de direção para cada tipo de controle.
10. WHEN háptica estiver habilitada, THE Sistema SHALL emitir vibração distinta para zebra, bloqueio de pneu, contato e perda de aderência.

---

### Requirement 4 — Física Simcade Autêntica

**User Story:** Como Piloto real de kart rental, eu quero que a física reproduza os fundamentos reais de pilotagem, para que meu conhecimento prático se traduza em vantagem no jogo.

#### Acceptance Criteria

1. THE Física_Engine SHALL utilizar Unity PhysX (Rigidbody) para integração, colisões e contatos, combinado com camada custom C# para dinâmica de kart.
2. THE Física_Engine SHALL simular eixo traseiro rígido com transferência de carga lateral ao esterçar, incluindo lift-off da roda traseira interna.
3. WHEN o Piloto aplica esterço excessivo em velocidade elevada, THE Física_Engine SHALL reduzir a velocidade do Kart proporcionalmente ao ângulo de deslizamento.
4. WHEN o Piloto freia em linha reta, THE Física_Engine SHALL aplicar predominantemente freio traseiro e reduzir distância de frenagem versus frenagem com esterço.
5. WHEN o Piloto freia com esterço aplicado, THE Física_Engine SHALL permitir sobre-esterço proporcional à intensidade de frenagem e ângulo.
6. WHEN o Kart passa sobre zebra, THE Física_Engine SHALL desestabilizar o Kart proporcionalmente à altura, ângulo e velocidade de entrada.
7. WHEN o Kart entra em superfície de grama ou sujeira, THE Física_Engine SHALL reduzir coeficiente de aderência em pelo menos 40% (hipótese de calibração).
8. WHEN o Kart segue outro Kart a menos de 1,5 comprimento de Kart de distância por pelo menos 1 segundo, THE Física_Engine SHALL reduzir o arrasto frontal progressivamente em até 8% (hipótese de calibração).
9. THE Física_Engine SHALL armazenar todos os parâmetros de categoria em ScriptableObjects separados por categoria.
10. THE Física_Engine SHALL aplicar perda de velocidade em TODAS as colisões proporcionalmente à velocidade relativa de impacto (escala CONTÍNUA parametrizável — severidade é um valor contínuo, NÃO binário); colisões de qualquer severidade resultam em perda de velocidade proporcional; colisões fortes NÃO acionam reposicionamento automático.
11. THE Física_Engine SHALL acionar recuperação segura (reposicionamento) SOMENTE quando o kart se encontrar em uma das seguintes condições: (a) preso/imóvel por mais de N segundos (configurável, hipótese: 4s), (b) invertido com ângulo > M graus (configurável, hipótese: 85°), (c) fora do perímetro recuperável da pista, (d) criando risco de segurança para outros karts. Colisões por si só, independente da severidade, NUNCA acionam recuperação automática.
12. THE Física_Engine SHALL executar em Fixed Timestep e utilizar testes com tolerâncias definidas; o Sistema NÃO deve afirmar determinismo bit-perfect entre diferentes arquiteturas/OS/dispositivos.
13. THE Física_Engine SHALL modelar forças longitudinais e laterais separadamente para cada eixo do Kart.
14. WHEN colisão de severidade moderada ou alta ocorre, THE Direção_de_Prova SHALL registrar evento com severidade para possível investigação; penalidade SHALL ser aplicada conforme análise (não automaticamente por velocidade de impacto).

---

### Requirement 5 — Categorias de Kart

**User Story:** Como Piloto, eu quero progredir por categorias de potência crescente, para que eu possa desenvolver habilidade gradualmente e ser desafiado de forma justa.

#### Acceptance Criteria

1. THE Sistema SHALL disponibilizar ao menos as categorias Escola (6,5 HP) e Rental Sport (13 HP) no MVP.
2. WHERE a Feature_Flag da categoria Rental Pro estiver ativa, THE Sistema SHALL disponibilizar a categoria 18 HP.
3. THE Sistema SHALL diferenciar cada categoria em aceleração, velocidade máxima, frenagem, aderência, inércia e sensibilidade a erro via ScriptableObject próprio.
4. THE Sistema SHALL exigir Licença da categoria anterior para desbloquear a próxima categoria.
5. WHEN o Piloto participa de corrida online, THE Sistema SHALL equalizar a categoria de todos os participantes para a categoria da Sessão.

---

### Requirement 6 — Escola de Pilotagem

**User Story:** Como iniciante, eu quero completar um currículo progressivo de pilotagem, para que eu aprenda os fundamentos antes de competir online.

#### Acceptance Criteria

1. THE Sistema SHALL disponibilizar os 10 módulos da Escola na sequência definida no GDD, com desbloqueio progressivo.
2. WHEN o Piloto inicia um módulo da Escola, THE Sistema SHALL exibir a Linha_Ideal completamente visível.
3. WHEN o Piloto conclui 50% dos módulos anteriores, THE Sistema SHALL reduzir a opacidade da Linha_Ideal em 50%.
4. WHEN o Piloto conclui todos os módulos anteriores ao último, THE Sistema SHALL ocultar completamente a Linha_Ideal.
5. WHEN o Piloto completa uma Volta, THE Sistema SHALL exibir delta de tempo por setor e feedback específico de: frenagem antecipada/tardia, entrada excessiva, ápice perdido e aceleração antecipada/tardia.
6. WHEN o Piloto conclui a prova de licença com tempo válido e dentro do critério definido, THE Sistema SHALL conceder a Licença da categoria correspondente.
7. IF o Piloto não atingir o critério de tempo da prova de licença, THEN THE Sistema SHALL exibir os setores com maior déficit e sugerir módulos de revisão.

---

### Requirement 7 — Bandeiras e Penalidades

**User Story:** Como Piloto, eu quero que as regras esportivas sejam aplicadas de forma justa e transparente, para que o jogo reflita o fair play do kart rental real.

#### Acceptance Criteria

1. THE Direção_de_Prova SHALL detectar e sinalizar as bandeiras: verde, amarela local, vermelha, azul, branca, preta e quadriculada.
2. WHEN bandeira amarela local é exibida no setor do Piloto, THE Sistema SHALL aplicar restrições conforme Ruleset ativo (comportamentos possíveis: redução de velocidade, proibição de ultrapassagem, e/ou delta de referência futuro); o comportamento específico SHALL ser parametrizável pelo RaceRuleset, sem impor limite fixo de velocidade.
3. WHEN o Piloto ultrapassa sob amarela, THE Direção_de_Prova SHALL registrar infração e aplicar penalidade de tempo de no mínimo 3 segundos (hipótese de calibração).
4. WHEN o Piloto corta a pista com ganho mensurável de tempo, THE Direção_de_Prova SHALL invalidar o tempo do setor ou aplicar penalidade de tempo equivalente.
5. WHEN o Piloto recebe bandeira preta, THE Sistema SHALL notificar visualmente e sonoramente e instruir retorno ao box em até 1 Volta.
6. WHEN o Kart fica imóvel por mais de N segundos (configurável, hipótese: 4s) fora do box, THE Fiscal SHALL acionar amarela local e iniciar protocolo de recuperação segura.
7. WHEN recuperação segura é concluída, THE Sistema SHALL tornar o Kart não colidível por 3 segundos antes de reintegrá-lo à corrida.
8. THE Direção_de_Prova SHALL separar detecção automática de infrações, decisão da direção e feedback ao Piloto em subsistemas distintos.
9. IF a colisão for ambígua conforme critério definido no ADR de regras, THEN THE Direção_de_Prova SHALL não aplicar penalidade automática e registrar evento para revisão.

---

### Requirement 8 — Inteligência Artificial dos Bots

**User Story:** Como Piloto, eu quero que os Bots se comportem de forma humana e justa, para que as corridas offline e online parcialmente preenchidas sejam desafiadoras e divertidas.

#### Acceptance Criteria

1. THE Bot SHALL usar a mesma Física_Engine do Piloto humano, sem modificadores de velocidade ocultos.
2. THE Sistema SHALL disponibilizar 5 perfis de Bot: iniciante, cauteloso, equilibrado, agressivo limpo e rápido.
3. WHEN o Bot comete erro intencional, THE Sistema SHALL aplicar desvio controlado dentro de tolerâncias parametrizadas por perfil.
4. THE Bot SHALL respeitar bandeiras, limites de pista e espaço lateral de outros Karts conforme regras do ADR esportivo.
5. WHEN um Piloto humano desconecta, THE Sistema SHALL substituí-lo por Bot com habilidade proporcional à posição do Piloto desconectado na corrida.
6. WHERE a funcionalidade de reconexão estiver ativa, THE Sistema SHALL permitir ao Piloto retomar controle do Kart em até 60 segundos após reconexão.

---

### Requirement 9 — Multiplayer e Integridade

**User Story:** Como Piloto competitivo, eu quero que o multiplayer seja justo e resistente a trapaças, para que minhas conquistas tenham valor real.

#### Acceptance Criteria

1. THE Sistema SHALL usar Photon Fusion 2 como camada de transporte para estado em tempo real, conforme ADR de networking.
2. THE Sistema SHALL operar inicialmente em Shared Mode onde State Authority é DISTRIBUÍDA entre os clientes — cada cliente tem autoridade sobre seu próprio NetworkObject (kart); NÃO há host autoritativo central validando estado em tempo real; cada cliente simula localmente e publica seu estado para os demais. Aprovado exclusivamente para protótipo, alpha privado e partidas sem prêmio.
3. THE Sistema SHALL incluir caminho de migração explícito para server authority (Host Mode ou Dedicated Server) antes de ativar ranked competitivo, campeonatos com prêmio, resultados oficiais, economia significativa ou matches públicos de alta competitividade.
4. THE Sistema SHALL operar com tick rate definido no ADR de networking e estratégia de interpolação validada para corridas de kart.
5. THE Sistema SHALL documentar limitações anti-cheat do Shared Mode: um cliente trapaceiro pode manipular o estado de seu próprio kart (velocidade, posição, tempo de volta) pois detém State Authority sobre ele; outros clientes apenas observam e interpolam o estado sincronizado; NÃO há entidade central com autoridade para rejeitar estados em tempo real.
6. THE Sistema SHALL implementar as seguintes defesas no Shared Mode: (a) plausibility checks no backend sobre resultados reportados, (b) detecção estatística de anomalias pós-corrida, (c) sistema de denúncia social, (d) telemetria mínima obrigatória para investigação.
7. THE Sistema SHALL validar resultados, posições e recompensas em backend (Cloud Code) antes de persistir; tempos, moedas, inventário e resultados NUNCA devem depender exclusivamente do client.
8. WHEN o Piloto reconecta após queda de conexão de até 30 segundos, THE Sistema SHALL reintegrar o Piloto na posição atual do Bot que o substituiu.
9. THE Sistema SHALL selecionar região de servidor com menor latência disponível e validada para a audiência brasileira (região específica a confirmar após validação técnica e contratual).
10. THE Sistema SHALL incluir no matchmaking os critérios: categoria, nível de habilidade, Índice_de_Pilotagem_Limpa e latência estimada.
11. THE Sistema SHALL rotular rankings de sessões operando em Shared Mode como NÃO-OFICIAIS; nenhum prêmio financeiro, economia significativa ou resultado oficial SHALL ser atribuído enquanto operar exclusivamente em Shared Mode. Rankings de Shared Mode são classificados como "Alpha/Casual" — sem validade competitiva.

---

### Requirement 10 — Progressão Esportiva

**User Story:** Como Piloto dedicado, eu quero uma progressão esportiva rica e transparente, para que meu esforço seja reconhecido sem depender de gastos financeiros.

#### Acceptance Criteria

1. THE Sistema SHALL conceder XP ao Piloto após cada Sessão concluída com base em: posição, voltas completadas, penalidades e comportamento limpo.
2. WHEN o Piloto acumula XP suficiente para o próximo nível, THE Sistema SHALL exibir animação de nível e desbloquear recompensa cosmética correspondente.
3. THE Sistema SHALL calcular Índice_de_Pilotagem_Limpa com base em infrações, contatos, abandonos e respeito a bandeiras.
4. THE Sistema SHALL exibir Licença, XP, Índice_de_Pilotagem_Limpa e histórico de corridas no perfil do Piloto.
5. THE Sistema SHALL persistir toda a progressão via UGS Cloud Save com reconciliação ao reconectar.
6. IF o Piloto tiver progressão local não sincronizada por mais de 24 horas, THEN THE Sistema SHALL alertar e solicitar sincronização antes da próxima Sessão online.

---

### Requirement 11 — Monetização Ética

**User Story:** Como Piloto, eu quero que itens pagos sejam exclusivamente cosméticos, para que eu possa competir em igualdade independente de quanto gasto.

#### Acceptance Criteria

1. THE Sistema SHALL restringir itens pagos exclusivamente a cosméticos: capacetes, balaclavas, luvas, macacões, pinturas de kart, adesivos, comemorações, molduras de perfil, remoção de anúncios e passe de temporada cosmético. NENHUMA compra SHALL oferecer qualquer vantagem de performance, gameplay, progressão ou economia competitiva.
2. THE Sistema SHALL equalizar performance de Kart no multiplayer independente de cosméticos adquiridos.
3. THE Sistema SHALL não incluir loot boxes com probabilidade oculta pagas no MVP.
4. THE Sistema SHALL NUNCA exibir qualquer formato de anúncio (intersticial, rewarded, banner ou qualquer outro) enquanto o Piloto estiver controlando o kart na pista — especificamente: NENHUM banner durante pilotagem. Anúncios são permitidos APENAS em menus, lobby, tela de resultados, garagem e entre sessões (fora da sessão ativa).
5. WHEN anúncio intersticial é exibido (fora de sessão ativa), THE Sistema SHALL respeitar intervalo mínimo de 5 minutos entre exibições.
6. WHERE o Piloto optar por remover anúncios via compra, THE Sistema SHALL desabilitar todos os intersticiais e banners para essa conta permanentemente.
7. THE Sistema SHALL registrar e permitir restauração de todas as compras realizadas conforme políticas de Android e iOS.
8. THE Sistema SHALL disponibilizar política de privacidade e termos de uso acessíveis antes do primeiro login.

---

### Requirement 12 — Performance e Qualidade Técnica

**User Story:** Como Piloto usando dispositivo Android modesto, eu quero que o jogo rode de forma estável, para que a experiência seja justa independente do hardware.

#### Acceptance Criteria

1. THE Sistema SHALL manter mínimo de 30 FPS sustentados em dispositivos Android com SoC de referência modesto definido na matriz de dispositivos do doc `16-test-strategy.md`. Este é o REQUISITO MÍNIMO obrigatório.
2. THE Sistema SHOULD almejar 60 FPS em dispositivos intermediários e avançados definidos na mesma matriz. 60 FPS é META/OBJETIVO apenas em dispositivos selecionados mid/high, não requisito obrigatório universal.
3. THE Sistema SHALL disponibilizar perfis de qualidade Baixo, Médio e Alto com detecção automática e ajuste manual.
4. THE Sistema SHALL reportar FPS, memória e latência via telemetria em tempo real. Para monitoramento térmico, SHALL usar Thermal Status API da plataforma quando disponível (sem exigir leitura exata de temperatura em graus — usar categorias: nominal, light, moderate, severe, critical quando disponíveis).
5. WHEN a média de FPS em JANELA de amostragem de 3 segundos cair abaixo de 28, THE Sistema SHALL reduzir automaticamente o perfil de qualidade em um nível. O upgrade SHALL ocorrer somente quando: (a) média de FPS em janela de 10 segundos superar 55, E (b) COOLDOWN de 30 segundos desde a última alteração tiver sido respeitado, E (c) HISTERESE: não oscilar entre níveis (requer margem mínima de 5 FPS acima do threshold de downgrade antes de considerar upgrade). Estes valores são hipóteses de calibração.
6. THE Sistema SHALL respeitar o orçamento de draw calls, triângulos, texturas e memória definidos no doc `12-art-audio-performance.md`.

---

### Requirement 13 — Analytics e Privacidade

**User Story:** Como responsável pelo produto, eu quero métricas de engajamento e performance confiáveis, para que eu possa tomar decisões baseadas em dados sem violar privacidade dos Pilotos.

#### Acceptance Criteria

1. THE Sistema SHALL registrar eventos de: conclusão de tutorial, tempo por módulo, abandono de módulo, Volta válida/inválida, delta por setor, tipo de controle ativo e assistências habilitadas.
2. THE Sistema SHALL registrar métricas de negócio: retenção D1/D7/D30, DAU/MAU, sessões por usuário, corridas iniciadas/concluídas/abandonadas, impressões de anúncio e conversão de compra.
3. THE Sistema SHALL anonimizar dados antes de envio a terceiros e não coletar dado sensível desnecessário (princípio de minimização de dados).
4. THE Sistema SHALL garantir base legal adequada conforme LGPD/GDPR para cada atividade de processamento de dados — que PODE ser consentimento, execução de contrato, interesse legítimo ou obrigação legal conforme a finalidade. NÃO afirmar que consentimento é a única base legal possível. A base legal apropriada por categoria de dado SHALL ser determinada por revisão jurídica obrigatória antes do lançamento.
5. THE Sistema SHALL disponibilizar mecanismo para o Piloto solicitar exclusão de dados pessoais e revogar consentimento quando este for a base legal utilizada. Transparência sobre quais dados são coletados e para quê é obrigatória independente da base legal.
6. THE Sistema SHALL suportar solicitações de exclusão de dados e revogação de consentimento independentemente da base legal utilizada para o processamento original. Princípio de minimização: coletar apenas dados necessários para a finalidade declarada.

---

### Requirement 14 — Build, Release e Automação

**User Story:** Como fundador com tempo limitado, eu quero um pipeline de build automatizado confiável, para que eu possa focar em validação humana em vez de tarefas manuais de build.

#### Acceptance Criteria

1. THE Sistema SHALL produzir builds Android (.aab) e iOS (IPA) via Unity Build Automation automaticamente por commit/push na branch de release.
2. THE Sistema SHALL executar suite de testes EditMode e PlayMode automaticamente antes de empacotar o build final.
3. IF algum teste crítico falhar, THEN THE Sistema SHALL bloquear o build e notificar o responsável via canal definido.
4. THE Sistema SHALL distribuir builds para teste interno (TestFlight internal, Google Play internal track) automaticamente sem aprovação humana. Distribuição para teste externo (TestFlight external, closed beta público) SHALL requerer aprovação humana explícita.
5. THE Sistema SHALL NUNCA publicar para produção (lojas públicas) automaticamente após commit. Publicação em produção SEMPRE requer gate humano explícito — jamais automático.
6. THE Sistema SHALL versionar builds com esquema semântico MAJOR.MINOR.PATCH e registrar número de build incremental.
7. THE Sistema SHALL utilizar identificadores por ambiente: produção (`br.com.suitedigital.rentalkartworld` — PLACEHOLDER PROVISÓRIO, pendente verificação de marca e decisão do fundador), staging (`.staging`) e desenvolvimento (`.dev`). NÃO criar apps definitivos nas lojas com este identificador até nome ser aprovado.

---

### Requirement 15 — Telemetria Mínima para Investigação Pós-Corrida

**User Story:** Como operador do sistema, eu quero telemetria mínima de cada corrida para que eu possa investigar anomalias e disputas mesmo operando em Shared Mode.

#### Acceptance Criteria

1. THE Sistema SHALL registrar, para cada Piloto em cada Sessão, a seguinte telemetria mínima: melhor tempo por setor, tempo total, penalidades aplicadas, eventos de desconexão, eventos de colisão com severidade e tipo de controle/assistências ativas.
2. THE Sistema SHALL persistir esta telemetria junto ao resultado da corrida no backend.
3. THE Sistema SHALL usar esta telemetria para plausibility checks automatizados e como evidência para investigação manual quando necessário.
4. THE Sistema SHALL definir formato extensível para telemetria, preparado para adicionar dados adicionais (posição GPS fictícia, inputs brutos, latência média) sem quebrar schema existente.

---

### Requirement 16 — Configuração de Pista (Track Configuration)

**User Story:** Como arquiteto do sistema, eu quero que cada pista suporte múltiplas configurações independentes, para que possamos oferecer variedade de layouts sem duplicar cenas inteiras.

#### Acceptance Criteria

1. THE Sistema SHALL suportar múltiplas configurações independentes por pista (ex: completo, curto, técnico, horário, anti-horário).
2. EACH TrackConfiguration SHALL ter: ID estável, nome de exibição, spline de traçado, posições de grid, linha de largada, linha de chegada, entrada/saída de box, checkpoints, setores de cronometragem, limites de pista, linha ideal, pontos de frenagem, caminho de bot, postos de fiscal, sinais, áreas de escape, pontos de recuperação.
3. EACH TrackConfiguration SHALL ter seus próprios recordes e rankings independentes (não compartilhados entre configurações).
4. THE Sistema SHALL NÃO implementar "reverso" meramente invertendo ordem de checkpoints — cada direção é uma configuração validada independente com dados próprios.
5. THE Sistema SHALL reutilizar dados compartilhados (geometria, texturas, colliders da cena) entre configurações sem duplicar cenas Unity inteiras.
6. THE Sistema SHALL referenciar configurações por ID estável (TrackConfigurationId) em toda a arquitetura.
7. **MVP:** 1 pista, 1 configuração (sentido horário). Arquitetura preparada para múltiplas configurações.

---

### Requirement 17 — Presets de Ambiente (Environment Presets)

**User Story:** Como piloto, eu quero correr em diferentes condições de iluminação (manhã, entardecer, noite), para que a experiência visual seja variada e imersiva.

#### Acceptance Criteria

1. THE Sistema SHALL suportar presets de ambiente: manhã (morning), entardecer (late afternoon), noite (night).
2. EACH EnvironmentPreset SHALL configurar: iluminação, skybox, reflexos, exposição, sombras, spotlights (noite), pós-processamento, público/crowd, visibilidade, áudio ambiente e orçamento de performance por tier.
3. THE Sistema SHALL preferir iluminação baked/mista e presets pré-calculados para manter performance mobile.
4. THE Sistema SHALL considerar que karts rental NÃO possuem faróis — corridas noturnas dependem exclusivamente de iluminação do kartódromo.
5. THE Sistema SHALL garantir que o modo noturno NÃO crie desvantagem injusta entre perfis gráficos diferentes (visibilidade essencial deve ser equivalente em Low/Medium/High).
6. **MVP:** Apenas preset "Dia" implementado. Arquitetura preparada para múltiplos presets.

---

### Requirement 18 — Condições de Pista (Track Conditions)

**User Story:** Como piloto, eu quero que condições variáveis de pista (seco, chuva) alterem o comportamento do kart, para que a adaptação a condições faça parte da habilidade de pilotagem.

#### Acceptance Criteria

1. THE Sistema SHALL suportar condições de pista: seco (dry), úmido (damp), chuva leve (light rain), chuva forte (heavy rain), sessão interrompida por condições inseguras.
2. EACH TrackCondition SHALL alterar: aderência longitudinal, aderência lateral, distância de frenagem, tração, aderência de zebra, aderência de grama, linha de borracha, poças, spray, visibilidade, partículas, áudio e háptica.
3. THE Sistema SHALL armazenar valores de condição como hipóteses de calibração em ScriptableObjects.
4. WHEN condição "chuva forte" atingir critério de segurança configurável, THE RaceRuleset PODE acionar bandeira vermelha conforme regulamento ativo.
5. **MVP:** Apenas condição "Seco" implementada. Arquitetura preparada para condições variáveis.

---

### Requirement 19 — Chaves de Contexto e Comparabilidade (Session Context and Leaderboard Keys)

**User Story:** Como sistema competitivo, eu quero chaves distintas para contexto completo e comparabilidade competitiva, para que tempos de contextos diferentes nunca sejam misturados em rankings e toda sessão tenha rastreabilidade completa.

#### Acceptance Criteria

1. THE Sistema SHALL definir SessionContextKey como contexto completo de uma sessão: TrackId + TrackConfigurationId + KartCategory + TrackCondition + EnvironmentPreset + GameMode + PhysicsVersion + TrackVersion + RulesetVersion + AssistClass + metadados de telemetria adicionais. Direction é propriedade canônica de TrackConfiguration — NÃO aparece como campo duplicado.
2. THE Sistema SHALL derivar de cada sessão uma LeaderboardKey canônica contendo APENAS as dimensões que determinam comparabilidade: TrackConfigurationId + KartCategory + TrackCondition + EnvironmentPreset + GameMode + PhysicsVersion + TrackVersion + RulesetVersion + AssistClass. Direction é codificada dentro de TrackConfigurationId.
3. THE Sistema SHALL usar LeaderboardKey para rankings, volta ideal teórica, consistência e ghost.
4. THE Sistema SHALL usar SessionContextKey para telemetria, auditoria, investigação de incidentes e reprodução.
5. THE Sistema SHALL NUNCA comparar tempos com LeaderboardKeys diferentes em um mesmo ranking.
6. WHEN mudança de física, potência, grip, superfícies, checkpoints, limites, cronometragem ou regras que POSSA alterar tempos (nova PhysicsVersion ou TrackVersion), THE Sistema SHALL criar nova versão competitiva, preservar registros históricos separadamente e NÃO misturar tempos antigos com novos. Mudanças exclusivamente cosméticas NÃO requerem nova versão. NÃO usar threshold fixo de tempo (ex: "0,5s") pois impacto depende do comprimento da pista.
7. **MVP:** Implementado com valores únicos (1 pista, 1 config, 1 condição, 1 preset). Arquitetura preparada para versionamento completo.

---

### Requirement 20 — Sistema de Cronometragem e Setores (Timing and Sectors)

**User Story:** Como piloto, eu quero cronometragem detalhada por setor com registro completo de metadados, para que eu possa analisar minha performance em profundidade.

#### Acceptance Criteria

1. THE Sistema SHALL fornecer cronometragem central com: volta atual, última volta, melhor volta pessoal, melhor volta da sessão, melhor volta do campeonato (quando aplicável), recorde da configuração, tempos por setor, delta acumulado, status válida/inválida, motivo de invalidação, volta ideal teórica.
2. EACH volta registrada SHALL incluir: LapId, PilotId, SessionId, SessionContextKey, número da volta, tempo total, tempos por setor, status válida/inválida, motivo de invalidação (se aplicável), penalidades, timestamp, versão de física, tipo de controle, assistências ativas, indicadores de latência.
3. THE Sistema SHALL usar precisão interna MAIOR que precisão de exibição (ex: interno em microsegundos, exibição em milissegundos).
4. THE Sistema SHALL requerer validação de plausibilidade antes de publicar tempos competitivos.
5. **MVP:** Cronometragem core implementada (volta, setores, PB, session best). Telemetria completa na arquitetura.

---

### Requirement 21 — Comparação de Setores (Sector Comparison)

**User Story:** Como piloto, eu quero ver após cada setor se estou mais rápido ou mais lento que minha referência, para que eu possa ajustar minha pilotagem em tempo real.

#### Acceptance Criteria

1. AFTER each sector completion, THE Sistema SHALL exibir: mais rápido/mais lento que referência, diferença numérica, tendência da volta, melhor setor pessoal, melhor setor da sessão.
2. THE Sistema SHALL usar convenção: negativo = mais rápido (verde), positivo = mais lento (vermelho), sem referência = cinza.
3. THE Sistema SHALL usar sinais, números, ícones E texto (não apenas cor) para acessibilidade de daltônicos.
4. DURING corrida, THE Sistema SHALL exibir apenas informação essencial de delta. Detalhes completos disponíveis após volta/corrida e em telemetria/escola.
5. **MVP:** Texto de delta após cada setor. Breakdown completo pós-corrida.

---

### Requirement 22 — Volta Ideal Teórica (Theoretical Ideal Lap)

**User Story:** Como piloto, eu quero saber qual seria meu tempo se juntasse meus melhores setores, para que eu tenha uma meta clara de melhoria.

#### Acceptance Criteria

1. THE Sistema SHALL calcular volta ideal teórica como soma dos melhores setores válidos pessoais para o MESMO LeaderboardKey.
2. THE Sistema SHALL exibir quais setores compõem a volta ideal e quando foram alcançados.
3. THE Sistema SHALL NÃO combinar setores de: pistas diferentes, layouts diferentes, direções diferentes, categorias diferentes, condições diferentes, versões de física diferentes, ou voltas inválidas.
4. **MVP:** Calculada e exibida pós-corrida.

---

### Requirement 23 — Rankings Temporais (Temporal Rankings)

**User Story:** Como piloto, eu quero ver minha posição em diferentes janelas de tempo (pessoal, sessão, semanal, mensal), para que eu tenha múltiplas motivações de melhoria.

#### Acceptance Criteria

1. THE Sistema SHALL suportar arquitetura genérica de rankings por janela temporal: sessão, pessoal, amigos, campeonato, diário, semanal, mensal, temporada, all-time.
2. ALL rankings SHALL respeitar LeaderboardKey — tempos de keys diferentes NUNCA aparecem no mesmo ranking.
3. THE Sistema SHALL definir: timezone (UTC interno), fronteiras de dia/semana/mês/temporada, política de empate (primeiro a alcançar vence), política de invalidação, política de mudança de versão de física.
4. **MVP:** Melhor pessoal, melhor da sessão, campeonato privado, all-time (versão atual). Arquitetura preparada para janelas temporais.
5. **Alpha/Beta:** Diário, semanal, mensal, amigos.
6. **Pós-lançamento:** Temporadas globais, regional, por pista parceira, registros históricos.

---

### Requirement 24 — Ghost e Desafio Assíncrono (Ghost and Async Challenge)

**User Story:** Como piloto, eu quero correr contra meu ghost pessoal e desafiar amigos assincronamente, para que eu possa competir e evoluir mesmo quando não estamos online ao mesmo tempo.

#### Acceptance Criteria

1. THE Sistema SHALL manter ghost da melhor volta pessoal por LeaderboardKey, armazenado LOCALMENTE no dispositivo.
2. THE Sistema SHALL preparar arquitetura de desafio assíncrono: piloto seleciona volta válida → gera desafio → amigo recebe código/convite → corre contra ghost → compara volta e setores → exibe vencedor/diferenças/revanche.
3. THE Ghost SHALL: não ter colisão, não interferir na física, usar amostras GRAVADAS de posição/rotação (NÃO depender de re-simulação determinística), estar associado a LeaderboardKey, ser descartado/marcado incompatível após mudanças relevantes de física/pista.
4. THE Sistema SHALL definir limites de tamanho e retenção de ghosts.
5. **MVP:** Ghost pessoal da melhor volta por LeaderboardKey (local apenas). Um ghost por LeaderboardKey.
6. **Alpha/Beta:** Múltiplos ghosts retidos, ghost de amigo via cloud sync + desafio assíncrono.

---

### Requirement 25 — Campeonato Privado (Private Championship — expande R2)

**User Story:** Como organizador de campeonato entre amigos, eu quero configurar um campeonato completo com pontuação, calendário e regras, para que possamos ter uma competição estruturada.

#### Acceptance Criteria

1. THE Sistema SHALL suportar configuração de campeonato: nome, admin, lista de participantes, calendário de etapas, pista+configuração por etapa, categoria, formato de classificação, número de voltas, sistema de pontuação, penalidades, descarte de piores resultados (opcional), critério de desempate, treino/classificação/corrida por etapa.
2. THE Sistema SHALL oferecer presets de pontuação configuráveis. MVP: preset único "Standard 10" (25-18-15-12-10-8-6-4-2-1). NÃO usar nome "F1" em produto ou modelo de dados.
3. THE Sistema SHALL suportar bônus opcionais (desabilitados por padrão): pole position, volta mais rápida, corrida limpa, mais posições ganhas.
4. **MVP:** Campeonato básico com preset "Standard 10". Sem bônus, sem descarte.
5. **Alpha/Beta:** Bônus, descarte, presets adicionais, customização completa.

---

### Requirement 26 — Consistência do Piloto (Pilot Consistency)

**User Story:** Como piloto, eu quero ver minha consistência de voltas, para que eu saiba se estou evoluindo além do tempo bruto.

#### Acceptance Criteria

1. THE Sistema SHALL calcular consistência a partir da variação entre voltas válidas comparáveis (mesmo LeaderboardKey).
2. THE Sistema SHALL exibir: melhor volta, média, variação (desvio padrão ou similar), diferença melhor-para-pior, número de voltas dentro de margem configurável do melhor.
3. THE Sistema SHALL NÃO reduzir habilidade a um score opaco — se exibir score, SHALL mostrar cálculo/explicação.
4. **MVP:** Estatísticas básicas pós-corrida.
5. **Alpha/Beta:** Consistência avançada com histórico.

---

### Requirement 27 — Caderno do Piloto (Pilot Notebook)

**User Story:** Como piloto dedicado, eu quero um registro completo da minha carreira, para que eu possa acompanhar minha evolução ao longo do tempo.

#### Acceptance Criteria

1. THE Sistema SHALL manter caderno do piloto contendo: melhor volta por pista/configuração, melhores setores, volta ideal, histórico de corridas, vitórias, pódios, poles, voltas mais rápidas, penalidades, abandonos, índice de pilotagem limpa, evolução de licença, consistência, tipo de controle, assistências, comparação com amigos.
2. **MVP:** Resumo simples (melhores tempos, posição média, vitórias/pódios).
3. **Pós-lançamento:** Histórico avançado, gráficos, filtros, evolução temporal.

---

### Requirement 28 — Instrutor de Pilotagem (Driving Instructor)

**User Story:** Como piloto em aprendizado, eu quero receber feedback contextual durante a pilotagem, para que eu aprenda técnicas sem precisar sair para consultar tutoriais.

#### Acceptance Criteria

1. THE Sistema SHALL fornecer feedback via: texto, elementos visuais e áudio (futuro).
2. THE Sistema SHALL incluir mensagens como: "Freie em linha reta", "Você freiou tarde", "Solte o freio antes do ápice", "Acelere progressivamente", "Boa saída de curva", "Você perdeu o ápice", "Bandeira azul, facilite a passagem".
3. THE Sistema SHALL respeitar regras de UX: mensagens curtas, sem repetição excessiva, cooldown entre mensagens, prioridades (segurança > técnica > informação), opção de desabilitar, volume separado, legendas, NÃO falar durante comunicações críticas da direção de prova, NÃO sobrecarregar iniciantes.
4. **MVP:** Texto + sinais visuais. Idioma pt-BR apenas, porém TODOS os textos devem utilizar Unity Localization / String Tables (zero strings hardcoded). Idioma inglês é Alpha/Beta.
5. **Alpha/Beta:** Áudio básico. Inglês.
6. **Pós-lançamento:** Coach adaptativo baseado em dados de performance.

---

### Requirement 29 — Desafios Diários/Semanais (Daily/Weekly Challenges)

**User Story:** Como piloto casual, eu quero desafios rotativos com objetivos variados, para que eu tenha motivação para jogar regularmente.

#### Acceptance Criteria

1. THE Sistema SHALL suportar sistema genérico de desafios: melhorar PB, voltas consistentes, corrida sem contato, corrida sem penalidade, vencer sem linha ideal, completar módulo de treinamento, ganhar posições limpamente, superar ghost.
2. THE Sistema SHALL recompensar com: XP, moeda não-premium, cosméticos, badges, progresso de passe de temporada. NENHUMA recompensa SHALL oferecer vantagem de performance.
3. THE Sistema SHALL permitir ativação e configuração de templates EXISTENTES no build via Remote Config. Remote Config pode ativar, desativar e parametrizar templates EXISTENTES no build. Novos tipos de desafio, lógica ou assets exigem atualização do aplicativo (NÃO é possível criar novos ScriptableObjects via Remote Config).
4. THE Sistema SHALL começar com poucos templates reutilizáveis.
5. **MVP:** Arquitetura de templates preparada. Desafio estático de onboarding OPCIONAL. Sistema de rotação diária/semanal é Alpha/Beta.
6. **Alpha/Beta:** Sistema completo com rotação diária/semanal.

---

### Requirement 30 — Cartão Compartilhável (Shareable Card)

**User Story:** Como piloto orgulhoso de um bom resultado, eu quero gerar um cartão visual para compartilhar nas redes sociais, para que eu possa mostrar minhas conquistas.

#### Acceptance Criteria

1. THE Sistema SHALL gerar cartão de resultado contendo: nome do piloto, posição, melhor volta, pista, categoria, data, capacete/kart, pódio (se aplicável), código de desafio (quando aplicável).
2. THE Sistema SHALL NÃO incluir informação pessoal sensível, NÃO publicar automaticamente — piloto DEVE explicitamente solicitar compartilhamento.
3. **Alpha/Beta:** Cartão gerado compartilhável. Tela de resultado preparada para screenshot no MVP.

---

### Requirement 31 — Direção de Prova Explicável (Explainable Race Direction)

**User Story:** Como piloto que recebeu penalidade, eu quero entender exatamente o que aconteceu e por quê, para que eu possa aceitar a decisão e aprender.

#### Acceptance Criteria

1. EVERY penalidade registrada SHALL incluir: tipo, momento (lap + setor + timestamp), regra aplicada, evidência disponível, valor da punição, consequência, origem (automática vs manual).
2. THE UI SHALL explicar de forma simples: o que aconteceu, onde, qual regra, qual punição.
3. **MVP:** Texto + volta + setor + regra.
4. **Pós-lançamento:** Replay/representação visual do incidente.

---

### Requirement 32 — Evolução da Pista (Track Evolution)

**User Story:** Como arquiteto de sistemas de simulação, eu quero uma arquitetura preparada para evolução dinâmica de pista, para que possamos adicionar grip progressivo e efeitos de borracha no futuro.

#### Acceptance Criteria

1. THE Sistema SHALL preparar arquitetura para: aumento progressivo de grip, linha de borracha, sujeira trazida da grama, mudança de grip após chuva, evolução por voltas/passagens de karts.
2. THE primeira implementação futura PODE usar multiplicadores por setor/superfície.
3. **MVP: NENHUMA implementação de evolução de pista. Apenas documentação de extension points (sem implementar interfaces, serviços ou ScriptableObjects sem consumidor real).**

---

### Requirement 33 — Lastro e Categorias de Peso (Ballast and Weight Categories)

**User Story:** Como organizador de campeonato avançado, eu quero opções de lastro e equalização por peso, para que a competição seja mais justa em categorias superiores.

#### Acceptance Criteria

1. THE Sistema SHALL reservar espaço na arquitetura para: categoria aberta, peso mínimo, lastro virtual, equalização de campeonato.
2. THE Sistema SHALL NÃO coletar peso corporal no MVP.
3. WHEN coleta de peso for implementada (pós-MVP), SHALL ser: opcional, justificada, protegida, com revisão de privacidade.
4. **Pós-MVP exclusivamente. Registrado no roadmap.**

---

### Requirement 34 — Procedimentos de Largada (Start Procedures)

**User Story:** Como piloto, eu quero que a largada reproduza procedimentos reais de kart, para que a experiência seja autêntica e tensa.

#### Acceptance Criteria

1. THE Sistema SHALL implementar no MVP: largada parada (standing start), sequência de semáforo (número de luzes configurável pelo Ruleset/StartProcedure — hipótese inicial: 5 luzes), detecção de queima de largada (false start), grid simples.
2. THE Sistema SHALL preparar arquitetura para pós-lançamento: largada lançada (rolling start), volta de formação, procedimentos especiais, regulamentos distintos por campeonato.
3. **MVP:** Standing start com semáforo e detecção de false start.

---

### Requirement 35 — Princípios de Interface Intuitiva (Intuitive Interface Design)

**User Story:** Como piloto em corrida, eu quero ver APENAS as informações necessárias para decisões imediatas, para que eu não seja distraído por excesso de dados.

#### Acceptance Criteria

1. DURING pilotagem ativa, THE Sistema SHALL exibir APENAS: posição, volta, tempo atual, delta simples, bandeira ativa, piloto próximo, indicadores de pedal/volante. NÃO exibir toda a telemetria simultaneamente durante corrida.
2. AFTER cada volta, THE Sistema SHALL exibir brevemente: tempo da volta, delta vs melhor, melhor volta, status válida/inválida.
3. AFTER corrida, THE Sistema SHALL exibir: resultado completo, setores, comparação, volta ideal, consistência, penalidades, XP, pontos de campeonato.
4. THE Sistema SHALL disponibilizar área de análise separada: histórico, gráficos, ghost, caderno, comparação detalhada.
5. THE Sistema SHALL suportar modos de interface: Essencial, Completo, Customizado.
6. **MVP:** Modo Essencial implementado. Arquitetura para modos.

---

### Requirement 36 — Itens Pós-MVP Formalizados (Post-MVP Roadmap Items)

**User Story:** Como fundador, eu quero que itens pós-MVP estejam formalmente documentados com requisitos mínimos, para que a arquitetura atual os considere mesmo sem implementá-los.

#### Acceptance Criteria

1. THE Sistema SHALL documentar e considerar na arquitetura os seguintes itens pós-MVP: Real Track Partner Platform, pistas oficiais via licenciamento, Addressables para track packs, pipeline para fotos/vídeos/fotogrametria/validação, campeonatos híbridos (virtual + real), integração futura de agendamento (booking), patrocínios e revenue sharing.
2. THE Sistema SHALL NÃO implementar nenhum destes itens no MVP.
3. THE Sistema SHALL garantir que a arquitetura MVP (Addressables, TrackConfiguration, SessionContextKey, LeaderboardKey, sistema de rankings) NÃO impeça a adição futura destes itens.
