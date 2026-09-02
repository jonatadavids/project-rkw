#!/bin/bash
set -e

UNITY="/Applications/Unity/Hub/Editor/6000.3.22f1/Unity.app/Contents/MacOS/Unity"
PROJECT="/Users/jonathan/Documents/git/kart/kit_inicial_kiro_kart_rental"
ADB="/Applications/Unity/Hub/Editor/6000.3.22f1/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb"
PKG="br.com.suitedigital.rentalkartworld.dev"

echo "=== 0/5 EditMode tests ==="
echo "(feche o Unity Editor antes se ele estiver aberto, senao o batchmode pode falhar por lock do projeto)"
rm -f "$PROJECT/rkw_editmode_results.xml" 2>/dev/null || true
"$UNITY" -batchmode -projectPath "$PROJECT" \
  -runTests -testPlatform EditMode \
  -testResults "$PROJECT/rkw_editmode_results.xml" \
  -logFile "$PROJECT/rkw_tests.log" || true

if [ -f "$PROJECT/rkw_editmode_results.xml" ]; then
  FAILED=$(grep -o 'result="Failed"' "$PROJECT/rkw_editmode_results.xml" | wc -l | tr -d ' ')
  if [ "$FAILED" != "0" ]; then
    echo ""
    echo "!!! $FAILED teste(s) EditMode falharam. Veja $PROJECT/rkw_editmode_results.xml e $PROJECT/rkw_tests.log"
    exit 1
  fi
  echo "EditMode tests OK (0 falhas)."
else
  echo "!!! Nao foi possivel gerar rkw_editmode_results.xml - veja $PROJECT/rkw_tests.log"
  tail -60 "$PROJECT/rkw_tests.log"
  exit 1
fi

# Pequena pausa para o processo do Unity (tests) liberar completamente o
# lock do projeto antes de abrir uma nova instancia para o proximo passo.
sleep 3

echo ""
echo "=== 1/5 PlayMode tests ==="
echo "(estes de fato ligam a fisica do jogo por alguns segundos cada -- pode demorar mais que o EditMode)"
rm -f "$PROJECT/rkw_playmode_results.xml" 2>/dev/null || true
"$UNITY" -batchmode -projectPath "$PROJECT" \
  -runTests -testPlatform PlayMode \
  -testResults "$PROJECT/rkw_playmode_results.xml" \
  -logFile "$PROJECT/rkw_playmode_tests.log" || true

if [ -f "$PROJECT/rkw_playmode_results.xml" ]; then
  FAILED=$(grep -o 'result="Failed"' "$PROJECT/rkw_playmode_results.xml" | wc -l | tr -d ' ')
  if [ "$FAILED" != "0" ]; then
    echo ""
    echo "!!! $FAILED linha(s) marcadas como Failed no XML do PlayMode (pode ser 1 teste so, contado em varios niveis da arvore -- olhe o XML). Veja $PROJECT/rkw_playmode_results.xml e $PROJECT/rkw_playmode_tests.log"
    exit 1
  fi
  echo "PlayMode tests OK (0 falhas)."
else
  echo "!!! Nao foi possivel gerar rkw_playmode_results.xml - veja $PROJECT/rkw_playmode_tests.log"
  tail -60 "$PROJECT/rkw_playmode_tests.log"
  exit 1
fi

sleep 3

echo ""
echo "=== 2/5 Build Android (development) ==="
"$UNITY" -batchmode -quit -nographics \
  -projectPath "$PROJECT" \
  -executeMethod RKW.Editor.BuildHelper.BuildAndroidDevelopment \
  -logFile "$PROJECT/rkw_build.log"

if ! grep -q "BUILD SUCCEEDED" "$PROJECT/rkw_build.log"; then
  echo ""
  echo "!!! BUILD FALHOU - ultimas linhas de $PROJECT/rkw_build.log:"
  tail -60 "$PROJECT/rkw_build.log"
  exit 1
fi
grep "BUILD SUCCEEDED" "$PROJECT/rkw_build.log"

echo ""
echo "=== 3/5 Verificando dispositivo conectado ==="
"$ADB" devices -l

echo ""
echo "=== 4/5 Instalando no Galaxy S25 ==="
"$ADB" install -r /tmp/rkw-dev.apk

echo ""
echo "=== 5/5 Abrindo o app, capturando screenshot e log ==="
"$ADB" logcat -c
"$ADB" shell am force-stop "$PKG" || true
"$ADB" shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1
sleep 5
"$ADB" shell screencap -p /sdcard/rkw.png
"$ADB" pull /sdcard/rkw.png "$PROJECT/rkw_screenshot.png"
"$ADB" logcat -d -s Unity:V *:S > "$PROJECT/rkw_logcat.txt" 2>&1 || true

echo ""
echo "OK — screenshot salvo em: $PROJECT/rkw_screenshot.png"
echo "Log do app salvo em: $PROJECT/rkw_logcat.txt"
echo "Resultados EditMode em: $PROJECT/rkw_editmode_results.xml"
echo "Resultados PlayMode em: $PROJECT/rkw_playmode_results.xml"
echo "Pode avisar o Claude que terminou; ele vai olhar tudo pela pasta conectada."
