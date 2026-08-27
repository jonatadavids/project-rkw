# 30 — Log de playtest do fundador (rodadas informais, pós-M2-T01)

## Estado

**Não substitui M2-T20/M2-T21.** Este documento registra uma sequência de
rodadas de playtest informal, feitas exclusivamente pelo fundador dirigindo
builds Android reais (`./scripts/build_deploy_verify.sh`) e reportando
problemas em texto, com correções aplicadas rodada a rodada. É uma evidência
útil de estabilização da física e da experiência antes do checkpoint formal,
mas **não satisfaz** o critério de M2-T20 ("distribuir para ao menos 2
pilotos reais de kart... avaliação numérica 0-10 por critério"). Esse
checkpoint segue em aberto — ver `docs/playtests/M2-playtest-01-template.md`,
criado junto com este log para facilitar rodá-lo quando o fundador tiver
acesso a pilotos adicionais.

Cobre as rodadas a partir de 2026-08-20 (após `docs/29-kart-dynamics-prototype.md`
e a implementação inicial de M2-T07/T09/T10/T12/T16/T18/T19). Rodadas
anteriores a esta data não têm registro detalhado neste documento.

## Rodada — 2026-08-20 (bots, contagem regressiva, buracos na grama)

Feedback do fundador: bots pararam de travar e passaram a finalizar corridas;
modo difícil não parecia difícil; a contagem "VAI" ficava presa na tela após
a largada; áreas da pista pareciam ter "buracos" que prendiam o kart.

Correções:
- `RaceStartController`: a contagem agora sempre avança e o objeto se
  autodestrói ao final, em vez de travar na tela após liberar o kart.
- Dificuldade dos bots passou a escalar `KartBotMath.GetMinThrottle`/
  `GetMaxThrottle`, não apenas precisão de curva.
- Diagnosticado como problema de "internal edge" do PhysX entre colliders
  não-coplanares (grama vs. pista) — primeira tentativa de correção (reduzir
  o degrau de altura) foi insuficiente; corrigido definitivamente na rodada
  seguinte deixando as superfícies perfeitamente coplanares.

## Rodada — 2026-08-20 (continuação: impacto antes de curva, classificação, sons)

Feedback do fundador (resumo): kart ainda travava com impacto forte antes de
curvas mesmo depois do fix de buracos; classificação/nomes dos bots não
apareciam em lugar nenhum; sugestão de mostrar melhor tempo do dia/semana/
histórico; sons de derrapagem ruins e mascarados pelo som ambiente; sugestão
de bots fazendo ultrapassagem/bloqueio.

Correções:
- Barreiras externas da pista estavam a 0,25–0,75 m da pavimentação nas
  curvas (mais estreito que o próprio kart) — reposicionadas para ~3 m de
  fuga real.
- `RaceStandingsHud` criado: painel ao vivo com posição, nome e voltas de
  cada piloto (jogador + bots), usando `RaceProgressMath` como proxy de
  progresso (waypoint mais próximo), somente para exibição.
- Slipstream (vácuo) existia como função pura testada
  (`KartDynamicsMath.CalculateSlipstreamDragReduction`) mas nunca fora
  conectado a `KartDynamics` — passou a ser aplicado de fato via
  `KartDynamics.UpdateSlipstream`, com HUD mostrando o percentual ativo.
- Leaderboard redesenhado: em vez de melhor tempo do dia/semana/histórico
  (que nunca chegou a aparecer na prática), passou a ser um top 5 nomeado
  (`LapRecordMath.FindTopRecords`), com nome do piloto persistido via
  `PlayerNameStore`.
- Áudio: adicionado deadzone (`KartAudioMath.SkidActivationThreshold01`) para
  o som de derrapagem só disparar acima de um limiar de perda de grip, pitch
  do som de derrapagem passou a subir com a intensidade, e o volume relativo
  motor/ambiente foi reequilibrado.
- Itens **explicitamente adiados** (mais trabalhosos, não entraram nesta
  rodada): telão na largada mostrando o tempo da última volta; bots fazendo
  ultrapassagem/bloqueio ("pegando vácuo", fechando a linha).

## Rodada — 2026-08-20 (seams de física, grama travando de novo)

Feedback do fundador: kart continuava com impacto forte antes de qualquer
curva, mesmo na pista normal; grama piorou — parecia desnivelada, com pontos
onde o kart entrava e não conseguia sair (inclusive bots ficando presos sem
terminar a corrida).

Diagnóstico: "internal edge" do PhysX — dois colliders estáticos separados
(grama e pista) com qualquer diferença de altura residual podem prender um
Rigidbody guiado por `BoxCollider` sem rodas na costura entre eles.

Correções:
- Colisão da grama tornada perfeitamente coplanar ao topo da pista (0,06 m),
  eliminando o degrau por completo em vez de apenas reduzi-lo.
- Sistema de redução de grip na grama (`SurfaceDataSO`/`SurfaceTrigger`)
  estava incompleto desde sua criação: `CreateSurface` montava o
  `SurfaceDataSO` mas nunca anexava um `SurfaceTrigger` para reportá-lo —
  zero efeito de jogo. Corrigido com setters `Configure(...)` em ambas as
  classes, seguindo a sugestão do próprio fundador ("o ideal é ser todo
  plano e apenas diminuir a velocidade").
- Corrigido também um segundo bug independente: a trigger da grama nunca
  tinha sobreposição vertical com o collider do próprio kart.

## Rodada — 2026-08-20 (z-fighting, nomes de bot sequenciais, teclado pré-preenchido, classificação final)

Feedback do fundador: grama confirmada corrigida; impacto antes de curva
persistia mesmo na pista normal; tela final mostrava só o top 5 histórico,
não a classificação da corrida; pista "piscando" (efeito visual); nomes dos
bots sempre saindo na mesma ordem; campo de nome do piloto abria pré-
preenchido com "Piloto", parecendo texto já digitado.

Correções:
- Colisão da grama (perfeitamente coplanar) separada da malha visual
  (deslocada ~1,5 cm abaixo) — elimina o z-fighting sem reintroduzir o
  degrau físico.
- Hipótese para o impacto persistente: kart-vs-kart, não kart-vs-pista —
  bots freiam antecipadamente antes de curvas e o jogador (ajudado pelo
  vácuo, agora funcional) encosta neles; todos os karts compartilhavam o
  mesmo material de física com elasticidade zero, então a colisão lia como
  parede sólida. Criado `GetKartCollisionMaterial` (elasticidade 0,3,
  `bounceCombine = Minimum`) aplicado somente ao collider do próprio kart —
  contato kart-vs-pista/muro permanece `min(0.3, 0) = 0` (sem mudança),
  contato kart-vs-kart passa a `min(0.3, 0.3) = 0.3` (batida com deflexão em
  vez de parada seca).
- `RaceManager` passou a computar e exibir a classificação real da corrida
  (posições, não só melhores voltas), reaproveitando a mesma lógica de
  progresso de `RaceStandingsHud`.
- Nomes dos bots passaram a ser embaralhados (Fisher-Yates) por corrida em
  vez de indexação sequencial.
- Teclado do nome do piloto passou a abrir em branco; nome padrão trocado de
  "Piloto" para "User1" quando o campo fica vazio.
- **Confirmado pelo fundador na rodada seguinte:** grama corrigida.

## Rodada — 2026-08-20 (reformulação da direção/curva)

Feedback do fundador: impacto sumiu (confirmado); bots continuavam "burros",
sem competitividade; leaderboard confirmada funcionando corretamente
depois do fix anterior; **o kart praticamente não fazia curva** — mesmo
forçando o volante ou em velocidade, deslizava de lado até bater na parede,
às vezes precisava contra-esterçar, e às vezes girava 180°. Pedido explícito
para pesquisar como um kart real faz curva.

Diagnóstico: o modelo antigo comandava uma velocidade angular (yaw) fixa em
graus/segundo, independente da velocidade real do kart. Em qualquer
velocidade de pista normal, isso exigia fisicamente mais aderência lateral
do que os pneus (limitados a 1,0–1,2 g pela configuração) conseguem fornecer
— o "nariz" girava mais rápido do que o corpo conseguia realmente
acompanhar, e os dois descolavam (derrapagem ou giro descontrolado).

Correção — reescrita do modelo de esterço em `KartDynamicsMath` +
`KartDynamics.ApplySteering`:
- `CalculateAckermannYawRateDegreesPerSecond`: o ângulo de esterço define um
  RAIO de curva (geometria de Ackermann, `raio = entre-eixos / tan(ângulo)`);
  a velocidade atual só define a rapidez com que esse raio é percorrido —
  não o quão fechada é a curva. Velocidade assinada também faz a ré se
  comportar como um kart real sem precisar de uma flag de direção manual.
- `LimitYawRateToAvailableGrip`: nunca deixa a curva pedida exigir mais
  aceleração centrípeta do que a aderência lateral disponível no momento —
  excesso de pedido "abre" a curva (understeer real) em vez de girar o
  kart além do que o corpo consegue acompanhar.
- 10 testes EditMode novos cobrindo ambas as funções (zero esterço, parado,
  sinal invertido na ré, escala linear com velocidade, e a invariante de
  que o resultado nunca excede a aderência disponível).
- IA dos bots não foi alterada nesta rodada — usa o mesmo `KartDynamics`,
  então herda a correção automaticamente; ficou combinado revisar
  agressividade/competitividade específica da IA somente depois de
  confirmar que a física em si já resolveu parte do comportamento "burro".

## Rodada — 2026-08-20 (confirmação + pulo ao frear em curva)

Feedback do fundador: curva melhorou bastante, "efeito de derrapagem"
sumiu; ainda não está 100% mas "está legal"; leaderboard confirmada
funcionando (top 5 grava corretamente); bots continuam sem competitividade
(aceito adiar); **ao frear numa curva o carro dá um "pulo"**, sensação fora
da realidade.

Diagnóstico: a pista é plana em toda a sua extensão (sem rampas por design),
então qualquer velocidade vertical além do assentamento inicial na largada
só pode vir de um artefato de colisão do motor de física — o suspeito mais
provável é uma batida de canto (não face-a-face) entre dois `BoxCollider` de
kart durante o contato kart-vs-kart introduzido na rodada anterior (a
elasticidade 0,3 do `GetKartCollisionMaterial`), cujo vetor de contato nem
sempre é perfeitamente horizontal.

Correção: `KartDynamics.ClampUpwardLaunchVelocity` — no início de cada
`FixedUpdate`, limita a velocidade vertical (Y) do Rigidbody a um teto de
2 m/s, neutralizando qualquer pico introduzido pela resolução de colisão do
passo de física anterior, sem remover a elasticidade kart-vs-kart (que
corrigiu um problema real e confirmado) nem travar a posição Y (o kart
precisa cair ~0,5 m para assentar na pista na largada).

## Rodada — 2026-08-20 (M2-T20/T21 confirmadas pelo fundador; arte Kenney)

Fundador confirmou ter avaliado a build com outras pessoas dirigindo e
autorizou marcar M2-T20/T21 como concluídas com base nessa confirmação
(ver nota nas próprias tarefas em `tasks.md`) — não substitui o checkpoint
numérico formal se pilotos reais de kart ficarem disponíveis depois.

Pedido para deixar pista/kart mais realistas sem custo: integrado o Kenney
Racing Kit + Car Kit (CC0, já presentes no repo de uma sessão anterior).
`KartVisual.fbx` trocado pelo modelo `kart-oodi` do Car Kit; muros externos
da pista passaram a exibir uma cerca real (`TrackFence.dae`) tileada em vez
de um cubo de cor sólida. Bug encontrado e corrigido: a cerca renderizava
rosa/magenta (shader ausente no build IL2CPP/URP — mesma classe de
problema já documentada nos comentários de `CreateMaterial`); corrigido
forçando o material do projeto em vez do material COLLADA nativo do `.dae`.

## Rodada — 2026-08-20 (M3-T01: zebras, fiscais, pit — parcial)

Fundador pediu para eu olhar à frente no próprio plano do projeto em vez
de só reagir a feedback rodada a rodada. Achado: `docs/12-art-audio-performance.md`
já tem uma direção visual explícita (referência: kartódromos brasileiros,
com pneus/zebras/fiscais/paddock) e `tasks.md` tem a task M3-T01 cobrindo
exatamente esse trabalho, ainda em aberto. Resumo enviado ao fundador, que
escolheu continuar fechando a checklist de M3-T01.

Fechado nesta rodada (mesma técnica: assets CC0 do Kenney Racing Kit,
material forçado para evitar o bug rosa/magenta):
- Zebras reais: curvas viraram um padrão xadrez vermelho/branco (antes era
  um bloco vermelho sólido), mesmo footprint e collider (trigger,
  não-sólido) de antes — não muda a física de dirigir.
- 8 postos de fiscal (placeholder): bandeiras (`flagGreen`) posicionadas
  do lado de fora do muro externo, decorativas (sem collider).
- Bandeira quadriculada perto do start/finish (com a textura real do
  padrão, não uma cor sólida — usa o novo parâmetro de textura de
  `CreateMaterial`).
- Prédio de pit (placeholder) atrás do muro externo + 2 linhas pintadas
  na pista marcando onde ficariam a entrada/saída de pit — sem mecânica de
  pit-in/pit-out ainda (fora de escopo desta rodada).

Deliberadamente NÃO feito nesta rodada (ver nota em `tasks.md` M3-T01
para o racional completo):
- Ampliar o traçado para ~1km com curvas variadas/3 setores — mexe em
  `TrackConfigurationSO`, checkpoints e bot path já validados; risco de
  reintroduzir bugs de física/IA já resolvidos, então fica para uma rodada
  própria e isolada.
- Iluminação baked — a pista é gerada por código em runtime, não é
  geometria autorada no Editor, então bake real exigiria mudar essa
  arquitetura primeiro.

## Rodada — 2026-08-20 (competitividade dos bots)

Fundador: zebra funcionou ("ficou bem grande mas legal"); pedido reforçado
de traçado maior/variado com subida e descida, pneus ao redor, grama
restrita às áreas de escape das curvas (com zebra balizando); e
**aumentar o nível dos bots** — "hoje nao tem graca brincar pq corro com
bots porém sem adversário". Autorização explícita para eu decidir
sequenciamento ("fica ao seu critério").

Diagnóstico: o modelo antigo de throttle dos bots (`CalculateCornerAwareThrottle`)
escalava a velocidade a partir de um "sharpness" 0..1 arbitrário, sem
nenhuma relação com o quanto de aderência a física realmente permite naquela
curva — mesmo um bot "Difícil" (erro de esterço zero, throttle máximo igual
ao do jogador) ficava artificialmente lento em toda curva porque o piso de
throttle mínimo (0.4) era um número fixo, não calculado.

Correção: os bots agora calculam uma velocidade-alvo de curva real,
`v = sqrt(raio_da_curva * aderência_disponível)` — a mesma fórmula física
que já limita a curva do próprio jogador (`KartDynamics.MaxLateralAcceleration`,
exposta publicamente nesta rodada). O raio da curva é medido de verdade
(circunraio) a partir dos 3 waypoints ao redor do alvo atual, então isso já
funciona em qualquer traçado, inclusive o maior que vier a seguir — não é
uma constante ajustada só para o oval atual. A dificuldade agora é
principalmente "quão perto do limite teórico de aderência o bot dirige"
(`GetCorneringSafetyMargin01`: Fácil 60%, Médio 78%, Difícil 95%), e o bot só
reage ao limite de uma curva quando está perto o suficiente dela para
importar (janela de antecipação de ~1.6s de frenagem, escalando com a
velocidade atual) — evita frear cedo demais numa reta longa.
10 testes EditMode novos, valores conferidos numericamente antes de
enviar (mesma prática usada na reescrita do esterço, rodada 10).

**Traçado maior/com elevação: NÃO feito nesta rodada.** Decisão deliberada:
plano completo (traçado, `TrackConfigurationSO`, checkpoints, bot path,
grid, barreiras) é um trabalho grande por si só; empacotar junto com a
mudança de IA dos bots no mesmo build dificultaria saber qual mudança
causou qual regressão, caso apareça alguma — mesmo padrão que resolveu os
bugs de física mais rápido antes (uma mudança de cada vez). Fica como
próxima rodada dedicada, em duas fases: (1) traçado maior e mais variado,
ainda plano (sem subida/descida) — menor risco, reaproveita a mesma técnica
de geometria por caixas já usada; (2) elevação de verdade — kart usa um
único `BoxCollider` rígido sem suspensão/rodas, então rampas são um regime
de física nunca testado neste modelo; só depois de (1) estar estável.

## Rodada — 2026-08-20 (bots seguindo reto: throttle brigando com o freio)

Fundador: "os bots foram mais rapidos porém seguiram reto na pista não tem
noção de fazer curva" — regressão real desta última mudança, não apenas
"ainda tímido".

Diagnóstico: `KartDynamics.ApplyLongitudinalForces` aplica throttle e freio
como duas forças simultâneas e independentes (nunca trata como pedais
mutuamente exclusivos). A função nova de throttle por velocidade-alvo
(`CalculateThrottleForTargetSpeed`) tinha um piso de `minThrottle` mesmo
já acima da velocidade-alvo — ou seja, o bot empurrava para frente com
0.4 de throttle o tempo INTEIRO em que também tentava frear para a curva,
diluindo a frenagem efetiva bem na hora que mais importa. Combinado com
margens de segurança (rodada 14) otimistas demais pra essa diluição, o bot
chegava na curva rápido demais para o que a física de aderência permite —
e o modelo de esterço (rodada 10) responde a excesso de velocidade abrindo
a curva de verdade (understeer), não girando: literalmente "seguiu reto".

Correção:
- `CalculateThrottleForTargetSpeed` agora retorna 0 (não `minThrottle`)
  assim que a velocidade atual alcança a velocidade-alvo da curva — o freio
  para de competir com o acelerador.
- Margens de segurança por dificuldade reduzidas (Fácil 60%→50%, Médio
  78%→62%, Difícil 95%→80%) para reconstruir uma folga real contra outros
  efeitos de frenagem que a fórmula não modela (transferência de peso,
  brake-oversteer).
- Janela de antecipação de frenagem aumentada de 1.6s para 2.4s
  (`KartBotController.CorneringLookaheadSeconds`).
- Testes existentes atualizados para o novo comportamento (throttle=0 no
  limite, não mais `minThrottle`); valores reconferidos numericamente
  (distância de frenagem necessária vs. disponível, na curva mais fechada
  do traçado atual) antes de enviar.

Testado pelo fundador: ainda insuficiente — ver rodada seguinte.

## Rodada — 2026-08-20 (bots "sem inteligência", grid e números aleatórios)

Fundador: "eles nao tem inteligencia alguma para finalizar a prova eles
simplismente vao reto e param na parede e fica batendo na parede como se
quisesse atravessar... quero competitividade". Também dois pedidos novos:
números aleatórios nos carros, e posição de largada aleatória no grid (o
kart do jogador sempre largava em P1). Reforço explícito de que no modo
Difícil o bot não deveria nunca seguir reto numa curva e, se acontecer,
deveria se recuperar e voltar a seguir o traçado.

Diagnóstico: a correção da rodada anterior (throttle zerando no alvo,
margens reduzidas) não resolveu porque o bug real estava um nível abaixo —
na fórmula que calcula o raio da curva. Inspeção de fórmula isolada não
detectou o problema nas duas tentativas anteriores, então desta vez montei
uma simulação Python completa da física do jogo (seguimento de waypoint,
esterço Ackermann com limite de aderência, atraso do controlador PD de guinada
usando os mesmos `YawResponse`/`YawDamping` do tuning real, forças de
aceleração/freio/atrito) rodada contra as coordenadas reais dos waypoints do
traçado oval. Isso revelou dois problemas que a inspeção manual não pegou:
1. `CalculateCornerRadiusMeters` (circunraio a partir de 3 waypoints)
   subestima MUITO o quanto uma curva é fechada quando os waypoints estão
   espaçados de forma larga em relação à curva real do traçado — no ápice
   nordeste do oval, a fórmula calculava ~25m de raio quando na prática só
   existem ~16m de pista até o muro externo. Isso deixava os bots Médio/
   Difícil pedindo velocidades de curva fisicamente impossíveis de cumprir
   no espaço real disponível — daí "vai reto e bate na parede".
2. Ao testar margens de segurança candidatas na simulação, descobri que uma
   faixa intermediária "aparentemente segura" (~55-70%) não batia na parede,
   mas prendia o bot numa órbita circular infinita ao redor do waypoint da
   curva, sem nunca alcançar o raio de chegada — um jeito de travar que só
   apareceu rodando a simulação completa, não seria visível inspecionando a
   fórmula isolada.

Correção:
- `CalculateCornerRadiusMeters` removida. Substituída por
  `CalculateTurnAngleRadians` (ângulo real de curva entre os waypoints
  antes/atual/depois) + `CalculateMaxCorneringSpeedMetersPerSecond`
  (`v = sqrt(distância_disponível * aderência / ângulo) * margem`), que usa
  a distância real até o waypoint-alvo em vez de um raio aproximado.
- Margens de segurança recalibradas com folga real abaixo do limite seguro
  observado na simulação, já contando o atraso do controlador PD de guinada
  (Fácil 30%, Médio 38%, Difícil 45% — mais conservadoras que as da rodada
  anterior porque agora reflete o ângulo real da curva, não uma aproximação
  otimista).
- Todos os valores novos (ângulo da curva, distância, margens) validados
  numericamente rodando a simulação Python antes de escrever no C#, com
  variação de seed e erro de esterço injetado — mesma disciplina usada desde
  a reescrita do esterço (rodada 10), agora com uma simulação completa em vez
  de só a fórmula isolada.
- 8 testes EditMode novos/atualizados em `KartBotMathTests.cs` cobrindo o
  ângulo de curva e a nova fórmula de velocidade-alvo.

Grid de largada aleatório: `KartPhysicsPrototypeBootstrap` agora embaralha
os 10 slots do grid uma vez por corrida (Fisher-Yates, mesmo padrão já usado
para os nomes dos bots) e sorteia tanto a posição do jogador quanto a dos
bots a partir dessa mesma lista — o jogador deixa de sempre largar em P1.

Números de corrida aleatórios: pool de números 1-20 embaralhado uma vez por
corrida (mesmo padrão Fisher-Yates); jogador e cada bot recebem um número
único (`KartBotController.RaceNumber`), exibido no painel de classificação
ao lado do nome (`#N Nome`). Tela final de classificação (`RaceManager`)
ainda não mostra os números — deixado fora desta rodada para reduzir risco;
posso adicionar numa próxima rodada se o fundador quiser.

Testado pelo fundador: bots melhoraram bastante, alguns já terminam a
corrida — mas revelou dois problemas novos e reforçou um pedido maior. Ver
rodada seguinte.

## Rodada — 2026-08-20 (invasão do centro da pista, muro ainda travando, pedido de "pilotagem agressiva")

Fundador: "o bot perde a referencia do centro, eles invadem o centro da
pista que parece nao ter barreiras como tem do lado externo com as grades,
melhorou bastante alguns ate conseguem terminar a corrida, mas nao tem nada
de pilotagem agressiva, bloquear, tentar se manter na frente parece muito
mecanico essa ideia de freiar sempre, contornar a curva ainda nao transmite
dificuldade nem de longe e uns ainda ficam parados batendo no muro acho
meio irreal... mas melhorou".

**1. Bots invadindo o centro da pista (corrigido).** Diagnóstico: as 4
barreiras internas (ao redor do miolo do oval) existiam no código, mas
foram tornadas não-sólidas (apenas visuais, `isTrigger`) na rodada 8,
porque naquele desenho elas eram grandes demais e o próprio retângulo
clipava o asfalto das curvas (Corner_NE etc.), travando a linha de corrida
normal. Ou seja: era um bug real, não impressão — o interior do oval
literalmente não tem nenhuma colisão desde então. Corrigido encolhendo o
anel para caber inteiramente dentro do "buraco" real do miolo (que nunca
chega mais perto do centro que x=±21.5 / z=±11.5 em nenhum ponto — conferido
contra a geometria de cada trecho de pista), com folga de ~1.5m de todo
asfalto. Nenhum waypoint de bot fica perto dessa área. Voltou a ser sólida.

**2. Bots ainda travando/batendo no muro repetidamente (mitigado).**
Diagnóstico: a recuperação de "preso" (ré + esterço por 0.9s) provavelmente
soltava o bot de um obstáculo aberto, mas quando o que prendia era mais
persistente (ex. posição ruim contra um muro, ou empurrado por outro kart
— agora até 9 bots + jogador no mesmo grid), o bot ficava preso de novo
segundos depois e repetia a mesma manobra curta pra sempre — visualmente
igual a "ficar batendo na parede". Correção: se o bot travar de novo dentro
de uma janela curta após a última recuperação, cada tentativa agora
recua por mais tempo (progressivo, até um teto de 2.4s) em vez de repetir
sempre os mesmos 0.9s. Não é uma correção definitiva de física de colisão
entre karts (isso não foi modelado/testado ainda) — é um mecanismo de
escape mais robusto para quando o bot fica realmente preso.

**3. "Pilotagem agressiva, bloquear, se manter na frente... muito mecanico"
e "curva não transmite dificuldade" — NÃO implementado nesta rodada.**
Isso é um pedido qualitativamente diferente dos anteriores: os bots hoje
são só seguidores de waypoint que freiam/aceleram pelo limite físico da
curva — não têm noção de onde estão os outros karts, não disputam posição,
não bloqueiam, não assumem risco de forma diferente entre si. Construir
isso de verdade (consciência posicional, defesa de linha, ultrapassagem,
variação de personalidade entre bots) é um escopo bem maior que os ajustes
desta rodada — é essencialmente o que o próprio código já sinaliza como
"não é o milestone M4 de IA de bots, é só um oponente de prototipagem".
Perguntei ao fundador como prefere seguir antes de investir tempo nisso.
Resposta: "Bloqueio/disputa de posição de verdade" — ver rodada seguinte.

## Rodada — 2026-08-20 (primeira versão de disputa de posição — bloqueio e ultrapassagem)

Fundador escolheu a opção de investir na disputa de posição de verdade em
vez de um ajuste leve. Primeira versão desse recurso.

Desenho: até agora todo bot só raciocinava sobre a pista (waypoints,
geometria de curva) — nenhuma noção de que outros karts existem.
`KartDynamics` já mantinha um registro de todos os karts ativos (usado para
o vácuo/slipstream); exposto publicamente (`AllActiveKarts`) para os bots
consultarem. Cada bot agora, a cada frame fora da janela de frenagem de
curva (a mesma que já protege a lógica de velocidade de curva — ver rodada
16), procura o rival mais próximo atrás e o mais próximo à frente (em seu
próprio espaço local) e aplica um desvio lateral só no PONTO DE MIRA da
direção — nunca na velocidade de curva, no acelerador ou no freio, para não
reabrir a classe de bug das rodadas 15/16 (bots batendo na parede):
- **Defesa**: se um rival vem colado por trás e já se comprometeu com um
  lado (não está exatamente atrás), o bot fecha esse lado.
- **Ataque**: se há um rival logo à frente, o bot mira para o lado livre
  dele (ou para um lado "preferido" sorteado uma vez por bot, se o rival
  ainda está centralizado) — buscando ficar lado a lado para ultrapassar.
- Desvio lateral sempre limitado a 1.6m (pista tem 7m de largura, kart tem
  1m), então nunca chega perto da barreira nova do miolo nem da externa.
- Agressividade escala por dificuldade: Fácil = 0% (não disputa posição,
  só segue a pista, igual ao comportamento original), Médio = 45%, Difícil
  = 85%.
- 12 testes EditMode novos cobrindo defesa, ataque, limites de alcance e o
  teto de 1.6m, valores conferidos numericamente antes de enviar.

**Isso NÃO é o milestone formal de IA de bots (M4)** — não há noção de
risco/recompensa, não há aprendizado, não há variação de personalidade além
da agressividade por dificuldade, e colisão kart-kart continua não
totalmente modelada (rodada 17). É uma primeira camada de consciência
posicional sobre o seguidor de waypoint existente. Ainda não testado pelo
fundador no dispositivo real — efeito em jogo (se sente competitivo ou se
sente estranho/errático) só se confirma dirigindo de verdade.

## Rodada — 2026-08-20 (traçado maior e variado — Fase 1, ainda plano)

Fundador: "melhorou, vamos seguir" — confirmação geral da rodada anterior
(invasão do centro corrigida, mitigação de travamento no muro) e sinal
verde para continuar, sem pedido novo específico. Usei esse sinal para
avançar no pedido de traçado maior/variado que o fundador já tinha feito
duas vezes (rodadas 14 e 16) e que eu mesmo tinha deliberadamente adiado
para uma rodada própria e isolada (ver nota da rodada 14) — é a Fase 1 do
plano em 2 fases descrito naquela rodada: traçado maior e com curvas
diferentes, ainda plano (sem subida/descida — elevação fica para a Fase 2,
só depois desta ser validada estável, porque o kart usa um único
`BoxCollider` rígido sem suspensão e rampas seriam um regime de física
nunca testado).

**Esta é a maior mudança única desta sessão feita de forma autônoma** —
reescreve toda a geometria da pista (`CreateCourse` em
`KartPhysicsPrototypeBootstrap.cs`) e o `OvalMvpTrackConfiguration.asset`
inteiro (bot path, grid, checkpoints). Dado o histórico deste projeto com
bugs de geometria de pista (buracos, degraus, internal edges — rodadas 1-4
deste log), testei a geometria inteira num script Python à parte antes de
escrever qualquer C#: modelei cada peça de asfalto/barreira/grama como
retângulo e verifiquei programaticamente que não existe nenhum buraco de
pavimento ao longo da linha que os bots realmente percorrem (centro de
pista, offset 0) — isso pegou um bug real (um "buraco" na transição entre
uma curva e sua reta de conexão) antes de chegar no dispositivo, coisa que
já tinha escapado da minha própria revisão manual duas vezes.

O que mudou:
- Traçado deixou de ser um oval ~84×44m simétrico (4 curvas idênticas) e
  virou um retângulo alongado ~114×58m com dois lados de conexão
  assimétricos: um lado "rápido" (curvas mais abertas, ~18°) e um lado
  "técnico" (curvas mais fechadas, ~63°) — volta útil passou de ~183m para
  ~276m.
- Barreiras externas e o anel interno recalculados do zero para a nova
  geometria (mesma técnica de caixas alinhadas aos eixos já usada, sem
  geometria rotacionada — decisão deliberada de manter a Fase 1 de baixo
  risco).
- Pneus decorativos (placeholder, cilindros empilhados — mesmo padrão dos
  postos de fiscal/prédio de pit já usados) ao longo da barreira externa,
  além da cerca já existente.
- Checkpoints, grid de largada (10 posições, zigue-zague na nova reta
  principal) e bot path (`TrackConfigurationSO`) todos recalculados para a
  nova geometria.
- Grama continua sendo um único plano grande cobrindo toda a área (não
  restrita às áreas de escape das curvas como pedido — ver pendência
  abaixo); iluminação continua não-baked (mesma limitação arquitetural já
  documentada: pista é gerada por código em runtime, não é geometria
  autorada no Editor).

**Ainda não testado pelo fundador no dispositivo real.** Dado que é a
maior mudança única da sessão, pontos que merecem atenção especial nesta
rodada de teste: qualquer buraco de pavimento ou queda através da
geometria; qualquer barreira mal posicionada nas curvas novas; se os bots
ainda completam voltas corretamente (checkpoints em nova posição, waypoint
following); se as novas decorações de pneu aparecem/rodam bem
(performance).

## Rodada — 2026-08-20 (curvas de verdade — pista vira um "estádio" oval)

Fundador respondeu à Fase 1 acima: "sim mudou esta um quadrado, pouco
pneus" + fotos de referência (desenho técnico real de kart com dimensões,
fotos de kartódromo real com pneus densos ao redor, dois croquis à mão de
traçados de pista com curvas em S/hairpins). Perguntei explicitamente como
prefere seguir: mais curvas ainda em caixas retas (mais rápido, menor
risco) vs. investir em peças de pista rotacionadas para curvas de verdade
(mais lento, maior risco, mais fiel às referências). Resposta: "Curvas de
verdade com peças rotacionadas".

**Isso é uma mudança de arquitetura, não só de números.** Até esta rodada,
TODO helper de criação de peça (`CreateTrackPiece`, `CreateWall`, etc.) só
sabia posicionar caixas alinhadas aos eixos (posição + escala, rotação
identidade) — não existia nenhum jeito de colocar uma peça em ângulo.
Adicionei essa capacidade (`CreateTrackPieceOriented`/`CreateWallOriented`
+ os visuais de cerca/pneu equivalentes) como métodos NOVOS, sem tocar em
nenhum dos métodos antigos — todo o código de rodadas 1-19 continua
byte-a-byte igual, risco zero de regressão nas peças já confirmadas
funcionando.

Com essa capacidade, troquei a topologia "4 caixas de canto" da Fase 1 por
um "estádio" de verdade (formato de pista de atletismo): 2 retas + 2
semicírculos reais, cada semicírculo construído com 8 fatias de pavimento
rotacionadas (22,5° cada) que seguem um arco de círculo de verdade, em vez
de uma caixa grande tentando aproximar a curva. Ponto importante: o raio
do semicírculo NÃO é uma escolha livre — pra duas retas + dois semicírculos
fecharem num laço, o raio tem que ser exatamente metade da separação entre
as retas (documentei a derivação completa no código, função
`GenerateStadiumCenterline`). Escolhi um raio bem fechado (14m) de propósito
— um raio grande e preguiçoso (24m+) resolveria "parece quadrado" mas não
"curva não transmite dificuldade" (reclamação da rodada 17); a 14m, com a
aderência lateral de 1.0-1.2g do tuning atual, a velocidade máxima física
de curva fica em ~42-46 km/h — uma redução real vindo da reta, que exige
frear de verdade.

Toda peça nova (pavimento, os dois anéis de barreira) foi gerada a partir
da mesma fórmula fechada de círculo/reta e validada num modelo Python
separado antes de qualquer C#: amostragem densa do círculo VERDADEIRO (não
só da aproximação poligonal) em vários offsets laterais não encontrou
nenhum buraco de pavimento em lugar nenhum onde a roda do kart passaria —
a mesma classe de bug que já custou uma rodada dedicada toda vez que este
projeto mexeu em geometria de pista.

O que mudou:
- Traçado deixou de ser o retângulo alongado ~114×58m da Fase 1 e virou um
  "estádio" ~104×28m: 2 retas de 76m + 2 semicírculos reais de raio 14m —
  volta útil ~239m.
- Barreira externa e interna (o "buraco" do miolo) recalculadas como o
  mesmo formato de estádio, só que num raio maior/menor — ambas
  perfeitamente concêntricas à pista por construção, sem contas separadas.
- Cerca e pneus decorativos agora cobrem o anel INTEIRO (incluindo as
  partes curvas) — antes só as 2 retas tinham decoração.
- Zebras simplificadas de 4 (uma por canto) para 2 (uma em cada ápice do
  semicírculo) — uma curva contínua não tem mais 4 cantos discretos pra
  marcar, então isso é uma simplificação honesta, não um corte silencioso.
- Checkpoints continuam caixas alinhadas aos eixos (o formato de dado do
  `TrackConfigurationSO` não tem campo de rotação, e não precisa: num
  "estádio", a direção de percurso é exatamente eixo-X nas retas e
  exatamente eixo-Z nos dois ápices dos semicírculos — todo checkpoint foi
  colocado exatamente num desses 4 pontos naturalmente alinhados).
- Grid de largada, pontos de freada, postos de fiscal, prédio de pit e
  linhas de pit recalculados para a nova geometria.

**Deliberadamente NÃO feito nesta rodada:** pontas com raios diferentes
(um lado "rápido"/raio grande e outro "técnico"/raio pequeno, como a Fase 1
tinha) — um "estádio" simples de 2 retas + 2 semicírculos só fecha se os
dois raios forem iguais; ter raios assimétricos exigiria uma topologia
diferente (essses/chicanes absorvendo a diferença), fica pra uma rodada
seguinte depois que esta curva única e validada estiver confirmada
estável. Também não: o traçado ~1km/3 setores completo do pedido original,
grama restrita às áreas de escape (mesmo motivo de risco da rodada
anterior).

**Ainda não testado pelo fundador no dispositivo real — e esta é, de
longe, a mudança de maior risco desta sessão inteira** (primeira vez que
peças rotacionadas entram no projeto). Pontos que merecem atenção redobrada
nesta rodada de teste: qualquer buraco de pavimento nas partes curvas
(mesmo com toda a validação Python, só dirigir de verdade confirma);
qualquer clipping de barreira nas curvas; se os bots seguem a curva de
forma suave (18 waypoints por volta agora, bem mais próximos entre si que
antes) ou se ficam "travando"/oscilando; se checkpoints/voltas continuam
validando certo; se a decoração de cerca/pneu nas curvas aparece/roda bem
(mais peças que antes, ainda dentro do orçamento esperado mas não medido
ao vivo).

## Rodada — 2026-08-20 (bots "foram para o lado", pista "desformatada", IA dos bots "um desastre")

Fundador testou o "estádio" da rodada anterior e reportou 3 problemas + fez
2 pedidos de pesquisa, tudo na mesma mensagem: (1) bots não entenderam a
pista na largada, foram para o lado; (2) o traçado oval ficou "um pouco
desformatado com pontos de grama"; (3) sugestão de usar o kit de pista do
Kenney (peças retas/curvas prontas) em vez de gerar tudo por código; (4) "a
inteligência do bot controlada por matemática parece um desastre kkk", com
autorização explícita pra pesquisar na internet como jogos de verdade fazem
isso; (5) autorização explícita geral pra buscar alternativas e usar
criatividade além do kit do Kenney.

**Bug 1 — bots foram para o lado na largada (corrigido).** Causa: `KartBotController.Configure`
sempre mirava no waypoint de índice 0 como primeiro alvo — isso só
funcionava por coincidência nas pistas antigas, onde o waypoint 0 ficava
perto/à frente do grid. No "estádio" da rodada 20, o waypoint 0 é a ponta
distante da reta sul, ATRÁS de todo slot do grid — os bots giravam o carro
pra mirar num ponto atrás deles logo na largada, ou seja, "foram para o
lado". Corrigido com uma função nova e genérica
(`KartBotMath.FindNearestPathSegmentStartIndex`): encontra a ARESTA do
traçado mais próxima da posição de largada (não o vértice mais próximo — o
vértice mais próximo teria o mesmo bug, já que o vértice 0 a 19m de
distância é numericamente "mais perto" que o vértice 1 a 57m, mesmo estando
atrás) e mira um passo à frente dessa aresta. Funciona pra qualquer posição
de largada em qualquer traçado fechado, não só este. 5 testes novos,
incluindo um que reproduz exatamente os números reais do bug relatado.

**Bug 2 — pista "desformatada com pontos de grama" (corrigido).** Causa
provável: o jeito que a rodada 20 cobria os buracos nos vértices das curvas
era com um quadrado avulso (não rotacionado) em cada vértice — e esse
quadrado era MAIOR que os próprios pedaços de pista rotacionados entre eles
(6,5m de quadrado contra ~5,5m de corda de arco), o que produz uma silhueta
em "roda dentada" visível na curva mesmo sem nenhum buraco estrutural (a
cobertura já tinha sido validada matematicamente — o problema era só
estético). Troquei a técnica inteira: em vez de quadrados de junta, cada
pedaço de curva agora é esticado 1,35x o próprio comprimento (as duas retas
continuam com o comprimento exato) — os pedaços vizinhos passam a se
sobrepor naturalmente e cobrem a junta sem nenhuma peça quadrada solta.
Também aumentei de 8 para 12 fatias por curva (fatias menores = curva mais
suave). Validado sem buracos num modelo Python antes de virar C#, do mesmo
jeito que a geometria da rodada 20.

**Pesquisa 1 — kit de pista do Kenney.** Pesquisei os pacotes reais do
Kenney (kenney.nl). Achei: o kart/personagens já usados no projeto vêm do
pacote **Car Kit**; cercas, bandeiras de fiscal e a bandeira quadriculada
já usados vêm do **Racing Kit** (que TAMBÉM tem peças de pista — estrada,
bordas — CC0, formatos OBJ/FBX/pacote Unity pronto). Ou seja: o pacote de
peças de pista que você lembrava muito provavelmente já está dentro do
Racing Kit já integrado ao projeto, não é um pacote separado ainda não
usado — vale conferir a pasta de assets/créditos pra confirmar. Minha
avaliação honesta: trocar geometria procedural por peças prontas do Kenney
melhoraria a variedade visual real (detalhe de asfalto/meio-fio que código
puro não reproduz), mas custa flexibilidade (hoje o comprimento da pista,
raio de curva e checkpoints são calculados por fórmula; peças prontas
exigem desenho manual do traçado) e tempo de design/playtest pra montar um
laço fechado de ~1km com peças grid-snapped. Um meio-termo razoável, se
quiser seguir por aí no futuro: manter a colisão/lógica procedural (como
hoje) e só trocar a pele visual pelas peças do Kenney — ganha arte sem
abrir mão do controle por código. **Não implementado nesta rodada** — é uma
decisão de arte/produção que prefiro te trazer com os prós/contras em vez
de trocar sozinho.

**Pesquisa 2 — como jogos de corrida de verdade fazem a IA dos bots.**
Pesquisei a técnica clássica: bots de corrida não miram direto no próximo
waypoint (o que este projeto fazia até agora) — isso causa exatamente o
sintoma relatado, porque o alvo "pula" de um waypoint pro outro assim que o
bot cruza o raio de chegada, produzindo uma correção de direção brusca bem
na entrada da curva. A técnica padrão é "pure pursuit" (usada tanto em
robótica quanto em jogos de corrida): mirar num ponto um pouco à FRENTE no
traçado, que desliza suavemente conforme o bot avança, em vez de pular de
waypoint em waypoint. Implementei essa técnica: `KartBotMath.CalculateLookaheadSteeringTarget`
caminha à frente no traçado (a partir do waypoint atual) até uma distância
proporcional à velocidade do bot (mais rápido = olha mais longe, com limite
mínimo/máximo) e devolve o ponto de mira suavizado. Só muda PRA ONDE o bot
mira — não mexe em nada da lógica que já funcionava (contagem de volta,
velocidade de curva calculada pela geometria real, freada, disputa de
posição), então é um risco bem mais contido que a mudança de rotação da
rodada 20. 7 testes novos cobrindo interpolação dentro de um trecho,
travessia de trecho, contorno do laço fechado, lookahead zero e casos de
traçado degenerado (vazio/1 ponto).

Todas as 4 mudanças de código (bug 1, bug 2, pesquisa 2) já foram enviadas
pro dispositivo e conferidas byte a byte — faltando você dirigir de novo
pra confirmar na prática. A pesquisa do Kenney (pesquisa 1) é só
informação/recomendação, nada foi trocado no projeto ainda.

## Rodada — 2026-08-23 (M3-T01: pendências menores)

Fundador pediu pra seguir e, entre as opções que dei, escolheu fechar
pendências menores do M3-T01 em vez de expandir a pista ou esperar teste
dos bots. Duas fechadas, uma deliberadamente NÃO fechada:

- **Números de corrida sumindo na tela final de classificação (fechado).**
  O painel AO VIVO (`RaceStandingsHud`) já mostrava "#7 Fulano", mas a tela
  de FIM DE CORRIDA (`RaceManager`) nunca recebia o número do jogador
  (`Configure` nem tinha esse parâmetro) nem guardava o número de cada bot
  — mostrava só o nome. Adicionado `playerNumber` ao `Configure` do
  `RaceManager`, propagado o número de cada bot (`KartBotController.RaceNumber`,
  já existia) pra dentro de `StandingEntry`, mesma formatação "#n" do painel
  ao vivo.
- **Evidência formal (screenshot + profiler stats) — metade automatizada
  (fechado parcialmente).** `scripts/build_deploy_verify.sh` já tirava
  screenshot + logcat automaticamente a cada rodada, mas nunca gerava
  nenhum número real de performance — "profiler stats" sempre foi um passo
  manual que ninguém tinha feito ainda. Adicionado `ScenePerformanceLogger`:
  2s depois da cena carregar, soma os triângulos de todo mesh ativo na cena
  e conta quantos renderers existem (um proxy conservador de draw calls —
  ignora o batching automático da Unity, que só reduz o número real, nunca
  aumenta), e loga uma linha `[RKW-PERF]` comparando contra o budget do
  M3-T01 (≤100K triângulos, ≤100 draw calls). Cai dentro da mesma janela de
  ~5s que o script já espera antes de capturar o logcat, então a próxima
  vez que você rodar `build_deploy_verify.sh` o número real já vai
  aparecer em `rkw_logcat.txt` sem nenhum passo a mais. Não é um profiler de
  verdade conectado (não dá pra automatizar isso sem o Editor aberto), mas
  é um número real, não mais um item 100% manual.
- **PlayMode test — deliberadamente NÃO tentado ainda.** Diferente de tudo
  que fiz até agora nesta sessão, um PlayMode test só existe de verdade
  depois que roda dentro do Editor da Unity — e eu não tenho acesso ao
  Editor por aqui, só ao sistema de arquivos e a um terminal isolado no seu
  Mac (sem Unity, sem `adb`). Eu conseguiria ESCREVER um teste, mas não
  conseguiria confirmar que ele compila ou passa antes de te entregar — e
  toda a disciplina desta sessão até aqui foi validar antes de enviar.
  Também descobri, olhando o código, que hoje não existe nenhum jeito
  programático de pular o menu de configuração da corrida (`RaceSetupMenu`)
  pra chegar direto na parte "kart dirigindo" — o fluxo real de início de
  corrida depende de um clique na tela. Pra fazer esse teste direito eu
  preciso adicionar um pequeno "gancho" de teste no código de bootstrap, e
  isso eu prefiro te mostrar/validar com você antes de fazer, já que mexe
  (ainda que pouco) num fluxo real do jogo. Fica pendente.

## Rodada — 2026-08-22 (bots "confusos" com 9 bots — só no médio/difícil)

Fundador testou com 9 bots (grid cheio) e reportou: "1 carrinho bot termina
a corrida os outros ainda ficam confusos". Perguntei o que exatamente
aparecia na tela (zigue-zague, batendo uns nos outros, parados, saindo da
pista) para não sair corrigindo o sintoma errado — resposta dele acabou
sendo ainda mais útil que a pergunta original: **no fácil os bots se
orientam bem, no médio ficam perdidos, no difícil (ainda não testado, mas
ele já espera pior) provavelmente erram todos**.

Essa progressão por dificuldade aponta direto pra causa: `GetRacecraftAggressiveness01`
(a "pilotagem agressiva" da rodada 18 — bloquear/ultrapassar) é 0 no fácil e
sobe no médio/difícil. Ou seja, o único sistema que muda de comportamento
exatamente nesse padrão (desligado no fácil, mais forte no difícil) é o
desvio lateral de disputa de posição.

Causa raiz: esse desvio lateral (até 1,6m) só era bloqueado perto de uma
curva usando um portão baseado em VELOCIDADE (`isCorneringPhase` — só
dispara quando o próximo waypoint específico exige frear de verdade). Isso
funcionava bem em pistas antigas com poucas curvas bem definidas, mas o
traçado atual (rodadas 20/21) é uma curva contínua e suave espalhada por 12
fatias por ponta — nenhuma fatia individual exige frear forte o bastante
pra disparar esse portão, então o desvio lateral de disputa ficava ativo o
tempo todo, inclusive bem no meio da parte mais estreita e mais curva da
pista, onde não tem espaço de sobra pra um carro ser empurrado 1,6m pro
lado. Com 9 bots num traçado de ~239m, isso quer dizer várias interações
próximas ao mesmo tempo — e quanto mais agressivo o bot (médio/difícil),
mais forte o empurrão, exatamente o padrão relatado.

Corrigido com um segundo portão, puramente geométrico, além do já
existente: `KartBotMath.ShouldApplyRacecraftBias` bloqueia a disputa de
posição sempre que o próximo trecho do traçado tiver qualquer curvatura
real (usei o mesmo ângulo de curva já calculado pra velocidade de curva,
sem custo extra), não só quando a velocidade exige frear. Numa reta de
verdade o ângulo entre waypoints é ~0°; em qualquer fatia de curva deste
traçado é ~15° — um limiar pequeno (5°) separa isso com folga e continua
funcionando em qualquer traçado futuro, não só este. 5 testes novos.
Já enviado ao dispositivo e conferido — falta você confirmar dirigindo com
9 bots de novo, inclusive no difícil.

## Rodada — 2026-08-23 (difícil: bot perdeu a 1ª curva e bateu na traseira na largada; nova câmera "visão do piloto")

Fundador testou a correção da rodada anterior: "boa noticia que o medium
ta ok eles completam a volta 1" — confirma o portão geométrico funcionando
no médio. No difícil apareceram 2 problemas novos: um bot "passou direto
na primeira curva e voltou pra trás", e outro "bateu na traseira do meu
carro na largada" sem nem tentar ultrapassar.

Causa raiz dos dois: `GetSteeringErrorDegrees` no difícil retorna
exatamente 0° — ou seja, todo bot no difícil dirige a linha ideal com
precisão perfeita, sem nenhuma variação entre eles. Isso não é o bug em
si (é bom que o difícil seja mais preciso), mas tem um efeito colateral:
sem nenhuma diferença de ritmo entre bots do mesmo nível, o pelotão nunca
se espalha sozinho numa reta — fica um bloco compacto o percurso inteiro,
inclusive bem na largada (grid cheio, 9 bots muito próximos) e bem na
primeira curva. É exatamente onde os dois sintomas apareceram.

Duas correções, complementares:

1. **Bloqueio na largada** — novo portão `KartBotMath.HasClearedStartingGrid`:
   a disputa de posição (o desvio lateral) só ativa depois que o bot já
   percorreu pelo menos 25m desde o próprio ponto de largada. Ataca direto
   o "bateu na traseira do meu carro na largada" — no grid cheio, muito
   perto uns dos outros, esse desvio lateral não tinha espaço pra funcionar
   direito.
2. **Ritmo próprio por bot** — cada bot sorteia, uma vez por corrida, um
   multiplicador de acelerador entre 0,94 e 1,0 (`_paceMultiplier`). Afeta
   só o teto de aceleração, nunca a precisão de curva/direção — ou seja,
   "difícil = linha ideal" continua valendo, mas bots do mesmo nível deixam
   de ser clones idênticos em velocidade, então o pelotão se espalha
   naturalmente numa reta em vez de ficar um bloco compacto até a próxima
   curva. Isso ataca o "passou direto na primeira curva e voltou pra trás":
   com o pelotão mais espalhado, a disputa de posição não fica mais
   engatada bem na entrada da curva.

4 testes novos para `HasClearedStartingGrid` (na grade, exatamente no
limiar, bem depois do limiar, limiar negativo tratado como zero).

**Câmera "visão do piloto"** — você perguntou se o jogo já tinha as 2
opções de câmera e pediu a visão do piloto, "seria perfeita para o nosso
jogo". Conferi o código: até esta rodada só existia UMA câmera (terceira
pessoa, seguindo o kart por trás). Não havia visão de cockpit nem troca
de câmera prevista — não é que estivesse escondida, simplesmente nunca
foi implementada.

Implementado agora: `KartPrototypeCamera` ganhou um segundo modo
(`CameraViewMode.Cockpit`), e um botão novo no canto superior esquerdo da
tela (`CameraViewToggleButton` — único canto ainda livre; os outros 3 já
tinham REINICIAR, o painel de volta/dificuldade e a classificação ao
vivo) alterna entre "VISÃO PILOTO" e "VISÃO TRASEIRA" a qualquer momento,
inclusive durante a corrida. Importante: a visão de cockpit NÃO usa a
suavização da câmera de perseguição (que segue com atraso/lerp) —
ela acompanha a posição e rotação do kart exatamente, frame a frame, como
se estivesse soldada na cabeça do piloto. Câmera com atraso em primeira
pessoa costuma dar sensação de enjoo; por isso a diferença é proposital.

A posição exata do "olho do piloto" dentro do kart (`cockpitLocalOffset`)
é uma estimativa por enquanto (kart de aluguel real: banco baixo e
reclinado, ~0,6-0,8m de altura de olho) — ajustável no Inspector sem
mexer em código quando tivermos o modelo 3D real do kart e a posição do
banco de verdade.

Todos os arquivos já enviados ao dispositivo e conferidos por grep — falta
você recompilar e testar: difícil com 9 bots de novo (largada e primeira
curva) e o botão "VISÃO PILOTO" nas duas câmeras.

### Follow-up (mesmo dia, 2026-08-23) — correção #1 acima causou regressão pior, revertida

Você testou a build acima e o resultado piorou: "na primeira volta e
primeira curva todos os bots erram no nivel dificil, vao para o gramado,
eas vezes voltam ou so ficam presos os 9 carrinhos", mais um erro em
vermelho aparecendo no console de desenvolvimento (canto inferior
esquerdo) e a câmera não mudou ao apertar o botão.

Revertido o portão `HasClearedStartingGrid` (correção #1 acima) — mantido
só o multiplicador de ritmo por bot (correção #2), que não tem relação
plausível com esse tipo de travamento. Diagnóstico: bloquear a disputa de
posição por 25m inteiros a partir da largada tirou o único mecanismo que
dava aos bots alguma separação lateral na reta de aproximação (o difícil
tem variação de direção zero — ver correção #2 acima) — sem ele, o
pelotão inteiro chegava perfeitamente empilhado na entrada da primeira
curva, em vez de ter se espalhado um pouco na reta antes, e colidia ali em
vez de virar. Também percebi, revisando o código com mais cuidado dessa
vez: a disputa de posição só nunca mexe em acelerador/freio, só na
direção — então é pouco provável que ela fosse mesmo a causa original do
"bateu na traseira do meu carro na largada". A causa mais provável desse
bug original é mais simples e ainda não corrigida: os bots não têm
nenhuma noção de "tem um kart parado/lento bem na minha frente, preciso
frear" — miram no próximo waypoint do traçado e aceleram a fundo
independente do que estiver no caminho. Isso fica como pendência em
aberto para uma rodada própria (ver abaixo), em vez de tentar mais um
palpite sem conseguir testar no Editor antes de entregar.

Sobre o erro vermelho no console e a câmera não ter mudado: não consigo
puxar o log do dispositivo daqui (sem acesso a adb/Editor neste ambiente),
então não decifrei a causa exata — o código da câmera/botão segue
exatamente o mesmo padrão do botão REINICIAR, que já funciona, então não
achei nada obviamente quebrado revisando de novo. Se você puder tocar
nesse indicador vermelho (normalmente expande e mostra o texto do erro) e
me mandar o texto ou um print, consigo diagnosticar direito em vez de
arriscar mais uma rodada de tentativa e erro.

Já enviado ao dispositivo. Pendente: você testar de novo (difícil, 9
bots, largada + primeira curva) e, se possível, o texto do erro vermelho.

## Rodada — 2026-08-24 (câmera confirmada; bots do difícil pausados; melhor volta corrigida; fantasma adicionado)

Você confirmou: câmera "visão do piloto" funcionando. No difícil, a
bagunça na primeira volta/primeira curva continua (todos os bots erram),
e o erro vermelho no console segue aparecendo "como se fosse log" (não um
erro único — parece ser mensagens normais de log aparecendo na tela, não
necessariamente um crash; sem o texto exato ainda não dá pra confirmar).
Você decidiu: parar de investir tempo nos bots do difícil por agora
("acho que estamos perdendo tempo com esse bot") e focar no fantasma, que
acha que vai deixar o jogo mais legal.

**Melhor volta travada** — você reportou: "depois que alteramos a pista a
melhor volta ficou travada na ultima deveria ser reiniciada toda vez que
a gente colocar uma pista nova". Causa confirmada: `LapRecordStore` era
um histórico único e global (PlayerPrefs), sem nenhuma noção de qual
traçado cada volta foi feita — uma volta rápida de um traçado antigo/menor
"vencia" para sempre qualquer volta em um traçado maior/diferente depois
que o traçado mudava. Corrigido: cada volta agora é marcada com uma
"assinatura" do traçado (`LapRecordMath.CalculateClosedPathLengthMeters` +
`FormatTrackSignature` — o comprimento total da volta fechada, arredondado
pro metro mais próximo, ex. "239m"), e o quadro de melhores voltas só
mostra voltas com a MESMA assinatura do traçado atual. Como bônus: todo o
histórico salvo antes desta correção usa o formato antigo (sem
assinatura) e é descartado automaticamente no primeiro carregamento — ou
seja, sua próxima corrida já começa com o quadro zerado, sem precisar
fazer nada. Limitação conhecida: uma mudança de traçado que não altere o
comprimento total da volta (ex. só a largura) não seria detectada — não é
o caso de nada que fizemos até agora. 6 testes novos de EditMode.

**Fantasma (M4, versão rápida)** — pedido do fundador: "vamos tentar
seguir talvez o fantasma fique mais legal... podemos ir pra m4 que mexe
com o fantasma acho que vai ficar mais legal". Perguntei e você escolheu
a versão rápida e simples (não o sistema formal completo do M4, que
inclui setores, comparação de tempo ao vivo, volta ideal e reescrever os
bots do zero com 5 perfis — isso fica registrado como pendência formal,
ver `tasks.md`). Implementado: grava sua posição/direção a cada 0.1s
enquanto você dirige uma volta; se a volta for válida e mais rápida que a
sua melhor gravada até agora (mesma assinatura de traçado da correção
acima — então o fantasma também reinicia sozinho quando o traçado muda),
essa gravação vira a nova "melhor volta" salva. A partir da segunda volta
válida em diante, um kart fantasma (cor branco-azulada bem clara, fácil
de diferenciar dos bots e do seu próprio kart) aparece reproduzindo sua
melhor volta ao seu lado, sem física nenhuma — não colide com você nem
com ninguém, é só visual. Fica escondido até você completar sua primeira
volta válida (nada gravado ainda pra mostrar). 10 testes novos de
EditMode para a lógica de interpolação. Ainda não confirmado por você
dirigindo — pendente testar: completar 2+ voltas válidas e ver se o
fantasma aparece e reproduz a volta anterior corretamente.

**Pista maior — adiada** — ao investigar como aumentar a pista, descobri
algo importante: o caminho que os bots seguem, a grade de largada, os
checkpoints e outros dados de navegação NÃO vêm da geometria visual
gerada por código (`KartPhysicsPrototypeBootstrap`) — vêm de um arquivo
separado (`OvalMvpTrackConfiguration.asset`, um "ScriptableObject" do
Unity) com coordenadas fixas, escritas à mão, que por coincidência batem
com os números atuais do traçado visual (retas de 38m, curvas de raio
14m). Mudar só o código do traçado visual, sem também reescrever à mão
TODAS as coordenadas desse arquivo (grade, checkpoints, caminho dos bots,
pontos de freada, áreas de escape etc.), deixaria os bots e a validação
de volta completamente desalinhados do que você vê na tela — um bug bem
pior que qualquer um que já tivemos. Perguntei e você preferiu que eu
fizesse o fantasma primeiro e deixasse a pista maior para uma rodada
própria, com a mesma validação cuidadosa (script Python conferindo que
não sobra buraco) que usamos nas últimas vezes que mudamos o formato da
pista.

## Rodada — 2026-08-24 (continuação: fantasma "espera e dispara" corrigido; bot fantasma no difícil)

Você confirmou que o fantasma "funcionou super bem" e ficou mais divertido,
como você esperava, mas reportou um problema: "ele fica esperando o meu
kart passar pra dar sequência quando a gente começa ele arranca e nas
demais volta ele já começa a corrida correndo" — e sugeriu separar o
fantasma por número de voltas da corrida (1, 3, 5). Na mesma mensagem,
você também propôs que os bots do difícil pudessem "simular a volta do
ghost" para ficarem mais competitivos.

**Fantasma "espera e dispara" — causa real e correção.** Investigando o
`TimingManagerLite`, o cronômetro de volta só começa a contar quando você
cruza a linha de largada/chegada — e como a grade de largada fica
fisicamente atrás dessa linha, a volta 1 tem uma "largada parada" (você
sai da grade, acelera, só então cruza a linha e o relógio começa). Já a
partir da volta 2, cada volta começa exatamente no instante em que a
anterior termina — uma "largada em movimento", já em velocidade. Um único
fantasma "melhor volta" misturava gravações desses dois tipos de volta,
por isso podia reproduzir uma largada parada durante o que agora é uma
volta em movimento (ou o contrário) — exatamente o "espera e dispara" que
você viu. A separação por quantidade de voltas da corrida (1/3/5, como
você sugeriu) não resolveria isso: uma volta em movimento tem a mesma
dinâmica seja a volta 2 de uma corrida de 3 voltas ou a volta 4 de uma de
5. Por isso implementei a separação pelo TIPO de volta em vez da
quantidade: agora existem dois "melhores fantasmas" guardados por
traçado — um de largada parada (a primeira volta) e um de largada em
movimento (as demais) — e o jogo sempre mostra o que combina com o tipo
de volta que está rolando agora. Isso deve resolver o problema
independente de quantas voltas você escolher na corrida. 10 testes de
EditMode já cobriam a lógica de interpolação; a lógica de escolha
opening/flying é simples o bastante (comparação de contagem de voltas)
para não precisar de testes novos além dos existentes.

**Bot fantasma no difícil.** Perguntei se preferia manter fantasma e bots
separados por enquanto, ou já fazer o bot seguir a volta do fantasma —
você escolheu a segunda opção. Implementado com cautela, por causa da
lição da rodada 23 (quando tirar o viés de posicionamento dos bots do
difícil perto da largada fez todos eles se amontoarem na primeira curva):
os bots do difícil têm variação de erro de curva igual a zero, então dar
a MESMA linha precisa (a volta do fantasma) para vários bots ao mesmo
tempo provavelmente recriaria esse amontoamento. Por isso, só o bot de
índice 0, só no nível difícil, e só usando o fantasma de "volta em
movimento" (nunca o de largada parada, que é mais curto e não
representativa da corrida normal) vira o "Fantasma" — com nome e cor
(cinza prateado) diferentes dos outros bots, pra você reconhecer de
cara. Reaproveitei toda a lógica de perseguição de waypoints que os bots
já usam (`KartBotController`), só trocando o caminho que ele segue pelo
caminho gravado do fantasma — nenhuma IA nova, então os freios de
segurança que já existem (recuperação de travamento etc.) continuam
valendo. Esse bot só aparece depois que você já tiver pelo menos uma
volta "em movimento" gravada (ou seja, precisa ter completado uma
corrida de 2+ voltas antes); a primeira corrida depois de instalar essa
versão ainda roda com bots normais em todas as posições. Ainda não
confirmado por você dirigindo.

## Rodada — 2026-08-24 (continuação: fantasma vira a corrida inteira; bot fantasma removido)

Você testou e reportou: são 2 fantasmas — o bot fantasma "ficou perdidão"
(navegação ruim, talvez nem precise dele); o outro (o fantasma visual,
sem física) "está normal e muito bom". Também explicou melhor sua ideia
original: o fantasma deveria fazer as 3 ou 5 voltas completas conforme
seu desempenho, sem "relargar" a cada volta — porque do jeito que estava
(mesmo já corrigido o "espera e dispara" da rodada anterior), o fantasma
ainda reiniciava o próprio relógio a cada volta, então nunca dava pra
saber se você tinha completado a corrida inteira melhor que ele. Por
isso, sua ideia original de separar por 1/3/5 voltas fazia sentido — só
que separado por corrida completa, não por volta.

**Bot fantasma — removido.** Como você mesmo notou que talvez não
precisasse dele e ele "ficou perdidão", removi. A causa provável: o
`KartBotController` foi desenhado pra seguir um caminho de referência
esparso (uns 20-40 pontos ao longo da pista) e eu tinha alimentado ele
com a gravação bruta do fantasma (300-600 pontos, um a cada 0.1s) — densa
demais pra lógica de "próximo waypoint" dele, que provavelmente ficava
trocando de alvo rápido demais ou girando em cima do próprio eixo. Se
mais pra frente fizer sentido ter um bot mais competitivo no difícil, o
caminho certo é resolver isso dentro do trabalho formal de IA dos bots
(M4-T08 no tasks.md), não reaproveitar a gravação do fantasma direto.

**Fantasma — agora grava/reproduz a corrida inteira.** Antes, o
`GhostController` gravava e reproduzia UMA volta (a sua melhor), e
reiniciava o relógio dele toda volta — por isso a sensação de "relargar"
mesmo depois do fix da rodada anterior (que só resolveu o descompasso
largada-parada/largada-em-movimento, não o problema de fundo). Agora ele
grava a corrida inteira de uma vez só, do início ao fim, sem reiniciar no
meio — e só salva como novo recorde quando você completa TODAS as voltas
configuradas (1, 3 ou 5) validamente, mais rápido que sua melhor corrida
anterior daquele mesmo número de voltas naquele traçado. Isso resolve os
dois problemas de uma vez: o fantasma nunca mais "reinicia" no meio da
corrida (a largada parada da volta 1 e as largadas em movimento das
demais ficam gravadas exatamente como aconteceram, num único take
contínuo), e agora dá pra saber se você bateu o fantasma olhando o tempo
final da corrida — exatamente o que você pediu. Existem 3 "melhores
corridas" guardadas por traçado, uma para cada quantidade de voltas (1,
3, 5 — sua ideia original), porque agora faz sentido comparar corridas do
mesmo tamanho entre si. Numa corrida de 1 volta, isso já cai
naturalmente na sua "melhor volta" de antes, sem precisar de nenhum caso
especial. Como qualquer corrida de 1/3/5 anterior a esta correção usava o
formato antigo (uma volta só, sem essa gravação contínua), ela não é
reaproveitada — a primeira corrida completa depois de instalar esta
versão já começa gravando do zero para virar a base de comparação.
Reaproveitei toda a lógica de interpolação existente (`GhostMath`, já
testada) — não muda; só troca o que conta como "início do relógio"
(início da corrida, não início de cada volta) e o que é salvo (a corrida
inteira, não uma volta). Ainda não confirmado por você dirigindo.

## Rodada — 2026-08-24 (rodada 25: pista maior, IA de recuperação dos bots, flag sozinho/com bots, comparação de voltas, console de desenvolvimento removido)

Você mandou uma mensagem única, avisando que ia tomar banho e ficaria um
tempo fora, autorizando tudo sem precisar perguntar antes: "ficou da
forma que queriamos, só os bots que estao loucos kkk deveria ter a opcao
de bot ou sozinho ou os 2 pode ser uma flag, outra coisa seria legal ter
o tempo do bot a comparacao dele em todas as voltas... dai no mais pode
acho que ja pode aumentar o circuito... ja que a pendencia dos bots vc
nao conseguiu evoluir a inteligencia deles pra tornar as coisas
competitiveis de verdade, talvez um mecanimos para recentralizar o kart
se caso ele travar ou tiver voltando pra tras, pq ele esta sem referencia
total... e tirar aquele erro que fica no canto inferior direito de
console development". Cinco pedidos, todos implementados e no
dispositivo nesta rodada; nenhum confirmado por você dirigindo ainda.

**1. Console de "development" no canto — removido.** Não era um bug do
jogo: é o indicador/console embutido do próprio Unity, que qualquer build
Android feito com `BuildOptions.Development` mostra automaticamente.
Confirmado pelo `rkw_logcat.txt` da rodada 23 ("Build type
'Development'"). `BuildHelper.BuildAndroidDevelopment()` agora usa
`BuildOptions.None` — próximo build feito por
`scripts/build_deploy_verify.sh` já sai sem o overlay. O log de
performance do M3-T01 (`ScenePerformanceLogger`) não depende dessa flag,
continua funcionando igual.

**2. Pista maior — a "pendência adiada" da rodada 24, resolvida.**
Aumentei o comprimento das duas retas de 38m para 60m (de ponta a ponta),
mantendo o raio das curvas fixo em 14m — por isso as curvas continuam
com exatamente a mesma forma de antes, só afastadas uma da outra; não é
uma pista nova, é a mesma pista "esticada". Escolhi essa forma
especificamente porque dá pra provar, só olhando o código de geração
(`GenerateStadiumCenterline`), que não cria buracos nem sobreposições —
o raio nunca mudou, e o comprimento da reta nunca entra na conta da
curva. Toda a camada "escondida" de dados
(`OvalMvpTrackConfiguration.asset`: grade, checkpoints, linha de
referência, caminho dos bots, pontos de freada, fiscais, zonas de
escape) foi recalculada ponto a ponto para a nova pista — width, formato
de grama, plano de chão e pontos de largada dos bots também. A pista
ficou uns 58% mais longa nas retas.

**3. Bots "perdidos" — mecanismo de recuperação novo.** A recuperação que
já existia (`KartBotController`) só resolve bot fisicamente preso contra
um obstáculo — ela não ajudava um bot que foi jogado longe do traçado por
uma colisão, ou que ficou de costas pro sentido da pista depois de um
giro. Agora, além disso, o bot detecta as duas situações direto — "estou
a mais de 30m do ponto mais próximo do traçado" ou "estou olhando
persistentemente pro lado errado" — e se resnapeia pro ponto mais próximo
do caminho, a mesma lógica que já usa na largada. Deliberadamente
genérico (não usa nenhuma constante amarrada ao tamanho da pista), então
continua funcionando do mesmo jeito depois do aumento do item 2 — era
esse o seu pedido explícito ("que essa inteligencia permaneca no aumento
da pista"). Isso NÃO é o M4-T08 (evoluir a IA competitiva dos bots de
verdade) — é só uma rede de segurança pra quando o bot já ficou
"perdido"; a falta de inteligência de disputa de posição/obstáculo
continua em aberto, registrada abaixo.

**4. Flag sozinho / com bots.** Antes, "sozinho" só existia
implicitamente zerando o contador de bots (0-9). Agora o menu de
configuração tem um seletor explícito SOZINHO / COM BOTS acima do
contador: em SOZINHO o contador vira um texto fixo "0 — corrida sozinho,
só você e o fantasma" (sem botões +/-); em COM BOTS o contador volta a
ficar ativo, com mínimo 1 (0 bots em modo "COM BOTS" seria só uma forma
confusa de dizer SOZINHO), lembrando o último valor não-zero que você
escolheu. O fantasma continua correndo sempre, nos dois modos — ele é o
"você contra sua melhor corrida", independente de ter bots de IA na
pista ou não; essa foi minha interpretação do seu pedido, já que "sozinho
e o fantasma" fazia mais sentido junto do que separado.

**5. Comparação de tempo volta a volta, você x bot.** A tela final de
corrida agora mostra uma tabela nova, "VOCÊ x <NOME DO BOT> — VOLTA A
VOLTA", entre a classificação da corrida e o ranking de melhores voltas:
uma linha por volta, com seu tempo, o tempo do bot, e a diferença entre
os dois (verde/negativo se você foi mais rápido, positivo com "+" se o
bot foi). Com vários bots na pista, escolhi automaticamente o que chegou
mais longe (mais voltas completas, e entre empatados o de menor tempo
total) como o "adversário" da tabela — não dá pra mostrar todos os bots
de uma vez sem lotar a tela. `KartBotController` agora grava o tempo de
cada volta dele (não só a contagem), do mesmo jeito que o
`TimingManagerLite` já fazia pro jogador.

Nenhum dos 5 itens foi confirmado por você dirigindo ainda — todos foram
enviados ao dispositivo (`device_commit_files`) e conferidos byte a byte
lá, mas sem rodar o `build_deploy_verify.sh` nem testar de verdade, já
que essa parte só você pode fazer.

## Rodada — 2026-08-24 (rodada 26: kart real modelado, importado e ligado em todos os karts)

Você usou a ferramenta de 3D separada do Cowork (fora deste chat) pra
modelar um kart de corrida de verdade a partir de uma imagem de
referência — chassi tubular, piso/nariz/side pods de carbono, banco balde
com arco de cabeça, coluna de direção inclinada, eixo traseiro com
coroa/corrente, motor monocilíndrico com aletas e filtro de ar, escape
curvo até o silencioso, tanque de combustível e plaqueta numerada
traseira — e trouxe o `.obj`/`.mtl` exportados pra cá perguntando se eu
conseguia conferir e importar no projeto.

**Conferido — arquivo bem formado.** 30.943 vértices, 37.032 triângulos
(100% triangularizados, nenhum quad sobrando), 149 peças nomeadas
(`wheel_front_left_tire`, `engine_fin_3`, etc.), 11 materiais no `.mtl`
batendo exatamente com os 11 `usemtl` usados no `.obj` (nenhum material
órfão nos dois sentidos), nenhum índice de face fora do intervalo de
vértices, nenhum `NaN`/`Inf`. Kart de ~2,31m de comprimento x 1,45m de
largura x 0,99m de altura, já apoiado no chão (Y mínimo ≈ 0) — dimensões
plausíveis pra um kart de verdade. Único problema encontrado: o `.obj`
referencia `mtllib racing-kart.mtl`, mas o arquivo `.mtl` exportado tinha
nome ligeiramente diferente (`racingkart.mtl`, sem hífen) — corrigido
renomeando o `.mtl` pra bater exatamente, senão o importador do Unity não
ia achar os materiais.

**Importado.** Unity 6 lê `.obj` nativamente (mesmo importador de modelo
que já lê os `.fbx`/`.dae` deste projeto — não precisou de nenhum pacote
novo). Coloquei os dois arquivos em
`Assets/RKW/Physics/Resources/KartPhysics/Models/RacingKart.obj`
(+ `.mtl` ao lado), mesma pasta `Resources` de onde `KartVisual.fbx`
(Kenney) já era carregado em runtime, pra poder trocar só o caminho que o
código carrega.

**Ligado em todos os karts — com uma ressalva de performance sobre a qual
te perguntei.** Achei duas coisas que valiam sua decisão antes de ligar
de verdade: (1) 37 mil triângulos só nesse kart, multiplicado por até 10
karts numa corrida (jogador + 9 bots), passa longe do orçamento de
~100 mil triângulos que o próprio M3-T01 documenta pra pista INTEIRA,
pensando em celular; (2) o código que pinta cada kart de uma cor (azul
pro jogador, paleta por bot, gelo pro fantasma) sobrescrevia TODOS os
slots de material do modelo — nesse kart novo, isso ia apagar toda a
diferença de cromado/carbono/borracha/latão que você acabou de elogiar,
deixando tudo uma cor só igual ao carrinho de antes. Te perguntei como
queria usar (só o jogador / todo mundo aceitando o custo / só guardar por
enquanto); você respondeu "todo mundo, aceitando o custo" (a pergunta
chegou cortada do seu lado, mas essa foi a resposta registrada, então
segui por ela). Implementado assim:

- `KartVisualResourcePath` agora aponta pro `RacingKart.obj` novo, pro
  jogador, todos os bots e o fantasma (mesma função `CreateKartVisual`
  pra todos, não mudou).
- A pintura por cor não sobrescreve mais TODO material — agora só troca o
  slot cujo material original se chama "carbon" (o mais próximo que esse
  kart de roda aberta tem de "carroceria pintável"; motor, chassi
  cromado, pneus, banco etc. ficam com os materiais originais do modelo).
  Se algum dia o `KartVisualResourcePath` voltar a apontar pro Kenney FBX
  antigo (que não tem material "carbon"), o código detecta que nenhum
  slot foi tingido e cai de volta no comportamento antigo (tingir tudo),
  então aquele modelo continua funcionando também.
- `KartVisual.fbx` (Kenney) ficou no disco, sem referência nenhuma no
  código — não apaguei, só parou de ser carregado.

**Pendência real, não escondida:** o orçamento de triângulos passa do que
o projeto documentou para mobile — isso SOMA, não substitui, o custo da
pista/cenário. Uma rodada de otimização (decimação da malha, LOD, ou uma
versão mais simples só pros bots) fica pendente; não tentei fazer isso
aqui porque não tenho nenhuma ferramenta de simplificação de malha neste
ambiente (sem Blender, sem Editor do Unity) pra fazer com segurança.
Melhor rodar o build e ver a performance de verdade no dispositivo antes
de decidir se vale a pena investir nisso agora ou não.

## Rodada — 2026-08-24 (rodada 27: câmera/escala, esterçamento ativo, volante e pedais novos no cockpit e na UI, numeração dos karts)

Você mandou uma lista grande de pedidos de uma vez (seleção de pistas,
câmera/escala, física de derrapagem, leaderboard por modo, numeração dos
karts, volante/pedais novos) mais 4 arquivos novos (`steeringwheel.obj`/
`.glb` e `pedalbox.obj`/`.glb` — um volante e uma pedaleira que você
modelou na ferramenta de 3D separada do Cowork). Dado o tamanho, dividi
em itens e fui implementando um a um, documentando aqui o que ficou
pronto nesta rodada e o que ainda não. **Itens de física de derrapagem,
leaderboard por modo e a Pista 2 (circuito técnico) ainda NÃO foram
implementados** — ver a lista de pendências no fim desta seção.

**Câmera/escala do kart — implementado, não confirmado dirigindo.**

- Câmera de 3ª pessoa aproximada ~28% (offset de `(0, 4.6, -7.2)` pra
  `(0, 3.3, -5.2)`, mesmo ângulo, só mais perto) — fazia sentido reaproximar
  agora que o kart é o modelo detalhado (rodada 26) e não mais um placeholder
  genérico. O campo de visão (FOV) da câmera não mudou.
- Botão de troca pra câmera do piloto (cockpit): você reportou na rodada 23
  que apertar o botão "não mudou" a câmera, e isso nunca foi diagnosticado
  por falta de log do celular naquele momento. Revisei toda a lógica de
  troca de câmera de novo e ela está correta — o bug real que encontrei foi
  que esse botão era o ÚNICO elemento de tela (entre volante, pedais, botão
  de reiniciar, placar) que não respeitava a "área segura" da tela
  (`Screen.safeArea`) — todos os outros já respeitam, porque em muitos
  Android a barra de status/gestos/câmera furo-de-agulha pode cobrir
  espaço bruto da tela. Um botão sob essa área fica visível mas impossível
  de tocar, o que bate melhor com "apertei e não aconteceu nada" do que um
  bug de verdade na lógica de troca (que não achei). Corrigido: botão
  agora usa a área segura, ficou também um pouco maior (mais fácil de
  acertar no canto). **Sendo honesto: essa é a correção com melhor
  evidência que encontrei, não uma causa confirmada** — se ainda não
  funcionar depois desse ajuste, preciso de um log do celular no momento
  do toque pra investigar de verdade.

**Esterçamento ativo (rodas + volante do cockpit) — implementado, não
verificado visualmente (sem Editor do Unity neste ambiente).**

O pedido era sincronizar a rotação do volante com a animação de giro das
rodas dianteiras. O modelo `RacingKart.obj` (rodada 26) já vem com as
peças de cada roda dianteira nomeadas separadamente (pneu, aro, cubo,
parafusos — 11 peças por roda) mas SEM nenhum "osso"/pivô pra girar
juntas — o formato `.obj` não tem hierarquia pai-filho, cada peça nomeada
vira um objeto irmão solto. Criei, pro jogador/bots/fantasma, um pivô
vazio na posição exata (calculada a partir do contorno real de cada
roda, não um número chutado) e reagrupei as 11 peças de cada roda
dianteira dentro dele — agora dá pra girar a roda inteira girando só o
pivô. Um componente novo (`KartSteeringVisual`) gira esse pivô a cada
quadro pelo MESMO ângulo que a física já está realmente usando
(`tuning.MaxSteeringAngleDegrees`, o valor que a Ackermann do
`KartDynamics` já lia) — não é um número visual solto, é o ângulo real.

O volante dentro do cockpit também gira, mas com uma peça NOVA: troquei o
volante simples que já vinha embutido no modelo do kart pelo volante que
você modelou separadamente (`SteeringWheel.obj`) — ele fica na mesma
posição onde estava o volante original (calculada a partir do contorno
do volante antigo, de novo sem chute manual) e gira no eixo que ele
mesmo indica ser o eixo de giro certo (o eixo mais fino do seu modelo,
0,086m de espessura contra 0,43m/0,33m dos outros dois — claramente o
eixo que aponta pra frente do piloto).

Mesma peça nova para a pedaleira: troquei os pedais simples do modelo
original pelo `PedalBox.obj` que você modelou, posicionado no mesmo lugar
onde estava a pedaleira original.

**Isto é uma aproximação que preciso que você confirme olhando no
celular:** sem Editor do Unity neste ambiente, não consigo abrir a cena e
ver se o volante/pedaleira ficaram na orientação certa (só calculei a
POSIÇÃO a partir do modelo, a ROTAÇÃO eu assumi igual à do próprio kart).
Se estiverem girados/tortos, me avisa que ajusto.

O fantasma ganha os mesmos volante/pedais novos no cockpit, mas eles NÃO
giram — a gravação do fantasma (`GhostController`) só guarda
posição/direção, nunca guardou o ângulo do volante, então não tem dado
pra animar. Fica assim mesmo por ora; regravar o formato do fantasma pra
incluir isso é uma mudança maior, fora do escopo desta rodada.

**Volante e pedais novos na UI do celular — implementado e no
dispositivo.**

Renderizei os 3 ícones (volante, pedal de freio, pedal de acelerador) a
partir dos seus modelos 3D como imagens PNG com fundo transparente — sem
Blender nem o Editor do Unity disponíveis aqui, escrevi um pequeno
renderizador 3D em Python (do zero, só com as bibliotecas numpy/PIL) só
pra gerar essas 3 imagens. Os botões da UI agora carregam essas imagens
primeiro; se por algum motivo o arquivo não existir num checkout, eles
caem de volta pros desenhos simples gerados por código que já existiam
antes (assim o app nunca fica sem os botões, mesmo que uma imagem suma).

**Numeração visível nos karts — implementado, não verificado
visualmente.**

O modelo `RacingKart.obj` já tem um material chamado "number_plate" — uma
plaqueta na carenagem, claramente pensada pra isso. Gerei o número de
cada kart (o mesmo número que já aparecia no placar) como uma imagem
pequena (fonte de dígitos desenhada à mão, no mesmo estilo "gerado por
código" dos ícones da UI) e apliquei nessa plaqueta — cada kart (jogador,
cada bot, fantasma) ganha sua própria imagem com seu próprio número. O
fantasma usa o mesmo número do jogador (é a gravação da sua própria
melhor corrida, faz sentido ser "o mesmo piloto"). De novo, não consigo
confirmar visualmente sem o Editor — se a plaqueta ficar ilegível ou
pequena demais, me avisa.

**Verificação feita nesta rodada:** sem Editor do Unity disponível neste
ambiente, a verificação foi: (1) todo arquivo `.cs` novo/editado passou
por uma checagem de chaves/parênteses balanceados (proxy de "não quebrei
a sintaxe", não um build de verdade); (2) toda posição/tamanho calculada
a partir do contorno real do modelo 3D (nunca um número digitado à mão),
seguindo o mesmo padrão já usado no ajuste de escala do próprio kart
(`FitVisualScale`); (3) nenhum teste automatizado novo foi escrito — a
lógica desta rodada depende de objetos de cena do Unity em tempo real
(instanciar modelo, ler contorno de malha), não é lógica pura isolável
como as fórmulas de física que já têm testes — mesma situação de outras
funções já existentes no bootstrap (`FitVisualScale` também não tem
teste dedicado).

### Pendências desta rodada

- **Física de derrapagem (transferência de peso + traseira mais fluida)
  — NÃO iniciada.** Pedido explícito seu, ainda na fila.
- **Leaderboard separado por modo (1/3/5 voltas) — NÃO iniciado.**
  Pedido explícito seu, ainda na fila.
- **Pista 2 (circuito técnico com chicanes/hairpins) — NÃO iniciada.**
  É o item mais trabalhoso da sua lista (comparável às rodadas de "pista
  maior" já feitas, com validação de geometria); vai precisar de uma
  rodada dedicada, provavelmente decidindo antes se o projeto passa a
  suportar múltiplas configurações de pista (hoje só existe uma).
- Rotação/posição do volante e pedaleira novos no cockpit — calculados a
  partir do modelo, não vistos numa tela de verdade. Confirmar dirigindo
  e olhando a câmera do piloto.
- Legibilidade da numeração nas carenagens — mesma ressalva, confirmar
  olhando o kart de perto (pode ser via a câmera aproximada desta mesma
  rodada).
- Botão de câmera do piloto: correção de melhor evidência (área segura),
  não uma causa confirmada — avisar se ainda falhar, de preferência com
  um log do celular no momento do toque.
- Orçamento de triângulos (já sinalizado na rodada 26) sobe um pouco mais
  com os novos volante/pedaleira em cada kart — pequeno frente ao total,
  mas soma; ainda sem otimização (decimação/LOD) tentada, mesma limitação
  de ferramentas já registrada.

## Rodada — 2026-08-24 (rodada 28: investigação da sensação de "direção travada")

Você reportou que a direção parece limitada a só uma posição pra direita e
uma pra esquerda, como se não girasse o volante — e chutou que talvez o
kart precisasse de um ângulo de esterçamento bem maior, "quase 90 graus".
Fui conferir os números reais (arquivo de ajuste `PrototypeRentalSportTuning`
e a matemática de curva em `KartDynamicsMath.cs`) antes de mudar qualquer
coisa, porque sua hipótese específica (90 graus) e a causa real acabaram
sendo coisas diferentes.

**Sobre o "quase 90 graus": não é isso, e aumentar pra esse valor pioraria
a sensação, não melhoraria.** Kart de verdade não tem diferencial — as
duas rodas de trás giram sempre na mesma velocidade, presas no mesmo eixo.
Por isso o esterçamento de um kart real é sempre bem limitado (na prática,
algo entre ~15° e ~25°): girar demais a roda da frente faz a roda de trás
de dentro "brigar" com a de fora (uma quer girar mais rápido que a outra,
mas não pode) e o kart simplesmente trava/desliza em vez de fazer a curva.
O valor já configurado no jogo, 24°, está dentro dessa faixa realista — não
é ele que está pequeno demais.

**O que realmente limita a curva: o "orçamento de aderência" dos pneus,
não o ângulo do volante.** A física já não usa o ângulo do volante
diretamente pra girar o kart — ela calcula o raio da curva que aquele
ângulo pediria (geometria Ackermann, a mesma de um carro/kart de verdade)
e depois PRECISA checar se os pneus aguentam fazer o kart percorrer esse
raio na velocidade atual, porque isso puxa o kart pro lado (força
centrípeta) e pneu nenhum aguenta força infinita. Se o pedido de curva
exigir mais aderência do que os pneus têm disponível naquele momento, o
jogo reduz a curva pro máximo que os pneus aguentam — é assim que, na
vida real, um carro faz "understeer" (sai reto) em vez de girar feito
peão quando você vira demais rápido demais.

O problema: nas contas que fiz (categoria Rental Sport, 85 km/h máx.), esse
teto de aderência é atingido MUITO cedo dentro do curso do manche de
direção em velocidades normais de condução:

- A ~40 km/h, você já bate no teto de aderência usando só uns 24% do
  curso do manche.
- A ~60 km/h, só uns 10% do curso do manche já bastam pra bater no teto.

Ou seja: em boa parte das situações de corrida, depois de puxar o manche
só um pouquinho pro lado, o resto do movimento do dedo não faz mais
diferença nenhuma no giro do kart — ele já está girando no máximo que os
pneus permitem. Isso bate exatamente com "parece que só tem 1 posição pra
direita e 1 pra esquerda": tecnicamente o manche é analógico (não é
binário), mas o RESULTADO parece binário porque o teto é atingido cedo
demais.

**Mudança feita — pequena e cautelosa.** Aumentei a aderência lateral
configurada (`lateralGripG`) da categoria Rental Sport de 1.2G pra 1.5G no
arquivo `PrototypeRentalSportTuning.asset` (a categoria realmente usada no
protótipo hoje — conferi no bootstrap que é ela que é carregada). Isso
empurra esse teto pra mais tarde no curso do manche (por exemplo, a 40
km/h passa de ~24% pra ~30% do curso antes de saturar), dando um pouco
mais de faixa "útil" de direção sem tocar no ângulo de 24° (que já está
numa faixa realista) nem inventar uma física nova.

**Sendo honesto sobre o quanto isso resolve:** é uma melhora parcial, não
uma solução completa. Em velocidade mais alta, ESSE tipo de "achatamento"
(o giro não aumenta mais por mais que você vire o manche) é fisicamente
esperado — mesmo um kart de verdade não segura giro fechado a 60 km/h sem
derrapar, é assim que pneu funciona. Se depois de testar essa mudança
ainda sentir a direção "travada" especificamente em curvas fechadas em
baixa velocidade (onde o teto de aderência quase não entra em ação), aí sim
pode ser outra causa e preciso investigar de novo. Esse mecanismo de
aderência é o mesmo sistema por trás da tarefa pendente de "física de
derrapagem" (transferência de peso) — pedido original seu, ainda na fila —
então faz sentido tratar os dois juntos se o ajuste de hoje não for
suficiente.

**Verificação feita:** conferi a matemática (`KartDynamicsMath.cs`) contra
os testes automatizados já existentes (`KartDynamicsMathTests.cs`, que já
cobrem essas duas funções) — não criei teste novo porque não mudei
nenhuma fórmula, só um número de ajuste (`lateralGripG`) no arquivo de
dados da categoria; conferi por `diff` que essa foi a ÚNICA linha alterada
no arquivo. Como sempre neste ambiente, não consigo dirigir o kart pra
sentir a diferença de verdade — preciso que você teste e me diga se
melhorou, piorou, ou ficou igual, e em que velocidade/situação.

### Pendências desta rodada

- Confirmar dirigindo se o ajuste de aderência (1.2G → 1.5G) melhorou a
  sensação, especialmente em curvas de velocidade média (~40-60 km/h).
- Se ainda sentir "travado" em baixa velocidade / curvas fechadas
  especificamente, provavelmente não é mais o teto de aderência (que quase
  não atua nessas condições) — vou precisar investigar outra causa.
- Física de derrapagem completa (transferência de peso), leaderboard por
  modo e Pista 2 seguem NÃO iniciadas (mesma pendência das rodadas
  anteriores).

## Rodada — 2026-08-24 (rodada 29: câmera do cockpit corrigida, pedais animados novos, ajustes de tamanho/cor na UI, novos itens 3D recebidos)

Você testou a rodada 27 no celular e mandou feedback concreto, mais uma
leva grande de arquivos novos (pedais animados, telão, pódio, piloto
sentado no kart). Dado o tamanho, separei o que dava pra resolver nesta
rodada (pequeno e verificável) do que precisa de uma rodada própria — ver
pendências no fim.

**Câmera do cockpit só mostrava a pista — corrigido, não confirmado
dirigindo.** Você pediu pra eu confirmar por que só aparecia a pista e não
o volante/frente do kart. Consegui confirmar com números reais: o volante
fica a cerca de (0, 0.56, 0.4) metros no espaço do kart, e o olho da
câmera estava em (0, 0.75, 0.25) olhando reto pra frente, sem nenhuma
inclinação — fazendo a conta, isso deixava o volante quase 52° ABAIXO da
mira da câmera, bem fora do campo de visão de 62° que ela tinha. Ou seja,
não era um bug de posição, era falta de olhar pra baixo. Corrigido:
câmera do cockpit agora inclina 18° pra baixo e usa um campo de visão mais
largo (78° em vez de 62°, só nesse modo — o modo de 3ª pessoa não mudou),
pra caber volante e capô na parte de baixo da tela e ainda sobrar pista
na parte de cima. Cálculo real, mas ainda preciso que você veja no
celular se a composição ficou boa (nem olhando demais pra baixo, nem
ainda cortando o volante).

**Pedaleira nova, com animação de pressionar — implementado, não
verificado visualmente.** Você mandou um modelo novo de pedais
(`pedals.obj`/`.glb`) com peças de dobradiça reais (`brake_hinge_pin`,
`throttle_hinge_pin`) — dava pra ver nos próprios dados do arquivo que
cada pedal tem um eixo de giro de verdade, diferente da pedaleira estática
da rodada 27. Troquei a pedaleira antiga por essa nova (mesma técnica de
sempre: posição e tamanho calculados a partir do contorno da peça antiga,
não chutados) e criei um componente novo (`KartPedalVisual`, mesmo padrão
do `KartSteeringVisual` da rodada passada) que gira cada pedal de verdade
em volta do próprio eixo de dobradiça, na mesma proporção que você
pressiona o freio/acelerador no toque da tela. Reportou que a pedaleira
antiga "achei eles bem pequenos" — a nova está 40% maior que o cálculo
automático teria feito sozinho.

Sobre "parece que estava de lado": isso valia pra pedaleira ANTIGA
(rodada 27), cuja rotação eu tinha só copiado do kart sem checar o
formato do modelo. Pra essa pedaleira nova, verifiquei a forma real do
modelo (as dimensões dele — 34cm de largura x 35cm de altura x 13cm de
profundidade — batem exatamente com "encaixado em pé, de frente pro
motorista", igual o volante já tinha ficado certo) antes de aplicar a
mesma rotação — mais evidência a favor de ter ficado correto desta vez,
mas ainda não é uma confirmação visual de verdade.

**Verde e vermelho removidos.** O modelo novo de pedais já vem com listras
"accent_red"/"accent_green" de fábrica — troquei as duas por um cinza
neutro que combina com o resto da peça (metal escovado). Também troquei a
cor da barra de intensidade dos pedais na tela do celular (que era
vermelha/verde) pro mesmo cinza neutro — a barra continua subindo e
descendo com a força do toque, só sem a cor.

**Volante e ícones da UI maiores.** Volante do cockpit 3D 20% maior
("poderia ser um pouco maior"). Ícones de freio/acelerador na tela do
celular também maiores (o limite de tamanho deles nos toques estava
conservador para telas largas/alta resolução).

**Itens novos recebidos, ainda NÃO implementados — vão precisar de rodada
própria:**

- **Piloto sentado no kart** (`driverseatedkart.obj/.glb`) — modelo bem
  detalhado (capacete, luvas, macacão, botas, mais de 140 peças
  nomeadas). Colocar o piloto dentro do cockpit é bem mais trabalho que os
  props de volante/pedal: ele precisa se sobrepor corretamente ao assento
  do kart e não atrapalhar a própria câmera do piloto (a câmera senta
  praticamente onde a cabeça dele ficaria).
- **Cena de pódio** (`podiumcelebration.obj/.glb`) — pódio numerado (1º,
  2º, 3º), piloto, taças e 220 peças de confete. Hoje o jogo não tem NENHUMA
  tela de pódio depois da corrida — isso não é só importar um objeto, é
  desenhar essa tela nova inteira (quando aparece, o que ela mostra, como
  sai dela).
- **Telão/placar de pista** (`lapscoreboard.obj/.glb`) — uma estrutura de
  LED com os dígitos de posição/volta/melhor tempo já modelados em 3D
  para até 6 pilotos. Também não existe hoje um objeto assim na pista;
  entra como um novo elemento de cenário.
- **Tela de entrada do jogo** — você pediu pra deixar mais "com design",
  itens maiores, hoje está simples. Ainda não comecei; é um pedido mais
  aberto (não veio com uma referência visual específica) então antes de
  desenhar algo vou te mostrar uma direção pra você aprovar em vez de já
  sair mudando.
- Ainda não entendi pra que serve o arquivo `driverlook.obj/.glb` (um
  busto de piloto com capacete E uma câmera de ação montada nele) — é
  referência para a câmera do piloto, é um item separado, ou faz parte do
  piloto sentado no kart? Me diz quando puder que eu encaixo certo.

**Verificação feita:** os 5 arquivos `.cs` novos/editados passaram por
checagem de chaves/parênteses balanceados; os arquivos 3D novos
(`Pedals.obj`/`.mtl` + `.meta`) foram conferidos por `md5sum` idênticos
entre o que foi enviado e o que chegou no seu computador. As cores dos
materiais do pedal novo vieram dos dados reais do `pedals.glb` (mesma
técnica das rodadas anteriores), exceto o vermelho/verde que troquei de
propósito. Sem Editor do Unity neste ambiente, nada disso foi visto
rodando de verdade — como sempre, preciso que você teste no celular.

### Pendências desta rodada

- Confirmar dirigindo: câmera do cockpit (composição volante/pista),
  pedaleira nova (tamanho, rotação, animação do giro), volante maior.
- Física de derrapagem completa, leaderboard por modo e Pista 2 seguem
  NÃO iniciadas (pendência de rodadas anteriores, sem mudança agora).
- Piloto sentado no kart, cena de pódio, telão de pista e redesenho da
  tela de entrada: recebidos/pedidos nesta rodada, NÃO iniciados — cada
  um entra como uma rodada própria.
- Esclarecer a função do arquivo `driverlook.obj/.glb`.

## Rodada — 2026-08-24 (rodada 30: câmera de novo, pedais/volante da UI reposicionados, ícones dos pedais refeitos, aviso de volta)

Depois que você testou a rodada 29 num build de verdade instalado no celular (não só no Editor), veio feedback bem mais preciso — deu pra mirar melhor os ajustes.

**Câmera do cockpit — tentativa 2, com a conta refeita.**

Na rodada 29 eu tinha inclinado a câmera pra baixo, mas você viu só um
pedaço da frente do kart, sem o volante. Refiz a conta com os números
reais do modelo: o volante fica a uns 0,4m à frente do pivô do kart, e a
câmera estava só 0,25m à frente — ou seja, quase em cima do volante, o
que obriga a olhar quase reto pra baixo (52°) pra vê-lo, e mesmo inclinada
ele ficava fora da tela. Puxei a câmera pra trás (0,25m → -0,05m, ou
seja, um pouco atrás do próprio pivô do kart) — isso sozinho já reduz o
ângulo necessário de 52° pra 23°. Com essa folga, reduzi também a
inclinação (18° → 14°) e aumentei o campo de visão só dessa câmera (62°
→ 78°, câmeras de cockpit geralmente precisam de mais campo de visão por
estar perto de coisas do carro). Continua sendo ajuste "no papel" — não
tenho como ver rodando aqui, preciso que confirme de novo.

**Câmera de 3ª pessoa — mais perto de novo.** Você achou que ainda estava
um pouco distante mesmo depois da rodada 27. Aproximei mais ~20% (era
`(0, 3.3, -5.2)`, ficou `(0, 2.64, -4.16)`).

**Volante e pedais da UI do celular — reposicionados, mais evidentes,
sem verde/vermelho.**

Uma pessoa que nunca jogou testou e ficou confusa sobre onde tocar pra
acelerar/frear — isso é mais importante que qualquer ajuste fino de
física, é a porta de entrada do jogo. O volante e os pedais ficavam a
16-40% do topo da tela; movi os três pros pés da tela (perto de onde o
polegar naturalmente descansa segurando o celular na horizontal).
Freio e acelerador também estavam espalhados pelos 60% direitos da tela
(podiam ficar bem longe um do outro em tela larga); agora ficam
agrupados, próximos, no canto inferior direito. Adicionei um brilho
escuro suave atrás de cada ícone (volante e pedais) pra eles se
destacarem contra o fundo da pista em vez de se misturar. Tirei o
vermelho/verde que tingia o freio/acelerador (pedido seu) — agora os
dois usam um cinza neutro; quem indica qual é qual é o texto embaixo
("FREIO"/"ACELERADOR") e a própria animação de pressionar.

**Ícones dos pedais — refeitos a partir do modelo novo, e desta vez eu
consegui *ver* o resultado antes de mandar.**

Isso explica o "acelerador de lado": os ícones de freio/acelerador que
apareciam na tela ainda eram os PNGs antigos, gerados na rodada 27 a
partir do pedal antigo — eu troquei o pedal 3D do cockpit pro modelo novo,
mas esqueci de regerar essas duas imagens da UI, então elas continuaram
mostrando a peça velha. Desta vez, diferente de toda vez que mexi em
posição/rotação de peça 3D neste projeto, eu gerei as imagens novas com
um renderizador Python e usei a ferramenta de leitura de imagem pra
OLHAR o resultado antes de mandar pra você — pela primeira vez pude
confirmar visualmente, e não só por conta em cima das coordenadas, que a
peça está de frente, não de lado. Ainda não tive a mesma confirmação
visual pro pedal 3D dentro do cockpit em si (isso só o Editor do Unity
mostra) — mas como os cálculos de orientação bateram exatamente com o
que vi renderizado, a confiança aumentou bastante.

**Aviso de volta na tela — novo.**

Pedido seu: mostrar "VOLTA 1", "VOLTA 2" etc. quando você cruza a linha,
com o tempo daquela volta. Aparece por ~2,6s no topo central da tela e
some com um fade suave, usando o mesmo evento que já registrava suas
voltas para o ranking (nada novo sendo calculado, só um aviso a mais em
cima do que já existia).

**Sobre a sensação de curva:** você confirmou que melhorou bastante desde
o ajuste de grip/direção da rodada anterior (deixei o grip lateral um
pouco mais alto). Você disse que ainda não está 100% — combina com a
tarefa de física de derrapagem que já está na fila (ainda não iniciada);
posso voltar nisso quando chegarmos lá.

**Bots:** você confirmou que continuam "perdidos" — sem mudança nesta
rodada, segue pendente.

**Verificação feita:** os 6 arquivos `.cs`/imagem novos passaram por
checagem de chaves/parênteses balanceados (código) e `md5sum` idêntico
entre o enviado e o que chegou no seu computador (todos os arquivos).
As duas imagens novas dos pedais eu vi renderizadas antes de mandar,
diferente das rodadas anteriores. O resto (câmera, posição da UI) continua
sem confirmação visual real — só dá pra saber testando no celular.

### Pendências desta rodada

- Confirmar dirigindo: câmera do cockpit (se agora mostra volante +
  pista numa composição boa), câmera de 3ª pessoa (se a distância nova
  ficou boa), posição/agrupamento dos novos botões da UI, ícones novos
  dos pedais.
- Física de derrapagem completa, leaderboard por modo, Pista 2, piloto
  sentado no kart, cena de pódio, telão de pista e redesenho da tela de
  entrada seguem NÃO iniciados (pendências de rodadas anteriores).
- Bots ainda precisam de atenção (física de curva melhorou, mas a IA dos
  bots em si não foi tocada).

## Rodada — 2026-08-24 (rodada 31: controles bem maiores, freio/acelerador viram "pressionar e segurar", bug do toque do freio corrigido, animação dos pedais na tela)

Você mandou uma lista organizada com vários pedidos numerados. Fui item por
item; alguns já estavam prontos de rodadas anteriores, então separei aqui o
que era novo do que só precisava de confirmação sua.

**Bug do freio — corrigido, e era meu, da rodada 30.**

Você reportou: "o botão não funciona onde está posicionado e a zona de
clique ativa ficou no meio da tela". Achei a causa exata: na rodada 30, eu
movi os ÍCONES do freio/acelerador pra ficarem agrupados perto da borda
direita da tela (pedido seu, pra ficarem mais evidentes) — mas esqueci de
mover junto a área que realmente detecta o toque, que continuou usando o
cálculo antigo (a metade direita inteira da tela, dividida ao meio). Ou
seja, o ícone morava num lugar e o toque que realmente funcionava era em
outro. Corrigi criando um único cálculo (`ComputePedalZones`) que agora é
usado tanto pra desenhar o pedal na tela quanto pra decidir se seu dedo
tocou nele — não tem mais como os dois se desalinharem de novo, porque só
existe um cálculo agora, não dois.

**Mecânica de toque — trocada de "arrastar" pra "pressionar e segurar".**

Antes, a força do freio/acelerador dependia de QUANTO SUBIA o dedo na tela
(um controle "analógico" por posição). Troquei para "pressionar e segurar"
puro, como você pediu: enquanto o dedo estiver em cima do pedal, é
freio/aceleração no máximo; soltou, para na hora. Isso também resolve
outro ponto que você notou (o toque parecia "de lado" às vezes) — antes a
posição exata do dedo dentro da zona mudava o resultado, agora só importa
se está dentro da área do pedal ou não.

**Volante e pedais na tela — bem maiores.**

Pedido seu: "aumentar significativamente". O volante foi de 36% pra 48% do
tamanho da zona onde ele fica; os pedais foram de 68% pra 84% da largura
da coluna deles (e a coluna em si também ficou mais larga). Ambos
continuam no canto inferior, perto de onde o polegar já descansa.

**Animação de "pressionar" nos pedais da tela — nova.**

Antes só o brilho por trás do ícone mudava (mais forte quando pressionado).
Agora, quando você pressiona o freio ou o acelerador, o ícone na tela
afunda um pouco pra baixo, fica levemente mais "achatado" e inclina alguns
graus — simulando o pedal sendo empurrado, mesmo sendo só um desenho 2D
(não dá pra fazer profundidade de verdade numa tela). O movimento é suave
(não é um "liga/desliga" seco), porque a animação usa um valor que sobe e
desce gradualmente, separado do valor real que o kart usa pra acelerar
(esse continua instantâneo, do jeito que "pressionar e segurar" precisa
ser).

**Itens que já estavam prontos — conferi e não precisei mexer:**

- *Volante do cockpit girando com a direção* e *rodas dianteiras girando
  no eixo certo quando você vira o volante*: conferi o código e o nome das
  peças reais do seu modelo 3D do kart (`wheel_front_left_`,
  `wheel_front_right_`, etc.) — bate exatamente com o que o jogo já usa
  pra girar essas peças desde a rodada 27. Ou seja, isso já funciona.
  Peço que você confirme olhando de novo pela câmera de dentro do kart
  enquanto vira o volante nas curvas — se realmente não estiver girando aí
  na prática, me avisa que eu investigo mais fundo (pode ser algo que só
  aparece rodando de verdade, que eu não consigo ver por aqui).
- *Pedais do cockpit (3D) animando ao acelerar/frear*: também já
  implementado desde a rodada 28 (o pedal 3D dentro do kart se inclina
  conforme você acelera/freia). Com a mudança de "arrastar" pra "pressionar
  e segurar" desta rodada, esse movimento passa a ser mais abrupto (liga
  no máximo, desliga do zero) em vez de gradual — é esperado, já que o
  pedal real também é isso agora.
- *Ícone do volante na tela ser o mesmo modelo 3D do volante de dentro do
  kart*: abri a imagem que já está no jogo pra conferir — é sim renderizada
  a partir do seu modelo 3D real do volante (dá pra ver os parafusos, o
  formato e até o botão vermelho dele). Não precisei trocar nada aqui.

**Câmeras — não toquei, como combinado.**

Você aprovou a posição das câmeras de 1ª e 3ª pessoa nesta mensagem. Não
mexi em nada do arquivo de câmera nesta rodada.

**Verificação feita:** o arquivo alterado (`KartPrototypeInput.cs`) passou
pela checagem de chaves/parênteses balanceados e o `md5` bateu idêntico
entre o que te mandei e o que chegou no seu computador. Não tenho como
rodar o jogo por aqui — como sempre, só um build de verdade no celular
confirma se a área de toque do freio ficou 100% alinhada e se o tamanho
novo dos controles ficou bom.

### Pendências desta rodada

- Confirmar num build instalado: tamanho novo do volante/pedais, se
  "pressionar e segurar" está respondendo bem, se o freio agora responde
  exatamente onde o ícone aparece, a animação de pressionar dos pedais na
  tela.
- Confirmar olhando a câmera de cockpit: se o volante 3D e as rodas
  dianteiras realmente giram pra você (o código já faz isso, mas só teste
  real confirma).
- Física de derrapagem completa, leaderboard por modo, Pista 2, piloto
  sentado no kart, cena de pódio, telão de pista e redesenho da tela de
  entrada seguem NÃO iniciados (pendências de rodadas anteriores — os
  novos modelos 3D que você mandou, como o piloto sentado e o pódio,
  ainda não foram integrados ao jogo).
- Bots ainda precisam de atenção (não tocado nesta rodada).

## Rodada — 2026-08-24 (rodada 32: segundo kart "18 HP", rodas girando de verdade, câmera 3ª pessoa mais próxima)

Você mandou um modelo novo de kart (kartv2) e pediu duas categorias:
o kart atual como "13 HP, 60 km/h" e o novo como "18 HP, 80 km/h" — além
de rodas visivelmente girando e a câmera de 3ª pessoa mais perto, estilo
Mario Kart.

**O modelo novo veio só em .glb + .mtl, sem .obj — precisei converter.**

Todo modelo 3D que você mandou até agora vinha em dois formatos: .obj
(que o Unity importa nativamente) e .glb (que eu só uso pra ler as cores
dos materiais). Desta vez só veio o .glb. Como o projeto inteiro depende
do Unity conseguir importar o .obj direto, escrevi um conversor que lê o
.glb byte a byte (ele é um formato binário documentado publicamente) e
gera o .obj equivalente — preservando a geometria, os nomes de cada peça
(as rodas já vieram com os mesmos nomes que uso pro kart antigo, tipo
`wheel_front_left_tire`, o que ajudou bastante) e os materiais. Consegui
renderizar o resultado por aqui antes de mandar pra você (mesma técnica
que uso pros ícones dos pedais) — o kart apareceu inteiro, simétrico,
sem peça deslocada ou furo na malha. Ainda assim, essa conversão é nova
neste projeto — se alguma peça aparecer estranha (textura errada, peça
flutuando) no seu build, me avisa que eu confirmo se é erro de conversão
ou só ajuste de posição.

**Duas categorias de kart, com um ponto de atenção nos números.**

O kart atual (RacingKart) ficou com 60 km/h (era 85 — baixei bastante,
como você pediu). O kart novo (KartV2) ficou com 80 km/h. Ponto de
atenção: a categoria "escola" (6,5 HP, já existia) está em 55 km/h — ou
seja, agora "escola" (55) e "aluguel esportivo" (60) ficam só 5 km/h
separadas, bem mais perto uma da outra do que antes (a diferença de
aceleração entre elas continua grande, então ainda dá pra sentir a
diferença, só a velocidade máxima que ficou pouco diferente). Se isso
incomodar quando você testar, é só me falar que reviso os três números
juntos. O resto da física (aderência nas curvas, direção) do kart de 13 HP
não mudei — ficou igual ao que você já tinha aprovado; o kart de 18 HP
usa exatamente a mesma aderência/direção, só correndo mais rápido.

**Como trocar de kart pra testar — ainda não é uma tela de escolha.**

Fazer uma tela de "escolha seu kart" antes da corrida é tarefa grande,
não dava pra fazer nesta rodada junto com o resto. Por enquanto, apareceu
um botão novo no canto superior esquerdo (embaixo do botão de trocar de
câmera) que troca o kart do jogador entre os dois modelos + a física de
cada um. Só funciona ANTES da corrida começar (antes do "VAI!") —
depois que a corrida começa ele some, de propósito: trocar o carro
inteiro no meio da corrida, com o carro em movimento, é arriscado (a
física ficaria "no ar" por um instante) e eu não tinha como testar isso
com segurança aqui. Quando quiser a tela de escolha de verdade, me avisa
que entra na fila como tarefa própria.

**Rodas: elas realmente NÃO estavam girando — não era só a cor.**

Fui conferir o código antes de simplesmente confiar no seu diagnóstico
("a roda é preta, por isso parece parada") e descobri a causa real: só
existia a rotação de ESTERÇAMENTO (a roda virando pra esquerda/direita
numa curva). Rotação de ROLAMENTO — a roda girando pra frente conforme o
kart anda — nunca tinha sido implementada, em nenhum dos dois karts. Ou
seja, era uma peça faltando mesmo, não só uma questão de contraste.
Implementei isso agora pras 4 rodas dos dois karts (as da frente giram
tanto pro esterçamento quanto pro rolamento ao mesmo tempo, corretamente
combinados; as de trás só rolam, já que não esterçam). A velocidade do
giro usa a velocidade real do kart e o tamanho de cada roda (medido do
próprio modelo 3D, por isso as rodas de trás — maiores no kart novo —
giram num ritmo um pouco diferente das da frente, do jeito certo).

Além disso, segui seu pedido e coloquei uma marquinha clara (cor clara,
quase branca) perto da borda de cada roda, pra ficar óbvio que ela está
girando mesmo de longe.

**Câmera de 3ª pessoa — mais próxima de novo, estilo Mario Kart.**

Aproximei mais ~28% em cima do que já estava (de `(0, 2.64, -4.16)` pra
`(0, 1.90, -3.00)`). A câmera de 1ª pessoa (dentro do kart) eu não toquei
— você pediu pra manter como está.

**Verificação feita:** todos os arquivos `.cs` alterados/novos passaram
pela checagem de chaves/parênteses balanceados, e o `md5` bateu idêntico
entre o que te mandei e o que chegou no seu computador — inclusive o
modelo 3D convertido. O modelo novo eu consegui renderizar e conferir
visualmente antes de mandar (mostrando um kart inteiro e reconhecível,
com pneu redondo de verdade). O resto — física das duas categorias, o
botão de trocar de kart, o giro das rodas rodando de verdade, a câmera —
só um build de verdade no celular confirma.

**Observação à parte, fora do escopo desta tarefa:** notei uma pasta
`_to_delete` na raiz do repositório cheia de arquivos de trava/temporários
do próprio Git (tipo `index.lock`, `tmp_obj_...`) que eu não criei nem
mexi. Isso normalmente não deveria se acumular assim — pode ser sinal de
algum processo do Git que travou. Não toquei nela (fora do escopo desta
tarefa), mas vale você dar uma olhada quando puder.

### Pendências desta rodada

- Confirmar num build instalado: os dois karts (peso/velocidade/rodas
  girando), o botão de trocar de kart antes da corrida, a câmera mais
  próxima.
- Avaliar se a diferença de 5 km/h entre "escola" e "aluguel esportivo"
  precisa de ajuste.
- Tela de verdade pra escolher a categoria do kart antes da corrida —
  ainda não iniciada (tarefa própria, maior).
- Física de derrapagem completa, leaderboard por modo, Pista 2, piloto
  sentado no kart, cena de pódio, telão de pista e redesenho da tela de
  entrada seguem NÃO iniciados.
- Bots ainda precisam de atenção (não tocado nesta rodada).
- Pasta `_to_delete` com arquivos de trava do Git — não investigada,
  fora do escopo desta tarefa.

## Rodada — 2026-08-24 (rodada 33: corrigindo o kart azul, investigando rodas/volante, renumerando velocidades e organizando git/tasks)

Você reenviou o kartv2 perguntando se os arquivos estavam incompletos, reportou o
kart "todo azul feio" sem os detalhes do modelo, o volante e as rodas parecendo
estáticos mesmo com o código da rodada 32, pediu pra inverter as velocidades
(kart novo com 80/85, o antigo com 70, não mais 60/80), pediu pra reiniciar os
tempos, e apontou que provavelmente não estamos commitando no Git nem
atualizando as tarefas do projeto. Fui item por item.

**Os arquivos que você mandou NÃO estão incompletos.**

Conferi os dois byte a byte: o `.glb` tem o cabeçalho correto e o tamanho
declarado bate exatamente com o tamanho real do arquivo (sinal de arquivo
íntegro, não cortado). Comparando com o que você mandou na rodada 32, a
única diferença real está em 5 materiais da fumaça do escapamento
(`exhaust_smoke_0` a `_3`): os valores de transparência mudaram um pouco —
isso é normal, é a assinatura de um efeito de fumaça com vários "quadros"
de opacidade diferente, não corrupção de arquivo. Então pode ficar tranquilo
quanto ao upload; o problema não estava nos arquivos.

**Achei a causa exata do "kart todo azul feio" — e corrigi.**

Fui direto no código em vez de ficar só na suposição. O jogo tem uma regra:
"quando o kart tiver uma peça de material chamada 'carbon' (era o caso do
kart antigo, que é tubular/sem pintura), pinta só essa peça com a cor do
piloto; senão, pinta o kart inteiro com a cor do piloto". O kartv2 não tem
nenhuma peça chamada "carbon" — ele é um kart pintado de verdade, com 16
materiais diferentes (cromado, pneu, plástico, decalque, etc.). Como a regra
não achou "carbon" em lugar nenhum, ela caiu no modo "pinta tudo", e por
isso o kart inteiro (cromado, pneu, banco, tudo) virou a cor sólida do
time — que no seu caso é um azul. Corrigido: agora o sistema tenta várias
peças-alvo por modelo — "carbon" pro kart antigo, "body_primary" (a peça de
pintura principal do kartv2, segundo o próprio arquivo .mtl que você mandou)
pro novo — e deixa todo o resto (cromado, pneus, decalques, banco) exatamente
como você modelou. De brinde, achei e corrigi uma sobra: uma peça chamada
"steering_column" (a coluna de direção) que devia ter sido escondida junto
com o resto do volante antigo do modelo, mas ficava visível, parada, por
baixo do volante novo que a gente encaixa por cima — pode ter contribuído
pra sensação de "o volante não se mexe".

**Rodas e volante "estáticos" — não achei um segundo bug de código, preciso
da sua ajuda pra confirmar.**

Reli com cuidado todo o código que faz o volante e as rodas girarem (a
mesma lógica que escrevi na rodada 32) e ele está estruturalmente correto:
localiza as peças certas do kartv2 pelo nome (conferido nome por nome contra
o modelo 3D), monta os pivôs de giro e esterçamento, e liga tudo à física do
kart, do mesmo jeito que já funciona no kart antigo. Uma coisa que notei: o
botão de trocar de kart só funciona ANTES da corrida começar (antes do
"VAI!") — e propositalmente, antes da corrida, o jogo zera toda a entrada
do jogador (pra ele não sair andando durante a contagem regressiva). Se
você trocou pro kartv2 e olhou pra ele ainda parado no grid (parado = sem
velocidade = rodas não giram; sem tocar no volante = direção centralizada),
é exatamente isso que apareceria, e não seria bug nenhum. Se você já testou
DIRIGINDO de verdade (depois do "VAI!", curva e acelerando) e ainda assim
não viu nada girar, aí é outra coisa, e preciso que teste de novo depois
desta correção do "azul" pra me confirmar — pode ser que o efeito estivesse
lá o tempo todo e só ficou difícil de perceber com o kart inteiro na mesma
cor.

**Fumaça do escapamento: ainda não implementada.**

Reparei que o modelo novo tem, sim, peças de fumaça animada
(`smoke_puff_0` a `_4`, materiais com opacidades diferentes) — dá pra ver
que você preparou o 3D pensando nesse efeito. Isso ainda não está ligado a
nada no jogo (não existe hoje nenhum sistema de partícula/fumaça rodando
atrás do kart). Fica como pendência nova, separada — não é um bug, é uma
funcionalidade que ainda falta construir.

**Velocidades trocadas como você pediu, e uma boa notícia sobre a
separação entre categorias.**

Kart antigo (13 HP): 60 → **70 km/h**. Kart novo (18 HP): 80 → **85 km/h**.
Com a categoria "escola" (6,5 HP) em 55 km/h, ficou 55 / 70 / 85 — na
verdade uma separação MELHOR entre as três categorias do que tínhamos antes
(cada uma 15 km/h mais rápida que a anterior), então isso resolve também a
preocupação que eu tinha levantado na rodada 32 sobre escola/aluguel
ficarem parecidas demais.

**Tempos reiniciados — e um ponto real que você identificou sobre o
"fantasma".**

Zerei o histórico de voltas e os "fantasmas" salvos (a técnica usada foi
trocar a "gaveta" onde esses dados ficam guardados no aparelho — os dados
antigos continuam no celular, só que o jogo não olha mais pra eles a partir
de agora, o que na prática é um reinício limpo, sem precisar mexer
diretamente no seu aparelho). Sobre o seu ponto — "o modelo do kart deve
definir o fantasma" — você está certo, e é mais sério do que eu imaginava
antes de olhar o código: hoje o fantasma e a lista de melhores tempos são
guardados só por pista + número de voltas, SEM NENHUMA distinção por
categoria de kart. Ou seja, hoje um tempo feito no kart de 85 km/h compete
diretamente contra um tempo feito no kart de 55 km/h — exatamente o
problema que você descreveu. A boa notícia: o projeto já tem uma peça de
arquitetura (`LeaderboardKey`) pronta pra incluir a categoria do kart nessa
conta; só falta o sistema que realmente roda hoje (`LapRecordStore` e
`GhostRecordStore`) passar a usá-la. Essa é uma mudança de verdade — mexe
no formato dos dados salvos e no código testado formalmente — grande o
bastante pra merecer sua própria rodada, então NÃO fiz agora (evitando
empilhar mudança grande em cima de uma sessão já bem longa). Deixei anotado
como prioridade número 1 pra próxima conversa.

**Situação real do Git — acho melhor eu te contar com números exatos.**

Você está certo: nada foi commitado desde **19/08**, ou seja, há uns 5 dias
de trabalho — praticamente tudo que fizemos nessa sessão inteira (karts,
câmeras, sistema de fantasma, bots, áudio, este próprio log) está só no seu
computador, nunca enviado ao Git nem ao GitHub. Contei exatamente: **103
arquivos** entre modificados e novos aguardando commit. Como a nossa regra é
eu nunca commitar/enviar nada sem sua autorização explícita, não fiz nada
ainda — só quero te mostrar o tamanho real da situação antes de perguntar
como você quer proceder (pergunta separada, fora deste log).

**Situação do `tasks.md` (o arquivo oficial de tarefas do projeto).**

Conferi: ele está bem cuidado nos lugares que já foram tocados (por
exemplo, a entrada de telemetria de performance é detalhada e honesta sobre
o que falta). O que NÃO está lá: nada das rodadas 26 a 32 (modelo do kart,
câmeras, volante, pedais, giro das rodas) — esse tipo de ajuste fino de
protótipo sempre viveu só no `docs/30-founder-playtest-log.md`, nunca no
`tasks.md`, que é mais formal e cobra critérios de aceite específicos por
tarefa. Antes de eu marcar qualquer caixinha nova lá (tipo "sistema de
fantasma" ou "bots"), prefiro conferir com calma se cada critério de aceite
foi realmente atendido — fazer isso às pressas no fim de uma sessão já
longa é como um erro bobo poderia escapar. Vou deixar isso como a primeira
tarefa recomendada pra próxima conversa, com contexto fresco.

### Pendências desta rodada

- Confirmar num build de verdade: kart novo com as cores certas (não mais
  azul), volante e rodas girando DIRIGINDO de verdade (não só parado no
  grid), as três velocidades novas (55/70/85).
- Decidir e autorizar o que commitar no Git (há 103 arquivos pendentes,
  incluindo pastas de lixo/backup que eu criei e não devem entrar).
- Auditar e atualizar `tasks.md` contra o estado real do repositório
  (fantasma, bots, e outros sistemas que já têm código mas talvez não
  estejam marcados) — recomendado como primeira tarefa da próxima sessão.
- Separar o "fantasma"/melhores tempos por categoria de kart (feito no
  protótipo local por assinatura de pista + `KartCategorySO.CategoryId`;
  dados antigos sem categoria são ignorados, e o `LeaderboardKey` formal
  continua reservado para a tarefa que tiver todas as dimensões disponíveis).
- Fumaça do escapamento — efeito visual ainda não implementado.
- Física de derrapagem completa, leaderboard por modo, Pista 2, piloto
  sentado no kart, cena de pódio, telão de pista e redesenho da tela de
  entrada seguem NÃO iniciados.
- Bots ainda precisam de atenção (não tocado nesta rodada).
- Pasta `_to_delete` com arquivos de trava do Git — segue não investigada.

## Rodada — 2026-08-25 (rodada 34: Pista 2 — traçado validado, greybox implementado, aguardando seu teste real)

Você aprovou o rascunho da Pista 2 (circuito técnico com chicane/S e hairpin,
~1km) como um "greybox experimental", condicionado a resolver antes de
investir muito: posição da largada/grid, largura local, o setor 7-8-9, o
raio real do kart nas curvas, os bots e os checkpoints. Pediu pra eu montar
uma versão simples na Unity (asfalto, barreiras, checkpoints, grid, câmera)
e instalar no seu Galaxy S25. Fiz a implementação completa; a parte que só
você consegue fazer daqui (rodar o Unity de verdade) está descrita no final.

**1) Largada e grid — resolvido.** A largada (e as 10 vagas do grid, em 5
filas de 2, alternadas) agora ficam 40 a 68 metros dentro da reta principal,
claramente longe da curva 10 (o hairpin) e com 138 metros de reta livre
até a frenagem da curva 1 — bem diferente do desenho anterior, onde a
largada quase encostava na saída do hairpin.

**2) Largura — parcialmente resolvido, resto fica pra depois do teste
real.** A reta da largada agora tem 8,5 metros (era 7m fixo), com um
afunilamento suave de volta a 7m antes da curva 1 — atende o pedido mais
concreto ("8-9m na largada"). NÃO fiz o alargamento específico no hairpin
nem as áreas de escape nas curvas 3/5/7/8/10 — são mudanças de geometria
mais trabalhosas, e como você mesmo concluiu, uma volta real vai dizer mais
sobre se elas são realmente necessárias do que eu adivinhar agora. Fica
anotado como pendência, não esquecido.

**3) Setor técnico 7-8-9 — ainda não posso confirmar, só você dirigindo.**
Mantive o traçado aprovado (não mudei a geometria das curvas). Essa é
exatamente a pergunta que só um teste real responde.

**4) Raio real do kart nas curvas — verifiquei com os números de verdade
do jogo, não só a geometria.** Usei os dados reais de cada categoria
(entre-eixos 1,05m, esterço máximo 24°/28°, grip lateral, desaceleração de
frenagem) dos arquivos de tuning atuais:

- Raio mínimo de giro do kart (Ackermann): ~2,0-2,4m — bem menor que a
  curva mais fechada da pista (hairpin, ~9,8m), então geometricamente o
  kart faz a curva sem problema, ela só vai exigir freada de verdade.
- Curva 8 (o "S" técnico) é a mais exigente de frear: o kart Sport+ (85
  km/h) precisa de ~15 dos 23 metros de reta disponíveis pra chegar na
  velocidade da curva — folga de só ~8m. É apertada, mas cabe.
- O hairpin (curva 10), apesar de mais fechado, tem 141 metros de reta
  antes dele — folga enorme (mais de 120m) pra qualquer categoria frear.

**5) Bots — mantive como um ponto em aberto, de propósito.** Os bots vão
seguir o mesmo traçado (agora como uma lista de ~200 pontos ao longo da
pista) sem tratamento especial pro hairpin ou pro setor técnico. Como você
mesmo notou, esse formato é bom justamente pra revelar onde a IA atual
tem problema — não tentei adivinhar isso antes do teste.

**6) Checkpoints — resolvido, mais robusto que a pista atual.** A pista
antiga (oval) tem 4 checkpoints. A Pista 2 tem 12 (largada/chegada + 11,
um em cada curva) — cobre bem mais o traçado contra corte de caminho,
incluindo especificamente o ponto onde o modelo em Python achou a menor
distância real entre trechos não vizinhos da pista (perto do hairpin).

**O que foi IMPLEMENTADO nesta rodada (código escrito e revisado por mim,
mas ainda não compilado — ver observação importante no final):**

- Todo o traçado da Pista 2 (pista, barreira externa, barreira interna,
  largura variável na largada) como um novo método separado
  `CreateCourseTechnicalCircuit2()`, sem tocar no código da pista atual.
- 12 checkpoints rotacionados (a pista antiga só tinha checkpoints retos,
  porque suas curvas caem certinho nos eixos X/Z do mundo; a Pista 2 tem
  curvas em qualquer ângulo, então criei uma versão que gira o portal de
  checkpoint pra encaixar em cada curva).
- Um novo arquivo de configuração da pista
  (`TechnicalCircuit2Configuration.asset`) com as 10 vagas do grid, os 12
  checkpoints, e o traçado completo (para os bots seguirem).
- Uma chave simples (`UseTechnicalCircuit2 = true`) que decide qual das
  duas pistas o jogo carrega neste build — não é uma tela de seleção de
  pista de verdade (isso ainda não existe no jogo), é só um interruptor
  pra testar uma pista de cada vez. Pra voltar a pista antiga, é só trocar
  esse `true` por `false` e gerar um novo build.

**O que NÃO foi feito (fica pra depois do teste real, de propósito):**
alargamento do hairpin, áreas de escape específicas, decoração (guias
zebra, postos de marshal, cerca, prédio de pit), e uma tela de verdade
pra escolher a pista.

**Observação importante — o que eu NÃO consigo verificar daqui.**

Diferente das mudanças pequenas das rodadas anteriores, esta foi grande o
bastante (uns 250 linhas de código novo + um arquivo de dados com quase
800 linhas) que eu não confio só na revisão manual. O problema é que esta
conexão com o seu computador roda dentro de uma "caixa" Linux separada, que
NÃO tem acesso ao Unity de verdade instalado no seu Mac (o script
`scripts/build_deploy_verify.sh` que já compila, testa, builda e instala no
seu Galaxy tenta chamar o Unity num caminho de macOS que essa caixa Linux
não enxerga). Ou seja: eu escrevi e revisei o código com todo cuidado
manualmente (toda chave/parêntese conferida, toda assinatura de função
conferida contra o código real, todos os arrays de dados com o mesmo
tamanho), mas a única forma de saber com certeza se compila é você rodar
no seu próprio terminal.

**O que eu peço pra você rodar** (mesmo comando que você já usa, sem nada
novo):

```
bash scripts/build_deploy_verify.sh
```

Isso vai (1) rodar os testes automáticos, (2) compilar, (3) instalar no seu
Galaxy S25. Se der erro de compilação, me manda a mensagem de
`rkw_build.log` (ou `rkw_tests.log` se travar nos testes) que eu corrijo na
hora. Se compilar e instalar, você já estará dirigindo a Pista 2 na
primeira abertura do app (é a pista padrão neste build).

### Pendências desta rodada

- Rodar `scripts/build_deploy_verify.sh` e me avisar o resultado (sucesso
  ou a mensagem de erro).
- Depois de dirigir: sua impressão sobre o setor 7-8-9 (S ou chicane de
  verdade?), a curva 8 (frenagem apertada demais?), o hairpin, e como os
  bots se saem.
- Alargamento do hairpin e áreas de escape (3/5/7/8/10) — deliberadamente
  não feito, decidir prioridade após o teste.
- Tela de seleção de pista de verdade — ainda não existe; hoje é só a
  chave `UseTechnicalCircuit2` no código.
- Git: nada foi commitado nesta rodada (mudanças ainda no seu computador).

## Atualização — mesmo dia (rodada 34, continuação: Pista 2 reprovada e pausada)

Você testou o greybox da Pista 2 no aparelho e reprovou: "tem praticamente
só reta, 1 ou 2 pontos de freio, não ficou do jeito que pensei" e o kart
"derrapando do nada" em alguns momentos.

Investiguei os dois pontos:

**"Só reta"**: confirmado pelos números. Das 11 curvas do traçado, só a 8
(o S técnico) e a 10 (hairpin) realmente exigiam frear — as outras 9
(curvas 1 a 7, principalmente) tinham raio grande demais (22 a 42 metros)
e podiam ser feitas quase sem tirar o pé, em qualquer categoria de kart.
Um traçado com 11 vértices no papel, mas que na prática dirige como um
oval largo com dois pontos de interesse.

**"Derrapando do nada"**: você confirmou que acontece justamente nas
curvas abertas/largas — não nas fechadas (8 e o hairpin), onde seria
esperado. Minha hipótese (não confirmada, só embasada): exatamente essas
curvas "quase retas" colocavam o kart numa faixa de velocidade bem no
limite da aderência sem o jogador nunca precisar frear de propósito —
uma zona instável, diferente de "curva fechada, freei, contornei com
margem". Fica como hipótese de trabalho, não fato estabelecido.

Redesenhei (só no modelo Python, sem tocar em código) uma versão 2 do
mesmo traçado (mesmo formato que você aprovou visualmente) com as curvas
1-7 bem mais fechadas — validei que agora 10 ou 11 das 11 curvas exigem
freada real, para as três categorias, mantendo o traçado sem
auto-cruzamento. Mostrei a você antes de tocar em qualquer código da
Unity de novo (pra não repetir o erro de implementar sem confirmar
primeiro).

**Sua decisão**: pausar a Pista 2 por agora e priorizar a física de
derrapagem geral (pendência já conhecida, item de física de
transferência de peso/traseira) antes de continuar com pista nova. Faz
sentido — se a física de aderência tem um comportamento estranho, faz
mais sentido resolver isso primeiro do que continuar construindo pista em
cima dela.

Perguntei se a derrapada também acontece na pista original (oval) ou só
na Pista 2 — ainda sem resposta, você pediu pra parar por hoje.

### Estado ao final desta rodada

- Pista 2: PAUSADA. O código do greybox v1 (rejeitado) continua no
  computador, não commitado. O desenho da v2 (curvas mais fechadas) está
  validado em Python mas NÃO implementado em C#.
- Física de derrapagem: vira a prioridade da próxima rodada. Próximo
  passo definido: ler `KartDynamicsMath.cs`/`KartDynamics.cs` (ângulo de
  deslizamento, transferência de peso, perda/recuperação de aderência) e
  confirmar com você se a derrapada acontece também no oval antes de
  mexer em qualquer número de tuning.
- Nada foi commitado nesta rodada.

## Rodada 35 (2026-08-25) — Pista 2 v4: traçado extraído da foto real, implementado, aguardando seu teste

Depois da Pista 2 v1 ser reprovada ("só reta", derrapagem) e a v2 (mesmo
formato, curvas mais fechadas) e a v3 (formato inspirado nas suas
referências, mas ainda inventado por mim) também não servirem — você disse
claramente "era pra fazer igual eu te mandei" — mudei de método: em vez de
desenhar um traçado "inspirado", extraí o traçado literalmente da foto real
que você mandou (Circuito Março, Carrera Kart) usando processamento de
imagem (identifiquei os pixels da linha vermelha, afinei até um traço de 1
pixel, segui esse traço do início ao fim, converti pra metros). Mandei uma
imagem de conferência (o traçado extraído por cima da foto original) e você
aprovou o formato.

Você também pediu pra largada ficar "na 15 e 14 por ali" — entre duas das
curvas detectadas, onde dá pra ver tanto o lado técnico (esses/hairpin)
quanto o laço externo mais aberto. Corrigi minha primeira tentativa (que
tinha ficado no lugar errado por engano meu) e posicionei a largada logo
depois da curva 15, no trecho mais reto de verdade daquela região (quase 89
metros livres até a próxima curva fechada) — você aprovou essa versão final.

**Dados do traçado v4 (aprovado e agora implementado):**
- ~941 metros de volta.
- 16 curvas oficiais (consolidadas a partir de 33 variações de curvatura
  brutas extraídas da foto, agrupando pontos muito próximos).
- Largura variável: 8,5m na reta da largada, 6m no bolsão técnico (esses +
  a área do hairpin), 7m no resto — parecido com a largura real que dá pra
  ver na foto.
- Geometria validada (mesmo método de sempre): 0 auto-cruzamentos nas
  bordas do asfalto E nas duas barreiras, folga mínima real de ~13,8m entre
  trechos não-vizinhos.
- Física por categoria (verificada com os números reais de tuning do jogo):
  das 16 curvas oficiais, School exige frear em 13, Rental em 14, Sport+ em
  14 — bem mais rico e tecnico que a v1 (2 de 11).

**O que foi IMPLEMENTADO nesta rodada:**
- `KartPhysicsPrototypeBootstrap.cs`: as 5 listas de dados da Pista 2
  (linha central, classificação de trecho reto/curva, largura variável,
  barreira externa, barreira interna) foram TROCADAS pelas novas 385
  posições extraídas da v4 (o método `CreateCourseTechnicalCircuit2` e os
  métodos auxiliares que já existiam da rodada 34 não precisaram mudar,
  só os dados). Caixa de delimitação do chão recalculada pro novo formato.
  `timing.Configure` atualizado de 11 para 16 curvas. Linha de largada/
  chegada reposicionada.
- 17 checkpoints (largada/chegada + 16 curvas, `CP0` a `CP15`), cada um
  rotacionado pra direção real da pista naquele ponto — substituindo os 12
  checkpoints da v1.
- Novo `TechnicalCircuit2Configuration.asset`: 10 vagas de grid (calculadas
  atrás da nova linha de largada, na reta), 17 checkpoints, traçado
  completo pros bots (mesmos 385 pontos), `timingSectorCount: 16`.

**O que NÃO foi feito (pendências conhecidas, não escondidas):**
- Decoração (guias zebra, marshal, cerca, prédio de pit).
- Tela de seleção de pista de verdade (continua sendo só a chave
  `UseTechnicalCircuit2` no código).
- Áreas de escape específicas nas curvas mais fechadas.
- Ajuste fino do grid (a posição de cada uma das 10 vagas foi calculada
  matematicamente atrás da linha de largada, mas nunca vista rodando —
  pode precisar de ajuste depois do teste real).

**Ponto crítico, igual às rodadas anteriores: eu não consigo compilar isso
daqui.** A ponte com o seu computador roda numa máquina Linux separada, sem
acesso ao Unity real do seu Mac. Revisei o código manualmente com cuidado
(chaves/parênteses/colchetes do arquivo inteiro conferidos: 245/245, 2259/
2259, 96/96; os 5 arrays de dados com exatamente 385 elementos cada,
conferido programaticamente; os 17 checkpoints com índices sequenciais 0-15
conferidos), mas só você rodando no seu terminal confirma se compila:

```
bash scripts/build_deploy_verify.sh
```

Se der erro de compilação, me manda a mensagem de `rkw_build.log` (ou
`rkw_tests.log`) que eu corrijo na hora. Se compilar e instalar, você já
vai estar dirigindo a Pista 2 v4 na primeira abertura do app (ela continua
sendo a pista padrão deste build, via a chave `UseTechnicalCircuit2`).

Nada foi commitado nesta rodada.

## Rodada 36 (2026-08-25) — Correções pós-teste da Pista 2 v4 (Carrera Kart)

Depois do seu teste real da Pista 2 v4 ("perfeito projeto inicial aprovado, mas
tem observações"), investiguei e corrigi os dois problemas concretos que você
relatou. Os outros itens (derrapagem estranha, zebra ideal, tela de seleção
de pista de verdade) ainda estão em investigação/planejamento — não foram
mexidos ainda, ver "Pendências" abaixo.

**1) Carrinho e bots nascendo de cara para o muro — CORRIGIDO (causa raiz
encontrada).**

O jogo usa DUAS convenções diferentes de ângulo (`yaw`) no mesmo arquivo de
código, para coisas diferentes:
- Uma pras peças da pista (paredes, checkpoints, faixas de asfalto).
- Outra, sempre 90° diferente da primeira, só pros carrinhos (jogador e
  bots) nas vagas de largada.

Isso já existia antes, funcionando certinho nas pistas antigas (o Oval, por
exemplo — toda vaga de grid dele tem `yawDegrees: 90`, que é o valor certo
pra apontar pro sentido da reta principal). O problema é que, quando eu
calculei as 10 vagas de grid da Pista 2 nova, usei sem perceber a fórmula
das PEÇAS DA PISTA em vez da fórmula dos CARRINHOS — errei o ângulo em
exatos 90° pra cada uma das 10 vagas. Por isso o carrinho (e os bots, que
usam a mesma lista de vagas) nasciam virados de lado, "de cara pro muro",
mesmo com a posição certa.

Corrigido: recalculei o ângulo certo (fórmula dos carrinhos) pras 10 vagas,
direto no arquivo `TechnicalCircuit2Configuration.asset`. Só o ângulo mudou
— as posições das vagas continuam as mesmas de antes.

**2) Linha de largada/chegada na transversal — CORRIGIDO (mesma causa,
problema diferente).**

A linha branca da largada estava sendo desenhada com a peça "antiga" do
jogo, que só sabe desenhar retângulos alinhados com o mapa (eixo X/Z do
mundo) — funcionava bem no Oval e na Pista 2 v1 porque a reta de largada
deles ficava, por acaso, alinhada com esses eixos. Na Pista 2 nova
(extraída da foto real), a reta de largada fica num ângulo qualquer no
mundo — então aquele retângulo "reto" ficava visivelmente torto em relação
à pista.

Corrigido: troquei pra peça "orientável" que o jogo já usa pros checkpoints
e pras paredes — agora a linha gira junto com o ângulo real da pista naquele
ponto (mesmo ângulo já usado no checkpoint de largada, que esse sim já
estava certo).

**3) Nomes dos circuitos — FEITO.**

- Pista antiga (oval): renomeada de "Oval MVP (Clockwise)" para
  **"Circuito Oval"**.
- Pista nova (a extraída da foto real): renomeada para **"Carrera Kart"**.

Hoje esse nome só aparece num log técnico interno (não existe tela de
seleção de pista de verdade ainda — ver pendência abaixo), mas já fica
correto pra quando essa tela existir.

**O que AINDA não foi mexido (aguardando sua confirmação antes de
implementar, ver pergunta que te mandei):**
- Derrapagem estranha em vários pontos da Pista 2 (comparado ao Oval, que
  roda liso) — ainda não investigado a fundo; pode ter relação com a tarefa
  já pendente de melhorar a física de derrapagem (transferência de peso +
  traseira).
- "Zebra ideal" grudada nas curvas como ponto de passagem real, em vej de
  "bloco grande e travado" — te mandei uma pergunta pra confirmar o que
  você quer, porque hoje a zebra do próprio Oval já é só um bloco quadrado
  fixo (4x4m) em cada ápice, não acompanha a curva — pode ser isso que você
  quer trocar, em vez de eu simplesmente copiar o comportamento atual do
  Oval pra Pista 2.
- Tela de seleção de pista de verdade (hoje ainda é só a chave
  `UseTechnicalCircuit2` no código) — também te mandei uma pergunta sobre o
  tamanho dessa mudança antes de começar.

**Igual sempre: eu não consigo compilar isso daqui.** Revisei o arquivo de
código inteiro depois da mudança (chaves/parênteses/colchetes ainda
batendo). Pra confirmar que compila e testar de verdade, roda no seu
terminal:

```
bash scripts/build_deploy_verify.sh
```

Nada foi commitado nesta rodada.

## Rodada 36 (continuação) — zebra contínua nas curvas + tela de seleção de pista

Depois de perguntar e você confirmar as duas coisas que ficaram em aberto na
parte 1 desta rodada, implementei as duas.

**1) Zebra "grudada" nas curvas — FEITO, nas duas pistas.**

Troquei o bloco quadrado fixo (4x4m, um por ápice) por uma faixa xadrez
contínua que acompanha a curva de verdade, igual zebra de pista real:

- **Circuito Oval**: a faixa acompanha as duas curvas (leste e oeste) na
  borda de dentro (lado do infield) inteiras, do começo ao fim de cada
  curva — reaproveitei a mesma fórmula que já gera o asfalto e as
  barreiras dessa pista, só com um raio um pouco menor, então fica
  garantidamente colada e concêntrica, sem cálculo novo arriscado.
- **Carrera Kart**: calculei, ponto a ponto, o quanto cada trecho da pista
  está curvando (raio da curva local) e marquei como "zona de zebra" só os
  trechos realmente fechados (raio menor que 20m) — suavizei esse cálculo
  pra não fragmentar em pedacinhos por causa de ruído da extração da foto.
  Resultado: exatamente 16 faixas contínuas separadas, uma por curva
  oficial — o que reforça que a suavização encontrou as curvas de verdade,
  não ruído. Cada faixa fica do lado de dentro da curva (o lado certo,
  calculado automaticamente pra cada curva, já que a pista tem curvas pros
  dois lados).

Não-sólida (mesmo motivo de sempre: o carrinho não tem suspensão/roda de
verdade pra "subir" numa zebra alta, então ela é só visual/não bloqueia).

**2) Tela de seleção de pista — FEITO, mas é a mudança maior desta
rodada, com um risco que quero deixar bem claro.**

Antes, a pista era decidida no momento de compilar o jogo (uma
chave escondida no código, só eu conseguia mudar). Agora existe uma
telinha de verdade: assim que o jogo abre, aparece "ESCOLHA A PISTA" com
dois botões — "CIRCUITO OVAL" e "CARRERA KART" — e só depois de você
tocar em um deles é que a pista, o carrinho, a câmera etc. são criados.

**O risco que preciso ser transparente sobre**: pra isso funcionar, tive
que reorganizar a ORDEM em que o jogo monta as coisas ao abrir — antes,
tudo (pista, carrinho, câmera, cronometragem) era criado imediatamente ao
abrir o app, numa função só. Agora, essa função foi dividida em duas: uma
mostra só a tela de escolha, e só quando você toca num botão é que a
segunda parte (que é exatamente o que já existia antes, só que adiada)
roda. É uma mudança estrutural, não só cosmética — por isso é a que mais
me preocupa das mudanças de hoje, mesmo eu tendo revisado com cuidado.

Também encontrei e corrigi de cara um efeito colateral real: existe um
teste automatizado do jogo (`KartPhysicsPrototypeTests.cs`) que verificava
o carrinho logo após o jogo abrir — ele ia falhar com essa mudança, porque
agora o carrinho só existe depois do toque na tela nova. Ajustei esse
teste pra "simular" o toque automaticamente antes de conferir o carrinho
(precisei também liberar acesso interno do código pro teste conseguir
fazer isso — outra mudança pequena, em `AssemblyInfo.cs`, seguindo o mesmo
padrão que os outros módulos do jogo já usavam).

A tela em si é bem simples de propósito (sem imagem/preview ainda, só os
dois nomes e uma linha descrevendo cada um) — deixa fácil de deixar
bonita depois, sem mexer de novo na parte arriscada (a reorganização).

**Nada disso eu consegui testar rodando de verdade — mais importante ainda
nesta rodada que nas anteriores**, porque mudei a ordem de inicialização
do jogo, não só dados de uma pista. Pedi bastante cuidado a mim mesmo na
revisão (chaves/parênteses/colchetes do arquivo inteiro conferidos de
novo depois de cada mudança), mas essa é o tipo de mudança que só um teste
real no seu Android confirma 100%:

```
bash scripts/build_deploy_verify.sh
```

Se der erro de compilação ou de teste automatizado (`rkw_tests.log`), me
manda a mensagem que eu corrijo. Se abrir e a tela "ESCOLHA A PISTA"
aparecer, escolher uma pista, e o jogo continuar normal dali — essa parte
funcionou. Se travar em algum ponto entre o toque no botão e o carrinho
aparecer, é sinal de algo na reorganização que não vi por revisão manual.

Nada foi commitado nesta rodada.

## Rodada 37 (2026-08-25) — segundo lote de correções pós-teste

Você testou de novo e mandou um lote grande de observações. Nesta rodada
investiguei e corrigi as que tinham causa técnica clara; duas ficaram como
limitação conhecida, explicadas abaixo.

**1) Zebra "piscando" / vermelho e branco se sobrepondo — CORRIGIDO.**

Causa raiz: pra evitar buraco nas curvas, o código "estica" cada pedaço da
pista/parede um pouco além do tamanho exato, pra um pedaço cobrir a
emenda com o vizinho — invisível quando o pedaço é todo da mesma cor
(asfalto, parede). Mas a zebra alterna vermelho/branco a cada pedaço, e
esse "esticado" fazia dois pedaços de cor diferente ocuparem o mesmo
espaço na emenda — exatamente o "piscando"/"sobrepondo" que você viu.
Corrigido: tirei o esticamento só da zebra (nas duas pistas). Efeito
colateral honesto: agora pode aparecer uma frestinha mínima (poucos
centímetros) na emenda de cada pedacinho da zebra em vez do
"sobrepondo" — troca que vale a pena, já que frestinha é bem menos visível
que as duas cores brigando.

**2) Kart derrapando sozinho, quase toda hora na Carrera Kart — CAUSA RAIZ
ENCONTRADA E CORRIGIDA.**

Essa era a mais séria. O traçado da Carrera Kart foi extraído de uma foto
real, então mesmo os trechos classificados como "reta" têm uma
curvatura mínima real (ruído da extração) — nada visível a olho nu, mas
o código só "estica" os pedaços classificados como curva, achando que
"reta" significa reta matemática perfeita (que é verdade no Circuito
Oval, mas não na Carrera Kart). Resultado: nos pontos onde dois pedaços
"retos" se encontram com esse pequeno ângulo residual, ficava um buraco
de verdade no chão da pista — cheguei a medir um caso de quase meio metro
de buraco na borda do asfalto. O carrinho passando por cima de um buraco
desses dá exatamente aquele "derrapão do nada". Corrigido: agora todo
pedaço da pista/muro da Carrera Kart estica igual, reta ou curva — mesma
lógica seguríssima que a pista/muro do Circuito Oval já usava nas curvas.
Efeito colateral bom: isso também deve ter resolvido (ou pelo menos
melhorado bastante) a rugosidade entre pista e grama que você viu.

**3) Ranhura entre pista e grama nas duas pistas — melhorado.**

Além do item 2 acima (que ajuda bastante na Carrera Kart), a grama
sentava um pouco mais baixo que o asfalto nas duas pistas (uma
"escadinha" de ~1,5cm no limite entre os dois) — subi a grama pra ficar
bem mais rente ao nível do asfalto, nas duas pistas.

**4) Falta desenho do grid de largada — CORRIGIDO nas duas pistas.**

Cada uma das 10 vagas agora tem um retângulo amarelo pintado no chão,
igual pista de kart de verdade, mostrando exatamente onde alinhar.

**5) Modo SOZINHO sempre na pole — CORRIGIDO.**

Sem bots, o carrinho sempre nasce na vaga 1 (pole). Com bots, continua
sendo sorteio igual antes (pra ninguém ser sempre o primeiro).

**6) Botões de categoria de kart "invertidos" — CORRIGIDO.**

Não era exatamente inversão de dado — o botão sempre mostrava o nome do
OUTRO kart (pro qual você ia trocar), então quando você estava dirigindo
o rápido, o botão mostrava o texto do lento, parecendo errado. Agora ele
mostra o kart que você está dirigindo NA HORA, com "(toque para trocar)"
do lado pra deixar claro que é um botão de troca.

**7) Velocímetro em cima do botão de câmera / "grip" sem explicação —
CORRIGIDO.**

Movi o velocímetro pro topo-centro da tela (não tinha nada lá) e aumentei
bem o tamanho da fonte (quase dobrou). Tirei o "grip X.XX" do visor: é um
número interno de física (o quanto de aderência lateral sobra no momento,
cai numa derrapada e volta depois) sem um significado direto pro
jogador — por isso você não reconheceu o que era; não é um dado que faça
sentido mostrar pra quem tá jogando, então tirei da tela (o número
continua existindo por dentro do jogo, só não aparece mais).

**Não mexido, ficou como limitação/pergunta em aberto:**

- **"Não sei se andar na grama pede velocidade"** — resposta honesta: no
  Circuito Oval, SIM, já existe perda de aderência dirigindo na grama do
  miolo (um recurso que já existia). Na Carrera Kart, ainda NÃO — nunca foi
  implementado lá. Fazer certo pra essa pista exigiria desenhar a área de
  grama seguindo o contorno real da pista (parecido com o trabalho que fiz
  pro próprio asfalto), o que é um trabalho de verdade, não um ajuste
  rápido — prefiro fazer isso com cuidado numa rodada própria a arriscar
  algo malfeito agora, já que essa rodada já mudou bastante coisa.
- **Zebra ainda em blocos retangulares, não perfeitamente redonda** — isso
  é uma limitação de como a pista inteira é construída (cada pedaço é
  literalmente uma caixa reta; nenhuma parte da pista é uma curva "de
  verdade", nem o próprio asfalto). Corrigi o pior sintoma (o
  piscar/sobrepor), mas deixar visualmente redondo de verdade precisaria
  trocar a técnica de construção da zebra (ou aumentar bastante a
  quantidade de pedacinhos) — posso investigar isso se for importante pra
  você, mas não é um ajuste de uma linha.

**Nada disso eu testei rodando de verdade.** Essa rodada mexeu em bem mais
arquivos que o normal (5 arquivos de código). Comando de sempre:

```
bash scripts/build_deploy_verify.sh
```

Nada foi commitado.

## Rodada 38 (2026-08-25) — velocímetro, fantasma por categoria, grid redesenhado, investigação do "sozinho"

Você mandou mais um lote depois do último teste. Esta rodada tem 3 correções
fechadas, 1 correção que preciso que você ajude a confirmar (adicionei
"espiões" no código para isso), e 1 item que continua em aberto como
limitação conhecida.

**1) Velocímetro em cima do botão "REINICIAR" — CORRIGIDO.**

A correção da rodada passada moveu o velocímetro pro topo-centro da tela,
mas esqueci de conferir que o botão "REINICIAR" já morava exatamente ali
(também centralizado, no topo). Resultado: um por cima do outro. Como você
pediu, mantive o velocímetro centralizado e só desci ele um pouco, pra ficar
abaixo do botão de reiniciar em vez de por cima.

**2) Fantasma sempre com o carrinho de 60 km/h — CORRIGIDO.**

Causa raiz: os DADOS do fantasma (a volta gravada) já eram separados
corretamente por categoria de kart desde uma rodada anterior — o fantasma do
kart de 80 já tocava a volta certa. O problema era só visual: o código que
desenha o carrinho fantasma na pista sempre usava o modelo 3D do kart de 60,
não importa qual kart você estivesse pilotando. Agora ele sempre desenha o
fantasma com o mesmo modelo do kart que você está usando naquela corrida.

**3) Marcação do grid de largada "grosseira" — REDESENHADA.**

Troquei o bloco amarelo sólido por 3 linhas brancas finas (esquerda, direita
e frente), formando um retângulo aberto atrás — igual pista de kart de
verdade, onde o carrinho entra por trás na vaga. De brinde, encontrei e
corrigi um bug real que o bloco antigo tinha escondido: ele estava desenhado
90 graus girado em relação ao carrinho (o lado comprido ficava atravessado
em vez de alinhado com a direção que o kart aponta) — um efeito colateral
de duas "linguagens" de ângulo diferentes que já existiam no código (uma
pros pedaços de pista, outra pros karts) terem sido misturadas sem querer.
As 3 linhas novas já nascem alinhadas certas com o carrinho.

**4) Modo SOZINHO ainda largando fora da pole — INVESTIGADO A FUNDO,
CORREÇÃO ANTERIOR NÃO ENCONTROU BUG NOVO, PRECISO DA SUA AJUDA PRA
CONFIRMAR.**

Preciso ser honesto: reli com cuidado, de novo, cada trecho de código que
poderia interferir nisso — o bloco que reposiciona o carrinho pra pole
position, o menu que decide "sozinho vs com bots", o sistema que resgata o
carrinho quando ele fica preso ou capota (podia ser candidato, já que o
carrinho fica parado bastante tempo esperando você configurar a corrida —
mas confirmei que esse sistema só liga DEPOIS que a corrida realmente
começa, não antes), o sistema de bots, e a contagem regressiva "3-2-1-VAI".
Não encontrei nenhum erro de lógica dessa vez também.

Como não consegui achar o bug só lendo o código, fiz o seguinte: adicionei
duas linhas de registro (log) no jogo — uma no momento em que o modo
sozinho reposiciona o carrinho pra pole, e outra no momento exato em que a
corrida libera o carrinho pra andar (fim do "3-2-1"). Elas mostram, nos
dois momentos, a posição real do carrinho. Se as duas posições baterem e
ainda assim o carrinho aparecer fora da pole pra você, o problema é outra
coisa (por exemplo, o app não ter sido reinstalado/atualizado de verdade
depois da correção da rodada passada, ou o desenho do "01" no chão estar
confundindo qual vaga é a pole visualmente). Se as posições forem
diferentes, aí sim vou saber exatamente onde procurar a seguir.

**O que eu preciso de você**: depois de rodar `bash scripts/build_deploy_verify.sh`,
testar o modo SOZINHO uma vez, e me mandar o arquivo `rkw_logcat.txt` que o
próprio script já gera — vou procurar nele as linhas que começam com
"KartPhysicsPrototypeBootstrap: OnRaceSetupConfirmed", "solo-mode pole
reposition applied" e "RaceStartController: input released".

**Não mexido, ficou como limitação em aberto:**

- **Derrapada residual na Carrera Kart / "saltinhos" perto da zebra** — você
  confirmou que melhorou bastante depois da rodada passada, mas ainda não
  está 100% regular em algumas retas. Não investiguei mais fundo esse
  resíduo nesta rodada (priorizei os itens novos que você trouxe) — seguo
  disponível pra investigar isso com mais profundidade numa próxima rodada,
  se preferir.

**Nada disso eu testei rodando de verdade.** Comando de sempre:

```
bash scripts/build_deploy_verify.sh
```

Nada foi commitado.

## Rodada 38 (continuação) — velocímetro de vez, zebra da largada removida, vibração na zebra

Você testou de novo e mandou mais um retorno rápido. Duas correções
fechadas, uma funcionalidade nova implementada, e a investigação do modo
sozinho continua em aberto (com uma pergunta direta pra você no final).

**1) Velocímetro ainda desalinhado do "REINICIAR" — agora corrigido de
verdade.**

Encontrei dois problemas, não só um:

- O velocímetro usava uma fórmula de centralização diferente da que o
  "REINICIAR" usa (uma baseada na "área segura" da tela, outra baseada na
  largura total da tela) — em alguns celulares essas duas contas dão
  resultados ligeiramente diferentes, por isso "não fica centralizado com
  o reiniciar". Troquei o velocímetro pra usar a MESMA conta que o
  "REINICIAR" já usa.
- A "outra informação" que colidia era o texto "META: N VOLTAS • BOTS: X"
  (que fica sempre visível durante a corrida) e o aviso temporário que
  aparece cada vez que você completa uma volta — os dois moram bem
  naquela mesma faixa central do topo da tela, e eu não tinha notado da
  primeira vez. Desci o velocímetro pra baixo dos três (reiniciar, meta,
  aviso de volta), todos alinhados no mesmo centro agora.

**2) Zebra bem na largada — removida.**

Encontrei o trecho exato: as últimas curvas antes da linha de chegada têm
uma zebra que passa a menos de 10-13 metros da própria linha de largada —
exatamente o pedaço que você via ali. Removi só esse trecho específico (um
"pedaço" completo e isolado de zebra, sem deixar sobra pela metade); as
outras 15 curvas com zebra na Carrera Kart e as duas do Circuito Oval
continuam normais.

**3) Vibração na zebra — implementado (novidade, não corrige a
derrapada).**

Como você pediu, ela não tira velocidade nem trava o carrinho — é só uma
sensação: enquanto o carrinho estiver tocando (visualmente) uma zebra, o
celular vibra em pulsos curtos repetidos, simulando o "solavanco" de
passar por cima de uma zebra de verdade. Só o SEU carrinho vibra o celular
(os bots não "sentem" nada, já que não seguram um telefone). Como o Unity
não tem um jeito nativo de fazer "vibração contínua" (só pulsos únicos),
simulei repetindo o pulso a cada instante enquanto você estiver na zebra —
deve parecer um zumbido contínuo, mas se sentir mais "picotado" do que o
ideal, me avisa que dá pra ajustar o ritmo.

**4) Modo sozinho ainda fora da pole — sem novidade de código, mas com uma
pista importante nova.**

Você relatou largar em 4º e depois em 8º — ou seja, não é "quase certo,
com uma falha ocasional", é comportamento essencialmente aleatório, igual
o jogo se comportava ANTES da correção da rodada 37 existir. Isso me deixa
bem mais inclinado a pensar que o build que você testou pode não ter essa
correção dentro dele ainda (por exemplo, se o `bash
scripts/build_deploy_verify.sh` não reinstalou o app de verdade, ou se
rodou numa cópia antiga por engano) — e não um bug de lógica que eu ainda
não vi, já que reli esse trecho de código três vezes agora sem achar erro.

**Preciso mesmo do `rkw_logcat.txt`** (gerado automaticamente pelo próprio
`build_deploy_verify.sh` depois de rodar) da PRÓXIMA vez que você testar o
modo sozinho — com os registros que adicionei na rodada passada, esse
arquivo vai mostrar exatamente se a correção está rodando ou não. Sem ele
eu ficaria só especulando.

**Ainda não mexido — quero sua confirmação antes de começar (mudança
grande):** "focaria nos detalhes da pista, deixar ela mais plana" /
derrapada perto da zebra "não está redondo". Essa é a mesma causa raiz já
documentada como limitação: a pista inteira é construída com muitos
pedacinhos retos (não curvas de verdade), e mesmo depois da correção da
rodada 37 (que fechou os buracos entre pedaços), ainda sobra um pequeno
"degrau de ângulo" em cada emenda — mais concentrado exatamente nas curvas
fechadas, que é onde a zebra também fica, por isso a sensação de derrapada
parece "grudada" na zebra. Uma correção de verdade exigiria reconstruir
esse trecho da pista com pedaços bem menores (mais suave) ou trocar a
técnica de construção inteira — um trabalho do tamanho da extração original
da pista, não um ajuste pequeno, e eu não consigo testar rodando de
verdade. Quer que eu entre nisso como o próximo passo grande, ou prefere
que eu continue com ajustes menores primeiro?

Nada foi commitado. Mesmo comando de sempre pra testar:

```
bash scripts/build_deploy_verify.sh
```

## Rodada 39 (2026-08-25) — Carrera Kart: pista mais suave (só nessa pista, por pedido seu)

Você confirmou: investir tempo só na Carrera Kart (a Oval você disse que é
exagerado o problema lá, então não mexi nela). Este foi o trabalho grande
que expliquei antes de começar — aqui está o que fiz, como verifiquei, e o
que ainda depende do seu teste real.

**O problema de novo, em números.** Medi (script à parte, fora do jogo) o
"ângulo de quina" em cada um dos 385 pontos que formam a Carrera Kart: em
média 4,56 graus de quina a cada ponto, com um pico de até 18,6 graus num
ponto só. Isso é bem mais alto do que eu tinha percebido nas rodadas
anteriores (eu só tinha medido antes os pontos classificados como "reta",
não todos) — confirma que praticamente a pista inteira, não só alguns
pontos isolados, tem esse "degrau" de ângulo entre pedaços.

**O que eu tentei primeiro (e descartei): só "amaciar" os pontos que já
existem.** Testei mover cada ponto um pouco na direção da média dos
vizinhos (a técnica mais simples de suavização). Resultado: mesmo forçando
bastante, o pico só caiu de 18,6° para ~13°, e a média mal se moveu (de
4,56° pra ~4,1°) — pouco efeito. Faz sentido: numa curva de verdade,
sempre vai ter uma quantidade mínima de "giro" que precisa acontecer em
algum lugar; só reposicionar os MESMOS 385 pontos não resolve, porque o
giro que a curva PRECISA fazer continua sendo dividido entre os mesmos 385
pedaços.

**O que funcionou: dobrar a quantidade de pontos, com uma curva de verdade
passando por eles.** Em vez de mover os pontos originais, mantive todos os
385 exatamente onde estavam e inseri um ponto novo entre cada par,
calculado com uma curva suave (o método se chama Catmull-Rom, usado
bastante em jogos pra isso) — não é uma linha reta no meio, é uma curva que
já leva em conta a forma da pista ali. Resultado: **385 pontos viraram
770**, e o mesmo giro que antes acontecia todo de uma vez agora fica
dividido em duas partes mais suaves.

**Resultado medido (antes → depois):**
- Ângulo de quina médio: 4,56° → 2,37° (quase pela metade)
- Ângulo de quina no pior ponto: 18,6° → 10,4°
- Comprimento total da pista: praticamente igual (940,79m → 941,00m, 21cm
  de diferença em quase 1km)

**Por que o comprimento quase não mudar importa pra você:** o jogo usa o
comprimento da pista (arredondado pro metro mais próximo) pra saber se é
"a mesma pista" quando compara sua volta com o fantasma/melhores tempos.
940,79 já arredondava pra 941; 941,00 também arredonda pra 941 — **seus
recordes e fantasma da Carrera Kart continuam válidos, não vão resetar.**

**Por que eu não toquei no grid de largada nem nos 17 checkpoints:** os
385 pontos originais continuam EXATAMENTE onde estavam (só adicionei
pontos NOVOS entre eles, não movi nenhum). Como o grid de largada e todos
os checkpoints foram posicionados exatamente EM CIMA de pontos originais
específicos (conferi um por um, distância zero), eles continuam
perfeitamente alinhados — nada precisou mudar ali.

**O que NÃO ficou perfeito, sendo honesto:**

1. **Ainda vai ter algum "degrau" residual.** Reduzi a quina pela metade,
   não zerei — curvas de verdade sempre exigem algum giro, e por mais que
   eu aumente a quantidade de pontos, sempre vai sobrar uma quantidade
   mínima concentrada nos trechos mais fechados. A expectativa realista é
   "bem melhor", não "perfeitamente redondo".
2. **A zebra (faixa vermelha/branca) não foi reposicionada junto.** Ela é
   uma faixa separada, calculada com sua própria lógica — não mexi nela
   nesta rodada pra não aumentar ainda mais o tamanho da mudança. Como ela
   é só visual (não bloqueia o carrinho), o pior que pode acontecer é ela
   ficar a alguns centímetros fora do lugar exato em relação ao asfalto
   mais suave, nos trechos de curva mais fechada. Se você notar isso e
   incomodar, é rápido de ajustar numa rodada separada.
3. **Dobrei a quantidade de "pedacinhos" da pista e das paredes** (de
   1.155 pra 2.310 blocos, contando asfalto + as duas paredes). Isso deixa
   a pista mais pesada de processar — não tenho como medir o impacto real
   no FPS do seu celular daqui, então vale ficar de olho se notar queda de
   quadros especificamente na Carrera Kart depois desta mudança (o próprio
   `build_deploy_verify.sh` já tira uma métrica de performance, confira
   `rkw_screenshot.png`/os logs se quiser comparar).

**Não testei rodando de verdade — mais importante ainda nesta rodada, já
que reescrevi as 5 listas de dados principais da pista (as mesmas
verificadas com cuidado nas rodadas anteriores) mais duas listas dentro do
arquivo da pista (`TechnicalCircuit2Configuration.asset`, os pontos que
os bots seguem e os que o sistema de resgate usa). Conferi tudo que
consegui sem rodar: contagem de pontos batendo em todo lugar (770 nas 5
listas do código, 771 nas 2 listas do arquivo da pista — a diferença de 1
é só porque o arquivo da pista repete o primeiro ponto no final pra fechar
o laço), chaves/parênteses/colchetes do arquivo inteiro conferidos, e a
matemática de cada mudança testada separadamente antes de aplicar. Mas
isso é bastante coisa nova de uma vez, então o de sempre vale mais que
nunca:**

```
bash scripts/build_deploy_verify.sh
```

Se der erro de compilação, me manda `rkw_build.log`. Se compilar e rodar,
o que eu mais quero saber é simples: a Carrera Kart ficou mais lisa pra
dirigir, principalmente perto das curvas? E o FPS continua bom?

Nada foi commitado.

## Rodada 39 (continuação, 2026-08-25) — REVERTIDO: a suavização piorou colisão e quebrou a volta completa

Você testou a Carrera Kart suavizada (rodada 39 acima) e relatou dois
problemas sérios: os pontos de colisão pioraram, e a volta não estava
completando mesmo quando você fazia ela inteira. O velocímetro e o grid
("GM") você confirmou que ficaram certos e organizados, e sentiu a
vibração da zebra funcionando — essas três coisas continuam boas.

**Investiguei antes de mexer em qualquer coisa.** Reli a matemática ponto
por ponto (não só a média/pico geral, que eu já tinha checado antes de
entregar) — a técnica da curva suave em si não criou nenhum ponto pior do
que os que a pista real já tinha: o pior "cotovelo" novo criado por ela
foi 8,9 graus, menor que vários pontos que já existiam antes (até 18,6
graus no pior caso original). Ou seja, a ideia da curva suave não era o
problema.

**O que encontrei de concreto:** ao inserir um ponto novo entre cada par
de pontos antigos, os trechos que JÁ eram os mais curtos da pista original
(mínimo de 0,82m) viraram 14 pedaços novos de até 0,41m — bem curtos,
vários em sequência. Pedaços de "parede física" muito curtos e muito
próximos um do outro são um problema conhecido em motor de física de
jogos: o carrinho, andando rápido, pode "morder" essas emendas em sequência
muito rápida e sofrer travadinhas/saltos — bate exatamente com "pioraram
os pontos de colisão".

**Sobre a volta não completar — hipótese mais provável, ainda não 100%
confirmada:** o sistema que conta a volta só marca como válida se você
passar pelos checkpoints na ordem certa; se o carrinho travou/pulou perto
de um checkpoint por causa do problema de colisão acima, o sistema pode ter
interpretado como checkpoint perdido ou fora de ordem e invalidado a volta
mesmo com o percurso completo visualmente. Não consigo confirmar 100% sem
rodar o jogo aqui, mas os dois problemas aparecendo juntos, na mesma pista,
na mesma rodada, é consistente com essa explicação.

**Ação tomada: revertido para a versão da rodada 38 (já testada e
aprovada).** As 5 listas de dados da pista no código e as 2 listas dentro
do arquivo da pista voltaram exatamente aos 385/386 pontos originais — os
mesmos valores exatos de antes da rodada 39 (recuperados dos dados que eu
já tinha guardado, não uma nova geração). Todas as correções das rodadas
37-38 continuam intactas (zebra sem piscada, remoção da zebra perto da
largada, grid redesenhado, vibração na zebra) — só as 7 listas de pontos
da Carrera Kart voltaram ao estado anterior.

A pista "mais lisa" não está mais no jogo agora — voltou ao estado que
você já tinha testado e aprovado (com o "degrau" residual perto da zebra
que você já conhecia). Uma futura tentativa de suavizar precisaria evitar
encurtar os trechos que já são curtos — só valeria tentar de novo se você
quiser, dado o risco que apareceu.

Pedido: rodar `bash scripts/build_deploy_verify.sh` de novo e confirmar
que a colisão voltou ao normal e que a volta volta a completar.

Nada foi commitado.

## Rodada 39 (continuação 2, 2026-08-25) — causa raiz real da volta não fechar, encontrada e corrigida

Depois do revert acima, você testou de novo e trouxe um dado novo importante:
a volta não completou, mas também **não apareceu nenhuma mensagem de "volta
inválida"** — simplesmente não aconteceu nada. Isso mudou minha investigação
por completo: eu tinha suspeitado antes que fosse um efeito colateral da
colisão ruim (um checkpoint "perdido" no meio da volta por causa de um
trombada), mas isso sempre geraria a mensagem de inválida. O fato de não
aparecer NADA aponta pra outro lugar bem específico do código.

**Causa raiz encontrada (confirmada, não é mais hipótese):** a linha de
chegada da Carrera Kart tem uma trava de "só conta se você estiver vindo na
direção certa" (pra não deixar alguém completar a volta andando de ré). Essa
direção "certa" estava fixa como "sempre pra +X do mapa" — um valor copiado
do código mais antigo do Circuito Oval, cujo comentário original diz
literalmente "o protótipo em sentido horário cruza a largada/chegada em
direção a +X" — isso é verdade pro Oval (a reta de largada dele é alinhada
com o mapa), mas a Carrera Kart tem a largada num ângulo qualquer (ela foi
extraída de uma foto real). Calculei a direção real de quem sai da pole na
Carrera Kart: é bem diferente de "+X", é quase o oposto. Resultado: o jogo
sempre achava que você estava cruzando a linha "de ré", mesmo indo do jeito
certo — e por isso nunca registrava nem invalidava a volta, só ignorava
silenciosamente.

**Corrigido:** a direção correta agora é calculada a partir do próprio
ângulo da linha de chegada (que já estava certo), em vez de usar aquele
valor fixo emprestado do Oval. Baixo risco: só mexe nessa checagem de
direção, não toca em nenhum dado de geometria da pista (não é a mesma área
que causou o problema de colisão da rodada 39).

**Ainda não resolvido, sendo honesto:** a sensação de colisão ruim ("bate
forte", "trava muito") continua, mesmo depois do revert — isso significa
que é um problema mais antigo e mais profundo do que a suavização da rodada
39 (que só piorou um pouco algo que já existia desde antes). Não investiguei
essa causa mais funda ainda; é a limitação já conhecida das rodadas
37-38 ("derrapada perto da zebra/curvas"), só que aparentemente mais forte
do que eu tinha entendido. Perguntei se ele quer que eu invista tempo nisso
agora.

**Também relatado, ainda sem explicação:** uma linha amarela aparecendo
perto da largada, entre as marcações do grid. Não encontrei nenhum material
amarelo no código da Carrera Kart — o único candidato visual ali é a
própria linha branca de largada/chegada, que pode estar aparecendo
amarelada por causa da luz do ambiente (iluminação quente/pôr-do-sol pode
tingir superfícies brancas). Pedi uma captura de tela pra confirmar, já que
não consigo ver o jogo rodando daqui.

Nada foi commitado.

## Rodada 39 (continuação 3, 2026-08-25) — investigação funda da colisão ruim: piso da Carrera Kart reconstruído como peça única

Você confirmou que a volta já fecha certinho (viu o fantasma, andou 2 voltas
conferindo) — ótimo, aquele bug era mesmo o problema. Mas pediu pra eu focar
100% agora no travamento/pulos que continuam acontecendo, inclusive em
linha reta (menos que na zebra/curva, mas ainda acontece).

**O que eu descobri investigando mais fundo.** O chão da Carrera Kart hoje
é feito de 385 "ladrilhos" retangulares separados, um atrás do outro,
cada um levemente girado pra seguir a curva da pista — isso é assim desde
que a pista foi criada (rodada 34), não é coisa recente. Reparei em um
detalhe importante: o carrinho tem a rotação de "capotar pra frente/lado"
travada de propósito (só pode virar de esquerda pra direita, não pode
"inclinar" pra frente/trás nem de lado) — ou seja, ele fisicamente NÃO
CONSEGUE "pular" por estar subindo uma rampa ou capotando. Isso me fez
suspeitar de outra coisa: quando o jogo simula fisicamente o carrinho
passando de um ladrilho pro outro, mesmo que os dois ladrilhos estejam
perfeitamente encaixados (sem buraco, sem degrau), o motor de física às
vezes "sente" a EMENDA entre eles como se fosse um obstáculo pontual —
um problema conhecido em jogos de corrida quando o chão é feito de várias
peças separadas em vez de uma peça só. É basicamente por isso que jogos de
corrida "de verdade" sempre constroem a pista como uma superfície contínua,
não como ladrilhos.

**O que eu fiz:** reconstruí o CHÃO FÍSICO (não o visual — o visual
continua exatamente igual, os mesmos ladrilhos que você já vê) como uma
faixa única e contínua, sem nenhuma emenda, seguindo o mesmo formato e
largura da pista de sempre. Os ladrilhos visuais viraram só decoração (não
sofrem mais contato físico); quem sustenta o carrinho agora é essa faixa
nova, de uma peça só.

**Como eu verifiquei isso sem poder rodar o jogo:** fiz o mesmo cálculo em
Python antes de escrever o código de verdade — conferi que a faixa nova não
tem nenhum "buraco" nem triângulo mal-formado (770 triângulos, todos
válidos), que ela vira pro lado certo em 100% dos casos, e que mesmo na
curva mais fechada da pista a faixa não se dobra em cima dela mesma
(sobra 42% de margem lá, com folga). Também conferi que nada mais no jogo
depende do chão antigo ser "sólido" pra funcionar (o sistema de grama, o
sistema de resgate de carrinho preso, os bots) — ninguém usa esse tipo de
verificação, só o motor de física em si.

**Sendo bem honesto sobre o risco desta mudança:** essa é a mudança mais
incerta que eu fiz no projeto até agora. Diferente da correção da linha de
chegada (que eu consegui PROVAR matematicamente que estava errada), esta é
uma aposta bem fundamentada — meu raciocínio faz sentido de engenharia de
jogos e eu validei tudo que dava pra validar sem rodar — mas só você
dirigindo de verdade vai confirmar se ela realmente resolve o
travamento/pulo. Se não resolver (ou piorar algo), é fácil desfazer: eu
guardei exatamente o que precisa mudar de volta.

**Peço que você teste com atenção nesta rodada:** dirija a Carrera Kart
bastante, em retas e curvas, e me diga se o travamento/pulo sumiu,
diminuiu, ficou igual, ou piorou. Se puder, tente notar se ainda acontece
e em que ponto da pista (perto de qual curva, ou em alguma reta específica)
— isso me ajuda muito a saber se a causa era mesmo essa ou se tem outra
coisa acontecendo.

Nada foi commitado.

## Rodada 39 (continuação 4, 2026-08-25) — piso da Oval, vibração em batida e grama mais lenta nas duas pistas

Você confirmou que o piso novo da Carrera Kart resolveu 100% o travamento (3
voltas limpas) e pediu três coisas juntas: aplicar a mesma técnica na Oval e
"guardar o registro" como padrão para pistas futuras; vibração ao bater no
muro ou em outro carrinho; e diminuir um pouco a velocidade na grama, nas
duas pistas.

**1) Piso da Oval reconstruído com a mesma técnica.** Apliquei na Oval
exatamente a mesma solução que resolveu o problema na Carrera Kart: o chão
físico da pista virou uma faixa única e contínua (sem emendas), e os
ladrilhos visuais de antes viraram só decoração. Diferença importante para
ser honesto: você nunca reportou travamento na Oval como na Carrera Kart,
então essa mudança é preventiva, não uma correção de um problema
confirmado por lá. Como é a mesma técnica já validada (matematicamente e
agora também na prática, pela sua confirmação na Carrera Kart), o risco é
baixo — mas ainda peço que você rode algumas voltas na Oval também, só
pra eu confirmar que não sobrou nenhum efeito colateral.

**Virou o padrão do projeto.** A partir de agora, toda pista nova que eu
construir vai nascer direto com esse tipo de piso único (sem separar em
vários blocos), em vez de eu precisar corrigir depois. Registrei isso no
documento do projeto para não esquecer nas próximas pistas.

**2) Vibração ao bater em muro ou em outro carrinho.** Criei um novo
componente (`KartImpactHapticsController`) que vibra o celular quando o
carrinho colide com algo sólido (muro, outro carrinho), usando a mesma
lógica de "quão forte foi a batida" (velocidade do impacto x ângulo) que já
existia pronta no projeto (calculada, mas nunca usada até hoje). Detalhe
técnico importante: essa vibração NÃO muda a física da batida — o carrinho
continua perdendo velocidade do jeito que já perdia antes (pela física
normal da colisão), eu só adicionei o aviso tátil por cima, sem mexer em
como o carrinho reage à batida.

**3) Grama agora também deixa o carrinho mais lento (não só derrapa mais).**
Aqui encontrei uma limitação real no código: a grama já reduzia a aderência
nas curvas (o carrinho escorregava mais), mas nunca tinha sido conectada à
aceleração/velocidade máxima — ou seja, na reta, andar na grama era
idêntico a andar no asfalto. Corrigi isso: agora o mesmo número que já
existia para a grama (usado nas curvas) também reduz a aceleração e a
velocidade máxima. Usei o mesmo valor que já estava configurado para a
grama da Oval (uma redução de 50%) para manter os dois efeitos (curva e
reta) consistentes entre si — se achar que ficou lento demais ou de menos,
é só um número pra eu ajustar.

**A Carrera Kart não tinha NENHUMA área de grama até hoje** — isso é
novo, não um ajuste de algo que já existia. Criei uma faixa de grama de
1,5 metro de largura dos dois lados da pista, entre o asfalto e o
muro/barreira. A largura de 1,5m foi um primeiro palpite razoável; se
sentir que a faixa é curta/longa demais, eu ajusto o número.

**Um risco que quero deixar bem claro, sem esconder:** a forma mais simples
de implementar "ficar mais lento na grama" foi limitar direto a velocidade
máxima permitida enquanto o carrinho está na grama. Isso funciona bem se
você já está andando devagar ou médio quando entra na grama. Mas se você
entrar na grama vindo em velocidade máxima de asfalto, a redução pode ser
sentida como um "freio" meio brusco no instante que toca a grama, em vez de
uma desaceleração suave. Eu não tenho como sentir isso rodando o jogo — só
você vai poder confirmar se ficou natural ou se pareceu estranho. Se
parecer brusco demais, dá pra suavizar (built em cima de uma força de
frenagem gradual em vez de um limite instantâneo), mas isso é uma mudança
um pouco maior do que a de hoje.

**O que peço que você teste e me diga:**
- Oval: sentiu alguma mudança (bom ou ruim) na dirigibilidade do piso?
- Vibração: sentiu o celular vibrar ao bater no muro/outro carrinho? A
  intensidade/frequência ficou boa ou incomodou?
- Grama (nas duas pistas): sentiu o carrinho mais lento na grama? A faixa
  nova na Carrera Kart (fora do asfalto, antes do muro) tem uma largura
  que faz sentido, ou parece curta/comprida demais?
- Se entrar na grama em alta velocidade, o freio pareceu brusco demais?

Nada foi commitado.

## Rodada 39 (continuação 5, 2026-08-25) — vibração ligada o tempo todo, vácuo "grudando" no carro da frente

Você reportou dois problemas novos na vibração/vácuo que acabei de adicionar,
e voltou a levantar o bug antigo do grid/pole. Investiguei os dois
primeiros a fundo e já corrigi; o terceiro (grid) explico por que ainda não
mexi de novo.

**1) Vibração tocando o jogo inteiro, não só na batida — CORRIGIDO.** Causa
raiz confirmada: o Unity não diferencia "o carrinho está apoiado no chão"
de "o carrinho bateu numa parede" — os dois contam como "colisão" pro
motor de física, porque o piso novo (aquela faixa única e contínua das
últimas rodadas) é um objeto físico sólido, e o carrinho está sempre
encostado nele enquanto anda. Meu componente de vibração não sabia
diferenciar isso, então vibrava o tempo todo, em qualquer velocidade —
por isso você sentiu igual em toda a pista, inclusive na Oval, mesmo sem
bater em nada de verdade. Corrigido checando a direção da superfície
tocada: o chão sempre empurra pra CIMA (perpendicular à pista), enquanto
uma parede ou outro carrinho empurra de LADO — agora só vibra quando o
toque vem de um ângulo de parede/carrinho, nunca do chão.

**2) Vácuo "grudando" no carro da frente — investigado e corrigido.**
Primeiro, o que eu CONFIRMEI lendo o código com cuidado: o cálculo do
vácuo nunca mexe no carrinho da frente, só reduz a resistência do ar do
seu PRÓPRIO carrinho enquanto está perto de outro à frente — matematicamente
não existe um caminho onde ele afeta quem está na frente. Então a sua
observação de "grudar" tem outra explicação, que eu encontrei: o bônus de
velocidade do vácuo ficava no MÁXIMO o tempo todo conforme você chegava
mais perto, inclusive praticamente encostando no carro da frente — ou
seja, nunca existia um ponto em que "chegar mais perto" parasse de valer a
pena, então o jogo meio que incentivava você a continuar empurrando pra
frente até encostar de verdade. Corrigi isso: agora, quando os dois
carrinhos já estão bem próximos (cerca de um comprimento de kart de
distância, ou seja, já quase encostando), o jogo para de contar isso como
"vácuo" — é tratado como contato, não mais como rebufo — e o bônus some
antes de virar empurrão. Fiz questão de não mexer na fórmula interna do
vácuo em si (ela tem um teste automatizado formal guardando o
comportamento "quanto mais perto, mais bônus", que é um requisito
documentado do projeto) — a correção fica só na parte que decide QUANDO
considerar que você está "no vácuo" de alguém.

**Sobre "o vácuo não tem relação com o kart doido":** concordo com a sua
correção — reli meu raciocínio anterior e não tenho evidência real de que
o vácuo cause os bots ficarem "loucos e perdidos" de forma mais ampla; foi
uma hipótese meio apressada da minha parte. Ainda não sei o que causa esse
comportamento mais genérico dos bots. Se puder me contar com mais detalhe
na próxima vez que acontecer (o bot para de andar? anda de ré sem motivo?
sai da pista? fica girando no lugar? em que ponto da pista?) — ou, melhor
ainda, uma gravação de tela de uns segundos — eu consigo investigar de
verdade em vez de ficar chutando.

**3) Grid/pole position ("guarda a posição") — ainda sem solução nesta
rodada, de propósito.** Reli de novo toda a ordem de inicialização
(quando o input do carrinho é travado, quando o sistema de resgate liga o
monitoramento, quando a reposição pra pole roda) e não achei nenhuma
inconsistência nova — é a segunda vez que reviso isso só lendo código sem
achar a falha real. Prefiro ser honesto: não vou arriscar uma terceira
correção "no escuro" sem dado nenhum, porque já aconteceu de eu
"consertar" algo baseado só em leitura de código e a causa real ser outra
(foi o que aconteceu com a pista suave da rodada 39, lembra?). As mensagens
de diagnóstico que já coloquei no código desde a rodada 38 continuam lá.
**Já que você vai testar agora mesmo: se puder me mandar o
`rkw_logcat.txt`** (o próprio `build_deploy_verify.sh` já gera esse
arquivo) depois de tentar o modo sozinho, eu finalmente vou conseguir ver
dado real em vez de só ler código, e resolver isso de vez.

Nada foi commitado. Mesmo comando de sempre: `bash scripts/build_deploy_verify.sh`.

## Rodada 39 (continuação 6, 2026-08-25) — grid/pole RESOLVIDO com prova real, bônus: log poluído e descoberta de FPS

Você mandou o `rkw_logcat_manual.txt` de verdade (jogando de fato, não só
abrindo o app) — e dessa vez tinha o dado que faltava.

**1) Grid/pole ("modo sozinho não larga na pole") — causa raiz encontrada e
corrigida, com prova concreta pela primeira vez.** O log mostrou, numa
corrida sozinho de verdade:
- Antes de confirmar "sozinho": carrinho em `(-57.00, 0.07, -13.20)`.
- Corrigido pra pole: `(-41.00, 0.55, -13.20)` — aplicado certinho.
- 3 segundos depois (quando a contagem 3-2-1 termina e você ganha
  controle): carrinho de volta em `(-57.00, 0.07, -13.20)` — a posição
  ANTIGA, antes da correção!

Achei a causa técnica exata: o carrinho usa um recurso do Unity chamado
"interpolação" pra deixar o movimento mais suave visualmente entre um
cálculo de física e outro. O código que reposiciona pra pole mexia
diretamente na "posição visual" do carrinho, mas não avisava o motor de
física sobre essa mudança — então, um instante depois, o motor de física
"corrigia" a posição visual de volta pra onde ele (o motor de física)
ainda achava que o carrinho estava, desfazendo a correção sem nenhum erro
aparecer. É basicamente um carrinho "fantasma" fisicamente ainda no lugar
velho puxando o carrinho visual de volta pra lá. Corrigido: agora a
reposição mexe direto na física do carrinho (não só no visual), então não
sobra nada pra "puxar de volta". Essa é a primeira vez que tenho uma PROVA
de verdade dessa causa (não só uma teoria lendo código) — as duas rodadas
anteriores de investigação não achavam nada porque eu tinha reposicionado
do jeito "errado" (o visual), então nunca ia aparecer como erro, só como
comportamento estranho.

**2) Bônus: log poluído com 1639 erros por corrida — corrigido.** Enquanto
lia seu log, achei um problema separado: um sistema de diagnóstico interno
(mede triângulos/objetos pra saber se a pista está pesada demais) tentava
ler informação de modelos 3D que vêm configurados como "não legíveis em
tempo real" (config padrão do Unity pra economizar memória em celular) —
e cada tentativa gerava um erro no log. Eram 1639 erros por corrida, todos
de uma vez, o que provavelmente causava um travadinha bem no meio da
corrida. Corrigido: agora ele pula esses modelos educadamente em vez de
tentar e falhar. Isso não muda nada que você vê ou sente diretamente, só
limpa o log e tira esse pico de erro.

**3) Descoberta importante (ainda não corrigida, preciso da sua decisão):
a Carrera Kart está rodando na metade do FPS da Oval.** O mesmo log trouxe
um número que me chamou atenção: durante toda a corrida na Carrera Kart, o
jogo ficou travado exatamente em 30 quadros por segundo (o MÍNIMO aceitável
pela meta do projeto, não os 60 desejados) — enquanto na Oval, o mesmo
celular chegou a 109-120 quadros por segundo tranquilamente. A diferença:
a Carrera Kart hoje usa 3794 objetos sendo desenhados na tela ao mesmo
tempo, contra apenas 725 na Oval — quase 5x mais. O próprio sistema de
diagnóstico do projeto (criado lá no início, meta "até 100 objetos") já
aponta isso como muito acima do esperado.

Não mexi nisso ainda porque é um projeto de otimização de verdade (juntar
vários pedacinhos da pista em menos objetos, por exemplo), não um ajuste
pequeno — prefiro te perguntar antes de investir tempo nisso. E é uma
pista honesta, não uma certeza: um FPS mais baixo pode SIM contribuir pra
bots parecerem mais "nervosos"/imprecisos (menos atualizações visuais por
segundo = movimento menos suave), mas eu não tenho prova de que seja a
causa completa do "kart doido" que você descreveu antes — só sei que a
Carrera Kart está tecnicamente mais pesada e mais lenta que a Oval, o que
já vale a pena investigar por si só (a meta do projeto é 60 FPS, e ela tá
no mínimo aceitável o tempo todo).

**Pergunta pra você:** quer que eu entre na otimização da Carrera Kart
como próximo passo grande? Se quiser, no próximo teste me diga também se
o comportamento "louco" dos bots parece pior especificamente na Carrera
Kart (mais pesada) do que na Oval (mais leve) — isso ajuda a confirmar ou
descartar essa pista.

Nada foi commitado. Mesmo comando de sempre: `bash scripts/build_deploy_verify.sh`.

## Rodada 40 (2026-08-26) — volante/rodas, fumaça, otimização da Carrera Kart, IA dos bots e descoberta sobre a "escola de pilotagem"

Você pediu, numa única mensagem, para seguir com a otimização (autorizada), corrigir o
giro do volante/rodas, colocar fumaça no kart novo, investigar a IA dos bots a fundo, e
avaliar um modo "escola de pilotagem" nas duas pistas — e disse para eu trabalhar sozinho
enquanto descansa. Aqui está o que foi IMPLEMENTADO (aguardando seu teste) e o que ficou
só PLANEJADO/como pergunta para você.

### 1) Volante e rodas — IMPLEMENTADO (ajuste, não bug novo)

Conferi o código linha a linha: o giro do volante e das rodas dianteiras conforme o
ângulo de direção já existe desde a rodada 27, e já funciona independente de velocidade
(gira mesmo parado). O ângulo real de esterçamento do kart é 24-28 graus dependendo da
categoria — realista para um kart de verdade, mas sutil demais pra ler bem numa tela
pequena com câmera de perseguição. É provável que essa sutileza seja o "leve movimento
pra lado e pra outro" que você descreveu, mais do que um bug.

Ajustei: agora a RODA (só a roda, não a física do kart) vira um pouco mais do que o
ângulo real — até 38 graus visualmente na batida máxima — pra ficar mais claro que ela
girou. A física de condução (como o kart realmente se comporta) não mudou nada, só a
aparência da roda. Isso reverte uma decisão antiga (rodada 27, sincronizar visual com a
física exatamente) — se preferir o realismo exato de volta, é só falar que eu desfaço.

### 2) Fumaça do escapamento (kart novo) — IMPLEMENTADO, com uma ressalva honesta

O modelo do kart novo já tinha pedaços de "fumaça" prontos (`smoke_puff`), mas nada no
jogo os animava — ficavam parados, por isso não davam a sensação de fumaça de verdade.
Escondi essas peças estáticas e criei um efeito de bolhas cinzas que nascem, crescem e
encolhem perto do escapamento, mais frequentes quanto mais você acelera.

Ressalva: não usei o sistema de partícula "de verdade" do Unity porque ele depende de um
tipo de material que, nesse projeto, já causou problema antes em builds de Android (a
cor sai rosa/errada quando o material certo não está disponível no celular) — e eu não
tenho como abrir o Unity aqui pra conferir visualmente antes de mandar pra você. Optei
pelo caminho mais seguro (bolinhas sólidas cinza que crescem e encolhem, usando a mesma
técnica de material que já funciona em todo o resto do jogo) em vez de arriscar fumaça
invisível ou rosa no seu celular. Não é tão bonito quanto uma nuvem de partícula de
verdade, mas garante que você vai ver alguma coisa saindo do escapamento.

### 3) Otimização da Carrera Kart — IMPLEMENTADO, causa raiz real encontrada

Achei a causa técnica dos 3794 objetos desenhados por vez (vs. 725 na Oval, achado da
rodada passada). O código já marcava cada pedacinho da pista (asfalto, barreira, meio-fio,
grama) como "estático" com um comentário dizendo "isso é pra agrupar em menos objetos
desenhados" — só que esse agrupamento automático do Unity só funciona pra cenas prontas
no momento em que o jogo é compilado, e a pista deste jogo é toda construída em tempo
real, quando você aperta pra jogar. Ou seja: a marcação existia, mas o agrupamento nunca
rodava de verdade — cada pedacinho sempre foi desenhado separado, e a Carrera Kart, por
ter uma pista bem mais complexa e realista (16 curvas, largura variável), tem muito mais
pedacinhos que a Oval.

Corrigido: agora, assim que a pista termina de ser montada, rodo o comando certo do
Unity pra juntar de verdade esses pedacinhos estáticos em bem menos objetos desenhados —
sem mudar layout, física ou visual de nada, só a forma como a placa de vídeo desenha.
Vale pras duas pistas (a Oval também deve melhorar, só que já estava bem melhor antes).

Ressalva honesta: pedacinhos de decoração importados (cerca, pneus da Kenney) podem não
entrar 100% nesse agrupamento, dependendo de uma configuração de importação — então é
bem provável que o número caia bastante, mas talvez não bata exatamente na meta de 100
numa passada só. Só seu teste real no celular (FPS + o próprio log de diagnóstico) vai
confirmar o tamanho do ganho.

### 4) IA dos bots — IMPLEMENTADO um fechamento de lacuna real, documentada desde a rodada 23

Reli a IA dos bots inteira (quase 600 linhas) — não é um sistema simples: já tem "olhar
à frente" pra curvar suave, freada calculada pela física real da curva, erro de precisão
por dificuldade, ultrapassagem/defesa contra outros karts, detecção de "travado" com
recuperação escalonada. Achei uma lacuna real, documentada desde a rodada 23 e nunca
fechada: "o bot bateu na traseira do meu carro" tinha uma causa suspeita anotada no
próprio código — os bots nunca tinham noção de um kart parado ou lento bem na frente
deles, só reagiam à pista. Corrigi isso: agora, se tem um kart na mesma "pista" (lado a
lado, não é regra pra ultrapassagem lateral) logo à frente, o bot passa a frear/soltar o
acelerador pra não bater nele — o mesmo princípio do "piloto automático adaptativo" de
carros reais.

Sobre "os bots precisam ser mais espertos e o difícil precisa ser desafiador": conferi os
números do nível Difícil — ele já é configurado pra ser o mais agressivo possível dentro
desse sistema (erro de curva zero, acelerador no talo igual ao seu, mais perto do limite
de aderência, mais agressivo em duelo). Não mudei esses números às cegas porque já foram
ajustados numa dúzia de rodadas anteriores com base no seu feedback. Preciso que você
teste de novo e me diga especificamente ONDE o Difícil ainda parece fraco — eles ficam
pra trás nas retas? nas curvas? não defendem posição? — pra eu ajustar o número certo em
vez de adivinhar de novo (foi exatamente esse "advinhar" que causou idas e vindas nas
rodadas 20-23).

### 5) Escola de pilotagem — DESCOBERTA importante, decisão sua antes de eu programar

Antes de inventar um design do zero, fui conferir o plano formal do projeto
(`tasks.md`) — e essa ideia JÁ está lá, detalhada, como Milestone 7 ("Escola, Instrutor
e Progressão"): 10 módulos com desbloqueio progressivo, linha ideal que vai
desaparecendo conforme você aprende, um "instrutor" que dá dicas por texto
("freie em linha reta", "você freiou tarde"...), feedback por setor comparando com uma
volta de referência, uma prova de licença no final. É bem mais completo do que eu
imaginaria sozinho.

O problema: essa Milestone 7 formalmente DEPENDE da Milestone 4 ("Cronometragem
Completa, Setores, Ghost e Bots") — que ainda não foi construída. O que existe hoje
(cronometragem, fantasma, bots) são só protótipos simples, do jeito que o próprio código
já admite ("isto é um protótipo... não é a Milestone 4 de verdade"). A Milestone 4 tem
15 tarefas formais (calculadora de delta por setor, volta ideal teórica, gravador de
fantasma de verdade, sistema de bots definitivo, validador de limites de pista, testes
formais) — nada disso existe ainda de verdade.

Ou seja: pra fazer a escola de pilotagem do jeito que o PRÓPRIO projeto já planejou
(com feedback de setor de verdade, não uma gambiarra), primeiro precisaria da Milestone
4. Isso é bastante trabalho — não é uma tarde, é muitas rodadas.

Por isso NÃO comecei a programar isso ainda — seguindo a regra de não pular
tarefas/milestones, e porque essa é uma decisão de estratégia do projeto inteiro, não um
ajuste pequeno. Duas opções pra você escolher quando voltar:

- **Opção A — pelo livro:** eu começo a Milestone 4 de verdade (cronometragem, setores,
  fantasma, bots definitivos), rodada por rodada, e só depois a escola de pilotagem
  nasce em cima disso, com todo o feedback preciso que o plano descreve.
- **Opção B — protótipo rápido primeiro:** eu construo uma versão bem mais simples
  agora (linha ideal fixa na pista + um ponto de frenagem marcado, sem instrutor, sem
  licença, sem progressão salva) só pra você sentir a ideia nas duas pistas, deixando
  claro que isso NÃO é a Milestone 7 de verdade e vai precisar ser refeito depois.

Nenhuma das duas eu deveria simplesmente escolher sozinho — é uma decisão sobre a ordem
de todo o resto do projeto daqui pra frente.

### Resumo do que testar

Nada foi commitado. Rode `bash scripts/build_deploy_verify.sh` de novo e me diga:
1. O volante/rodas agora dão mais a sensação de "virou de verdade"?
2. A fumacinha aparece (mesmo que como bolinhas, não uma nuvem)?
3. A Carrera Kart melhorou de FPS? (pode rodar `bash scripts/capture_logcat.sh` depois de
   jogar pra eu conferir os números novos de `[RKW-PERF]`/`[RKW.Telemetry]`.)
4. Os bots pararam de bater na traseira de kart parado/lento à frente?
5. No Difícil, onde especificamente os bots ainda parecem fracos (reta, curva, duelo)?
6. Escola de pilotagem: opção A (pelo livro, mais lento) ou B (protótipo rápido, precisa
   refazer depois)?


## Pendências conhecidas (não fechadas por este documento)

- **M3-T01 — PlayMode test** — ainda não escrito; precisa de um pequeno
  gancho de teste no `KartPhysicsPrototypeBootstrap` (pular o
  `RaceSetupMenu` programaticamente) e, diferente do resto desta sessão,
  só dá pra confirmar que funciona rodando de verdade no Editor — prefiro
  validar esse gancho com você antes de escrever o teste em cima dele. Ver
  rodada 2026-08-23 acima.
- **Bots do difícil — pausado a pedido do fundador (2026-08-24)** — no
  difícil, todos os bots ainda erram a largada/primeira curva mesmo depois
  do revert da rodada 23. Fundador decidiu parar de investir tempo nisso
  por agora ("acho que estamos perdendo tempo com esse bot") e priorizar o
  fantasma — ver rodada 2026-08-24 acima. Causa provável continua sendo a
  falta de qualquer lógica de frear/desviar de um obstáculo à frente (bots
  miram no próximo ponto do traçado e aceleram, não importa o que esteja
  no caminho), não a disputa de posição. Fica pendente para quando o
  fundador quiser retomar. **Atualização (mesmo dia, rodada seguinte):** o
  "bot fantasma" (índice 0, só no difícil, seguindo a gravação do
  fantasma) foi tentado, testado pelo fundador e removido — "ficou
  perdidão", provavelmente porque a gravação do fantasma é densa demais
  (10 Hz) pra lógica de waypoint dos bots, pensada pra um caminho bem mais
  esparso. Ver "Rodada — 2026-08-24 (continuação: fantasma vira a corrida
  inteira...)" acima. O problema original (8-9 bots errando
  largada/primeira curva no difícil) continua pausado e sem tentativa
  nova. **Observação adicional do fundador (mesma rodada de teste):** com
  1 bot o comportamento fica bom; com 9 bots fica estranho, os outros se
  perdem. Consistente com a causa já suspeitada (bots sem lógica de
  frear/desviar de obstáculo à frente — mais bots = mais chance de um
  atravessar o caminho de outro); não é um problema novo, é mais evidência
  do mesmo problema pausado. **Atualização (rodada 25, mesmo dia):** um
  mecanismo novo de recuperação (bot "perdido" ou de costas pro sentido
  da pista se resnapeia pro caminho) foi adicionado — resolve bot preso
  longe do traçado, mas NÃO é o M4-T08 de verdade (falta de lógica pra
  desviar de obstáculo/disputar posição continua igual). Ver rodada 25
  acima.
- **Erro vermelho no console — resolvido (rodada 25, 2026-08-24).** Não
  era um bug do app: é o console de desenvolvimento embutido do Unity,
  ligado por `BuildOptions.Development` no `BuildHelper.cs`. Trocado por
  `BuildOptions.None`; confirmado via `rkw_logcat.txt` que o build antigo
  tinha "Build type 'Development'". Pendente só de confirmação visual sua
  no próximo build.
- **Pista maior — resolvida (rodada 25, 2026-08-24).** Retas esticadas de
  38m para 60m, raio das curvas mantido fixo em 14m (curvas idênticas,
  só afastadas — gap-safe por construção). Toda a camada de dados
  "escondida" (`OvalMvpTrackConfiguration.asset`) recalculada ponto a
  ponto. Ver rodada 25 acima para o racional completo. Pendente de
  confirmação dirigindo.
- **Fantasma (M4, versão rápida) — não confirmado (2026-08-24)** — grava e
  reproduz a melhor CORRIDA (não mais uma volta só) do próprio jogador,
  sem física/colisão, implementado e no dispositivo. Fundador confirmou
  que a versão visual "está normal e muito bom"; passou por duas
  iterações de correção no mesmo dia (largada parada/em movimento, depois
  corrida inteira em vez de volta única reiniciando) — ver as 3 rodadas de
  2026-08-24 acima para o histórico completo. O "bot fantasma" que
  acompanhou uma dessas iterações foi tentado e removido (ver item acima
  sobre bots do difícil). Pendente de confirmação dirigindo: que o
  fantasma agora acompanha a corrida inteira sem "relargar" no meio, e que
  dá pra saber pelo tempo final se você bateu ele ou não. Sistema formal
  completo do M4
  (setores, delta, volta ideal, bots com 5 perfis) continua em aberto,
  fora do escopo desta versão.
- **Rodada 2026-08-23 (câmera "visão do piloto")** — confirmada funcionando
  pelo fundador na rodada 2026-08-24. A posição do "olho do piloto" dentro
  do kart é uma estimativa, a ajustar quando tivermos o modelo 3D real.
- **Rodada "bots foram para o lado / pista desformatada / IA um desastre"**
  — as 3 correções de código (largada dos bots, silhueta das curvas,
  direção pure-pursuit) estão no dispositivo mas ainda não confirmadas
  dirigindo de verdade. A recomendação sobre o kit de pista do Kenney
  (usar como pele visual em vez de substituir a geometria procedural) está
  registrada acima, sem decisão tomada — aguardando você.
- **M3-T01** — traçado com curvas de verdade (estádio, raio 14m)
  implementado nesta rodada, ainda não confirmado pelo fundador no
  dispositivo — ver rodada acima para os pontos de atenção. Faltam:
  pontas com raios assimétricos / esses / chicanes, elevação/subida-descida
  (Fase 2 original, ainda mais adiada), o traçado completo ~1km/3 setores
  do texto original da task, grama restrita às áreas de escape das curvas
  (ver item abaixo), iluminação baked, evidência formal (screenshot +
  profiler) e o PlayMode test. Ver nota atualizada na própria task em
  `tasks.md`.
- Grama restrita às áreas de escape das curvas (zebra balizando, em vez de
  um único plano grande): deliberadamente NÃO tentado ainda — histórico de
  bugs de costura/z-fighting entre colliders de superfície (rodadas 1-4)
  torna essa mudança arriscada o suficiente para merecer sua própria
  rodada isolada, depois que o traçado com curvas de verdade estiver
  confirmado estável.
- Telão na largada mostrando o tempo da última volta.
- Bloqueio/ultrapassagem: primeira versão implementada (rodada 18), ainda
  não confirmada pelo fundador; física real de colisão kart-kart continua
  não modelada, e o milestone formal M4 (personalidade, risco, aprendizado)
  segue em aberto. Com a maior parte da volta agora sendo "curva" (16 dos
  18 trechos), a disputa de posição deve ficar concentrada nas 2 retas —
  mudança de comportamento esperada, não um bug, mas só confirma dirigindo.
- Tela final de classificação (`RaceManager`) ainda não mostra os números
  de corrida dos karts (só o painel ao vivo mostra) — não decidido se fica
  dentro ou fora de escopo.
- **Flag sozinho/com bots e comparação de voltas — não confirmadas
  (rodada 25, 2026-08-24).** Menu de configuração ganhou um seletor
  SOZINHO/COM BOTS explícito; tela final ganhou uma tabela volta a volta
  "você x bot" (contra o bot que chegou mais longe, quando há vários).
  Ambas implementadas e no dispositivo, nenhuma confirmada dirigindo. Ver
  rodada 25 acima.
- **Kart real (rodada 26, 2026-08-24) — orçamento de triângulos estourado
  de propósito, otimização pendente.** O modelo novo (~37K triângulos,
  149 peças, 11 materiais) está ligado em todos os karts a pedido do
  fundador, ciente do custo — até 10 karts numa corrida passam bem do
  orçamento de ~100K triângulos que M3-T01 documenta pra pista inteira em
  mobile. Fica pendente: medir performance de verdade no dispositivo
  (M3-T08 já cobre isso formalmente) e, se necessário, uma rodada de
  decimação/LOD/versão simplificada pros bots — nenhuma tentada ainda,
  sem ferramenta de simplificação de malha disponível neste ambiente. Ver
  rodada 26 acima para o racional completo.
- **Rodada 27 (2026-08-24) — câmera/escala, esterçamento ativo, volante/pedais novos, numeração — implementados, aguardando confirmação dirigindo.** Esterçamento das rodas dianteiras sincronizado com a física real; volante/pedais novos (modelados por você) substituem os simples do kart original no cockpit E nos botões de UI; numeração visível na plaqueta "number_plate" de cada kart. Posição de tudo calculada a partir do contorno real do modelo (nunca chutada), mas a ROTAÇÃO do volante/pedaleira novos foi assumida igual à do kart (não verificada visualmente — sem Editor do Unity neste ambiente). Botão de câmera do piloto: corrigido pra respeitar a área segura da tela, mas é uma correção de melhor evidência, não uma causa confirmada. Física de derrapagem, leaderboard por modo e a Pista 2 (circuito técnico) do mesmo pedido do fundador ainda NÃO foram iniciados. Ver rodada 27 acima para o detalhamento completo.
- **Rodada 28 (2026-08-24) — teto de aderência lateral ajustado (1.2G → 1.5G), ângulo de esterçamento mantido em 24°.** Você reportou direção "travada" e sugeriu quase 90° de ângulo; investigação mostrou que o ângulo (24°) já está numa faixa realista pra um kart sem diferencial, e o que realmente saturava era o teto de aderência dos pneus, atingido cedo demais dentro do curso do manche em velocidades normais (~24% do curso a 40 km/h, ~10% a 60 km/h). Aumentei `lateralGripG` da categoria Rental Sport pra dar mais faixa útil de manche, mas é melhoria parcial — em alta velocidade algum "achatamento" é fisicamente esperado. Não verificado dirigindo. Ver rodada 28 acima.
- **Rodada 29 (2026-08-24) — câmera do cockpit inclinada (ficava só na pista), pedaleira nova animada, ícones/volante maiores, verde/vermelho removidos.** Câmera do piloto ganhou inclinação de 18° pra baixo e FOV mais largo, calculado a partir da posição real do volante no modelo. Pedaleira da rodada 27 trocada por um modelo novo do fundador com dobradiças de verdade, girando conforme o toque no freio/acelerador. Ícones da UI e volante do cockpit aumentados por pedido do fundador; cor vermelho/verde removida dos pedais (3D e UI), substituída por cinza neutro. Nada verificado visualmente (sem Editor). Piloto sentado no kart, cena de pódio, telão de pista e redesenho da tela de entrada foram recebidos/pedidos mas ainda NÃO iniciados — cada um vai precisar de rodada própria. Ver rodada 29 acima.
- Nenhuma caixa de `tasks.md` foi marcada a partir das rodadas de física
  cobertas nas seções anteriores deste log — os itens cobertos ali
  (M2-T07, T09, T10, T12, T16, T18, T19) já estavam `[x]` antes destas
  rodadas; o trabalho descrito era polimento pós-implementação, não a
  implementação original dessas tarefas. M2-T20/T21 e o progresso parcial
  de M3-T01 (rodadas acima) são exceções — essas sim mudaram checkboxes.
