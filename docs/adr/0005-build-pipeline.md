# ADR-0005: Build Pipeline

## Status

**Aceito**

## Contexto

O projeto precisa gerar builds Android (AAB) e iOS (IPA) automaticamente a partir de commits, com execução de testes pré-build. O fundador não possui infraestrutura própria de CI/CD para Unity.

## Alternativas Consideradas

| Solução | Prós | Contras |
|---|---|---|
| **Unity Build Automation (UCB)** | Integrado ao Unity; suporta iOS sem Mac local; zero infra | Custo por minuto de build; dependência Unity |
| GitHub Actions + GameCI | Flexível, self-hosted possível, free minutes | iOS requer macOS runner (caro ou self-hosted Mac); setup complexo |
| GitLab CI + GameCI | Similar a GitHub Actions | Mesmas limitações de iOS runner |
| Jenkins self-hosted | Gratuito, total controle | Requer Mac para iOS; manutenção pesada; overhead |

## Decisão

**Unity Build Automation** como pipeline primário de build.

## Justificativa

1. **iOS sem Mac:** UCB constrói iOS builds em cloud sem necessidade de Mac local do fundador.
2. **Integração nativa:** Configuração via Unity Dashboard; sem YAML complexo.
3. **Teste pre-build:** Executa EditMode/PlayMode tests antes de empacotar.
4. **Artefatos automáticos:** AAB e IPA prontos para upload em stores.
5. **Mínima manutenção:** Zero infra para gerenciar.

## Impacto

- Builds disparados por push em branches `release/*` e `main`.
- Testes EditMode executados como gate.
- Artefatos disponíveis para download no Unity Dashboard.
- Distribuição manual para TestFlight/Google Play (automação futura via Fastlane possível).

## Custo

| Plano | Build Minutes/mês | Preço Estimado |
|---|---|---|
| Free (Unity Personal) | 10 builds | $0 |
| Plus | Verificar | Incluso no Unity Plus/Pro |
| Pro | Verificar | Incluso |
| Extra minutes | Pay-per-use | ~$0.07/min |

> ⚠️ Verificar pricing atualizado do UCB. Build Android ~15 min; iOS ~25 min.

## Riscos

| Risco | Mitigação |
|---|---|
| Queue de builds longa | Planejar builds fora de horário de pico; cache |
| UCB descontinuado | Migrar para GameCI + GitHub Actions (macOS runner) |
| iOS signing issues | Configurar certificates via Unity Dashboard antecipadamente |
| Custo escala com frequência | Limitar builds a release branches; develop = local |

## Plano de Saída

1. **GameCI + GitHub Actions:** Dockerized Unity builds; macOS runner para iOS (custo ~$0.08/min).
2. **Mac mini self-hosted:** Comprar Mac mini para iOS builds locais.
3. **Fastlane:** Automação de upload para stores.

## Referências

- Unity Build Automation Documentation
- GameCI (GitHub Action for Unity)
- Fastlane Documentation
