# 29 — Protótipo de física do kart (M2-T01)

## Estado

**M2-T01 concluída em 2026-08-18, após validação humana no Galaxy S25.** Esta execução não iniciou M2-T02 ou tarefas posteriores e preservou M1-T07, M1-T12, M1-T13 e M1-T14 como pendentes.

## Escopo implementado

- `RKW.Physics` criado somente porque agora possui consumidores runtime e de teste.
- `KartDynamics` combina Rigidbody/PhysX com forças custom em `FixedUpdate` de 50 Hz.
- Aceleração, coasting, drag, freio básico, ré técnica limitada e limite de velocidade.
- Direção com resposta de yaw, perda de velocidade por esterço e aderência lateral limitada.
- Grip permanece pleno até o ângulo de pico e cai progressivamente após o pico; a recuperação também é progressiva.
- Transferência lateral de peso, lift-off da roda traseira interna e influência básica do eixo rígido.
- Cena `KartPhysicsPrototype` isolada, com piso, limites, obstáculos simples, uma câmera URP de acompanhamento e HUD técnico.
- Controles temporários de teclado e touch explícitos. O joystick virtual e os pedais definitivos continuam reservados para M2-T14.

A cena técnica não pertence ao `EditorBuildSettings` normal. Bootstrap e MainMenu permanecem as únicas cenas do fluxo regular.

## Hipóteses iniciais de calibração

| Parâmetro | Valor inicial |
|---|---:|
| Massa | 165 kg |
| Centro de massa | 0,22 m |
| Entre-eixos / bitola traseira | 1,05 m / 1,05 m |
| Velocidade máxima | 55 km/h |
| 0 → máxima | 8 s |
| Desaceleração de freio | 10 m/s² |
| Velocidade máxima de ré | 12 km/h |
| Desaceleração em coasting | 1,6 m/s² |
| Aderência lateral | 1,0 g |
| Slip de pico / perda plena | 8° / 28° |
| Grip mínimo após perda | 0,32 |
| Ângulo máximo de esterço | 28° |
| Yaw máximo | 105°/s |
| Limiar de lift-off interno | 0,62 |
| Rampa de acelerador | 0,20 s |

Esses números são hipóteses ajustáveis no `PrototypeSchoolTuning.asset`, não valores certificados nem calibração final de categoria. A criação das categorias Escola e Rental Sport completas continua em M2-T12.

## Controles técnicos

- Editor: `W`/seta para cima acelera; `A`/seta esquerda e `D`/seta direita esterçam; `S`, seta para baixo ou espaço freiam e, após parar, acionam a ré.
- Android: botões explícitos `ESQUERDA`, `DIREITA`, `ACELERAR` e `FREAR / RÉ` respeitam a safe area.
- O HUD mostra velocidade inteira, indicação de ré e razão de grip para calibração.

## Validação no Galaxy S25

O fundador dirigiu o protótipo físico, atingiu 32 km/h, realizou curvas, desviou dos obstáculos e confirmou o funcionamento do freio e da ré até 12 km/h. A direção para ambos os lados foi considerada adequada para esta primeira versão. O comportamento ainda será aperfeiçoado, mas o objetivo inicial da M2-T01 foi considerado completo.

Uma captura sanitizada da validação foi mantida fora do Git em `/tmp/rkw-m2-t01-controls-screen.png`. O APK Development IL2CPP/ARM64, logs e artefatos de build também permaneceram fora do repositório.

## Evidências técnicas de fechamento

- EditMode direcionado da física: 8/8 testes aprovados.
- EditMode completo: 67/67 testes aprovados.
- PlayMode direcionado do protótipo: 1/1 teste aprovado, incluindo estabilidade, câmera única, direção e transição de freio para ré.
- Android: APK Development IL2CPP/ARM64 gerado, instalado e validado no Galaxy S25.
- iOS: exportação Unity e compile check Xcode sem assinatura concluídos com sucesso.
- Compilação C#: sem erros ou warnings novos atribuíveis à M2-T01.

A execução PlayMode completa encontrou 18 testes aprovados, 7 ignorados por dependerem de flags explícitas de integração/captura e 1 falha preexistente fora da física: `MissingUiKey_ReturnsSafeMessageAndWarnsOnlyOnce` recebeu o warning sanitizado do Remote Config durante o Bootstrap. O teste direcionado da M2-T01 permaneceu verde; nenhuma alteração em Remote Config ou em testes de tarefas anteriores foi feita neste escopo.

## Limites preservados

- Sem música ou áudio definitivo.
- Sem frenagem traseira detalhada, bloqueio ou sobre-esterço de M2-T04.
- Sem superfícies de M2-T07, perda por colisão de M2-T09, recovery de M2-T10 ou slipstream de M2-T16.
- Sem joystick definitivo de M2-T14.
- Sem pista greybox completa, checkpoints, voltas ou cronometragem de M2-T18/M2-T19.
- Sem Photon, UGS, Remote Config, bots, corrida ou gameplay multiplayer.
