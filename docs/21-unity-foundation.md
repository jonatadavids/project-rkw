# 21 — Fundação do projeto Unity (M1-T01 a M1-T06)

## Escopo e ambiente

- Data da execução e das consultas: **2026-08-16**.
- Editor: **Unity 6.3 LTS — 6000.3.22f1 (1c726e1fb402), Apple Silicon**.
- Template oficial: **Universal 3D / URP** (`com.unity.template.urp-blank@17.0.14`).
- Render pipeline: **URP 17.3.0**.
- Photon Fusion 2 foi integrado exclusivamente na fundação mínima de M1-T04. A fundação UGS de M1-T05 usa somente Services Core, Authentication anônima e Cloud Save no ambiente `development`. M1-T06 adiciona apenas o fluxo Bootstrap → MainMenu com uGUI. CI/CD, gameplay, kart e pista não foram iniciados.

## Configuração base

| Item | Valor |
|---|---|
| Company Name | `Suite Digital` |
| Product Name | `Project RKW` |
| Identificador Android/iOS | `br.com.suitedigital.rentalkartworld.dev` — placeholder técnico provisório de desenvolvimento |
| Android | min API 26; target automático do SDK instalado (34+); ARMv7 + ARM64; IL2CPP |
| iOS | deployment target 15.0; ARM64; IL2CPP; exportação para projeto Xcode |
| Fixed timestep | `0.02 s` (50 Hz) |

O identificador não é definitivo e não autoriza cadastro de aplicativo, signing de distribuição ou publicação em loja.

## Assemblies atuais

Somente assemblies com consumidor neste bloco foram criadas:

- `RKW.Core`: identidade técnica compartilhada e sem referência ao UnityEngine.
- `RKW.Network`: ciclo mínimo de conexão/desconexão Photon Shared Mode, sem gameplay networking.
- `RKW.Backend`: login anônimo UGS e persistência JSON mínima no Cloud Save de desenvolvimento.
- `RKW.UI`: composição manual do Bootstrap, carregamento additive, safe area e menu placeholder.
- `RKW.Core.EditMode.Tests`: teste de sanidade da fundação.
- `RKW.Network.EditMode.Tests`: contrato mock do ciclo de transporte.
- `RKW.Backend.EditMode.Tests`: contrato de persistência e round-trip determinístico do DTO de transporte.
- `RKW.PlayMode.Tests`: teste de sanidade do player loop.

`RKW.Physics`, `RKW.Controls` e demais assemblies não foram antecipadas. Serão adicionadas no milestone/tarefa em que o primeiro consumidor real aparecer, seguindo a lista incremental de M1-T02. `RKW.Editor` só será criada quando houver uma ferramenta de editor persistente.

## Contratos de identidade do domínio

`LeaderboardKey` e `SessionContextKey` são objetos de domínio imutáveis. Seus IDs preservam exatamente o texto validado e usam comparação ordinal case-sensitive; não há normalização silenciosa com `Trim()` nem imposição de lowercase.

Esses objetos não devem ser serializados diretamente com `UnityEngine.JsonUtility`. Não serão adicionados `[Serializable]`, setters, construtores vazios ou campos públicos apenas para satisfazer o serializer do Unity. DTOs próprios de transporte e persistência serão criados quando Cloud Save, Cloud Code ou outro consumidor real for implementado.

O formato persistente deverá representar enums por nomes estáveis, sem depender de seus valores ordinais. Antes de qualquer integração com Cloud Save ou Cloud Code, o DTO e seu serializer deverão ter teste obrigatório de round-trip (`deserialize(serialize(value)) == value`).

## Framework de testes e avaliação do FsCheck

O projeto usa Unity Test Framework `1.6.0`, com NUnit fornecido pela extensão oficial `com.unity.ext.nunit@2.0.5`. EditMode e PlayMode têm um teste mínimo cada.

FsCheck **não foi incorporado** neste bloco. A avaliação de 2026-08-16 encontrou:

- FsCheck `3.3.4` oferece binário `netstandard2.0`, compatível em princípio com o perfil .NET Standard do Unity, mas depende de `FSharp.Core >= 5.0.2`.
- FsCheck.NUnit `3.3.3` tem como alvo `net6.0` e exige NUnit `>= 4.0.0 < 5.0.0`; isso não constitui compatibilidade comprovada com o NUnit empacotado pelo Unity Test Framework.
- A compatibilidade real com importação Unity, stripping e IL2CPP em Android/iOS não foi demonstrada pelas fontes consultadas.

Decisão: manter NUnit puro agora e avaliar FsCheck em spike próprio quando a primeira propriedade de lógica pura existir. A incorporação exigirá pacote aprovado, teste EditMode e build IL2CPP Android/iOS verdes; nenhuma DLL será copiada manualmente sem essa prova.

As 34 propriedades obrigatórias do MVP permanecem no escopo. Até uma integração FsCheck validada existir, serão implementadas com NUnit e geradores determinísticos próprios, seed registrada e pelo menos 100 casos por propriedade. Toda falha deverá informar a seed e o índice do caso para reprodução. A reavaliação futura do FsCheck não bloqueia M1.

Fontes oficiais consultadas em 2026-08-16:

- [Unity Manual — suporte a perfis .NET](https://docs.unity3d.com/6000.0/Documentation/Manual/dotnet-profile-support.html)
- [NuGet Gallery — FsCheck](https://www.nuget.org/packages/FsCheck)
- [NuGet Gallery — FsCheck.NUnit](https://www.nuget.org/packages/FsCheck.NUnit/)

## Evidências locais

Execução final em 2026-08-16:

| Verificação | Resultado |
|---|---|
| Importação e compilação final do projeto | Sucesso, exit code 0 |
| EditMode `ProjectIdentity_UsesDevelopmentPlaceholder` | 1/1 passou; 0 falhas |
| PlayMode `PlayerLoop_AdvancesOneFrame` | 1/1 passou; 0 falhas |
| Android development build | Sucesso; APK IL2CPP ARMv7 + ARM64 gerado temporariamente (70 MiB) |
| iOS development export | Sucesso; projeto Xcode IL2CPP ARM64 gerado temporariamente (1,1 GiB) |

Os artefatos e logs foram gerados em `/tmp` e não fazem parte do repositório. O Android usou o debug signing local do Unity, sem keystore de produção. O iOS foi validado até a exportação Xcode; archive, signing e upload não foram executados.

Durante a primeira tentativa do build Android, uma `Library` reconstruída a partir de importações previamente interrompidas omitiu fontes URP do grafo de compilação. O cache foi preservado em `/tmp`, recriado integralmente e o build subsequente passou. Nenhuma fonte do pacote URP foi alterada.

## Próximas assemblies (não criadas)

- Física e controles: quando M1-T04+ ou a tarefa específica correspondente for autorizada e houver consumidor.
- M4: `RKW.Timing`, `RKW.Timing.Tests`, `RKW.Bots`, `RKW.Bots.Tests`, `RKW.Telemetry`.
- M5: ampliar `RKW.Network` somente com consumidores reais; criar `RKW.Network.Tests` separado se o volume de testes justificar.
- M6: `RKW.Race`, `RKW.Race.Tests`.
- M7: `RKW.School`.
- M8: ampliar `RKW.Backend` e seus testes somente com consumidores de perfil, autoridade e economia; criar `RKW.Championship` e `RKW.Championship.Tests`.
- Quando houver consumidor: `RKW.Track`, `RKW.Track.Tests`, `RKW.Editor`.
