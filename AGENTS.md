# AGENTS.md — Regras para Agentes de IA (Codex)

## Propósito

Este documento define regras duráveis que qualquer agente de IA (Codex, Kiro, Gemini) deve seguir ao trabalhar neste repositório. Violações destas regras são bloqueantes para merge.

---

## Regras Gerais

### 1. Ler Antes de Editar

- **SEMPRE** ler `README.md`, `AGENTS.md`, `docs/00-index.md` e os ADRs relevantes antes de iniciar qualquer trabalho.
- **SEMPRE** ler a história do backlog (`docs/18-product-backlog.md`) que está sendo implementada, incluindo critérios de aceite e dependências.
- **NUNCA** assumir comportamento sem confirmar na documentação.

### 2. Uma História por Branch/PR

- Cada Pull Request implementa **exatamente uma** user story.
- Branch naming: `feature/US-{ID}-{slug}` (ex: `feature/US-010-physics-base`).
- Nenhum PR deve misturar múltiplas histórias.

### 3. Nunca Alterar Arquitetura Silenciosamente

- Mudanças arquiteturais (nova dependência, novo serviço, mudança de modelo de rede) **exigem ADR** aprovado.
- Se durante implementação uma decisão arquitetural parecer necessária, **parar e pedir aprovação** antes de implementar.

### 4. Sem Segredos no Repositório

- **NUNCA** commitar: API keys, tokens, passwords, certificates, keystores, .env files com valores reais.
- Usar variáveis de ambiente ou secret managers.
- Se um arquivo contém segredo acidentalmente, **alertar imediatamente** e reverter.

### 5. Sem Pacote Unity sem ADR

- **NUNCA** adicionar pacote Unity (via Package Manager ou .unitypackage) sem ADR ou aprovação explícita.
- Exceção: packages já aprovados nos ADRs existentes.

### 6. Manter Builds Android e iOS

- Após qualquer mudança, **verificar** que builds Android e iOS compilam sem erros.
- Se um build quebra, **corrigir antes de commitar** ou reverter.

### 7. Criar Testes

- Lógica pura (física, regras, economia, serialização): **EditMode tests obrigatórios**.
- Fluxos de jogo (corrida, checkpoints, voltas, bandeiras): **PlayMode tests**.
- Serialização/parse: **Property-based round-trip test obrigatório**.
- Economia: **Invariante: saldo ≥ 0 sempre**.

### 8. Executar Testes e Registrar Evidências

- Rodar todos os testes antes de submeter PR.
- PR description deve incluir output dos testes ou screenshot de evidência.
- Nenhum teste existente pode ser removido sem justificativa.

### 9. Manter Performance Budgets

- Respeitar budgets definidos em `docs/12-art-audio-performance.md`.
- Se uma mudança ultrapassa o budget (draw calls, triângulos, memória, frame time), **otimizar antes de submeter**.
- Incluir profiling screenshot se mudança afeta performance.

### 10. Sem Marcas/Assets sem Licença

- **NUNCA** usar nomes, logos, traçados de pistas ou pinturas de marcas reais sem licença.
- **NUNCA** incluir assets de terceiros sem verificar licença (verificar asset store license, CC license, etc.).
- Pistas devem ser fictícias no MVP.

### 11. Sem Pay-to-Win

- **NUNCA** implementar item ou mecânica que dê vantagem competitiva em troca de pagamento.
- Monetização é exclusivamente cosmética no multiplayer equalizado.
- Qualquer mudança de economia deve respeitar `docs/10-progression-economy.md` e `docs/11-monetization-liveops.md`.

### 12. Não Confiar em Dados do Cliente

- Moeda, inventário, resultado de corrida e ranking são **server-authoritative**.
- **NUNCA** implementar lógica que permita ao client incrementar moeda, alterar posição final ou modificar inventário diretamente.
- Toda operação sensível deve passar por Cloud Code ou backend.

### 13. Preservar Acessibilidade

- Manter controles configuráveis (posição, tamanho, sensibilidade).
- Respeitar modo canhoto, alto contraste, redução de movimento.
- Novos elementos de UI devem seguir guidelines de `docs/05-controls-accessibility.md`.

### 14. Atualizar Documentação

- Se o comportamento implementado difere da documentação, **atualizar a documentação no mesmo PR**.
- Novos parâmetros de ScriptableObjects devem ser documentados em `docs/04-driving-physics.md`.
- Novas regras devem atualizar `docs/06-race-rules-flags.md`.

### 15. Definição de "Done"

Uma história está **done** quando:

1. ✅ Critérios de aceite (Given/When/Then) passam.
2. ✅ Testes automatizados escritos e verdes.
3. ✅ Nenhuma regressão na suite existente.
4. ✅ Performance dentro do budget.
5. ✅ Diff revisado (pelo fundador ou self-review documentado).
6. ✅ Documentação afetada atualizada.
7. ✅ Build Android e iOS compilam.

---

## Workflow por Tarefa

```
1. Ler docs relevantes (README, AGENTS, ADRs, história)
2. Criar branch: feature/US-{ID}-{slug}
3. Implementar com testes
4. Rodar testes locais
5. Verificar build
6. Verificar performance (se aplicável)
7. Atualizar docs (se comportamento mudou)
8. Submeter PR com:
   - Descrição da história
   - Output de testes
   - Riscos identificados
9. Aguardar revisão humana
```

---

## Prompt de Handoff para o Codex

> Leia `README.md`, `AGENTS.md`, `docs/00-index.md`, todos os ADRs e a primeira história Ready do backlog. Antes de editar, produza um plano curto, identifique riscos e confirme os testes que provarão os critérios de aceite. Implemente somente essa história em uma branch própria. Execute os testes aplicáveis, revise o diff e atualize a documentação afetada. Não prossiga para a próxima história sem minha aprovação.

---

## Contato para Decisões Bloqueantes

Se uma decisão bloqueante surgir durante implementação, **parar e registrar** em `docs/20-open-questions.md` com:
- ID da questão
- Contexto
- Opções possíveis
- Recomendação

Aguardar aprovação do fundador antes de prosseguir.
