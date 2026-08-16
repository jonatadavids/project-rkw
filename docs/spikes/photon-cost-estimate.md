# Spike M0-T02 — Estimativa de custo Photon Fusion

## Registro da consulta

- Data: **2026-08-16**
- Moeda: USD, sem impostos, câmbio ou suporte premium.
- Fonte: página oficial de preços do Photon Fusion, consultada na data acima.

## Tiers públicos atuais

| Plano | Uso | Preço | Tráfego incluído | Observações |
|---|---|---:|---:|---|
| 20 CCU | Desenvolvimento | Grátis | 60 GB/mês | Development only; cap rígido, sem burst |
| Free 100 CCU | Lançamento | Grátis | 0,3 TB/mês | Um app por cliente; games only; cap rígido |
| 100 CCU | Lançamento | US$ 95 uma vez/12 meses | 0,3 TB/mês | Um app; sem burst |
| 500 CCU | Produção | US$ 125/mês | 1,5 TB/mês | Burst incluído |
| 1.000 CCU | Produção | **US$ 250/mês** | 3,0 TB/mês | Burst incluído |

Excesso de tráfego na região South America custa **US$ 0,10/GB**. CCU é calculado pela soma dos picos por região; não é média mensal.

## Mapeamento por fase

| Cenário | Demanda | Recomendação | Custo base |
|---|---:|---|---:|
| Alpha privado não comercial | até 20 CCU | Plano Development 20 | US$ 0 |
| Beta/lançamento pequeno | até 100 CCU | Free 100, se a conta/app for elegível | US$ 0 |
| Alternativa para 100 CCU | até 100 CCU | Plano one-time de 12 meses | US$ 95/12 meses |
| Crescimento intermediário | até 500 CCU | Plano 500 | US$ 125/mês |
| Lançamento projetado | até 1.000 CCU | Plano 1.000 | **US$ 250/mês** |

O orçamento anterior do repositório (~US$ 95/mês para 100 CCU e ~US$ 295/mês para 500 CCU) está desatualizado em relação à consulta atual.

## Sensibilidade de tráfego

O design estima ~10 KB/s por jogador. Um CCU contínuo por 30 dias consumiria aproximadamente 25,9 GB, muito acima da regra de bolso de 3 GB incluídos por CCU. Na prática jogadores não ficam conectados 24/7, mas o orçamento deve ser validado por horas conectadas e telemetria real.

Exemplo apenas ilustrativo para o plano de 1.000 CCU:

- 3 TB incluídos;
- cada 1 TB adicional em South America custa aproximadamente US$ 102,40;
- tráfego de entrada e saída conta para o limite.

## Recomendação

1. Usar 20 CCU durante desenvolvimento fechado e migrar para Free 100 antes de teste comercial, se elegível.
2. Não contratar 1.000 CCU antecipadamente; criar alerta ao atingir 70% e 85% do cap.
3. Registrar CCU pico por região, bandwidth/jogador e GB/mês desde M5.
4. Planejar **US$ 250/mês + margem de tráfego** para 1.000 CCU, revalidando preço antes de qualquer compromisso.
5. Manter o gate de server authority separado: comprar CCU não resolve as limitações anti-cheat do Shared Mode.

## Conclusão de Q-MP-02

**Decisão humana em 2026-08-16:** Photon Development 20 CCU foi aprovado para desenvolvimento fechado. Nenhuma contratação de Photon 1.000 CCU foi autorizada. O valor pesquisado de US$ 250/mês, com 3 TB incluídos, permanece apenas referência de planejamento e deverá ser revalidado antes de qualquer contratação.

## Fonte oficial

- [Photon Fusion Pricing](https://www.photonengine.com/Fusion/Pricing)
