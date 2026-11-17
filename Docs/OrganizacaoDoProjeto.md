# Organização do projeto

Revisão: 17/11/2026. Nenhum asset foi excluído.

| Pasta | Finalidade |
| --- | --- |
| `Assets/Art/Player`, `Enemies`, `Environment` | Arte específica do jogo |
| `Assets/Art/ThirdParty` | Pacotes importados: KenneyFantasyUI, MaleAdventurer e TinyRPG, com seus arquivos de licença |
| `Assets/Resources` | Arte e fontes carregadas dinamicamente; preservar os caminhos usados em `Resources.Load` |
| `Assets/Scenes` | Cena principal `SampleScene.unity`, habilitada no build |
| `Assets/Archive/RecoveryScenes` | Quatro cópias de recuperação, fora da lista de cenas do build |
| `Assets/Prefabs` | Prefabs separados por função |
| `Assets/Scripts` | Código de jogo separado por função |
| `Assets/Editor` | Ferramentas exclusivas do editor |
| `Assets/Settings/Input` | Configuração do Input System |
| `Assets/Settings/Rendering` | Perfil de volume e configurações globais de renderização |
| `Assets/Settings` | Demais configurações e templates do Unity |
| `Assets/TextMesh Pro` | Recursos do pacote TMP; mantidos na estrutura original |
| `Tools/ArtSources/Packs/DreadRooms3` | Pacote fonte do cenário, incluindo formatos alternativos e licença |
| `Tools/ArtSources` | Ferramentas e documentação de geração de arte |
| `Tools/ArtBackups` | Imagens originais e versões anteriores |
| `Tools/Validation` | Verificações e auditoria estática |
| `Docs` | Documentação e relatório de candidatos sem referência |

## Possíveis arquivos sem uso

A lista detalhada está em `AssetsSemReferencia.csv`. Ela indica ausência de referências GUID externas e de caminhos literais detectados, não comprova que um arquivo pode ser apagado. Referências construídas dinamicamente, ferramentas externas e dependências internas de pacotes exigem revisão manual. Também pode haver grupos inteiros sem uso que se referenciam entre si e não aparecem nesta lista.

- `Assets/Art/Enemies/Ghost.png`: sem referência GUID externa detectada; candidato de arte antiga.
- `Assets/Art/ThirdParty/KenneyFantasyUI`: muitos arquivos originais sem referência direta. A interface atual carrega cópias selecionadas de `Resources/UI/Fantasy`.
- `Assets/Art/ThirdParty/MaleAdventurer`: vários sprites e animações sem referência direta; preservados como biblioteca de arte.
- `Assets/Art/ThirdParty/TinyRPG`: parte do pacote não tem referências detectadas. O pacote também contém o Demon_A usado pelo projeto e pela validação; não remover a pasta inteira.
- `Assets/Archive/RecoveryScenes`: quatro backups sem participação no build atual. Podem ser úteis para recuperar trabalho.
- `Assets/Settings/Lit2DSceneTemplate.scenetemplate`: template de criação de cenas; utilidade de editor, não conteúdo da partida.
- `Assets/Art/móveis`: pasta vazia, preservada.

## Materiais que não são lixo

- `Tools/ArtSources/Packs/DreadRooms3` não é importado pelo Unity, mas `BuildDreadFloor.ps1` usa seus tiles para reconstruir o piso. Outras resoluções e formatos são fontes opcionais.
- `Tools/ArtBackups` contém backups intencionais.
- Licenças, documentação, scripts, shaders e recursos sob `Resources` ou `Editor` não são classificados automaticamente como descartáveis.
- `Library`, `Temp`, `Logs`, `UserSettings`, `.csproj` e `.slnx` são gerados pelo ambiente. Não foram removidos durante esta organização.

## Ferramenta antiga a revisar

`RiftbornVisualSetupEditor.cs` aguarda quadros em `Assets/Art/Player/Frames`, pasta atualmente ausente. O script `Tools/prepare_player_sprites.py` pode gerar esses quadros a partir da imagem fonte ainda existente. Essa é uma preparação antiga, não uma prova de código inútil; os dois arquivos foram preservados. As notas em `Assets/Art/ART_NOTES.md` descrevem esse fluxo anterior; o piso atual está documentado em `Tools/ArtSources/DreadFloor.md`.

## Validação e manutenção

- Os 592 GUIDs originais foram preservados e não há GUID duplicado após as mudanças.
- Os arquivos `.meta` acompanharam os assets movidos.
- Os caminhos explícitos das ferramentas de geração do piso e validação do Gunter foram atualizados.
- A cena principal e os caminhos de `Resources` foram mantidos.
- Verificação estática realizada; esta organização não foi validada em Play Mode.
- Para atualizar o CSV: `python Tools/Validation/audit_assets.py`, a partir da raiz do projeto.
