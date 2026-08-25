# Estado da rodada 33

**Data da consolidação:** 2026-08-24

**Base avaliada:** `11ce34852adfc5d2828068d240ce20817d856191`

**Natureza:** diagnóstico e rastreabilidade; este documento não transforma protótipos informais em tarefas concluídas.

## Resultado executivo

A rodada 33 está preservada localmente em um commit com 135 arquivos. O projeto compila e as suítes automatizadas atuais passam. A evolução é substancial e jogável, mas o commit reúne trabalho de várias rodadas e não deve servir como precedente para novos commits monolíticos.

Não foi encontrada nova implementação de UGS Cloud Code nesse commit. A expressão “Cloud Code” usada no relato da rodada provavelmente se referia à ferramenta de desenvolvimento; a fundação UGS existente não foi reclassificada.

## Evidências verificadas após o commit

| Verificação | Resultado |
|---|---|
| Integridade Git | `git fsck --full --no-dangling` aprovado; zero objetos temporários marcados como garbage |
| EditMode | 310/310 aprovados |
| PlayMode | 20/20 testes aplicáveis aprovados; 7 integrações ignoradas pelas condições explícitas da suíte |
| Android | APK local ARM64/IL2CPP gerado com sucesso; arquivo com aproximadamente 47,5 MiB |
| iOS | Exportação Unity e compilação Xcode/IL2CPP sem assinatura aprovadas |
| Segredos | Nenhum token, senha, keystore, certificado ou arquivo de assinatura identificado no conteúdo rastreado |
| Worktree ao fim da validação | Limpo |

Os artefatos, logs, resultados XML e runners temporários permaneceram fora do Git. O Xcode emitiu aviso de ícone App Store 1024×1024 ausente e o código Unity ainda contém avisos de APIs obsoletas; nenhum deles impediu a compilação local.

## O que a rodada 33 corrigiu ou consolidou

- O recolor do kart passou a reconhecer `body_primary` no kartv2, preservando os demais materiais, em vez de aplicar uma cor sólida ao modelo inteiro.
- A peça visual antiga `steering_column` passou a ser ocultada quando o conjunto novo é usado.
- As hipóteses atuais de velocidade estão separadas em 55 km/h (Escola), 70 km/h (13 HP) e 85 km/h (18 HP).
- Os dados locais de tempos/ghost mudaram de namespace, deixando resultados de calibrações anteriores fora da leitura atual.
- O repositório passou a conter o kartv2, controles e visuais adicionais, bots, áudio, corrida, HUD, câmera e ghost experimental descritos no log de playtest.

Esses itens descrevem o código existente. A correção visual do kartv2, o giro das rodas e o volante ainda precisam de uma confirmação física específica dirigindo o build posterior à correção da rodada 33.

## Pendências obrigatórias

1. **Ghost e recordes por categoria:** `GhostRecordStore` e `LapRecordStore` ainda usam chaves próprias e não incorporam a categoria/modelo de kart. Um resultado do kart de 85 km/h pode competir com categorias mais lentas. A migração deve usar uma identidade canônica própria e teste de compatibilidade; não será feita dentro desta estabilização documental.
2. **M2-T20/M2-T21:** houve aprovação qualitativa e testes informais, mas faltam notas por critério de pelo menos dois pilotos. Os checkboxes permanecem abertos.
3. **Git LFS:** nenhum asset está atualmente rastreado por LFS. Adotar LFS para novos binários é uma história separada; migrar blobs já publicados exige decisão explícita sobre reescrita de histórico.
4. **Proveniência de assets:** licenças já documentadas, como Kenney CC0, devem ser preservadas. Modelos e texturas novos precisam de registro de origem/autoria antes de publicação externa.
5. **Manutenibilidade:** `KartPhysicsPrototypeBootstrap.cs` concentra responsabilidades demais e deve ser decomposto incrementalmente, sem alterar comportamento, em tarefa própria.
6. **Performance e aparelhos:** Galaxy S25 cobre high-tier. Android low/mid e os critérios formais de performance continuam pendentes conforme os gates existentes.

## Estado Git e recuperação

- `main` local está um commit à frente de `origin/main`; nenhum push foi feito durante esta consolidação.
- Foi criada a referência local `safety/round-33-before-stabilization` apontando para o commit avaliado.
- Locks órfãos e objetos temporários foram colocados em quarentena após backup, sem remover licença Unity, projeto ou dados de jogador.
- O backup técnico desta sessão foi criado em `/tmp`; ele é proteção operacional temporária e não substitui o repositório remoto.

## Próximas histórias recomendadas

1. Revisar e publicar esta estabilização documental.
2. Configurar Git LFS e registrar proveniência dos assets em uma história própria.
3. Separar ghost/recordes por categoria com migração e testes próprios.
4. Decompor o bootstrap em mudanças pequenas e verificáveis.
5. Executar o playtest formal M2-T20 e somente então reavaliar o Exit Gate M2.
