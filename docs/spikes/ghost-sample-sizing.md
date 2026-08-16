# Spike M0-T06 — Tamanho e frequência de amostras de ghost

## Registro

- Data do cálculo: **2026-08-16**
- Base: `design.md` define amostras gravadas de posição/rotação, 30 Hz, ghost local por `LeaderboardKey` e alvo aproximado tratado neste spike como limite de 50 KiB para o arquivo completo.
- Não houve consulta externa: este spike é cálculo de engenharia sobre a especificação do projeto.

## Formato proposto

### Header versionado

| Campo | Bytes estimados |
|---|---:|
| Magic + schema version + flags | 8 |
| IDs/hashes de ghost e LeaderboardKey | 48 |
| Lap time, sample rate/count e bounds de quantização | 32 |
| CRC/checksum | 4 |
| Reserva de evolução | 36 |
| **Header planejado** | **128** |

### Sample de 10 bytes

| Campo | Codificação | Bytes |
|---|---|---:|
| Posição X/Y/Z | 3 × signed int16 quantizado relativo à origem/bounds da pista | 6 |
| Rotação | smallest-three quaternion comprimido em 32 bits | 4 |
| Timestamp | implícito por índice / 30 Hz | 0 |
| **Total** |  | **10** |

A escala de posição deve garantir erro máximo ≤ 1 cm dentro dos bounds da pista. Se int16 não alcançar essa resolução para todos os eixos, usar origem por setor/chunk ou aumentar apenas o eixo necessário; não reduzir silenciosamente a precisão.

## Cálculos

Fórmulas:

```text
samples = ceil(duração_segundos × 30)
payload = samples × 10 bytes
tamanho_planejado = header_128 + payload + 10% de folga
```

| Volta | Samples | Payload | Com header + 10% | Resultado vs limite de arquivo de 50 KiB |
|---:|---:|---:|---:|---|
| 45 s | 1.350 | 13.500 B | ~14,6 KiB | 29% do alvo |
| 60 s | 1.800 | 18.000 B | ~19,5 KiB | 39% do alvo |
| 90 s | 2.700 | 27.000 B | ~29,1 KiB | 58% do alvo |

Mesmo uma volta de 90 s produz um arquivo planejado abaixo de 50 KiB com boa margem. Para comparação, uma estrutura ingênua de 32 bytes/sample (timestamp float + Vector3 float + Quaternion float) chegaria a ~84,4 KiB em 90 s antes de acomodar toda a estrutura do arquivo e falharia no limite.

## Estratégia de compressão

1. **Obrigatório:** posição quantizada e quaternion comprimido; timestamp implícito.
2. **Opcional após profiling:** delta encoding por sample com escape para keyframe.
3. Keyframe absoluto a cada 1 segundo para limitar corrupção/acúmulo de erro.
4. Compressão de bloco (LZ4/Deflate) somente se reduzir tamanho sem custo perceptível; validar em aparelho low-tier.
5. Sem inputs, forças ou estado físico no ghost MVP: replay é visual e não determinístico.

## Limites e retenção

- Frequência padrão: **30 Hz**.
- Frequência mínima permitida sem nova validação visual: 20 Hz; interpolação obrigatória.
- Hard cap do arquivo completo: **50 KiB por ghost**, incluindo header, payload, checksum e quaisquer outros metadados ou estruturas auxiliares persistidas.
- Máximo: 1 ghost local por `LeaderboardKey` no MVP.
- Se encoding ultrapassar o cap, tentar compressão; se ainda exceder, downsample adaptativo preservando início/fim e curvas, registrando sample rate efetivo.
- Gravação temporária deve ser transacional: escrever arquivo novo, validar checksum e então substituir PB anterior.

## Protocolo de validação futura

Em M4, testar voltas sintéticas de 45/60/90 s e uma volta no hard cap. Medir tamanho, erro posicional máximo, erro angular, tempo de encode/decode, alocação GC e ausência total de colisão/interferência física.

## Conclusão

O alvo de **50 KiB para o arquivo completo** está validado por cálculo e foi aprovado na revisão humana de 2026-08-16 para ghost local em 30 Hz. Esse limite inclui header, payload, checksum e demais metadados. O formato base de 10 bytes/sample produz arquivos planejados de aproximadamente 15–29 KiB para voltas de 45–90 s, deixando margem para evolução dentro do cap.
