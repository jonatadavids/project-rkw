#!/bin/bash
# Round 39 (continuação 5/6): o build_deploy_verify.sh tira a "foto" do log
# do celular rápido demais -- ele abre o jogo, espera só 5 segundos, e já
# salva o log ali (rkw_logcat.txt). Isso é tempo suficiente pra abrir o
# app, mas nunca pra escolher pista, apertar "sozinho" e começar a
# corrida -- então os registros de diagnóstico que interessam (a
# reposição pra pole, etc.) nunca chegam a aparecer nesse arquivo.
#
# Este script separado NÃO mexe no build_deploy_verify.sh -- é só um
# atalho extra: rode ele DEPOIS de já ter jogado (celular ainda ligado no
# cabo USB), e ele salva tudo que ainda está guardado na memória de log do
# celular até aquele momento, incluindo o que aconteceu enquanto você
# jogava.
#
# Como usar:
#   1. Abra o jogo, jogue normalmente (modo sozinho, do jeito que quiser).
#   2. Sem fechar o jogo (ou logo depois de fechar), rode:
#        bash scripts/capture_logcat.sh
#   3. Me manda o arquivo que ele cria: rkw_logcat_manual.txt

ADB="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
PROJECT="/Users/jonathan/Documents/git/kart/kit_inicial_kiro_kart_rental"

"$ADB" logcat -d -s Unity:V *:S > "$PROJECT/rkw_logcat_manual.txt"
echo "Log salvo em: $PROJECT/rkw_logcat_manual.txt"
