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
