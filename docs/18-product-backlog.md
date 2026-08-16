# 18 — Product Backlog

## Objetivo e Escopo

Épicos e histórias priorizadas com critérios de aceite, dependências, testes esperados, telemetria e estimativas. Nenhuma história XL chega ao Codex sem ser quebrada.

---

## Épicos

| Épico | Descrição | Milestone |
|---|---|---|
| E-01 | Setup de Projeto e Infra | M1 |
| E-02 | Física e Dirigibilidade | M2–M3 |
| E-03 | Pista e Visual | M3 |
| E-04 | Modos de Jogo Offline | M3–M4 |
| E-05 | Bots e IA | M4 |
| E-06 | Multiplayer | M5 |
| E-07 | Escola de Pilotagem | M6 |
| E-08 | Progressão e Economia | M7 |
| E-09 | Garagem e Cosméticos | M7 |
| E-10 | Backend e Cloud | M5–M7 |
| E-11 | Monetização | M9 |
| E-12 | Telemetria e Analytics | M3–M9 |
| E-13 | Release e CI/CD | M1, M9 |

---

## Histórias

### E-01 — Setup de Projeto

#### US-001: Inicializar Projeto Unity

| Campo | Valor |
|---|---|
| **ID** | US-001 |
| **Título** | Inicializar Projeto Unity 6.3 LTS (`6000.3.22f1`) |
| **User Story** | Como desenvolvedor, eu quero um projeto Unity 6.3 LTS (`6000.3.22f1`) configurado com URP, Input System, Cinemachine e Addressables, para que eu tenha a base técnica para desenvolvimento. |
| **Prioridade** | Must |
| **Dependências** | ADR-0001, ADR-0004 |
| **Estimativa** | S |
| **Milestone** | M1 |

**Critérios de Aceite:**

- Given projeto Unity criado, When abro no Unity Editor, Then URP está configurado como render pipeline ativo.
- Given projeto Unity criado, When verifico Package Manager, Then Input System, Cinemachine e Addressables estão instalados.
- Given projeto Unity criado, When faço build Android e iOS, Then builds compilam sem erros.

**Testes Esperados:** Build Android + iOS sem erros; packages resolvidos.

**Telemetria:** N/A (setup).

---

#### US-002: Configurar CI/CD com Unity Build Automation

| Campo | Valor |
|---|---|
| **ID** | US-002 |
| **Título** | Configurar Unity Build Automation |
| **User Story** | Como fundador, eu quero builds automatizados para Android e iOS ao fazer push, para que eu não precise fazer builds manualmente. |
| **Prioridade** | Must |
| **Dependências** | US-001, ADR-0005 |
| **Estimativa** | M |
| **Milestone** | M1 |

**Critérios de Aceite:**

- Given push na branch release, When Unity Build Automation executa, Then AAB para Android é gerado sem erros.
- Given push na branch release, When Unity Build Automation executa, Then IPA para iOS é gerado sem erros.
- Given teste EditMode falha, When build é executado, Then build é bloqueado e notificação é enviada.

**Testes Esperados:** Push de teste → build → artefatos gerados.

**Telemetria:** Build success/failure rate.

---

#### US-003: Configurar Git LFS e .gitignore

| Campo | Valor |
|---|---|
| **ID** | US-003 |
| **Título** | Configurar Git LFS para assets |
| **User Story** | Como desenvolvedor, eu quero Git LFS configurado para assets binários, para que o repositório não cresça descontroladamente. |
| **Prioridade** | Must |
| **Dependências** | Nenhuma |
| **Estimativa** | XS |
| **Milestone** | M1 |

**Critérios de Aceite:**

- Given .gitattributes configurado, When push arquivo .fbx/.png/.wav, Then arquivo é armazenado via LFS.
- Given .gitignore configurado, When verifico repo, Then pastas Library/, Temp/ e Builds/ estão excluídas.

**Testes Esperados:** Push de asset binário; verificar LFS tracking.

**Telemetria:** N/A.

---

### E-02 — Física e Dirigibilidade

#### US-010: Criar modelo de física base do Kart

| Campo | Valor |
|---|---|
| **ID** | US-010 |
| **Título** | Modelo de física base (aceleração, frenagem, esterço) |
| **User Story** | Como Piloto, eu quero que o kart acelere, freie e esterçe com sensação de eixo traseiro rígido, para que a dirigibilidade seja autêntica. |
| **Prioridade** | Must |
| **Dependências** | US-001 |
| **Estimativa** | L |
| **Milestone** | M2 |

**Critérios de Aceite:**

- Given kart parado, When aplico acelerador progressivamente, Then kart atinge velocidade máxima conforme curva da categoria.
- Given kart em velocidade, When aplico freio em linha reta, Then kart desacelera com predominância traseira e para em distância conforme ScriptableObject.
- Given kart em curva, When aplico esterço excessivo, Then kart perde velocidade proporcionalmente (amarra).
- Given kart em curva com velocidade, When alivia peso traseira interna, Then kart curva eficientemente.

**Testes Esperados:** EditMode — cálculos de aderência; PlayMode — kart completa circuito dentro de tempo esperado.

**Telemetria:** `lap_completed`, `sector_time`.

---

#### US-011: ScriptableObjects de Categorias

| Campo | Valor |
|---|---|
| **ID** | US-011 |
| **Título** | ScriptableObjects para Escola e Rental Sport |
| **User Story** | Como desenvolvedor, eu quero parâmetros de física por categoria em ScriptableObjects, para que ajustes não exijam mudança de código. |
| **Prioridade** | Must |
| **Dependências** | US-010 |
| **Estimativa** | S |
| **Milestone** | M2 |

**Critérios de Aceite:**

- Given ScriptableObject "Escola" criado, When atribuído ao kart, Then aceleração, vel. máxima e aderência refletem categoria 6,5 HP.
- Given ScriptableObject "Rental Sport" criado, When atribuído ao kart, Then parâmetros refletem categoria 13 HP.
- Given mudança de valor no ScriptableObject, When executo corrida, Then comportamento reflete o novo valor sem rebuild.

**Testes Esperados:** Unit tests comparando parâmetros carregados vs esperados.

**Telemetria:** N/A.

---

#### US-012: Sistema de Vácuo (Slipstream)

| Campo | Valor |
|---|---|
| **ID** | US-012 |
| **Título** | Implementar efeito de vácuo |
| **User Story** | Como Piloto, eu quero ganhar velocidade ao seguir outro kart de perto, para que o vácuo funcione como na vida real. |
| **Prioridade** | Must |
| **Dependências** | US-010 |
| **Estimativa** | M |
| **Milestone** | M3 |

**Critérios de Aceite:**

- Given kart atrás de outro a ≤ 1,5 comprimentos por ≥ 1 s, When alinhado a ±15°, Then arrasto reduz progressivamente até 8%.
- Given kart sai do cone de vácuo, When distância > 1,5 comprimentos, Then arrasto retorna ao normal em 2 s.
- Given efeito ativo, When visualmente observado, Then partículas/distorção sutis são exibidas.

**Testes Esperados:** Property test: `arrasto(dist_menor) <= arrasto(dist_maior)`.

**Telemetria:** `slipstream_used`.

---

#### US-013: Superfícies e Zebras

| Campo | Valor |
|---|---|
| **ID** | US-013 |
| **Título** | Efeito de superfícies (grama, zebra, sujeira) |
| **User Story** | Como Piloto, eu quero feedback claro quando o kart sai do asfalto, para que eu entenda os limites da pista. |
| **Prioridade** | Must |
| **Dependências** | US-010 |
| **Estimativa** | M |
| **Milestone** | M3 |

**Critérios de Aceite:**

- Given kart entra em grama, When detecta collider de grama, Then aderência reduz em ≥ 40%.
- Given kart sobre zebra, When velocidade > threshold, Then kart desestabiliza proporcionalmente.
- Given kart sobre sujeira, When detecta collider, Then aderência e aceleração reduzem.

**Testes Esperados:** PlayMode test de transição de superfície; EditMode de coeficientes.

**Telemetria:** `collision` com propriedade surface_type.

---

### E-04 — Modos Offline

#### US-020: Tomada de Tempo com Ghost

| Campo | Valor |
|---|---|
| **ID** | US-020 |
| **Título** | Modo Tomada de Tempo (3 voltas + ghost) |
| **User Story** | Como Piloto, eu quero correr 3 voltas e ver meu ghost para melhorar meu tempo. |
| **Prioridade** | Must |
| **Dependências** | US-010, US-030 (pista) |
| **Estimativa** | M |
| **Milestone** | M3 |

**Critérios de Aceite:**

- Given Piloto inicia tomada de tempo, When completa 3 voltas, Then sessão encerra e exibe melhor tempo.
- Given melhor tempo anterior existe, When nova sessão inicia, Then ghost da melhor volta é visível.
- Given Piloto bate seu melhor tempo, When sessão encerra, Then ghost é atualizado.

**Testes Esperados:** PlayMode — contagem de voltas; ghost serialization round-trip.

**Telemetria:** `lap_completed`, best_lap event.

---

#### US-021: Corrida Offline (10 voltas)

| Campo | Valor |
|---|---|
| **ID** | US-021 |
| **Título** | Corrida solo offline de 10 voltas |
| **User Story** | Como Piloto, eu quero correr 10 voltas contra o relógio antes de enfrentar bots, para que eu possa praticar. |
| **Prioridade** | Must |
| **Dependências** | US-010, US-030 |
| **Estimativa** | S |
| **Milestone** | M3 |

**Critérios de Aceite:**

- Given Piloto inicia corrida solo, When completa 10 voltas, Then resultado é exibido com melhor volta e tempo total.
- Given corrida em andamento, When bandeira quadriculada é exibida na volta 10, Then sessão encerra.

**Testes Esperados:** PlayMode — corrida completa solo com checkpoints.

**Telemetria:** `race_completed`.

---

### E-05 — Bots

#### US-040: Bot com Navegação por Waypoints

| Campo | Valor |
|---|---|
| **ID** | US-040 |
| **Título** | Bot navega a pista por waypoints |
| **User Story** | Como Piloto, eu quero bots que completem voltas de forma autônoma, para que corridas offline tenham adversários. |
| **Prioridade** | Must |
| **Dependências** | US-010, US-030 |
| **Estimativa** | M |
| **Milestone** | M4 |

**Critérios de Aceite:**

- Given bot instanciado com perfil "equilibrado", When corrida inicia, Then bot completa voltas sem sair da pista (> 95% do tempo).
- Given bot usando mesma física, When comparado a humano, Then tempos estão dentro de ±10% do benchmark da categoria.

**Testes Esperados:** PlayMode — bot completa 10 voltas; tempo dentro de range.

**Telemetria:** bot_lap_time, bot_off_track_events.

---

#### US-041: 5 Perfis de Bot

| Campo | Valor |
|---|---|
| **ID** | US-041 |
| **Título** | Implementar 5 perfis de dificuldade de bot |
| **User Story** | Como Piloto, eu quero bots com dificuldades variadas, para que a corrida seja desafiadora em todos os níveis. |
| **Prioridade** | Must |
| **Dependências** | US-040 |
| **Estimativa** | M |
| **Milestone** | M4 |

**Critérios de Aceite:**

- Given perfil "iniciante", When corre 10 voltas, Then média de tempo > perfil "rápido" em ≥ 15%.
- Given perfil "rápido", When corre, Then comete erros em < 5% das curvas.
- Given todos os perfis, When correm na mesma sessão, Then posição final ordena por perfil (rápido > equilibrado > iniciante).

**Testes Esperados:** PlayMode — 10 voltas por perfil; comparar tempos.

**Telemetria:** bot_profile, bot_lap_time, bot_errors.

---

### E-06 — Multiplayer

#### US-050: Sala Privada por Código

| Campo | Valor |
|---|---|
| **ID** | US-050 |
| **Título** | Criar e entrar em sala privada por código |
| **User Story** | Como Piloto, eu quero criar uma sala com código para jogar com meus amigos. |
| **Prioridade** | Must |
| **Dependências** | ADR-0002, US-010 |
| **Estimativa** | M |
| **Milestone** | M5 |

**Critérios de Aceite:**

- Given Piloto cria sala, When sala é criada, Then código de 6 caracteres é gerado e exibido.
- Given código compartilhado, When outro Piloto insere código correto, Then é adicionado à sala.
- Given sala com 2+ humanos, When host inicia, Then corrida começa para todos simultaneamente.
- Given código incorreto, When Piloto tenta entrar, Then erro amigável é exibido.

**Testes Esperados:** Integration test — criar + join + iniciar.

**Telemetria:** `private_room_created`, `private_room_joined`.

---

#### US-051: Sincronização de Estado a 30 Hz

| Campo | Valor |
|---|---|
| **ID** | US-051 |
| **Título** | Sincronização de posição/velocidade via Photon Fusion |
| **User Story** | Como Piloto online, eu quero ver outros karts se movendo suavemente, para que a experiência seja justa. |
| **Prioridade** | Must |
| **Dependências** | US-050 |
| **Estimativa** | L |
| **Milestone** | M5 |

**Critérios de Aceite:**

- Given 4 humanos conectados, When corrida em andamento, Then posições atualizam a 30 Hz sem teleporte visível.
- Given latência de 50 ms, When observando outro kart, Then interpolação suaviza movimento.
- Given latência > 150 ms, When corrida em andamento, Then corrida permanece jogável com correções visíveis mas sem crash.

**Testes Esperados:** Network test com latência simulada (30/50/150 ms).

**Telemetria:** `latency_sample`, `fps_sample`.

---

### E-07 — Escola

#### US-060: Módulo 1 — Equipamentos e Segurança

| Campo | Valor |
|---|---|
| **ID** | US-060 |
| **Título** | Escola Módulo 1: Equipamentos |
| **User Story** | Como iniciante, eu quero aprender sobre os equipamentos de kart de forma interativa. |
| **Prioridade** | Must |
| **Dependências** | US-030 (pista/cena) |
| **Estimativa** | S |
| **Milestone** | M6 |

**Critérios de Aceite:**

- Given Piloto inicia módulo 1, When briefing é apresentado, Then todos os itens de equipamento são mostrados interativamente.
- Given briefing completo, When Piloto confirma conclusão, Then módulo é marcado como concluído e módulo 2 desbloqueia.

**Testes Esperados:** PlayMode — conclusão de módulo persiste.

**Telemetria:** `tutorial_completed` (module_id: 1).

---

### E-10 — Backend

#### US-070: Autenticação e Perfil

| Campo | Valor |
|---|---|
| **ID** | US-070 |
| **Título** | Login + criação de perfil via UGS Auth |
| **User Story** | Como Piloto, eu quero fazer login e ter meu progresso salvo na nuvem. |
| **Prioridade** | Must |
| **Dependências** | ADR-0003 |
| **Estimativa** | M |
| **Milestone** | M5 |

**Critérios de Aceite:**

- Given primeiro acesso, When Piloto faz login via Google/Apple/Guest, Then Player ID é criado no UGS.
- Given login bem-sucedido, When Piloto escolhe display name, Then nome é persistido.
- Given perfil existente, When abre app em outro dispositivo com mesma conta, Then progressão é restaurada.

**Testes Esperados:** Integration test — flow de auth + profile creation.

**Telemetria:** `session_start`.

---

## Primeiras 10 Histórias Prontas para o Codex (Ordem)

| # | ID | Título | Milestone |
|---|---|---|---|
| 1 | US-003 | Configurar Git LFS e .gitignore | M1 |
| 2 | US-001 | Inicializar Projeto Unity 6.3 LTS (`6000.3.22f1`) | M1 |
| 3 | US-002 | Configurar Unity Build Automation | M1 |
| 4 | US-010 | Modelo de física base do Kart | M2 |
| 5 | US-011 | ScriptableObjects de Categorias | M2 |
| 6 | US-013 | Efeito de superfícies (grama, zebra, sujeira) | M3 |
| 7 | US-012 | Sistema de Vácuo (Slipstream) | M3 |
| 8 | US-020 | Tomada de Tempo com Ghost | M3 |
| 9 | US-021 | Corrida Offline 10 voltas | M3 |
| 10 | US-040 | Bot com Navegação por Waypoints | M4 |

---

## Notas

- Histórias adicionais para E-07 (Escola módulos 2–10), E-08 (Economy), E-09 (Garagem), E-11 (Monetização), E-12 (Telemetria) e E-13 (Release) serão detalhadas quando seus milestones se aproximarem.
- Nenhuma história deve ser estimada como XL. Se o escopo parecer XL, quebre antes de implementar.
- Cada história deve ser implementada em uma branch própria com PR.

---

## Links Relacionados

- [GDD](./02-game-design-document.md)
- [Roadmap](./17-roadmap.md)
- [Testes](./16-test-strategy.md)
- [AGENTS.md](../AGENTS.md)
