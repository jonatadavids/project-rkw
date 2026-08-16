# Spike M0-T01 — Limites e custo do UGS Cloud Save

## Registro da consulta

- Data: **2026-08-16**
- Escopo: Cloud Save Data e Player Files; preços em USD, sem impostos e sem conversão cambial.
- Fontes: exclusivamente documentação e páginas oficiais da Unity, listadas ao final.

## Limites confirmados

| Recurso | Limite oficial |
|---|---:|
| Cloud Save Data por jogador e por access class | 5 MiB somados entre todas as chaves |
| Chaves por jogador e por access class | 2.000 |
| Player Files por jogador | 1 GB somado |
| Arquivos por jogador | 200 |
| Rate limit da API Cloud Save | 600 requisições/minuto por jogador |
| Batch comum | até 20 itens |
| Batch administrativo entre access classes | até 100 itens |

As classes `default`, `public` e `protected` têm limites próprios. Isso não deve ser usado para fragmentar artificialmente um documento grande: os dados devem ser separados por autoridade e necessidade de acesso.

## Preços confirmados

| Métrica mensal | Franquia gratuita | Excedente |
|---|---:|---:|
| Dados armazenados | 5 GiB | US$ 0,50/GiB |
| Escritas | 1.000.000 | US$ 0,025/10.000 |
| Leituras | 1.000.000 | US$ 0,009/10.000 |

UGS usa pay-as-you-go. Sem forma de pagamento cadastrada, ultrapassar a franquia pode bloquear os serviços/APIs do projeto; monitorar `Administration > Service Usage` e configurar billing antes de produção.

## Estimativa do Project RKW

Orçamento conservador por jogador:

| Dado | Orçamento |
|---|---:|
| Perfil, licenças, XP e estatísticas resumidas | 12 KiB |
| Settings e layout de controles | 4 KiB |
| Progresso da escola e melhores tempos resumidos | 8 KiB |
| Metadados do ghost local | 2 KiB |
| Folga para schema/versionamento | 6 KiB |
| **Total planejado** | **32 KiB/jogador** |

O ghost completo permanece local no MVP; apenas seus metadados entram no Cloud Save. Histórico detalhado de corridas e telemetria não deve crescer indefinidamente dentro do perfil.

### Custo por população

Hipótese operacional para estimar I/O: 30 sessões por MAU/mês, com 2 leituras e 2 escritas de Cloud Save por sessão. Os números não incluem Economy, Cloud Code ou Analytics.

| Jogadores ativos | Storage estimado | Leituras/mês | Escritas/mês | Custo Cloud Save estimado/mês |
|---:|---:|---:|---:|---:|
| 1.000 | 0,031 GiB | 60.000 | 60.000 | US$ 0,00 |
| 10.000 | 0,305 GiB | 600.000 | 600.000 | US$ 0,00 |
| 100.000 | 3,052 GiB | 6.000.000 | 6.000.000 | **US$ 17,00** |

Para 100 mil MAU, a estimativa é US$ 12,50 em escritas e US$ 4,50 em leituras; o storage ainda fica dentro dos 5 GiB gratuitos. É uma projeção, não cotação contratual.

## Recomendação

1. Usar poucas chaves versionadas: `profile`, `settings`, `school_progress` e `ghost_metadata`.
2. Manter progressão/economia protegida e escrita pelo servidor; settings podem usar acesso default e write locks.
3. Salvar por evento relevante e com debounce, nunca a cada frame/volta parcial.
4. Manter ghost binário local no MVP. Se houver cloud ghost no Alpha/Beta, avaliar Player Files separadamente.
5. Definir budgets de 32 KiB/jogador e 120 escritas/mês/jogador como alertas iniciais.
6. Instrumentar bytes, leituras, escritas, respostas `429` e retries antes do Alpha.

## Conclusão de Q-BD-01

**Resolvida e aprovada para planejamento:** 5 MiB e 2.000 chaves por jogador/access class são suficientes com ampla folga. A estratégia de 32 KiB/jogador foi aprovada na revisão humana de 2026-08-16. O risco principal não é capacidade por jogador, mas frequência agregada de leitura/escrita.

## Fontes oficiais

- [Unity Cloud Save — limites de Player Data](https://docs.unity.com/en-us/cloud-save/concepts/player-data)
- [Unity Cloud Save — limites de Files](https://docs.unity.com/cloud-save/concepts/files)
- [Unity Cloud Save — limite de requisições da REST API](https://docs.unity.com/en-us/cloud-save/tutorials/rest-api)
- [UGS Pricing — Cloud Save](https://unity.com/products/gaming-services/pricing)
- [Unity — Pricing and billing](https://docs.unity.com/en-us/services/pricing-and-billing)
