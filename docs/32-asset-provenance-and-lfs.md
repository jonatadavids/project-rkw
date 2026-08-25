# 32 — Proveniência de assets e Git LFS

## Objetivo

Registrar a adoção de Git LFS e separar claramente assets com origem conhecida daqueles que ainda precisam de comprovação antes de qualquer publicação externa. Este registro não concede licença nem substitui os termos do fornecedor.

## Política Git LFS

A partir da US-003, formatos de arte, modelos, áudio, vídeo e pacotes Unity definidos em `.gitattributes` são armazenados como ponteiros Git LFS. A configuração inclui obrigatoriamente FBX, PNG e WAV e também formatos equivalentes usados ou plausíveis no pipeline Unity.

O histórico já publicado não será reescrito automaticamente. Portanto:

- blobs binários de commits anteriores continuam no histórico normal;
- o estado atual desses arquivos é convertido para LFS em um novo commit;
- versões futuras seguem LFS;
- qualquer migração retroativa exige backup, coordenação de todos os clones e autorização explícita para force-push.

`Library/`, `Temp/`, builds, logs, arquivos de assinatura e segredos continuam excluídos pelo `.gitignore`.

## Inventário de proveniência

| Grupo | Localização | Origem registrada | Estado para publicação externa |
|---|---|---|---|
| Kenney Car Kit | `Assets/Art/Kenney/CarKit/` | Identificado no histórico do projeto como Kenney Car Kit, CC0 | Origem conhecida; anexar cópia da licença/URL oficial ao registro antes do release |
| Kenney Racing Kit | `Assets/Art/Kenney/RacingKit/` | Identificado no histórico do projeto como Kenney Racing Kit, CC0 | Origem conhecida; anexar cópia da licença/URL oficial ao registro antes do release |
| Photon Fusion 2 | `Assets/Photon/` | SDK oficial fixado durante M1-T04 | Uso regido pelos termos Photon; não alterar nem redistribuir separadamente sem revisão dos termos |
| RacingKart/KartV2 e peças associadas | `Assets/RKW/Physics/Resources/KartPhysics/Models/` | Arquivos fornecidos pelo fundador durante as rodadas de prototipagem | Autoria/licença ainda precisa de confirmação documental; não liberar externamente como asset reutilizável |
| Ícones e texturas procedurais RKW | `Assets/RKW/Physics/Resources/KartPhysics/Textures/` e código gerador | Produção interna/procedural do protótipo | Uso interno permitido; registrar autoria final antes do release |
| Áudio sintético de validação | `Assets/RKW/Audio/` | Síntese procedural própria, sem conteúdo externo | Não é áudio final; manter identificação como material técnico |

## Regras para novos assets

Antes de adicionar um asset, registrar:

1. nome e versão do pacote/arquivo;
2. autor ou fornecedor;
3. URL de origem;
4. licença e data da consulta;
5. comprovante local quando permitido;
6. restrições de redistribuição, atribuição, marca ou uso comercial.

Assets sem essas informações podem ser usados apenas como material local de avaliação e não devem entrar em builds distribuídos externamente.

## Verificação da US-003

- `git check-attr` deve retornar `filter: lfs`, `diff: lfs` e `merge: lfs` para FBX, PNG e WAV.
- `git lfs ls-files` deve listar os assets rastreados convertidos no estado atual.
- Um clone limpo precisa executar `git lfs pull` para materializar os arquivos completos.
- A configuração não autoriza reescrita retroativa, publicação ou aumento de custos sem nova decisão humana.
