# Spike M0-T04 — Procedimento de exclusão de dados no UGS

## Registro da consulta

- Data: **2026-08-16**
- Natureza: protocolo técnico preliminar, **não é parecer jurídico**.
- Fontes: documentação oficial Unity, Apple, Google e legislação/autoridades oficiais.

## Conclusão principal

`AuthenticationService.DeleteAccountAsync()` apaga somente a conta do Unity Authentication. A própria Unity exige que o desenvolvedor apague separadamente os dados associados em cada serviço UGS. Portanto, **Authentication deve ser o último passo**, depois da limpeza/cascade e da confirmação das operações.

## Fluxo proposto

1. Receber solicitação em dois canais: dentro do app e página web externa.
2. Exigir autenticação recente/reautenticação; nunca aceitar apenas `PlayerId` informado livremente.
3. Exibir consequências irreversíveis, tratamento de compras/assinaturas e prazo estimado; obter confirmação explícita.
4. Gerar `deletionRequestId`, timestamp UTC e estado do workflow sem copiar dados pessoais desnecessários.
5. Colocar conta em estado `deletion_pending`: bloquear novas partidas, compras e novas escritas não essenciais.
6. Avaliar legal hold/retenções com jurídico. Separar dados retidos do perfil operacional e restringir acesso.
7. Executar cascade por ambiente (`production`, `staging`, `development`) e por serviço.
8. Verificar por leitura administrativa que os dados apagáveis não estão mais presentes.
9. Apagar Unity Authentication por último e invalidar tokens/sessões locais.
10. Confirmar ao titular por canal previamente verificado, sem expor detalhes sensíveis.
11. Reter somente trilha mínima da execução quando houver base legal e prazo aprovados pelo jurídico.

## Runbook por serviço

| Serviço | Ação técnica | Observação/gap |
|---|---|---|
| Cloud Save Data | Enumerar todas as chaves e access classes; apagar cada item via Admin API | Não foi encontrado endpoint oficial de bulk-delete completo; paginar e repetir idempotentemente |
| Cloud Save Files | Enumerar e apagar todos os arquivos do jogador | Tratar separadamente de Data |
| Leaderboards | Usar o endpoint admin `.../leaderboards/scores/players/{playerId}/purge` | Remove score de todos os leaderboards vivos |
| Economy inventory | Enumerar e apagar cada `playersInventoryItemId` | API pública permite delete por item |
| Economy currencies | Não tratar `balance = 0` como exclusão | A documentação de privacidade diz que não há exclusão nativa; abrir solicitação à Unity |
| Economy — DSR | Submeter pedido pelo canal oficial indicado na página de privacidade do Economy | Necessário para exclusão efetiva dos dados do serviço |
| Analytics | Usar a função oficial `Request Data Deletion`, conforme SDK aplicável | Parar coleta/consentimento antes da solicitação |
| Authentication | `DeleteAccountAsync()` ou DELETE `/v1/users/{playerId}` | **Último passo; irreversível** |
| UPA/identity providers | Informar e oferecer fluxo próprio quando usado | Apagar conta do jogo não apaga automaticamente conta Apple/Google/Unity Player Account |
| Photon/AdMob/Crashlytics/IAP | Mapear identificadores e executar processo próprio do fornecedor | Fora do UGS; obrigatório no inventário de dados antes do lançamento |

## Idempotência e segurança

- Cada etapa grava status `pending/running/succeeded/failed` e pode ser repetida.
- Credenciais administrativas ficam somente no backend/secret manager; nunca no client.
- Respostas `404` após retry são tratadas como “já ausente”.
- Respeitar `429` com `Retry-After` e backoff.
- Não excluir Authentication até todos os serviços dependentes concluírem ou até existir rota administrativa segura sem token do jogador.
- Testar primeiro com contas sintéticas em `development` e `staging`.

## Exceções e retenção legal

Retenção não é automática. Consultor jurídico deve determinar base, escopo e prazo para, por exemplo:

- comprovantes fiscais e registros de transações/IAP;
- prevenção de fraude, chargebacks e segurança;
- exercício regular de direitos/contencioso;
- obrigações de plataforma ou autoridade.

O usuário deve ser informado sobre o que foi retido, por quê e por quanto tempo. Dados retidos não podem continuar sendo usados para gameplay, marketing ou perfilamento.

## Critérios de aceite para implementação futura

- solicitação disponível in-app e por URL externa;
- cascade testado em conta sintética contendo dados em todos os serviços;
- zero dados operacionais retornados nas verificações pós-exclusão;
- Auth apagada por último;
- falhas recuperáveis e auditáveis;
- confirmação ao usuário;
- matriz de retenção aprovada pelo jurídico.

## Conclusão de Q-BD-03

**Aprovado na revisão humana de 2026-08-16 somente como protocolo preliminar.** Implementação, retenções, texto ao usuário e conformidade continuam sujeitos a validação técnica/jurídica. O gap mais importante é Economy: a própria documentação oficial informa ausência de funcionalidade nativa de exclusão, exigindo solicitação à Unity. Isso deve ser validado em staging e pelo suporte antes do lançamento.

## Fontes oficiais

- [Unity Authentication — Delete accounts](https://docs.unity.com/en-us/authentication/delete-accounts)
- [Unity Authentication API — Delete player](https://services.docs.unity.com/docs/client-auth/)
- [Unity Cloud Save Admin API](https://services.docs.unity.com/cloud-save-admin/v1/)
- [Unity Economy API](https://services.docs.unity.com/economy/v2/index.html)
- [Unity Economy — Privacy overview](https://docs.unity.com/en-us/economy/privacy-and-consent/overview)
- [Unity Leaderboards Admin API](https://services.docs.unity.com/leaderboards-admin/)
- [Unity Analytics FAQ](https://docs.unity.com/en-us/analytics/faq)
- [Apple — Offering account deletion in your app](https://developer.apple.com/support/offering-account-deletion-in-your-app/)
- [Google Play — Account deletion requirements](https://support.google.com/googleplay/android-developer/answer/13327111)
- [LGPD — Lei 13.709/2018](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)
- [GDPR — Regulation (EU) 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng)
