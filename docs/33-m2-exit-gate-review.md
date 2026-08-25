# 33 — Revisão do Exit Gate M2

**Data da revisão:** 2026-08-25  
**Base:** `cbfe2a3784c11629f1d585591a0c04006f8c21de`  
**Natureza:** revisão documental; nenhum teste, build ou serviço externo executado

## Resultado

**Exit Gate M2 aprovado pelo fundador em 2026-08-25, com exceção humana rastreada.**

Todos os critérios técnicos possuem evidência publicada. O único desvio do critério original é a ausência de uma segunda avaliação numérica formal de piloto real de kart. Existe uma avaliação completa, com média 6,5/10 e nenhum critério abaixo de 5, além de playtests informais do fundador e de outras pessoas. Esses relatos informais não são convertidos artificialmente em um segundo formulário.

O fundador aceitou explicitamente a evidência técnica existente, aprovou a exceção de uma avaliação numérica formal em vez de duas e autorizou concluir M2-T21. A aprovação não apaga o critério original: uma segunda avaliação poderá ser anexada futuramente.

## Matriz de evidências

| Critério do gate | Estado | Evidência publicada |
|---|---|---|
| Kart dirigível e física avaliada | Comprovado | `docs/29-kart-dynamics-prototype.md`, `docs/30-founder-playtest-log.md` e `docs/playtests/M2-playtest-01.md` |
| Controles touch em aparelho real | Comprovado | Galaxy S25, validações M2-T01/M2-T14 e log de playtests |
| Ao menos 8 properties de física | Comprovado | 9 properties: P4, P5, P6, P7, P8, P9, P10, P11 e P12 |
| Escola e Rental Sport diferenciáveis | Comprovado | M2-T12/M2-T13 e parâmetros em `KartCategorySO` |
| Asfalto, grama e zebra alteram grip | Comprovado | M2-T07/M2-T08 e validações iterativas no S25 |
| Frenagem reta/esterçada diferenciada | Comprovado | M2-T04/M2-T05/M2-T06 |
| Slipstream funcional | Comprovado | M2-T16/M2-T17 e ligação runtime registrada no log do fundador |
| Recovery por condições seguras | Comprovado | M2-T10/M2-T11; imobilidade aprovada no Galaxy S25 em 2026-08-24 |
| Volta cronometrada e tempo visível | Comprovado | M2-T19 e teste físico no Galaxy S25 em 2026-08-25 |
| Checkpoints, validade e direção da chegada | Comprovado | sequência obrigatória, chegada reversa ignorada e tentativa incompleta sem tempo, validadas no S25 |
| Start/finish funcional | Comprovado | M2-T18/M2-T19 e validação física de 2026-08-25 |
| Média ≥ 6 e nenhum item < 5 | Comprovado | média 6,5; notas 5/7/7/7 em `docs/playtests/M2-playtest-01.md` |
| Aprovação de ao menos 2 pilotos | Exceção proposta | 1 avaliação numérica formal; relatos adicionais permanecem informais e não são contados |

## Pendências que não bloqueiam esta fundação

- Melhorar realismo da perda e recuperação de aderência, cujo critério recebeu nota 5.
- Criar pista fictícia mais técnica para avaliar curvas, frenagem e tangência.
- Evoluir bots, apresentação, ícones e hierarquia de HUD.
- Anexar uma segunda avaliação numérica quando houver oportunidade.

Esses itens permanecem rastreados em tarefas posteriores e no playtest. Eles não invalidam o vertical slice comprovado, mas também não são declarados concluídos por esta revisão.

## Decisão humana registrada

Em 2026-08-25, o fundador declarou explicitamente que:

1. aceita a evidência técnica existente;
2. aprova a exceção de uma avaliação numérica formal em vez de duas neste gate;
3. autoriza marcar M2-T21 como concluída sem reduzir nem remover o critério original.

Com essa decisão, o M2 está formalmente encerrado. Nenhuma tarefa M3 foi iniciada por esta revisão documental.
