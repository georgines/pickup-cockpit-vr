# CLAUDE.md — meu_primeiro_vr

Instruções de contexto e fluxo de trabalho para o Claude Code neste projeto Unity VR.

---

## Especificações do Projeto

| Campo | Valor |
|---|---|
| **Nome** | meu_primeiro_vr |
| **Engine** | Unity 6000.3.13f1 (Unity 6) |
| **Plataforma alvo** | Android — Meta Quest (OpenXR) |
| **Pipeline de renderização** | URP (`PC_RPAsset`) |
| **Input System** | New Input System |
| **SDK principal** | Meta XR All-in-One SDK v201.0.0 |
| **API Compatibility** | .NET Standard 2.0 |
| **Cena principal** | `Assets/Scenes/minha_cena.unity` |

---

## Hierarquia da Cena (`minha_cena`)

```
minha_cena
├── [BuildingBlock] Camera Rig          # OVRCameraRig principal (Meta XR)
│   ├── TrackingSpace
│   │   ├── LeftEyeAnchor / CenterEyeAnchor / RightEyeAnchor
│   │   ├── LeftHandAnchor
│   │   │   └── [BuildingBlock] Hand Tracking left
│   │   └── RightHandAnchor
│   │       └── [BuildingBlock] Hand Tracking right
│   └── [BuildingBlock] OVRInteractionComprehensive
│       ├── OVRHmd / OVRHands / OVRControllers
│       ├── LeftInteractions / RightInteractions
│       ├── OVRLeftHandVisual / OVRRightHandVisual
│       ├── OVRLeftControllerVisual / OVRRightControllerVisual
│       └── Locomotor
│           ├── PlayerController (tag: Player, layer: 4)
│           ├── BodyTeleportInteractor
│           ├── SmoothMovementTunneling
│           └── WallPenetrationTunneling
│
├── modulo_captura_eventos              # Captura de inputs/eventos globais
│
├── ambiente                            # Cenário 3D externo
│   ├── chao
│   ├── arvores (arvore_1..3, LOD0/LOD1)
│   ├── arbustos (arbusto, LOD0/LOD1)
│   ├── pedras (pedra_1..7)
│   └── luzes
│       └── luz_direcional
│
├── conteudo                            # UI e interações VR
│   └── menu_canvas
│       ├── Imagem_menu
│       ├── botao_ligar       (ISDK_RayCanvasInteraction)
│       ├── botao_buzinar     (ISDK_RayCanvasInteraction)
│       └── botao_fechar_menu (ISDK_RayCanvasInteraction)
│
└── carro                               # Veículo principal
    ├── rodas (4x mesh + 4x CarCollider)
    ├── porta_esquerda / porta_direita
    ├── carroceria / estrutura2
    ├── velocimetro / tacometro / deracao
    ├── ponto_camera_interna
    │   └── camera_interna
    └── camera_externa
```

---

## Scripts do Projeto

### Núcleo (`Assets/core/`)
| Script | Responsabilidade |
|---|---|
| `MusicaFundoController.cs` | Controla música de fundo |
| `MusicaFundoControllerEditModeTests.cs` | Testes do controlador de música |

### Carro (`Assets/carro/scripts/`)
| Script | Responsabilidade |
|---|---|
| `ControleCarro.cs` | Física e movimento do carro |
| `ControleCamera.cs` | Alternância câmera interna/externa |
| `TocadorSomCarro.cs` | Sistema de som do motor (i6 German) |
| `RespawnCarro.cs` | Reposicionamento do carro |
| `EntradaTeclado.cs` | Input via teclado (editor/debug) |
| `EntradaXbox.cs` | Input via controle Xbox |
| `DiagnosticoEntradaCarro.cs` | Debug de entradas |
| `IEntradaCarro.cs` | Interface de entrada do carro |
| `IEntradaCamera.cs` | Interface de entrada da câmera |
| `IRadioCarro.cs` | Interface do rádio |
| `AssistenteConfiguracaoCarroLowPoly.cs` | Editor helper |
| `UtilitarioConfiguracaoCarro.cs` | Editor utilitário |
| `CarroScriptsEditModeTests.cs` | Testes do carro |

### Menu/VR (`Assets/menu/scripts/`)
| Script | Responsabilidade |
|---|---|
| `VRMotoristaCarro.cs` | Lógica principal do motorista VR |
| `EntradaVR.cs` | Input via controladores Meta Quest |
| `VRMotoristoCarroEditor.cs` | Editor customizado |

### Assets de Terceiros (`Assets/cenario/`)
- **BOKI/LowPolyNature** — pacote de cenário low-poly (árvores, arbustos, pedras)
- **BOXOPHOBIC** — utilitários de shader e skybox

---

## MCPs Disponíveis

Este projeto usa dois servidores MCP que devem ser **consultados antes de qualquer modificação**:

### 1. `unity-mcp` (Unity AI Relay)
Conexão direta com o Unity Editor aberto. Use antes de qualquer mudança na cena, scripts ou assets.

| Ferramenta | Quando usar |
|---|---|
| `Unity_ManageScene` (GetHierarchy) | Verificar estado atual da hierarquia |
| `Unity_GetProjectData` | Visão geral do projeto |
| `Unity_FindProjectAssets` | Localizar assets por nome/tipo |
| `Unity_ManageGameObject` | Inspecionar/modificar GameObjects |
| `Unity_ManageScript` | Ler scripts existentes antes de editar |
| `Unity_GetConsoleLogs` | Verificar erros/warnings antes de agir |
| `Unity_Camera_Capture` | Capturar screenshot da cena |
| `Unity_SceneView_CaptureMultiAngleSceneView` | Ver cena em múltiplos ângulos |
| `Unity_ValidateScript` | Validar C# antes de aplicar |
| `Unity_RunCommand` | Executar comandos no editor |
| `Unity_Profiler_*` | Analisar performance |

### 2. `meta-horizon-mcp` (Meta Developer Docs)
Documentação oficial do Meta Quest. Use antes de qualquer trabalho com SDK Meta XR.

| Ferramenta | Quando usar |
|---|---|
| `meta_docs_search` | Buscar documentação de APIs Meta XR |
| `meta_docs_get_page` | Ler página completa de doc |
| `search_api_reference` | Buscar referência de classe/método |
| `get_api_details` | Detalhes de classe específica (OVR*, ISDK*) |

---

## Regras de Fluxo de Trabalho

### ANTES de qualquer modificação, obrigatoriamente:

1. **Modificação de cena ou GameObject**
   ```
   → Unity_ManageScene(GetHierarchy) para ver estado atual
   → Unity_ManageGameObject para inspecionar o objeto alvo
   → Unity_GetConsoleLogs para checar erros existentes
   ```

2. **Criação ou edição de script C#**
   ```
   → Unity_ManageScript para ler o script existente
   → Unity_FindProjectAssets para ver dependências
   → Unity_ValidateScript após escrever o código
   → Unity_GetConsoleLogs após aplicar
   ```

3. **Qualquer coisa envolvendo SDK Meta XR (OVR*, ISDK*, OpenXR)**
   ```
   → meta_docs_search para confirmar API atual
   → get_api_details para assinatura correta do método
   → Nunca assumir nomes de namespace/método de memória
   ```

4. **Modificação de assets (materiais, prefabs, animações)**
   ```
   → Unity_FindProjectAssets para localizar o asset
   → Unity_ManageAsset para inspecionar antes de mudar
   ```

5. **Diagnóstico de problemas**
   ```
   → Unity_GetConsoleLogs (sempre o primeiro passo)
   → Unity_Camera_Capture para ver o estado visual da cena
   → Unity_Profiler_* se o problema for de performance
   ```

### Regras gerais

- **Nunca editar arquivos `.unity`, `.prefab` ou `.asset` diretamente** — usar sempre as ferramentas MCP do Unity.
- **Nunca assumir que a API Meta XR está correta de memória** — o SDK evolui rápido (atualmente v201.0.0). Consultar `meta-horizon-mcp` sempre.
- **Testar no simulador Meta XR SDK** (não no headset físico) — comportamentos específicos do simulador já estão tratados no código.
- **Scripts de editor** ficam em `Assets/*/editor/` e herdam de `Editor` — não incluir em builds.
- **Testes EditMode** já existem para carro e áudio — manter e expandir ao adicionar funcionalidades.
- O carro usa `IEntradaCarro` / `IEntradaCamera` como interfaces — qualquer nova entrada deve implementá-las.
- Sons do carro usam o pacote **i6 German Free** com clips separados por estado (idle, aceleração, desaceleração, maxRPM).

---

## Estrutura de Pastas (`Assets/`)

```
Assets/
├── _Recovery/          # Cenas de backup (não editar)
├── carro/
│   ├── audio/          # Trilha (gta-vice-cite.mp3)
│   ├── dependencia_terceiros/  # i6 German Free sound pack
│   ├── scripts/
│   │   ├── camera/
│   │   ├── controle/
│   │   ├── editor/
│   │   ├── entradas/
│   │   ├── interface/
│   │   ├── tocador_som/
│   │   └── utilitarios/
│   └── testes/
├── cenario/
│   ├── BOKI/LowPolyNature/
│   └── BOXOPHOBIC/
├── core/
│   └── audio/
│       ├── scripts/
│       └── testes/
├── menu/
│   └── scripts/
└── Scenes/
    └── minha_cena.unity
```
