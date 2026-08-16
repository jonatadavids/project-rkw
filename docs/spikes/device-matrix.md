# Spike M0-T05 — Matriz inicial de dispositivos

## Registro da consulta

- Data: **2026-08-16**
- Fontes: páginas oficiais dos fabricantes e documentação oficial Unity/Android.
- Objetivo: registrar a cobertura física disponível e as lacunas que precisam ser preenchidas até o gate de performance do M3, sem impor compra de aparelhos específicos.

## Matriz parcialmente confirmada

| Tier | Dispositivo/cobertura | Disponibilidade | Hardware a registrar no início dos testes | Papel no teste |
|---|---|---|---|---|
| Low Android | Modelo ainda não definido | **Pendente de aparelho emprestado para o gate de performance do M3** | Modelo/SKU, SoC, GPU, memória física e versão do SO | Gate mínimo de 30 FPS, memória e estabilidade térmica |
| Mid Android | Modelo ainda não definido | **Empréstimo ou aparelho de piloto/testador** | Modelo/SKU, SoC, GPU, memória física e versão do SO | Meta de 60 FPS e cobertura de hardware intermediário real |
| High Android | Samsung Galaxy S25 | **Disponível** | Variante/SKU exato, SoC, GPU, memória física e versão instalada do SO | Qualidade High, Vulkan, thermal e performance |
| High iOS | iPhone 17 | **Disponível** | Modelo/SKU exato, SoC, GPU, memória reportada e versão instalada do iOS | Qualidade High, ProMotion, thermal e performance |

O nome comercial não determina sozinho a configuração efetiva. No início dos testes, registrar o modelo/SKU exato e a memória observada no build por `SystemInfo`, além de conferir a ficha oficial correspondente à variante. A Apple não divulga RAM física na ficha técnica pública do iPhone 17; o valor operacional será o reportado pelo aparelho/build.

## Justificativa

- Galaxy S25 e iPhone 17 confirmam cobertura física high-tier nas duas plataformas.
- O gate mínimo depende de um Android low-tier real e não pode ser inferido a partir dos aparelhos high-tier.
- Um Android mid-tier emprestado ou de piloto/testador é suficiente, desde que sua identificação e seus resultados sejam documentados.
- Pixel 8, Galaxy A35, iPhone 13 e iPhone 15 Pro deixam de ser requisitos de aquisição; não é necessário comprá-los.

## Protocolo de confirmação de hardware

Antes do primeiro teste real:

1. confirmar número exato do modelo/SKU, memória reportada e versão instalada do SO;
2. registrar `SystemInfo.deviceModel`, `operatingSystem`, `processorType`, `graphicsDeviceName`, `graphicsMemorySize` e `systemMemorySize`;
3. registrar separadamente RAM física/reportada e qualquer recurso de RAM virtual; RAM virtual não substitui a física na classificação;
4. testar em temperatura ambiente documentada, sem carregamento e com bateria entre 40% e 80%;
5. repetir uma corrida de 30 minutos e registrar FPS médio/P1, memória máxima, thermal status e bateria;
6. usar perfil Low no Android low-tier selecionado; Auto e override manual nos demais.

## Disponibilidade e plano de aquisição

| Prioridade | Necessidade |
|---:|---|
| P0 | Usar Samsung Galaxy S25 e iPhone 17 já disponíveis para cobertura high-tier |
| P0 | Obter por empréstimo um Android low-tier antes do gate de performance do M3 |
| P1 | Obter por empréstimo ou via piloto/testador um Android mid-tier antes da validação correspondente no M3 |
| P2 | Usar device farm apenas como complemento; thermal, touch e áudio exigem aparelho físico |

Não há autorização nem necessidade registrada para comprar Pixel 8, Galaxy A35, iPhone 13 ou iPhone 15 Pro. Os modelos Android low/mid podem variar conforme empréstimo ou disponibilidade de pilotos/testadores, desde que o tier, o SKU e as especificações observadas sejam documentados.

## Critérios por tier

| Tier | Critério obrigatório/meta |
|---|---|
| Low Android | ≥ 30 FPS sustentados por 30 min; RAM app ≤ 512 MB |
| Mid Android | Meta 60 FPS; sem glitches de áudio; RAM app ≤ 1 GB |
| High Android/iOS | Meta 60 FPS; qualidade High; RAM app ≤ 1,5 GB |

## Conclusão de Q-TS-01

**Matriz parcialmente confirmada na revisão humana de 2026-08-16.** Samsung Galaxy S25 e iPhone 17 estão disponíveis como high-tier. Android low-tier e mid-tier permanecem pendentes para o gate de performance do M3, por empréstimo ou aparelho de piloto/testador.

## Fontes oficiais

- [Samsung Brasil — especificações Galaxy S25 e S25+](https://www.samsung.com/br/smartphones/galaxy-s25/specs/)
- [Apple Brasil — especificações do iPhone 17](https://www.apple.com/br/iphone-17/specs/)
- [Unity 6.3 LTS (`6000.3.22f1`) — requisitos de sistema](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html)
- [Android GPU Inspector](https://developer.android.com/agi)
