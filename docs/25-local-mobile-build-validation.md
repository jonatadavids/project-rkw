# 25 — Validação local mobile — M1-T07 Etapa A parcial

## Escopo

- Execução: **2026-08-16**.
- Base: `main` em `c54555c46a9718c24406fb50a032c4d10fd6d627`.
- Unity: **6000.3.22f1**.
- Branch: `feature/US-006-local-mobile-build-validation`.
- Ambiente remoto consumido pelo aplicativo: somente Authentication anônima UGS em **`development`**.

Esta é somente a Etapa A local e parcial da M1-T07. O checkbox integral permanece aberto. Não foram configurados Unity Build Automation, triggers, Google Play, Apple Developer, service accounts, cobrança, signing de produção, TestFlight, faixas de teste ou distribuição.

## Suítes locais

| Verificação | Resultado |
|---|---|
| EditMode | 32/32 passaram; 0 falhas |
| PlayMode | 13/13 passaram; 0 falhas; 5 integrações/captura condicionais ignoradas |
| Retry | Coberto deterministicamente pela suíte PlayMode; não foi necessário alterar a conectividade do aparelho |

As execuções do Test Runner usaram Burst desabilitado somente no processo de testes, conforme a mitigação já documentada para o crash nativo isolado. Nenhuma configuração permanente do projeto foi modificada.

## Android local e Galaxy S25

O APK foi gerado como Development build com IL2CPP, somente ABI `arm64-v8a`, debug signing e o Bundle ID provisório `br.com.suitedigital.rentalkartworld.dev`. O manifesto confirmou `minSdkVersion 26`, `targetSdkVersion 36`, aplicação debuggable e orientação `sensorLandscape`. A assinatura foi verificada como Android Debug pelo APK Signature Scheme v2.

| Evidência técnica | Resultado sanitizado |
|---|---|
| APK | 105.007.865 bytes; SHA-256 `3284fc026b6660cc9ad66480668a3286d770dc0a8854c851af1e92ac5d2cef1d` |
| Instalação ADB | Sucesso no único Galaxy S25 autorizado pelo fundador |
| Cold launch | Activity iniciou com status `ok`; menu abriu sem crash |
| Autenticação | MainMenu carregou após Authentication anônima no UGS `development` |
| Câmera | 0 ocorrências sanitizadas de `No cameras rendering` |
| Estabilidade inicial | 0 crashes fatais e 0 exceções não tratadas na janela consultada |
| Aparelho | Samsung Galaxy S25 `SM-S931B`; Android 16; ABI `arm64-v8a` |
| Memória física | 11.380.216 kB (aprox. 10,85 GiB) reportados pelo SO |
| Tela | 1080×2340; densidade física 480 dpi |

Validação humana no aparelho:

- Landscape Left e Landscape Right acompanharam a rotação;
- Portrait permaneceu bloqueado em landscape;
- safe area preservou textos e botões fora das bordas e câmera frontal;
- `JOGAR`, `ESCOLA` e `GARAGEM` mantiveram o menu e exibiram “Disponível em breve”.

A captura sanitizada foi mantida somente em `/tmp/rkw-m1t07a-galaxy-s25-main-menu.png`, com 56.986 bytes e SHA-256 `2f18c9fbe572ca745d865fbd0233deae8d5c764c27b16cceb4b50f5f95bfc517`. Ela não contém Player ID, token, App ID, serial ou dado pessoal.

## Exportação e compile check iOS sem assinatura

O Unity exportou com sucesso um projeto Xcode Development IL2CPP ARM64 para `/tmp/rkw-m1t07a/ios-xcode` (aprox. 1,6 GiB). O `project.pbxproj` exportado tem SHA-256 `e545cab63e9c66c9e2813007261667884f344b57337ff7076d9507d085e2a5b8`.

O compile check utilizou Xcode 26.6 (`17F113`), SDK iOS 26.5, target/scheme `Unity-iPhone`, configuração Debug e destino genérico iOS. `CODE_SIGNING_ALLOWED=NO`, `CODE_SIGNING_REQUIRED=NO` e identidade vazia foram passados explicitamente. O build terminou com exit code 0 e produziu `ProjectRKW.app` não assinado, confirmado por `codesign`.

Foram emitidos 26 warnings em fontes e objetos gerados pelo Unity — APIs deprecated de GameController/UIKit/UnityRendering, asset catalog e objetos IL2CPP sem símbolos — e 0 erros. Nenhum warning veio de fonte RKW. Não houve archive, IPA, provisioning profile, certificado, Apple ID ou upload.

## Higiene e pendências

- APK, projeto Xcode, DerivedData, captura, XMLs e logs brutos permanecem fora do Git em `/tmp`.
- Os logs de dispositivo foram consultados somente por PID e reduzidos a contagens; nenhum Player ID, token, App ID, Android ID, IMEI, serial ou dado pessoal foi documentado.
- O runner de build local foi temporário e removido após gerar os artefatos.
- M1-T07 continua aberta: faltam AAB, Unity Build Automation, gates de CI, triggers e qualquer distribuição que venha a ser autorizada separadamente.
- Nenhuma outra tarefa foi executada ou alterada nesta Etapa A; os estados preexistentes de M1-T08 e M1-T09 foram preservados e M1-T10+ não foi iniciada por esta execução.
