# ADR-0006: Fonte de Regras Esportivas

## Status

**Aceito com adaptações**

## Contexto

O jogo precisa de um regulamento esportivo para bandeiras, penalidades, ultrapassagens, conduta e direção de prova. Kartódromos brasileiros seguem regulamentos locais variados, derivados da CBA/FIA mas com adaptações. Não existe um regulamento universal de kart rental.

## Alternativas Consideradas

| Fonte | Prós | Contras |
|---|---|---|
| **CBA (Confederação Brasileira de Automobilismo)** | Oficial no Brasil; reconhecida | Focada em competição federada; excessiva para rental; custo de licença |
| FIA Karting Regulations | Padrão global; completo | Focada em competição profissional; complexa para game casual |
| Regulamento de kartódromo específico | Autêntico para público BR | Varia por pista; não padronizável |
| **Regulamento custom do jogo (inspirado em CBA/FIA)** | Flexível; parametrizável; adaptado ao digital | Não é "oficial"; requer design próprio |
| Nenhum regulamento formal | Simples | Sem fair play; jogadores criam regras ad-hoc |

## Decisão

**Regulamento custom do jogo**, inspirado nos princípios da CBA e FIA Karting, adaptado para:
- Detecção automática (sem fiscal humano real);
- Penalidades proporcionais ao contexto digital;
- Parametrização via Remote Config;
- Experiência justa e divertida (não punitiva ao extremo).

## Princípios Adotados

| Princípio | Origem | Adaptação |
|---|---|---|
| Bandeiras (verde, amarela, azul, vermelha, preta, branca, quadriculada) | CBA/FIA | Implementação simplificada; amarela local por setor |
| Proibição de ultrapassagem sob amarela | CBA/FIA | Detecção automática por posição no setor |
| Penalidade por corte de pista | CBA/FIA | Automática com threshold de ganho de tempo |
| Contato evitável | CBA/FIA | Semi-automática; colisões ambíguas não penalizadas |
| Bandeira azul (facilitação) | FIA | Informativa no MVP; obrigatória se ignorar 3x |
| Direção contrária = bandeira preta | CBA | Automática; sem apelação |
| Queima de largada | CBA/FIA | Detecção por aceleração antes de lights-out |
| Abandono | Regras de kartódromo | Cooldown de matchmaking progressivo |

## Parametrização

Todas as regras são configuráveis via Remote Config:

```json
{
  "penalty_overtake_under_yellow_seconds": 3,
  "penalty_track_cut_gain_threshold_ms": 200,
  "penalty_contact_speed_threshold_kph": 5,
  "penalty_ignore_blue_count": 3,
  "penalty_abandon_cooldown_minutes": 15,
  "immunity_post_recovery_seconds": 3,
  "ambiguous_collision_speed_range_kph": [5, 10]
}
```

## Impacto

- Direção de Prova é um subsistema de software com detecção, decisão e feedback separados.
- Regras devem ser transparentes ao jogador (HUD de infração + explicação).
- Campeonatos privados podem ter regras custom (subset via Remote Config por sala).

## Riscos

| Risco | Mitigação |
|---|---|
| Penalidades automáticas injustas | Threshold conservador; colisões ambíguas sem penalidade; tuning com telemetria |
| Jogadores discordam das regras | Transparência total; Remote Config permite ajuste rápido |
| Falta de credibilidade por não ser "oficial" | Inspiração clara em CBA/FIA; comunicar na interface |
| Inconsistência com kartódromos reais | Declarar que é regulamento do jogo, não réplica de regulamento específico |

## Plano de Evolução

1. MVP: Regulamento base parametrizável.
2. Pós-MVP: Modos de regulamento (indoor BR, outdoor CBA, custom).
3. Futuro: Parceria com kartódromo para regulamento oficial licenciado.

## Referências

- Regulamento Geral de Karting CBA (2024)
- FIA International Sporting Code
- Regulamentos internos de kartódromos em Brasília (para referência qualitativa)
