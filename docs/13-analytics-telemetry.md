# 13 — Analytics e Telemetria

## Objetivo e Escopo

Definir eventos, propriedades, métricas-chave e guardrails de privacidade para tomada de decisão baseada em dados.

---

## Plataforma de Analytics

| Serviço | Uso |
|---|---|
| Unity Analytics | Eventos in-game, funis, retenção |
| Firebase Crashlytics | Crash reporting, ANRs |
| Remote Config (UGS) | Segmentação, A/B testing |
| Custom Backend (Cloud Code) | Métricas de economia, anti-cheat |

---

## Eventos Principais

### Onboarding e Tutorial

| Evento | Propriedades | Propósito |
|---|---|---|
| `tutorial_started` | module_id | Engajamento com escola |
| `tutorial_completed` | module_id, time_seconds, attempts | Dificuldade por módulo |
| `tutorial_abandoned` | module_id, time_spent, last_step | Identificar pontos de atrito |
| `license_earned` | category, time_total, attempts | Conversão escola → corrida |

### Gameplay

| Evento | Propriedades | Propósito |
|---|---|---|
| `race_started` | mode, category, player_count, bot_count | Distribuição de modos |
| `race_completed` | position, best_lap, total_time, penalties | Performance |
| `race_abandoned` | reason, lap_number, time_in_race | Churn dentro de corrida |
| `lap_completed` | lap_number, time, valid, sectors[] | Análise de consistência |
| `sector_time` | sector_id, time, delta_vs_best | Granularidade por curva |
| `penalty_applied` | type, severity, auto_vs_manual | Calibração de regras |
| `collision` | type, relative_speed, penalty_applied | Tuning de física |
| `slipstream_used` | duration, speed_gain | Balanceamento de vácuo |

### Controles e Assistências

| Evento | Propriedades | Propósito |
|---|---|---|
| `control_mode_set` | mode (joystick/wheel/tilt) | Preferência de controle |
| `assists_changed` | assist_type, old_value, new_value | Evolução de habilidade |
| `layout_customized` | elements_moved, elements_resized | Uso de personalização |

### Performance Técnica

| Evento | Propriedades | Propósito |
|---|---|---|
| `fps_sample` | avg_fps, p10_fps, quality_level | Saúde de performance |
| `memory_warning` | current_mb, threshold_mb | Risco de crash |
| `crash_report` | stack_trace, device, os_version | Estabilidade |
| `latency_sample` | ping_ms, packet_loss_pct, region | Qualidade de rede |
| `quality_downgrade` | from_level, to_level, trigger | Auto-adjust eficácia |
| `thermal_warning` | temperature_c, play_time_min | Impacto térmico |

### Matchmaking e Social

| Evento | Propriedades | Propósito |
|---|---|---|
| `matchmaking_started` | category, mmr, region | Demanda |
| `matchmaking_found` | wait_time_s, bots_needed | Tempo de espera |
| `private_room_created` | code, max_players | Uso social |
| `private_room_joined` | code, source (link/manual) | Viralidade |
| `disconnect` | duration_s, reconnected | Qualidade de rede |

### Economia e Monetização

| Evento | Propriedades | Propósito |
|---|---|---|
| `currency_earned` | type, amount, source | Inflação |
| `currency_spent` | type, amount, item_id | Sinks |
| `iap_initiated` | product_id, price_local | Intenção |
| `iap_completed` | product_id, price_local, receipt_valid | Receita |
| `iap_failed` | product_id, error_code | Problemas de pagamento |
| `ad_impression` | ad_type, placement | Revenue per impression |
| `ad_rewarded_watched` | reward_type, count_today | Engajamento com ads |
| `pass_purchased` | season_id, level_at_purchase | Timing de conversão |
| `pass_level_up` | season_id, level, days_remaining | Ritmo de progressão |

### Retenção e Sessão

| Evento | Propriedades | Propósito |
|---|---|---|
| `session_start` | day_since_install, returning | Retenção |
| `session_end` | duration_s, races_played | Engajamento |
| `daily_return` | streak_days | Hábito |

---

## Métricas Derivadas (Dashboards)

| Métrica | Fórmula | Frequência |
|---|---|---|
| D1/D7/D30 Retention | Users retornando / Users instalados no dia X | Diária |
| DAU / MAU | Unique users / dia ou mês | Diária |
| Stickiness | DAU / MAU | Diária |
| ARPDAU | Revenue total / DAU | Diária |
| Sessions/user/day | Total sessions / DAU | Diária |
| Races/user/day | Total races / DAU | Diária |
| Avg session duration | Soma duração / sessions | Diária |
| Matchmaking wait (P50/P95) | Distribuição wait_time_s | Diária |
| Crash-free rate | Sessions sem crash / total | Diária |
| FPS P10 | Percentil 10 de fps_sample | Diária |
| Clean driving index (avg) | Média do índice na base | Semanal |
| Coin sink/source ratio | Coins gastos / Coins criados | Semanal |

---

## Segmentação

| Segmento | Critério | Uso |
|---|---|---|
| Piloto real | Completou escola + > 50 corridas | Feedback qualitativo |
| Casual | < 3 sessões/semana | Otimizar onboarding |
| Whale (gasto alto) | Top 1% em IAP | Limites de gasto |
| Retornante | Ausente > 7 dias | Oferta de retorno |
| Novo | D0–D3 | FTUE optimization |

---

## Guardrails de Privacidade

| Regra | Implementação |
|---|---|
| Sem PII em eventos | Apenas IDs anônimos; sem nome/email em analytics |
| Consentimento explícito | Popup LGPD/GDPR no primeiro boot |
| Opt-out | Configurações > Privacidade > Desativar analytics |
| Dados mínimos | Não coletar localização precisa, contatos, fotos |
| Retenção de dados | 365 dias; depois anonimizar/deletar |
| Direito de exclusão | Endpoint para solicitação de delete |
| Terceiros | Apenas Unity Analytics + Firebase; sem data brokers |
| Crianças | Se < 13: sem ads personalizados; analytics limitados |

---

## Decisões Confirmadas

1. Unity Analytics + Firebase Crashlytics como stack primário.
2. Eventos granulares por setor/volta.
3. Dashboard semanal de economia.
4. Privacidade: consentimento explícito + opt-out.
5. Sem PII em eventos de analytics.

## Suposições

| ID | Suposição | Validação |
|---|---|---|
| AN-01 | Unity Analytics suporta volume do MVP sem custo extra | Verificar tiers de pricing |
| AN-02 | Eventos por setor não impactam performance (batching) | Profiling no milestone 3 |
| AN-03 | D1 ≥ 40% é alcançável com onboarding otimizado | A/B testing FTUE |

## Questões Abertas

- Q-AN-01: Usar Unity Analytics ou migrar para Amplitude/Mixpanel?
- Q-AN-02: Telemetria de replay para campeonatos (custo de storage)?
- Q-AN-03: Frequência de amostragem de FPS/latência (a cada frame vs a cada 5 s)?

## Links Relacionados

- [Segurança e Privacidade](./14-security-privacy-compliance.md)
- [Monetização](./11-monetization-liveops.md)
- [Backend](./09-backend-data-model.md)
