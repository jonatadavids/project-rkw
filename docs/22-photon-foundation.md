# 22 — Fundação Photon Fusion (M1-T04)

## Escopo e versão

- Data da instalação, consulta e validação: **2026-08-16**.
- Unity: **6000.3.22f1**.
- SDK: **Photon Fusion 2.1.1 Stable, build 2177**.
- Distribuição: pacote oficial obtido no dashboard/documentação Photon autenticado.
- Conteúdo importado: SDK Fusion e dependências de runtime/editor necessárias.
- Conteúdo excluído: `FusionMenu`, `FusionDemos`, samples e cenas demonstrativas.
- Dependência Unity adicionada pelo SDK: `com.unity.nuget.mono-cecil` **1.10.2**, usada pelo weaver.

O SDK está fixado pelos arquivos versionados em `Assets/Photon` e pelo `build_info.txt` do fornecedor. Nenhum download flutuante ou pacote sem versão é usado.

Fontes oficiais consultadas em 2026-08-16:

- [Photon Fusion — SDK download](https://doc.photonengine.com/fusion/current/getting-started/sdk-download)
- [Photon Fusion — conexão e matchmaking](https://doc.photonengine.com/fusion/current/manual/connection-and-matchmaking/matchmaking)
- [Photon Fusion — API de NetworkRunner](https://doc-api.photonengine.com/en/fusion/current/class_fusion_1_1_network_runner.html)

## Implementação mínima

`RKW.Network` contém somente o consumidor atual:

- `INetworkTransport`: conectar, consultar conexão e desconectar;
- `PhotonNetworkTransport`: `NetworkRunner` em Shared Mode;
- timeout finito e configurável;
- cancelamento por `CancellationToken`;
- falha convertida em resultado, sem crash;
- desligamento e destruição do runner após falha, timeout ou desconexão;
- repetição de conexão/desconexão sem processo ou runner órfão.

Não foram implementados lobby, matchmaking, sincronização, kart, pista, gameplay, input de rede ou configuração de produção. Operações adicionais serão adicionadas à interface somente quando tiverem consumidor real.

## App ID e ambiente

O App ID Fusion não é segredo administrativo, mas é tratado como configuração local não publicável por decisão do projeto:

- o asset versionado `PhotonAppSettings.asset` permanece com o campo Fusion vazio;
- o operador insere manualmente o App ID de desenvolvimento apenas durante validações locais;
- o valor deve ser removido antes de staging, patch ou commit;
- nenhum token administrativo pertence ao client;
- logs de evidência não devem conter o App ID;
- `RKW_RUN_PHOTON_INTEGRATION=1` habilita os testes locais que exigem conexão real, sem carregar o App ID na variável.

A aplicação de desenvolvimento provisionada pelo fundador está no plano **Free 100 CCU**. Isso não configura produção nem autoriza contratação, upgrade ou Photon 1.000 CCU.

## Evidências locais

| Verificação | Resultado |
|---|---|
| Compilação C# | Sucesso; sem warnings C# |
| EditMode | 8/8 passaram |
| PlayMode | 4/4 passaram |
| Conexão real | Duas conexões sucessivas em Shared Mode |
| Timeout | Tratado e finalizado sem runner órfão |
| Cancelamento | Tratado sem criar runner |
| Encerramento repetido | Nenhum `NetworkRunner` remanescente |
| Android compile check | Sucesso; sem gerar aplicativo |
| iOS compile check | Sucesso; sem exportar ou assinar |
| App ID em logs/repositório | Nenhuma ocorrência fora do asset local durante o teste; asset limpo antes do commit |

O protocolo regional aprovado em M0 permanece uma tarefa separada. M1-T05 e tarefas posteriores não fazem parte desta integração.
