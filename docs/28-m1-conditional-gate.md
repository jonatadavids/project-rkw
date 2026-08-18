# 28 — Decisão de entrada condicional no M2

## Estado da decisão

**Decisão humana registrada em 2026-08-17 (America/Sao_Paulo): entrada condicional autorizada somente para M2-T01.** O Exit Gate M1 integral permanece aberto. Esta revisão não executou código, testes, builds, Unity ou serviços externos e não iniciou M2-T01.

Os critérios originais não foram reduzidos nem dispensados. M1-T07, M1-T12, M1-T13 e M1-T14 permanecem `[ ]` até suas evidências integrais existirem.

## Critérios já comprovados por evidências publicadas

| Critério | Evidência existente | Estado para a fundação local do M2 |
|---|---|---|
| Unity, assemblies e testes | M1-T01–T03 e M1-T08–T09; [`docs/21-unity-foundation.md`](./21-unity-foundation.md) | Comprovado |
| Android local e exportação/compilação iOS | APK IL2CPP/ARM64 no Galaxy S25 e Xcode sem assinatura; [`docs/25-local-mobile-build-validation.md`](./25-local-mobile-build-validation.md) | Comprovado localmente |
| Photon Shared Mode | Conexão real e ciclo de vida mínimo; [`docs/22-photon-foundation.md`](./22-photon-foundation.md) | Comprovado em development |
| UGS Authentication e Cloud Save | Login anônimo e round-trip real; [`docs/23-ugs-foundation.md`](./23-ugs-foundation.md) | Comprovado em `development` |
| Localization | String Table UI, fallback e inicialização local; [`docs/26-localization-foundation.md`](./26-localization-foundation.md) | Comprovado |
| Remote Config | Flags base e integração real; [`docs/27-remote-config-foundation.md`](./27-remote-config-foundation.md) | Comprovado em `development` |
| Git e segurança básica | Histórico publicado em `main`, regras de ignore para caches/builds/logs/assinatura e evidências sem segredos nos documentos de fundação | Comprovado para desenvolvimento local |

## Exceções rastreadas

- **M1-T07 `[ ]`:** Android local e exportação/compilação iOS estão validados. Unity Build Automation, AAB/IPA distribuíveis, testes em trigger, lojas e distribuição permanecem adiados; nenhuma cobrança foi habilitada.
- **M1-T12 `[ ]`:** o snapshot de Brasília recomenda provisoriamente `sa` (aprox. 21 ms). Heartbeat de sessão e duas localidades brasileiras reais permanecem pendentes antes da decisão regional definitiva.
- **M1-T13 `[ ]`:** Unity Audio foi aprovado no Galaxy S25 high-end. Profiling de Audio CPU em Android low/mid permanece pendente para o gate de performance do M3; não existe declaração de CPU `< 2 ms` em low-tier.
- **M1-T14 `[ ]`:** o Exit Gate integral permanece aberto até todos os critérios originais serem comprovados.

## Justificativa e limites

As pendências acima afetam automação/distribuição, escolha regional definitiva e performance de áudio por tier. Elas não bloqueiam a implementação local de física, controles locais ou pista greybox. Por isso, M2-T01 pode ser iniciado condicionalmente em uma tarefa e branch próprias após esta decisão, sem implicar conclusão de M1.

Esta autorização não libera M2-T02+, produção, multiplayer de gameplay, região Photon definitiva, lojas, signing, triggers, distribuição ou cobrança. Cada pendência continua vinculada ao gate em que seu critério original é exigido.
