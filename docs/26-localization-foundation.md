# 26 — Fundação de localização da UI (M1-T10)

## Escopo e fontes

- Implementação e consultas: **2026-08-17**.
- Unity: **6000.3.22f1**.
- Unity Localization: **`com.unity.localization` 1.5.12**, fixado no manifest e resolvido exatamente no lock file.
- Localization declara Addressables 1.25.0 como dependência mínima; o Package Manager da Unity 6000.3.22f1 resolveu **Addressables 2.9.1**, conforme o lock file.
- Conteúdo remoto: nenhum. Locales, tabela, catálogo e bundles usam caminhos locais e são empacotados com o aplicativo.

Fontes oficiais consultadas em 2026-08-17:

- [Unity Localization 1.5 — manual oficial](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/index.html)
- [Unity Localization 1.5 — changelog oficial](https://docs.unity3d.com/Packages/com.unity.localization@1.5/changelog/CHANGELOG.html)
- [Unity Localization — fallback de locales](https://docs.unity3d.com/Packages/com.unity.localization@1.5/manual/Locale.html)
- [Unity Addressables 2.9 — inicialização](https://docs.unity3d.com/Packages/com.unity.addressables@2.9/manual/InitializeAsync.html)

Nenhum sample foi importado. Não foram criadas coleções `Instructor`, `Penalties` ou `HUD`, pois ainda não há consumidores. Elas serão adicionadas nas tarefas que introduzirem esses fluxos.

## Locales e tabela

`pt-BR` é o Project Locale e o único startup selector. `en` existe apenas como preparação técnica, sem seletor visível e sem tabela ou tradução inventada; seu metadata de fallback aponta para `pt-BR`. O String Database tem fallback explicitamente habilitado.

Existe somente a coleção `UI`, marcada para preload, com sete chaves estáveis:

| Chave | Valor pt-BR |
|---|---|
| `bootstrap.connecting` | Conectando... |
| `bootstrap.connection_failed` | Não foi possível conectar. Tente novamente. |
| `bootstrap.retry` | TENTAR NOVAMENTE |
| `menu.play` | JOGAR |
| `menu.school` | ESCOLA |
| `menu.garage` | GARAGEM |
| `menu.coming_soon` | Disponível em breve |

`KARTGRID` permanece literal e não traduzível por ser nome comercial provisório. `PROJECT RKW • PROTÓTIPO DEV` permanece literal como identificação técnica. Nenhum Product Name, Bundle ID, projeto UGS ou cadastro externo foi alterado.

## Inicialização e falhas seguras

O Bootstrap aguarda de forma assíncrona a inicialização local de Localization/Addressables e o preload da coleção `UI` antes de exibir o status ou autenticar. O processo não consulta rede. Cada etapa possui timeout configurável e conservador de **10 segundos** (máximo padrão de 20 segundos para as duas etapas). Os prefabs usam a mensagem emergencial `Texto indisponível.` como valor serializado, evitando campo vazio ou flash de um idioma incorreto durante a inicialização.

`UiLocalization` centraliza as chaves e atualiza as views quando o locale muda. A inicialização é compartilhada, idempotente e não cria operações concorrentes; o cancelamento de um chamador não cancela os demais. Se a inicialização ou o preload falhar ou exceder o timeout, o Bootstrap continua sem crash usando `Texto indisponível.` e somente um warning sanitizado. A tarefa original do SDK permanece observada para consumir eventual falha tardia, mas sua conclusão tardia não pode reativar a localização nessa execução.

Uma chave ausente retorna a mesma mensagem emergencial, emite um warning controlado somente na primeira ocorrência daquela chave e não causa crash. Esta é a única mensagem de infraestrutura mantida hardcoded no runtime. `pt-BR` continua sendo o fallback do conteúdo local; o fallback emergencial é usado apenas quando a infraestrutura não conclui com segurança.

## Evidências automatizadas

| Verificação | Resultado |
|---|---|
| Compilação C# | Sucesso; nenhum erro C# |
| EditMode | 44/44 passaram; package exato, settings, locales, fallback, coleção, conteúdo, falha, timeouts e conclusões tardias validados deterministicamente |
| PlayMode | 16 passaram; 0 falhas; 5 integrações/capturas condicionais ignoradas |
| Fallback `en` → `pt-BR` | Passou sem texto vazio e sem tabela inglesa |
| Chave ausente | Mensagem segura; warning único; sem crash |
| UGS real | 1/1 passou; Authentication anônima exclusivamente em `development` após a correção de timeout |
| Captura localizada | 1/1 passou; `/tmp/rkw-m1t10-main-menu.png`, 107.745 bytes, SHA-256 `22334130ac8aff10ce23d9d770322acb1814d62843fb25ade1b41315cd3575a0` |

A captura contém somente o menu localizado, KARTGRID e a identificação técnica. Não contém Player ID, token, App ID, credencial ou dado pessoal e permanece fora do Git.

## Builds e impacto medido

O APK Development foi gerado com IL2CPP, ARM64, assinatura debug e Bundle ID provisório. Terminou com 0 erros e três warnings contados pelo BuildReport, todos de toolchain, Fusion ou metadata de plataforma de pacote; nenhum veio de fonte RKW. O arquivo tem **56.459.142 bytes** e SHA-256 `d80a68d7f937f3f8daacabaa2573d3c45fc98adca504a3970fc5683193dad840`.

Os oito arquivos Addressables empacotados diretamente sob `assets/aa/` somam **14.979 bytes** no APK, incluindo catálogo, settings, locales e a tabela pt-BR. Os assets versionáveis ocupam aproximadamente 60 KiB em `Assets/Localization` e 172 KiB em `Assets/AddressableAssetsData`. O APK anterior da Etapa A tinha 105.007.865 bytes; a diferença de -48.548.723 bytes não é atribuída à localização porque os dois builds não preservam artefatos intermediários e condições de empacotamento suficientes para isolar causalidade. O valor atual e o payload Addressables direto são as medidas reproduzíveis desta tarefa.

No PlayMode final, inicialização mais preload levaram **114,131 ms**. O snapshot global do Profiler variou **-8.574.494 bytes** entre início e fim devido a coleta concorrente do processo; portanto ele demonstra ausência de aumento positivo retido naquela execução, mas não é tratado como medição isolada de heap do pacote. Uma medição em dispositivo poderá ser repetida no próximo gate de performance.

A exportação iOS Development IL2CPP gerou projeto Xcode de aproximadamente 1,6 GiB. O compile check usou Xcode 26.6 (`17F113`), SDK iOS 26.5, destino genérico iOS e `CODE_SIGNING_ALLOWED=NO`. Terminou com `BUILD SUCCEEDED`; o `.app` de aproximadamente 320 MiB foi confirmado como não assinado. Os warnings observados vieram de scripts/objetos gerados por Unity/Burst e da ausência deliberada de `Apple App Info` no Localization. Esse metadata nativo não foi criado porque M1-T10 não pode traduzir ou alterar Product Name, nome de loja ou identidade externa; as String Tables usadas dentro do aplicativo não são afetadas. Ele deverá ser reavaliado quando a identidade definitiva das lojas tiver consumidor e aprovação. Não houve erro de fonte RKW, Apple ID, provisioning profile, certificado, archive, IPA ou upload.

APK, exportação Xcode, DerivedData, screenshot, XMLs e logs permanecem em `/tmp` e fora do Git. Nenhuma configuração de loja, UBA, signing, serviço externo ou tarefa M1-T11+ foi iniciada.

Após a inclusão dos timeouts, os compile checks foram repetidos: Android Development IL2CPP/ARM64 terminou com sucesso e a exportação iOS seguida de `xcodebuild` com `CODE_SIGNING_ALLOWED=NO` terminou com `BUILD SUCCEEDED`. As cinco execuções Unity desta validação (EditMode, PlayMode, UGS, Android e exportação iOS) não emitiram diagnóstico C# novo.
