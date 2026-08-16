# ADR-0004: Rendering Pipeline

## Status

**Aceito**

## Contexto

O jogo é mobile-first com visual 3D semi-realista. Precisa de pipeline gráfico eficiente para atingir 30 FPS em Android modesto e 60 FPS em intermediário.

## Alternativas Consideradas

| Pipeline | Prós | Contras |
|---|---|---|
| **URP (Universal Render Pipeline)** | Otimizado para mobile, Shader Graph, performance previsível, ampla documentação | Menos features que HDRP; limitações de iluminação |
| HDRP | Gráficos avançados | Muito pesado para mobile; não suportado em Android/iOS modestos |
| Built-in Render Pipeline | Familiar, simples | Deprecated direction; sem Shader Graph moderno; menos otimizações |

## Decisão

**Universal Render Pipeline (URP)** como render pipeline do projeto.

## Justificativa

1. **Mobile-first:** Projetado para performance em dispositivos com GPU limitada.
2. **Shader Graph:** Permite criação visual de shaders sem HLSL manual.
3. **Performance previsível:** Batching, SRP Batcher, GPU instancing built-in.
4. **Forward rendering:** Ideal para mobile; menor overhead que deferred.
5. **Unity 6.3 LTS (`6000.3.22f1`):** URP é o pipeline principal para mobile na versão atual.
6. **Iluminação mista:** Baked + realtime mínimo para sol/sombras dinâmicas leves.

## Impacto

- Todos os materiais usam URP Lit/Unlit/Custom shaders.
- Assets importados devem ser compatíveis com URP.
- Shader Graph para efeitos customizados (vácuo, superfícies).
- SRP Batcher habilitado por padrão.

## Configurações Recomendadas

| Configuração | Valor |
|---|---|
| Rendering Path | Forward |
| SRP Batcher | Habilitado |
| Dynamic Batching | Habilitado (mobile) |
| GPU Instancing | Habilitado |
| Main Light Shadow | Enabled (Low: off; Med/High: on) |
| Additional Lights | 0–2 per-pixel; rest per-vertex |
| HDR | Off (Low); On (Med/High) |
| Post-Processing | Off (Low); Bloom (Med); Bloom+AO (High) |
| Anti-Aliasing | None (Low); FXAA (Med); SMAA (High) |
| Render Scale | 0.7 (Low); 1.0 (Med/High) |

## Riscos

| Risco | Mitigação |
|---|---|
| URP limitações para efeitos futuros | Shader Graph custom; aceitar visual semi-realista |
| Breaking changes em URP updates | LTS = patches estáveis; upgrade cauteloso |
| Performance em GPU Mali (Android baixo) | Profiling early; orçamento agressivo |

## Plano de Saída

Se URP for insuficiente:
1. Custom Scriptable Render Pipeline (extremo; improvável).
2. Render scale dinâmico + LOD agressivo como workaround.
3. Não migrar para HDRP (incompatível com mobile).

## Referências

- Unity URP Documentation
- URP Best Practices for Mobile
- Unity Shader Graph Documentation
