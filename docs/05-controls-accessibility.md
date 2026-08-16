# 05 — Controles e Acessibilidade

## Objetivo e Escopo

Especificar layout de controles mobile, modos de input, assistências e diretrizes de acessibilidade para garantir que o jogo seja jogável por todos os públicos.

---

## Layout Padrão

```
┌─────────────────────────────────────────────┐
│                                             │
│   [Volante/Joystick]         [Acelerador]   │
│        (esquerda)            (direita-sup)  │
│                                             │
│                              [Freio]        │
│                              (direita-inf)  │
│                                             │
│           [Olhar atrás]  (centro-sup)       │
└─────────────────────────────────────────────┘
```

---

## Modos de Direção

| Modo | Entrada | Descrição |
|---|---|---|
| Joystick Virtual | Polegar esquerdo | Arraste horizontal; retorna ao centro |
| Volante Virtual | Polegar esquerdo | Giro rotacional; feedback visual |
| Inclinação (Tilt) | Giroscópio | Inclinar dispositivo; sensibilidade configurável |

---

## Pedais

| Pedal | Posição | Comportamento |
|---|---|---|
| Acelerador | Direita superior | Progressivo com rampa temporal (≥ 150 ms para full throttle) |
| Freio | Direita inferior | Proporcional ao toque/área pressionada |
| Coasting | Ambos soltos | Desaceleração natural por atrito e arrasto |

### Acelerador Progressivo

- Sem pressão física real da tela (não presumir force touch).
- Opção A: Rampa temporal — segura = acelera progressivamente ao longo de ~300 ms.
- Opção B: Gesto vertical — posição do dedo no pedal determina intensidade.
- Ambos disponíveis; jogador escolhe nas configurações.

---

## Personalização de Layout

- Todos os elementos de controle são reposicionáveis via drag.
- Todos os elementos são redimensionáveis (escala 50%–150%).
- Configuração salva em Cloud Save por conta.
- Reset para padrão disponível.

---

## Assistências

| Assistência | Padrão | Configurável | Descrição |
|---|---|---|---|
| Direção assistida | Leve | Desligada/Leve/Média/Forte | Suaviza input de direção |
| Frenagem assistida | Leve | Desligada/Leve/Média | Ajuda a não travar pneus |
| Linha ideal | Visível (Escola) | Mostrar/Ocultar/Apenas curvas | Traçado de referência |
| Anti-spin | Ligado | Ligado/Desligado | Previne rodada acidental |
| Auto-recuperação | Ligado | Ligado/Desligado | Corrige saídas de pista leves |

---

## Acessibilidade

### Diretrizes

| Aspecto | Implementação |
|---|---|
| Modo canhoto | Espelha layout completo |
| Zona morta | Configurável 0%–30% |
| Sensibilidade | Slider por modo de input |
| Alto contraste | UI com contraste ≥ 4.5:1 |
| Redução de movimento | Desabilitar shake, bloom excessivo, particles |
| Tamanho de texto | Escalável (padrão/grande/muito grande) |
| Daltonismo | Paletas alternativas para bandeiras e indicadores |
| Háptica | Totalmente desabilitável ou por tipo |
| Áudio | Subtítulos para instruções da Escola; indicadores visuais para sons críticos |

### Conformidade

- Seguir WCAG 2.1 AA onde aplicável a jogos mobile.
- Validação completa requer testes com assistive technologies e expert review.

---

## Háptica

| Evento | Intensidade | Duração | Configurável |
|---|---|---|---|
| Zebra | Média | Contínua enquanto sobre | ✅ |
| Bloqueio de pneu | Forte-breve | 100 ms | ✅ |
| Contato lateral | Forte | 150 ms | ✅ |
| Perda de aderência | Leve pulsante | Enquanto deslizar | ✅ |
| Largada (luzes) | Leve | 50 ms por luz | ✅ |

---

## Requisitos Não Funcionais

| Requisito | Meta |
|---|---|
| Latência de input | < 16 ms (1 frame a 60 FPS) |
| Drag de layout | Feedback visual imediato |
| Persistência | Cloud Save < 500 ms para salvar layout |
| Compatibilidade | iOS 15+ / Android 8+ (API 26+) |

---

## Casos de Borda

- Dispositivo sem giroscópio → Tilt indisponível; fallback para joystick.
- Tela muito pequena (< 5") → Layout compactado; aviso de experiência subótima.
- Multitoque conflitante → Priorizar último toque em cada zona.
- Interrupção (chamada/notificação) → Pausar automaticamente em modo offline; manter input em online.

---

## Decisões Confirmadas

1. Sem aceleração automática como padrão.
2. Três modos de direção disponíveis.
3. Pedais no lado direito.
4. Layout reposicionável e redimensionável.
5. Háptica configurável por tipo de evento.

## Suposições

| ID | Suposição | Validação |
|---|---|---|
| CT-01 | Rampa temporal de 150 ms evita frustração de iniciantes | Teste A/B com variantes 100/150/250 ms |
| CT-02 | Volante virtual é preferido por pilotos reais | Survey + telemetria de uso |
| CT-03 | Assistência "Leve" como padrão equilibra todos os públicos | D7 retenção por grupo |

## Questões Abertas

- Q-CT-01: Suportar controllers Bluetooth (gamepad)?
- Q-CT-02: Feedback visual de intensidade do acelerador (barra/cor)?
- Q-CT-03: Tutorial específico para cada modo de controle?

## Links Relacionados

- [Física](./04-driving-physics.md)
- [GDD](./02-game-design-document.md)
- [Acessibilidade em Testes](./16-test-strategy.md)
