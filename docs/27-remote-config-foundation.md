# 27 — Fundação de Remote Config (M1-T11)

## Escopo e versão

- Implementação, consulta e validação: **2026-08-17**.
- Unity: **6000.3.22f1**.
- Pacote: **`com.unity.remote-config` 3.0.0**, fixado no manifest; seu runtime `com.unity.remote-config-runtime` foi resolvido em 3.0.0.
- Ambiente permitido: **`development`**. O cliente não armazena Environment ID, Player ID, assignment ID, token ou credencial.

Fontes oficiais consultadas em 2026-08-17:

- [Unity Remote Config — configuração do projeto](https://docs.unity.com/en-us/remote-config/configuring-your-project)
- [Unity Remote Config — API SDK](https://docs.unity.com/en-us/remote-config/sdk-api)
- [Unity Services — guia de versões](https://docs.unity.com/en-us/services/sdk-upgrades)

Nenhum sample, Deployment package, Analytics, Economy, Cloud Code ou outro serviço foi adicionado.

## Contrato de flags

`RemoteFeatureFlags` é uma allow-list imutável. Os únicos valores remotos aceitos nesta fundação são booleanos:

| Chave | Default local | Estado atual em development |
|---|---:|---:|
| `enable_multiplayer` | `false` | `false` |
| `enable_championship` | `false` | `false` |
| `enable_school` | `false` | `false` |
| `enable_ads` | `false` | `false` |

As flags são acessíveis por `IRemoteConfigService.Flags`. Nesta etapa não há consumidor que habilite gameplay, anúncios, campeonato, matchmaking ou qualquer UI nova. Remote Config só poderá ativar ou parametrizar código e conteúdo já presentes em uma versão publicada do aplicativo; não pode criar conteúdo novo remotamente.

## Bootstrap, falhas e privacidade

Após Authentication anônima confirmar `development`, o Bootstrap chama `RemoteConfigManager.LoadAsync` antes do carregamento additive de MainMenu. O fetch é compartilhado entre chamadores concorrentes e tem timeout finito padrão de **10 segundos**. Falha, indisponibilidade de autenticação ou timeout retornam imediatamente os quatro defaults locais seguros, sem bloquear o menu. Cancelamento do ciclo de vida do Bootstrap é propagado ao chamador; a tarefa do SDK continua observada para não produzir exceção tardia não observada.

O adaptador concreto recusa fetch sem Authentication do ambiente development confirmado. Ele não configura, consulta ou seleciona `production`. Logs registram apenas êxito/falha sanitizados, sem valores de identificadores, assignment, token ou dados de jogador.

## Evidências

| Verificação | Resultado |
|---|---|
| Resolução do pacote | `com.unity.remote-config` 3.0.0 resolvido com sucesso |
| EditMode direcionado | 7/7 passaram: defaults, allow-list, falha, timeout, cancelamento e concorrência |
| PlayMode direcionado | 1/1 passou: Authentication → Remote Config → MainMenu |
| Compilação C# | Sucesso; nenhum erro ou warning C# novo |
| Integração real | 1/1 passou em `development`; as quatro chaves existem e retornam `false` |

Não foram executados build Android/iOS, Cloud Save, Photon real, Analytics ou qualquer tarefa M1-T12+ nesta implementação.
