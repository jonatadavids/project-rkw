# 23 — Fundação Unity Gaming Services (M1-T05)

## Escopo e versões

- Data da instalação, consulta e validação: **2026-08-16**.
- Unity: **6000.3.22f1**.
- Cloud Save: **`com.unity.services.cloudsave` 3.4.0**.
- Authentication: **3.7.3**, resolvido como dependência oficial.
- Unity Services Core: **1.18.0**, resolvido como dependência oficial.
- Newtonsoft Json: **3.2.2**, dependência oficial usada para validação sintática sem reformatar o payload.
- Ambiente permitido em runtime e testes: **`development`**.

A integração contém somente Unity Services Core, login anônimo do Authentication e Player Data do Cloud Save. Samples não foram importados. Analytics, Economy, Cloud Code, Remote Config, Unity Player Accounts, IAP e Ads não foram habilitados nem consumidos.

Fontes oficiais consultadas em 2026-08-16:

- [Unity Manual — Cloud Save 3.4.0 para Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.services.cloudsave.html)
- [Unity Cloud Save — Get started](https://docs.unity.com/en-us/cloud-save/get-started)
- [Unity Cloud Save — Unity SDK](https://docs.unity.com/en-us/cloud-save/tutorials/unity-sdk)
- [Unity Authentication — Get started](https://docs.unity.com/en-us/authentication/get-started)
- [Unity Authentication — Anonymous sign-in](https://docs.unity.com/en-us/authentication/use-anon-sign-in)
- [Unity Services — Environments](https://docs.unity.com/en-us/services/service-environments)

## Ambiente e vínculo do projeto

Todo projeto UGS recebe automaticamente um ambiente `production`. A existência desse ambiente não autoriza seu uso. Esta fundação seleciona `development` em duas camadas:

1. o Environment Selector do Editor fica salvo em `ProjectSettings/Packages/com.unity.services.core/Settings.json`;
2. o adaptador do `UgsAuthenticationService` chama `InitializationOptions.SetEnvironmentName("development")` explicitamente antes da inicialização;
3. os adaptadores recusam autenticação e Cloud Save quando o ambiente `development` não foi confirmado pelo runtime.

O Project ID, Organization ID e Environment ID gerados pelo Unity identificam o projeto e o namespace remoto, mas não concedem privilégio administrativo. Eles permanecem em arquivos de configuração versionáveis do Unity. Nenhuma service account, chave privada, senha, token administrativo ou bearer token pertence ao cliente ou ao Git.

O vínculo foi feito manualmente pelo fundador na organização autorizada. Não houve configuração de produção, cobrança ou aplicativo em loja. A opção jurídica sobre público infantil não foi alterada pela implementação; Q-PV-02 continua aberta e com os bloqueios registrados em `docs/20-open-questions.md`.

## Implementação mínima

`RKW.Backend` foi criado agora porque M1-T05 é seu primeiro consumidor real:

- `ICloudPersistence`: contrato mínimo para salvar e carregar uma string JSON por chave;
- `UgsAuthenticationService`: inicialização idempotente e login anônimo, sem registrar Player ID ou tokens;
- `UgsCloudPersistence`: adaptador para Cloud Save Player Data;
- `UgsOperationTimeouts`: política configurável e sempre finita para inicialização, autenticação, save e load;
- adapters internos mínimos: separam o SDK dos testes determinísticos, sem criar uma abstração de backend além do consumidor atual;
- `CloudSaveSmokePayload`: DTO versionado exclusivo da prova de round-trip.

O contrato valida chaves sem normalização silenciosa, rejeita caracteres de controle e valida a sintaxe JSON antes do envio. A string aceita é enviada e devolvida sem reformatar, normalizar ou serializar novamente.

O limite de **32 KiB** é defensivo **por operação/valor** nesta fundação. Ele não garante sozinho o orçamento agregado de 32 KiB por jogador: várias chaves próximas desse limite excederiam o agregado. A estrutura de perfil e seu consumidor futuro deverão contabilizar e impor o orçamento agregado antes da integração definitiva. O limite oficial do Cloud Save continua sendo **5 MiB por jogador e access class**.

## Timeouts e cancelamento

Os padrões de produção são 15 segundos para inicialização, 15 segundos para autenticação, 10 segundos para save e 10 segundos para load. Todos são configuráveis por `UgsOperationTimeouts`, mas precisam ser positivos, finitos e suportados pelo temporizador da plataforma.

O SDK usado nesta fundação não recebe `CancellationToken` nas operações envolvidas. Por isso, o cancelamento do chamador é **best-effort**: o chamador recupera o controle sem aguardar indefinidamente, enquanto a tarefa original continua sendo observada para que uma falha tardia não se torne exceção não observada. Em especial, uma escrita já enviada ao serviço pode ser concluída pelo servidor depois de timeout ou cancelamento local. Consumidores futuros não devem interpretar cancelamento local como confirmação de rollback remoto.

Os testes usam adapters internos determinísticos para simular operação pendente, timeout, cancelamento e falha sem rede. Nenhum seam permite escolher `production`: a implementação concreta fixa e confirma exclusivamente `development` antes de permitir autenticação ou Cloud Save.

`LeaderboardKey` e `SessionContextKey` continuam objetos de domínio imutáveis e não são serializados diretamente com `JsonUtility`. O DTO do smoke test não é o perfil do jogador e não deve crescer para assumir essa função. Perfil, migração, write-lock de settings, reconciliação, cache offline e autoridade de Cloud Code terão consumidores e tarefas próprias.

## Login anônimo e privacidade

O Authentication reutiliza o token de sessão local quando disponível. Se esse token for perdido, a conta anônima não pode ser recuperada em outro dispositivo sem vínculo posterior com um provedor externo. Login Apple/Google, conta infantil e recuperação de conta não fazem parte de M1-T05.

O teste utiliza apenas marcador sintético, versão de schema e número sequencial. Nenhum nome, e-mail, idade ou outro dado pessoal é gravado. A chave de desenvolvimento é `rkw_m1_t05_smoke_v1`.

O requisito de notificações associado ao Digital Services Act e a avaliação jurídica/privacidade permanecem obrigatórios antes de Alpha externo. A fundação técnica local não substitui esse trabalho.

## Evidências

Execução final em 2026-08-16:

| Verificação | Resultado |
|---|---|
| Compilação C# | Sucesso; nenhum erro ou warning C# novo |
| EditMode | 32/32 passaram; inclui 100 round-trips com seed fixa `5309701` (`0x00510505`) e casos determinísticos de pendência, timeout, cancelamento e falha |
| PlayMode local | 6/6 executados passaram; 3 integrações externas ignoradas por flags |
| UGS real | 7/7 testes executados passaram; somente 2 integrações Photon permaneceram ignoradas |
| Authentication | Login anônimo bem-sucedido sem registrar Player ID ou token |
| Cloud Save | JSON salvo e carregado sem alteração pela chave `rkw_m1_t05_smoke_v1` |
| Ambiente | Log do Services Core confirmou override explícito para `development` |
| Android compile check | Sucesso; nenhum aplicativo gerado |
| iOS compile check | Sucesso; nenhum projeto Xcode, signing ou aplicativo gerado |
| `.meta` | Todos os assets e diretórios não vazios têm metadado; nenhum GUID duplicado |
| Segredos e artefatos | Nenhum segredo, credencial, build ou log rastreado; App IDs Photon continuam vazios |

Os logs e XMLs de teste foram gravados somente em `/tmp`. `Library/` e `Logs/` permanecem ignorados pelo Git. A chave foi confirmada visualmente pelo fundador no Player Data do Dashboard, usando o ambiente `development`, em 2026-08-16. O Player ID não é registrado na documentação ou no repositório.
