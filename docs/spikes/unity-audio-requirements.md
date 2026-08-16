# Spike M0-T07 — Requisitos do Unity Audio nativo no mobile

## Registro da consulta

- Data: **2026-08-16**
- Escopo: Unity 6.3 LTS (`6000.3.22f1`) Audio nativo em Android/iOS; pesquisa e protocolo, sem projeto Unity e sem teste prático em M0.
- Fontes: documentação oficial Unity.

## Findings confirmados

- `DSP Buffer Size` troca latência por desempenho: `Best Latency`, `Good Latency`, `Default` e `Best Performance` devem ser avaliados por dispositivo.
- Buffers menores elevam custo de CPU e risco de underrun/glitch; a Unity não promete uma latência final universal em Android/iOS.
- `Max Real Voices` limita fontes realmente mixadas; as fontes mais audíveis/prioritárias vencem.
- `Max Virtual Voices` controla quantas vozes o sistema acompanha e deve superar a quantidade máxima de vozes ativas.
- `Virtualize Effect` pode desligar efeitos/spatializers de fontes culled para economizar CPU.
- O Audio Profiler expõe total de sources, voices, CPU e memória, mas não inclui todo custo de decodificação em background.
- Audio Mixer suporta grupos, efeitos, snapshots, send/return e ducking; isso cobre o MVP sem middleware.
- Spatial audio custom/HRTF depende de plugin; o 3D/panning nativo básico não exige Wwise/FMOD.

## Arquitetura recomendada para o MVP

Um Audio Mixer com grupos:

```text
Master
├── Engine
├── TiresAndRoad
├── Impacts
├── RaceDirection
├── UI
└── Ambience
```

- Motor: loop 3D com pitch/volume parametrizados, no máximo uma voz principal por kart audível.
- Pneus/zebra/impactos: one-shots com pool e prioridade; evitar criação/destruição de AudioSource.
- UI e Direção de Prova: 2D, prioridade alta; ducking sobre Engine/Ambience.
- Ambiente: loop comprimido/streaming conforme tamanho, prioridade baixa.
- Spatialization: somente fontes do mundo; UI e mensagens críticas permanecem 2D.
- Configuração inicial para teste: 16 real voices, 32 virtual voices, `Default` ou `Good Latency`, `Virtualize Effect` habilitado.

Esses valores são hipóteses de teste, não configuração final.

## Limitações conhecidas

1. Latência varia por aparelho, driver, rota Bluetooth/fone e tamanho do DSP buffer.
2. `Best Latency` pode piorar estabilidade em Android low-tier.
3. Bluetooth adiciona latência fora do controle da Unity; medir alto-falante interno e fone separadamente.
4. Muitos efeitos, spatializers e decodificação simultânea elevam CPU/memória.
5. Não existe evidência oficial de que middleware seja necessário para o escopo MVP.

## Protocolo prático para M1-T13

### Cena de teste

- 4 loops de motor simultâneos;
- 4 loops de pneu/rolagem;
- 2 fontes de zebra;
- pool de 4 impactos;
- 1 ambiente;
- 1 UI e 1 comunicação de direção de prova;
- trajetória de fontes 3D passando ao redor do listener;
- alternância de snapshots Normal/Yellow/Results.

### Matriz

Em M1, executar primeiro no Samsung Galaxy S25 e no iPhone 17 disponíveis. Repetir no Android low-tier e no Android mid-tier quando forem obtidos por empréstimo ou disponibilizados por piloto/testador, obrigatoriamente antes do gate de performance do M3. Testar alto-falante interno; Bluetooth apenas como cenário adicional. Registrar modelo/SKU e memória via `SystemInfo` no início de cada teste.

### Casos

1. `Default`, `Good Latency` e `Best Latency`.
2. 8, 16 e 24 real voices; virtual voices sempre acima do total possível.
3. Spatialization/efeitos ligados e desligados.
4. 10 minutos parado + 20 minutos de corrida simulada.

### Métricas e critérios

| Métrica | Método | Critério inicial |
|---|---|---:|
| Audio CPU | Unity Audio Profiler | < 2 ms no low-tier |
| Audio memory | Profiler + import report | registrar pico; sem crescimento contínuo |
| Trigger → som | vídeo 240 fps/loopback, 30 repetições | documentar P50/P95; sem valor prometido antes da medição |
| Glitches/underruns | contador/log + audição com roteiro | 0 em 30 min |
| Vozes reais/virtuais | Audio Profiler | mensagens críticas nunca virtualizadas |
| GC allocations | CPU Profiler | 0 B/frame no steady state de áudio |
| Ducking | roteiro de comunicação | voz/UI inteligível sobre motor |

Escolher o menor buffer que passe 30 minutos sem glitches e com CPU < 2 ms no low-tier. Se `Best Latency` falhar, usar `Good Latency`/`Default` por tier.

## Decisão Q-AA-02

**Aprovada com condição na revisão humana de 2026-08-16:** Unity Audio nativo será usado no MVP, condicionado ao teste prático em M1. Wwise/FMOD não será adicionado sem novo consumidor, profiling que demonstre insuficiência ou decisão arquitetural aprovada. A configuração final permanece condicionada ao teste M1-T13.

## Fontes oficiais

- [Unity 6.3 LTS (`6000.3.22f1`) — Audio settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-AudioManager.html)
- [Unity 6.3 LTS (`6000.3.22f1`) — Audio Profiler module](https://docs.unity3d.com/6000.0/Documentation/Manual/ProfilerAudio.html)
- [Unity 6.3 LTS (`6000.3.22f1`) — Audio Mixer](https://docs.unity3d.com/6000.0/Documentation/Manual/AudioMixer.html)
- [Unity 6.3 LTS (`6000.3.22f1`) — AudioSource spatialize](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AudioSource-spatialize.html)
