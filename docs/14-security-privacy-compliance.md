# 14 — Segurança, Privacidade e Compliance

## Objetivo e Escopo

Definir políticas de segurança, proteção de dados, compliance regulatório (LGPD/GDPR), anti-cheat e controles para publicação em lojas.

---

## Segurança do Jogo

### Princípios

1. **Client nunca é confiável**: Moeda, inventário, resultados e ranking são server-authoritative.
2. **Defense in depth**: Múltiplas camadas de validação.
3. **Least privilege**: Cada serviço acessa apenas o necessário.
4. **Segredos fora do repositório**: Chaves, tokens e credentials em variáveis de ambiente ou secret managers.

### Modelo de Ameaças (MVP)

| Ameaça | Vetor | Impacto | Mitigação |
|---|---|---|---|
| Speed hack | Client modificado | Competitivo comprometido | Validação de velocidade no host/server |
| Teleport | Memory editing | Resultado inválido | Delta check entre ticks |
| Economy exploit | Client envia moeda | Inflação/perdas | Backend-only currency operations |
| Replay manipulation | Client falso resultado | Ranking corrupto | Backend valida resultado + telemetria |
| Account takeover | Credential leak | Perda de conta | OAuth providers; sem senha própria no MVP |
| DDoS | Flood de requests | Indisponibilidade | Rate limiting no UGS; Photon DDoS protection |
| Cheat engine | Modificar memória local | Vantagem injusta | Obfuscação + integrity checks |

### Anti-Cheat Técnico

| Camada | Implementação |
|---|---|
| Transport | Photon Fusion com validação de estado |
| Runtime | Code obfuscation (IL2CPP + strip) |
| Memory | Anti-tamper básico (hash de valores críticos) |
| Statistical | Detecção de outliers pós-corrida |
| Social | Sistema de report + revisão manual |

---

## Privacidade

### Dados Coletados

| Dado | Justificativa | Base Legal (LGPD) |
|---|---|---|
| Player ID (anônimo) | Identificação de conta | Execução de contrato |
| Email (se login Google/Apple) | Recuperação de conta | Consentimento |
| Idade (faixa) | Controle parental | Obrigação legal |
| Analytics (anônimos) | Melhoria do produto | Legítimo interesse / Consentimento |
| Dados de jogo (voltas, resultados) | Core functionality | Execução de contrato |
| Device info (modelo, OS) | Suporte técnico | Legítimo interesse |

### Dados NÃO Coletados

- Localização precisa (GPS).
- Contatos.
- Fotos/mídia.
- Dados financeiros (gerenciados pelas stores).
- Biometria.
- Peso/medidas corporais.

### Direitos do Titular (LGPD/GDPR)

| Direito | Implementação |
|---|---|
| Acesso | Export de dados via email em até 15 dias |
| Retificação | Alterar nome/email nas configurações |
| Exclusão | Delete account → anonimizar dados em 30 dias |
| Portabilidade | Export JSON dos dados do jogador |
| Oposição | Opt-out de analytics nas configurações |
| Revogação de consentimento | A qualquer momento nas configurações |

### Consentimento

- Popup no primeiro acesso explicando coleta.
- Link para Política de Privacidade completa.
- Opção granular: aceitar analytics / rejeitar analytics (mantendo funcionalidade core).
- Crianças (< 13 anos): consentimento parental obrigatório onde aplicável.

---

## Compliance com Lojas

### Google Play

| Requisito | Status |
|---|---|
| Política de Privacidade | Obrigatória (URL pública) |
| Data Safety form | Preencher com dados coletados |
| Target age group | 13+ (ou All ages com restrições) |
| IARC rating | A obter (esperado: PEGI 3 ou E) |
| Permissions justificadas | Apenas Internet + vibration |
| App Bundle (AAB) | Obrigatório |

### Apple App Store

| Requisito | Status |
|---|---|
| Privacy Nutrition Labels | Preencher App Privacy |
| App Tracking Transparency | Implementar ATT se usar IDFA |
| In-App Purchase review | Submeter todos os IAPs para revisão |
| Age rating | A obter via questionário Apple |
| TestFlight | Para beta distribution |
| Sign in with Apple | Obrigatório se outras auth sociais |

### Considerações para Crianças

| Regra | Implementação |
|---|---|
| COPPA (EUA) | Sem coleta de PII de < 13 sem consentimento parental |
| Anúncios personalizados | Desabilitados para < 13 |
| Chat/interação social | Sem chat aberto no MVP; emotes pré-definidos |
| Compras | Confirmação extra; limites |
| Conteúdo | Sem violência, linguagem ou conteúdo adulto |

---

## Gestão de Segredos

| Segredo | Storage | Acesso |
|---|---|---|
| API keys (UGS) | CI/CD env vars | Build pipeline only |
| Photon App ID | Unity Dashboard + env vars | Runtime (obfuscated in build) |
| AdMob App/Ad Unit IDs | Remote Config | Runtime |
| Keystore (Android) | GitHub Secrets | Build pipeline only |
| Certificates (iOS) | Apple Developer Portal + CI | Build pipeline only |

---

## Requisitos Não Funcionais

| Requisito | Meta |
|---|---|
| Tempo para exclusão de dados | ≤ 30 dias |
| Rate limiting | ≤ 100 requests/min por client |
| Uptime auth | 99,5% (SLA UGS) |
| Audit log | Todas as transações de economia logadas |
| Penetration test | Antes do soft launch (🧑‍💻 ação humana) |

---

## Decisões Confirmadas

1. Client nunca é source of truth para economia/ranking.
2. IL2CPP + code stripping para obfuscação.
3. OAuth providers para login; sem senha própria.
4. Consentimento explícito para analytics.
5. Age rating target: 13+ ou All Ages.
6. Sem chat aberto no MVP.

## Suposições

| ID | Suposição | Validação |
|---|---|---|
| SP-01 | UGS cumpre LGPD/GDPR com DPA padrão | Verificar DPA do Unity |
| SP-02 | IL2CPP é suficiente como anti-tamper no MVP | Monitorar reports de cheat |
| SP-03 | Age 13+ evita necessidade de COPPA compliance | Confirmar com consultoria jurídica |

## Questões Abertas

- Q-SP-01: Necessário consultor jurídico para Política de Privacidade? (🧑‍💻)
- Q-SP-02: Age gate: birth year ou checkbox "tenho 13+"?
- Q-SP-03: Plano de resposta a incidentes de segurança?
- Q-SP-04: Seguro contra vazamento de dados?

## Links Relacionados

- [Backend](./09-backend-data-model.md)
- [Multiplayer](./08-multiplayer-architecture.md)
- [Analytics](./13-analytics-telemetry.md)
- [Release](./15-android-ios-release.md)
