# Master Prompt para o Kiro — Projeto de jogo de kart rental

Copie todo este documento e envie ao Kiro em **Spec Mode**. O objetivo desta etapa é criar a documentação de engenharia e produto em Markdown. Não programe o jogo ainda.

---

## Papel do Kiro

Atue como um time sênior composto por game designer, arquiteto Unity, engenheiro de física, engenheiro multiplayer, especialista mobile, product manager, QA lead, especialista de segurança e monetização ética.

Transforme a visão abaixo em especificações executáveis para que um agente de desenvolvimento, principalmente o Codex, implemente o jogo com o mínimo de decisões implícitas. Questione somente contradições realmente bloqueantes. Para lacunas não bloqueantes, registre uma suposição explícita e marque-a para validação.

Não gere código de produção nesta fase. Gere exclusivamente documentos Markdown, diagramas Mermaid quando úteis, tabelas, critérios de aceite, riscos, decisões e backlog.

## Contexto do fundador

- Fundador solo com experiência em infraestrutura/cloud e kart rental; dados pessoais não são necessários na especificação.
- O fundador participa de um campeonato de kart na vida real e terá amigos pilotos como primeiros testadores.
- Equipamentos disponíveis: Mac, celular Android e iPhone para testes.
- Ferramentas disponíveis: Codex, Kiro e Gemini.
- Objetivo: publicar para Android e iOS, validar com a comunidade de kart rental e construir um negócio rentável sem pay-to-win.
- Disponibilidade presumida do fundador: 8 a 12 horas por semana para validação, testes e decisões.

## Visão do produto

Criar um jogo mobile multiplayer de kart rental com sensação autêntica, acessível a iniciantes e profundo para pilotos reais. O jogo não é um clone de Mario Kart: não possui armas, cascos, poderes fantasiosos ou turbo mágico. A vantagem vem de traçado, frenagem, retomada do acelerador, consistência, vácuo, defesa limpa e leitura de corrida.

Proposta de valor provisória:

> Aprenda a pilotar, conquiste sua licença e dispute campeonatos online de kart rental.

O visual deve ser 3D semi-realista, inspirado em kartódromos brasileiros: asfalto, barreiras de pneus, zebras, boxes, fiscais, placas de frenagem, arquibancada, paddock e clima. As pistas iniciais são fictícias para evitar uso não autorizado de nomes ou traçados reais. Parcerias com kartódromos reais podem ser adicionadas futuramente mediante autorização.

## Plataformas e tecnologia preferida

- Android e iOS a partir da mesma base de código.
- Unity 6.3 LTS (`6000.3.22f1`) vigente no início do projeto.
- C#.
- URP para renderização mobile.
- Unity Input System, Cinemachine e Addressables.
- Photon Fusion 2 para multiplayer em tempo real.
- Unity Gaming Services para Authentication, Player Names, Cloud Save, Economy, Leaderboards, Remote Config e Cloud Code quando necessário.
- Unity IAP para compras no aplicativo.
- AdMob ou solução equivalente, sempre com anúncios fora da corrida.
- GitHub privado com Git LFS.
- Unity Build Automation para builds Android/iOS.
- TestFlight para iOS e trilhas de teste da Google Play para Android.
- Desenvolvimento orientado por documentação, testes, telemetria e feature flags.

Não trate essas escolhas como absolutas: crie Architecture Decision Records para confirmar ou substituir cada uma com justificativa, impacto, custo, risco e plano de saída.

## Experiência principal de corrida

Formato inicial pretendido:

1. Jogador entra por matchmaking ou código de sala.
2. Sessão suporta até 10 pilotos; no MVP, começar com 4 humanos e completar vagas com bots.
3. Três voltas de tomada de tempo.
4. Grid definido pelo melhor tempo válido.
5. Corrida de 10 voltas.
6. Resultado com posição, melhor volta, penalidades, evolução de licença e recompensas.
7. Revanche ou retorno ao lobby.

Modos iniciais:

- Escola de pilotagem offline.
- Treino livre.
- Tomada de tempo com ghost da melhor volta.
- Corrida offline contra bots.
- Sala privada online.
- Partida rápida online.
- Campeonato privado entre amigos.
- Ranking competitivo e temporadas apenas após validação do MVP.

## Controles mobile

Não usar aceleração automática como padrão.

- Lado esquerdo: joystick virtual ou volante virtual.
- Alternativa: inclinação do celular.
- Lado direito: pedal de acelerador e pedal de freio.
- Soltar os dois pedais deve permitir coasting/desaceleração natural.
- Acelerador deve ter aplicação progressiva por rampa temporal ou gesto vertical; não presumir suporte a pressão física da tela.
- Botão opcional para olhar para trás.
- Controles devem ser reposicionáveis e redimensionáveis.
- Vibração/háptica configurável.
- Acessibilidade: canhoto, sensibilidade, zona morta, assistência de direção, assistência de frenagem, linha ideal, contraste e redução de movimento.

## Física e dirigibilidade

O jogo deve ser classificado como **simcade autêntico**: fundamentos do kart real com assistência progressiva para telas touch.

Comportamentos desejados:

- Eixo traseiro rígido e necessidade de aliviar a roda traseira interna durante a curva.
- Perda de velocidade quando o jogador esterça excessivamente e “amarra” o kart.
- Transferência de peso em frenagem, entrada e aceleração.
- Freio predominantemente traseiro nos rental; frenagem forte com esterço pode provocar sobre-esterço/rodada.
- Frenagem mais eficiente em linha reta.
- Zebras desestabilizam o kart conforme altura, ângulo e velocidade.
- Grama, sujeira e pista molhada reduzem aderência.
- Colisões leves retiram velocidade; impactos graves podem acionar recuperação e penalidades.
- Pneus frios podem ser uma configuração avançada, não requisito do primeiro protótipo.
- Peso do piloto deve ser considerado somente se houver sistema justo de equalização/lastro; nunca exigir dado corporal sensível sem necessidade.
- Vácuo reduz progressivamente o arrasto atrás de outro kart e permite ganhar velocidade na reta. O efeito deve depender de distância, alinhamento, tempo e velocidade, sem parecer nitro.
- O piloto que freia corretamente, atinge o ápice e acelera cedo deve ganhar tempo mensurável.

Categorias provisórias:

| Categoria | Potência | Propósito |
|---|---:|---|
| Escola | 6,5 HP | Controles e fundamentos |
| Rental | 9 HP | Primeiras corridas |
| Rental Sport | 13 HP | Intermediário |
| Rental Pro | 18 HP | Competitivo |
| Competição futura | 25–30 HP | Expansão posterior |

Cada categoria deve alterar aceleração, velocidade máxima, frenagem, aderência, inércia, sensibilidade e tolerância a erro — não apenas multiplicar velocidade.

Defina parâmetros em ScriptableObjects e crie uma estratégia de calibração com telemetria, testes A/B internos e comparação com pilotos reais, sem afirmar que os números equivalem a um kart físico certificado.

## Escola de pilotagem

Criar currículo progressivo:

1. Equipamentos e segurança: capacete integral, balaclava, luvas, macacão, calçado fechado, banco e pedais.
2. Acelerar, aliviar, frear e parar no box.
3. Slalom e movimentos suaves.
4. Frenagem em linha reta com placas 50/30/10.
5. Traçado: entrada, ápice e saída.
6. Controle de sobre-esterço e recuperação.
7. Ultrapassagem limpa e defesa.
8. Vácuo e distância de frenagem.
9. Bandeiras e conduta.
10. Prova de licença com voltas válidas e tempo mínimo.

A linha ideal deve poder começar totalmente visível e desaparecer conforme o progresso. Mostrar delta para melhor volta e feedback específico por curva: frenagem antecipada/tardia, entrada excessiva, ápice perdido e aceleração antecipada/tardia.

## Regras, bandeiras e fiscais

Modelar pelo menos:

- Verde: pista liberada/início.
- Amarela local: perigo, reduzir e não ultrapassar no setor.
- Vermelha: sessão interrompida.
- Azul: informar piloto prestes a levar volta e necessidade de facilitar passagem conforme regra adotada.
- Branca: veículo muito lento no setor, conforme regulamento escolhido.
- Preta: entrar nos boxes/desclassificação conforme decisão da direção de prova.
- Quadriculada: fim da sessão.

Criar ADR específico para qual regulamento esportivo será a fonte de verdade. Regras locais de kartódromos podem variar e devem ser parametrizáveis.

Fiscais ficam em postos protegidos. Quando um kart fica preso:

1. Acionar amarela local.
2. Proibir ultrapassagens no setor.
3. Avaliar janela segura.
4. Animar recuperação apenas quando seguro.
5. Reposicionar o kart com perda de tempo.
6. Tornar o kart temporariamente não colidível ao retornar.
7. Liberar o setor.

Se a recuperação física for complexa para o MVP, usar reset seguro com animação curta e penalidade, mantendo o mesmo fluxo esportivo.

Penalidades planejadas:

- Queima de largada.
- Ultrapassagem sob amarela.
- Corte de pista com ganho de tempo.
- Contato evitável.
- Empurrar outro kart de forma reiterada.
- Ignorar bandeira azul.
- Direção contrária.
- Abandono frequente de partidas.

Separar detecção automática, decisão da direção de prova e feedback ao jogador. Evitar punição automática injusta em colisões ambíguas.

## Bots

Bots são obrigatórios para impedir salas vazias.

- Devem usar a mesma física básica dos humanos.
- Perfis: iniciante, cauteloso, equilibrado, agressivo limpo e rápido.
- Erros humanos controlados, sem trapaça de velocidade.
- Respeitar bandeiras, limites e espaço lateral.
- Escalonar dificuldade por consistência, ponto de frenagem, defesa e aproveitamento do vácuo.
- Permitir substituição de piloto desconectado por bot e possível retomada do controle após reconexão.

## Multiplayer e integridade competitiva

Especificar claramente:

- Modelo de autoridade do MVP e do competitivo.
- Tick rate e estratégia de interpolação/predição a validar.
- Sincronização apenas do necessário.
- Lag compensation apropriada a corrida, sem esconder colisões impossíveis.
- Reconnect, abandono, host migration ou dedicated server.
- Regiões e latência para Brasil.
- Matchmaking por categoria, habilidade, reputação e ping.
- Salas privadas com código.
- Bots completando vagas.
- Estado de corrida resiliente a perda de pacote.
- Prevenção de speed hack, alteração de tempo, moeda, inventário e resultados.
- Resultados e recompensas validados em backend.
- Replay mínimo ou telemetria para investigação de campeonatos.

Começar simples, mas definir o caminho para servidor autoritativo antes de ranking valendo prêmio.

## Progressão e monetização ética

Separar progressão esportiva de monetização.

Não vender potência ou aderência no multiplayer equalizado.

Progressão conquistada:

- Licenças por categoria.
- Experiência de piloto.
- Índice de pilotagem limpa.
- Ranking e divisões.
- Medalhas de treinamento.
- Troféus e histórico de campeonatos.

Itens cosméticos:

- Capacetes e viseiras.
- Balaclavas.
- Luvas.
- Macacões e botas.
- Pinturas e números do kart.
- Adesivos originais.
- Aparência do box e equipe.
- Comemorações de pódio.
- Molduras de perfil.

Receita:

- Anúncio recompensado opcional para bônus não competitivo.
- Anúncios intersticiais somente em pausas naturais e com limite de frequência.
- Compra para remover anúncios.
- Cosméticos diretos.
- Passe de temporada cosmético.
- Patrocínios e placas nas pistas.
- Campeonatos e pistas licenciadas futuramente.
- Modo carreira pode ter upgrades mecânicos, mas o competitivo online deve equalizar equipamento.

Não usar loot boxes pagas no MVP. Não explorar crianças. Criar política de gasto, restauração de compras, consentimento, privacidade e controles parentais quando aplicável.

## Direção de arte e performance

- 3D semi-realista, não fotorealista.
- Proporções autênticas de kart rental.
- Iluminação baked/mista sempre que possível.
- LODs, occlusion culling, texture atlases, GPU instancing e object pooling.
- Personagens e espectadores distantes com baixa complexidade.
- Metas iniciais: 30 FPS estáveis em aparelhos Android modestos e 60 FPS em aparelhos intermediários/avançados quando possível.
- Definir orçamento por frame, memória, draw calls, triângulos, texturas, rede e tamanho do download.
- Qualidade Baixa, Média e Alta.
- Evitar marcas, logotipos, pilotos, pistas e pinturas reais sem licença.

## Áudio e háptica

- Motor responde a RPM/carga.
- Pneus comunicam limite de aderência.
- Zebra, grama, contato e barreira possuem feedback distinto.
- Vento cresce com velocidade.
- Bandeiras e direção de prova têm sinais sonoros discretos.
- Háptica configurável para zebra, bloqueio, contato e perda de aderência.
- Música não deve mascarar motor durante corrida.

## Analytics e indicadores

Definir eventos e propriedades para:

- Conclusão do tutorial.
- Tempo por aula.
- Abandono de aula.
- Volta válida/inválida.
- Delta por setor.
- Tipo de controle.
- Assistências ativas.
- FPS, memória, crash e latência.
- Matchmaking, tempo de espera e preenchimento por bots.
- Corrida iniciada/concluída/abandonada.
- Retenção D1, D7 e D30.
- DAU/MAU.
- Sessões e corridas por usuário.
- Impressões de anúncio e receita por usuário.
- Conversão e ticket de compra.
- Recompensas, moedas criadas/gastas e inflação.
- Índice de pilotagem limpa.

Definir guardrails de privacidade e evitar coletar dados desnecessários.

## Escopo do MVP

O MVP deve conter:

- Android e iOS.
- Uma pista fictícia outdoor.
- Categorias Escola 6,5 HP e Rental Sport 13 HP; 18 HP pode ficar atrás de feature flag.
- Um modelo base de kart com variações cosméticas.
- Escola com controles, frenagem e traçado.
- Treino livre.
- Tomada de tempo de três voltas.
- Corrida de dez voltas.
- Até quatro humanos online e seis bots inicialmente.
- Sala privada por código.
- Login, nome, perfil e cloud save.
- Ranking de melhor volta e campeonato privado.
- Bandeiras verde, amarela, azul, vermelha e quadriculada.
- Vácuo.
- Recuperação segura.
- Garagem e cosméticos básicos.
- Telemetria, crash reporting e configurações remotas.
- Build automatizado Android/iOS.

Fora do MVP:

- Pistas reais licenciadas.
- Premiação em dinheiro.
- Voz/chat aberto.
- Clãs complexos.
- Mercado entre jogadores.
- Danos mecânicos detalhados.
- Chuva dinâmica completa.
- 25–30 HP.
- Dedicated servers globais em grande escala, salvo se indispensáveis tecnicamente.

## Roadmap esperado

Planeje milestones com dependências e critérios de saída:

1. Preprodução e riscos.
2. Kart dirigível em pista cinza.
3. Vertical slice offline.
4. Corrida contra bots.
5. Multiplayer privado.
6. Escola e licenças.
7. Perfil, garagem e ranking.
8. Alpha com amigos do campeonato real.
9. Beta Android e TestFlight.
10. Soft launch.
11. Lançamento e LiveOps.

Planeje para um fundador não desenvolvedor usando agentes de IA, com validação humana obrigatória. Forneça estimativa otimista, base e pessimista. Diferencie horas de agente, horas humanas e tempo de calendário.

## Documentos obrigatórios a gerar

Crie a seguinte estrutura no repositório:

```text
docs/
  00-index.md
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
  18-product-backlog.md
  19-risk-register.md
  20-open-questions.md
  adr/
    0001-engine.md
    0002-networking.md
    0003-backend.md
    0004-rendering.md
    0005-build-pipeline.md
    0006-source-of-sporting-rules.md
AGENTS.md
README.md
```

## Requisitos de cada documento

Cada documento deve conter:

- Objetivo e escopo.
- Decisões confirmadas.
- Suposições.
- Requisitos funcionais.
- Requisitos não funcionais.
- Casos de erro e borda.
- Critérios de aceite verificáveis.
- Dependências.
- Riscos e mitigação.
- Questões abertas.
- Links para documentos relacionados.

O `18-product-backlog.md` deve conter épicos e histórias pequenas com:

- ID estável.
- Título.
- História do usuário.
- Prioridade MoSCoW.
- Dependências.
- Critérios de aceite em Given/When/Then.
- Testes esperados.
- Telemetria esperada.
- Estimativa relativa XS/S/M/L/XL.
- Milestone.

Nenhuma história XL pode chegar ao Codex. Quebre-a antes.

## AGENTS.md obrigatório

Escreva regras duráveis para o Codex:

- Ler documentos e ADRs relevantes antes de editar.
- Trabalhar em uma história por branch/PR.
- Nunca alterar arquitetura silenciosamente.
- Não incluir segredos no repositório.
- Não adicionar pacote Unity sem ADR/aprovação.
- Manter builds Android e iOS.
- Criar testes para lógica pura, física parametrizada, regras e backend.
- Executar testes e registrar evidências.
- Manter performance budgets.
- Não usar marcas/assets sem licença.
- Não implementar pay-to-win.
- Não confiar em dados do cliente para moeda, ranking ou resultado competitivo.
- Preservar acessibilidade e controles configuráveis.
- Atualizar documentação quando comportamento mudar.
- Definir “done” somente com critérios de aceite atendidos e revisão do diff.

## Estratégia de testes

Especifique:

- EditMode tests para matemática, regras, economia e serialização.
- PlayMode tests para fluxo de corrida, checkpoints, voltas, bandeiras e reset.
- Testes de física determinísticos dentro de tolerâncias.
- Testes de rede com latência, jitter, perda e desconexão.
- Testes anti-cheat.
- Testes de carga de lobby/matchmaking.
- Performance em matriz de aparelhos Android e iPhones.
- Testes de bateria e temperatura.
- Testes de compras, restauração e falhas.
- Testes de acessibilidade.
- Testes de publicação e privacidade.
- Sessões humanas com pilotos reais.

Crie uma matriz mínima de aparelhos e use placeholders para modelos ainda não confirmados.

## Critérios de qualidade dos Markdown

- Linguagem principal: português do Brasil.
- Termos técnicos podem permanecer em inglês quando padrão da indústria.
- Sem afirmações vagas como “otimizar bastante”. Use metas mensuráveis ou marque `TBD` com plano para medir.
- Não inventar números físicos apresentados como fatos. Valores iniciais devem ser “hipóteses de calibração”.
- Não ocultar riscos.
- Identificar explicitamente trabalho que exige ação humana no Unity Editor, consoles das lojas, testes físicos ou aprovação jurídica.
- Manter rastreabilidade entre visão, requisitos, backlog, testes e telemetria.
- Usar diagramas Mermaid somente quando melhorarem arquitetura, fluxo ou estado.
- Criar glossário para termos como CCU, DAU, tick rate, interpolation, prediction, apex, lift-off, slipstream e server authority.

## Saída final do Kiro

Ao terminar:

1. Gere todos os arquivos acima.
2. Mostre uma árvore de arquivos.
3. Liste decisões confirmadas.
4. Liste suposições.
5. Liste questões que bloqueiam a implementação.
6. Liste as dez primeiras histórias prontas para o Codex, em ordem.
7. Faça uma revisão cruzada procurando contradições.
8. Confirme que nenhum código de produção foi criado.

Não reduza o escopo documental por limite de resposta; gere os arquivos diretamente no workspace/repositório.
