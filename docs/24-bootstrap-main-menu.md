# 24 — Bootstrap e Main Menu placeholder (M1-T06)

## Escopo

- Data da implementação e das consultas: **2026-08-16**.
- Unity: **6000.3.22f1**.
- UI: **uGUI 2.0.0**, já presente no projeto.
- Serviços remotos consumidos: somente Authentication anônima do UGS no ambiente **`development`**.

Não foram adicionados pacotes. Localization, Remote Config, perfil, Cloud Save no fluxo de menu, Photon, matchmaking, gameplay, garagem e escola permanecem fora desta tarefa. A composição é manual e não usa container de DI.

Fontes oficiais consultadas em 2026-08-16:

- [Unity Manual — comparação dos sistemas de UI](https://docs.unity3d.com/6000.0/Documentation/Manual/UI-system-compare.html)
- [Unity Scripting API — LoadSceneAsync](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)
- [Unity Scripting API — LoadSceneMode.Additive](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SceneManagement.LoadSceneMode.Additive.html)
- [Unity Scripting API — Screen.safeArea](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Screen-safeArea.html)

## Fluxo e cenas

`Bootstrap` é a primeira cena nos Build Settings e permanece carregada. Ela cria manualmente o serviço de autenticação, solicita login anônimo e, após sucesso, carrega `MainMenu` com `LoadSceneMode.Additive`. A cena `MainMenu` passa a ser a cena ativa, sem descarregar `Bootstrap`.

Uma única câmera ortográfica mínima pertence à cena `Bootstrap`, limpa o display com a mesma cor de fundo do menu e não renderiza nenhuma layer. Como `MainMenu` não contém câmera e o carregamento é idempotente, o fluxo additive mantém exatamente uma câmera ativa, sem custo relevante de geometria, sombras, pós-processamento, HDR, MSAA ou occlusion culling.

Em falha ou timeout propagado pela fundação UGS, a tela mostra apenas a mensagem segura “Não foi possível conectar. Tente novamente.” e o botão `TENTAR NOVAMENTE`. Nenhum Player ID, token, exceção detalhada ou dado da conta aparece na UI. O Retry reutiliza o serviço existente e o carregamento é idempotente, impedindo uma segunda instância de `MainMenu`.

## Interface provisória

O menu usa somente `Image`, `Text` e `Button` do uGUI, com a fonte runtime interna do Unity. Não há arte, fonte ou asset externo.

- `KARTGRID` é exibido como **nome comercial provisório** do protótipo. Continua sujeito à pesquisa de marca, domínio e disponibilidade nas lojas e não altera Product Name, Bundle ID, projeto UGS ou qualquer cadastro externo;
- `JOGAR`, `ESCOLA` e `GARAGEM` são stubs grandes para toque;
- qualquer clique mantém as cenas atuais e exibe “Disponível em breve”;
- `PROJECT RKW • PROTÓTIPO DEV` identifica discretamente o ambiente técnico;
- `SafeAreaFitter` converte `Screen.safeArea` em anchors normalizados sem alocação contínua;
- `CanvasScaler` usa referência 1920×1080 e escala por largura/altura;
- Portrait e Portrait Upside Down estão desabilitados; Landscape Left e Landscape Right estão habilitados.

Foram exercitados layouts landscape representativos de 2340×1080 (Galaxy S25) e 2556×1179 (iPhone 17). A confirmação física nos aparelhos continuará sendo evidência humana; nenhum SKU ou dado do dispositivo foi inferido ou gravado pela UI.

## Estrutura

- `Assets/Scenes/Bootstrap.unity`: aplicação, EventSystem/Input System e prefab de status;
- `Assets/Scenes/MainMenu.unity`: prefab visual do menu;
- `Assets/RKW/UI/Prefabs/BootstrapStatusView.prefab`;
- `Assets/RKW/UI/Prefabs/MainMenuView.prefab`;
- `RKW.UI`: controllers, safe area e coordenação de cena;
- `IAuthenticationService`: contrato mínimo no `RKW.Backend`, criado porque o Bootstrap é seu primeiro consumidor.

A cena de template `SampleScene` foi substituída. Os perfis URP compartilhados foram preservados porque continuam referenciados pelos render pipeline assets.

## Evidências

Execução em 2026-08-16:

| Verificação | Resultado |
|---|---|
| Compilação C# | Sucesso; nenhum erro ou warning C# novo |
| EditMode | 32/32 passaram |
| PlayMode local | 13/13 executados passaram; 5 integrações/captura condicionais ignoradas; Retry, destruição durante autenticação pendente e câmera única após carga/repetição cobertos |
| Bootstrap com UGS real | 1/1 passou; Authentication e cena additive confirmadas em `development` |
| Android compile check | Sucesso; nenhum aplicativo gerado |
| iOS compile check | Sucesso; nenhum projeto Xcode, signing ou aplicativo gerado |
| Captura | PNG 2340×1080, SHA-256 `22334130ac8aff10ce23d9d770322acb1814d62843fb25ade1b41315cd3575a0` |

A captura está em `/tmp/rkw-m1-t06-main-menu.png`, fora do Git. Ela contém somente os textos estáticos do menu e nenhuma informação sensível.

Durante a validação, uma repetição do PlayMode encontrou um crash nativo isolado no compilador Burst antes dos testes. O runner deixou uma cena temporária, que foi removida do projeto. As execuções finais passaram com Burst desativado somente no processo do Test Runner; não houve alteração permanente nas configurações do projeto ou dos builds.
