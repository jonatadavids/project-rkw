# ADR-0001: Engine de Jogo

## Status

**Aceito**

## Contexto

O projeto precisa de uma engine para jogo mobile 3D multiplayer publicado em Android e iOS a partir de uma única base de código. O fundador não é desenvolvedor de jogos; a engine precisa ter ecossistema maduro, documentação rica e compatibilidade com agentes de IA para geração de código.

## Alternativas Consideradas

| Engine | Prós | Contras |
|---|---|---|
| **Unity 6.3 LTS (`6000.3.22f1`)** | C#, ecossistema mobile maduro, URP, Photon Fusion SDK, UGS, vasta documentação, suporte a agentes IA | Licenciamento controverso (resolvido 2024+), performance < engines nativas para AAA |
| Unreal Engine 5 | Gráficos AAA, Blueprints | C++ complexo, mobile não é forte, overhead para projeto indie/mobile, menos docs para mobile |
| Godot 4 | Open source, leve | Ecossistema multiplayer imaturo, 3D mobile menos testado, menos SDKs mobile |
| Custom (Kotlin/Swift) | Controle total | Requer expertise de jogo; multiplayer do zero; inviável para 1 pessoa |

## Decisão

**Unity 6.3 LTS (`6000.3.22f1`)** é adotado como engine do projeto.

## Justificativa

1. **Mobile-first:** URP otimizado para mobile; target Android/iOS na mesma base.
2. **Ecossistema:** Photon Fusion 2, UGS, AdMob, Unity IAP — tudo com SDKs nativos.
3. **C#:** Linguagem produtiva, tipada, compatível com geração por agentes de IA.
4. **Comunidade:** Maior base de conhecimento para mobile game dev; facilita debugging.
5. **Build Automation:** Unity Build Automation para CI/CD Android/iOS.
6. **LTS:** Suporte longo reduz risco de breaking changes.

## Impacto

- Todo o desenvolvimento será em C# com Unity Editor.
- Assets devem ser compatíveis com URP.
- Dependência do pricing de Unity (verificar tier para receita anual).

## Custo

- Unity Pro necessário se receita > $100K/ano (verificar threshold atual).
- Custo de Unity Build Automation por build.

## Riscos

| Risco | Mitigação |
|---|---|
| Mudança de pricing Unity | Monitorar; plano de saída para Godot se necessário |
| Bug em Unity LTS | Usar patches estáveis; reportar bugs |
| Deprecação de features | Seguir release notes; adaptar proativamente |

## Plano de Saída

Se Unity se tornar inviável:
1. Godot 4 com GDScript ou C# como alternativa mais próxima.
2. Exportar assets (FBX, PNG, WAV são portáveis).
3. Lógica de jogo em C# pode ser parcialmente portada.
4. Networking: Photon suporta múltiplas engines; migração possível.

## Referências

- Unity 6.3 LTS (`6000.3.22f1`) Roadmap
- Unity Pricing 2024+
- Photon Fusion 2 + Unity docs
