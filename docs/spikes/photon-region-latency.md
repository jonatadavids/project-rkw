# M1-T12 — Resultados de latência Photon por região

## Estado

**Parcial em 2026-08-18.** Brasília possui um snapshot válido de descoberta regional, mas não uma série temporal independente. A tarefa permanece desmarcada até existir heartbeat de sessão utilizável e medições reais de ao menos duas outras localidades brasileiras.

## Execução Brasília/DF

- Localidade declarada para a rede atual: **Brasília/DF**.
- Snapshot diagnóstico UTC: **2026-08-18T00:23:16Z**.
- Unity: `6000.3.22f1`; Fusion: 2.1.1 Stable build 2177.
- API: `NetworkRunner.GetAvailableRegions`, com o App ID Fusion de desenvolvimento mantido somente na configuração local.
- Candidatas do protocolo M0-T03: `sa`, `ussc`, `us`.
- Diagnóstico inicial: 15 regiões retornadas em um snapshot.
- Configuração sanitizada: App ID presente: sim; `UseNameServer`: sim; `FixedRegion`: vazia; protocolo: UDP.
- Falha global/exceção: nenhuma.

| Região | Ping no snapshot |
|---|---:|
| `sa` | ≈ 21 ms |
| `ussc` | ≈ 171 ms |
| `us` | ≈ 172 ms |

O resultado anterior de zero amostras foi invalidado: não era uma lista global vazia do Photon. O adaptador diagnóstico lia nomes de membros incompatíveis e não consumia o campo público tipado `RegionInfo.RegionPing` do SDK 2.1.1. A correção usa diretamente `RegionInfo.RegionCode` e `RegionInfo.RegionPing`, sem reflection, e separa falha global de descoberta, região não oferecida e ping individual inválido. Se a descoberta global vier vazia ou falhar, a sonda interrompe após a primeira tentativa e não atribui perda a cada região.

Os valores acima são os retornados pela API de descoberta regional do Fusion; não representam packet loss de uma sessão de gameplay. Nenhum IP público, Player ID, App ID, token, serial ou endpoint foi registrado.

### Limitação metodológica confirmada no SDK instalado

O Fusion 2.1.1 mantém internamente `_cachedRegionInfo` e `_lastRegionRequestTime`; o metadado instalado define `REGION_INFO_CACHE_TIME` em 10 segundos. A API pública `NetworkRunner.GetAvailableRegions(appId, cancellationToken)` não oferece parâmetro documentado para ignorar ou invalidar esse cache. Assim, chamadas sucessivas dentro dessa janela podem devolver o mesmo snapshot e não comprovam amostras independentes. Forçar o cache exigiria API interna ou reflection, opções não usadas nesta fundação.

Por isso, as repetições da execução diagnóstica foram descartadas metodologicamente. Não são calculados média, mediana, P95, jitter, variação ou perda a partir delas. A ferramenta agora faz uma única descoberta regional e mantém a agregação temporal separada, reservada a heartbeats independentes de uma sessão Photon real.

## Recomendação provisória

**Recomendação provisória para a rede medida em Brasília: `sa`.** O snapshot indicou aproximadamente 21 ms, contra 171–172 ms nas alternativas avaliadas. Isso não substitui mediana/P95 de heartbeat. A configuração do aplicativo não foi alterada e nenhuma região foi forçada. A recomendação definitiva aguarda o heartbeat e as duas localidades adicionais.

## Próximas localidades e procedimento reproduzível

Permanecem pendentes medições reais em pelo menos duas localidades, entre São Paulo/SP, Rio de Janeiro/RJ e Recife/PE (ou outra capital do Nordeste, com a substituição identificada).

1. Executar na rede local real, sem VPN e sem simular outra cidade.
2. Inserir o App ID Fusion apenas em `PhotonAppSettings.asset` localmente; nunca em Git, patch, logs ou documentação.
3. Rodar uma vez o teste PlayMode `PhotonRegionLatencyIntegrationTests.BrasiliaDevelopmentNetwork_CapturesRegionalDiscoverySnapshot` com `RKW_RUN_PHOTON_LATENCY=1`, ajustando apenas a localidade declarada no registro.
4. Registrar somente o snapshot sanitizado e a localidade declarada; não repetir `GetAvailableRegions` como se cada retorno fosse uma nova amostra.
5. Para mediana, P95, jitter e perda, abrir uma sessão Photon real na região candidata e executar o heartbeat do protocolo com aquecimento e amostras independentes.
6. Remover o App ID local antes de staging e acrescentar os resultados a este documento. Só então comparar heartbeat e snapshots para uma recomendação brasileira definitiva.

O heartbeat de sessão do protocolo M0-T03 permanece pendente e será executado para a região mensurável de cada localidade, sem mudar o escopo de produção.
