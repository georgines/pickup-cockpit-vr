# Pickup Cockpit VR

<p align="center">
  <img src="docs/img/imagem_projeto.png" alt="Menu VR com botão para dirigir a caminhonete" width="1024">
</p>

> **Demonstração em vídeo:** [https://youtu.be/xopF4vOyrFk](https://youtu.be/xopF4vOyrFk)

Projeto acadêmico de realidade virtual feito em Unity. A experiência permite explorar um ambiente 3D, abrir um menu VR flutuante e entrar na cabine de uma caminhonete para dirigir usando o Meta XR Simulator.

## Aluno

**Georgines Bezerra Pereira**

## Sobre o projeto

O **Pickup Cockpit VR** é uma demonstração automotiva em realidade virtual. O usuário começa fora do veículo, explora o ambiente, aproxima-se da caminhonete e usa um menu no espaço 3D para entrar na cabine.

Depois de entrar no carro, a câmera VR é posicionada no cockpit e o usuário passa a controlar a caminhonete com os comandos simulados do Meta Quest.

A proposta se encaixa no contexto do **Metaverso** como uma experiência de apresentação automotiva: em vez de apenas visualizar um veículo por fotos ou vídeos, o usuário entra no ambiente e experimenta a cabine em primeira pessoa.

## Objetivo

Criar uma experiência VR simples, navegável e interativa, aplicando fundamentos de XR com Unity, Meta XR SDK, OpenXR e Meta XR Simulator.

O fluxo principal da experiência é:

1. Explorar o ambiente em VR.
2. Aproximar-se do menu flutuante.
3. Clicar em **Dirigir Caminhonete**.
4. Entrar na cabine.
5. Receber a mensagem **DENTRO DO CARRO**.
6. Dirigir usando os comandos do Meta Quest pelo simulador.
7. Sair da caminhonete pelo botão **Menu** do meta quest.

## Tecnologias utilizadas

| Tecnologia | Uso no projeto |
|---|---|
| Unity 6 | Engine usada para criar a cena, os objetos e os scripts |
| Meta XR All-in-One SDK | Recursos XR voltados para Meta Quest |
| OpenXR | Base de execução XR do projeto |
| Meta XR Simulator | Testes no Windows sem depender de headset físico |
| Git LFS | Controle de arquivos grandes do Unity |
| C# | Scripts de interação, entrada VR e controle do carro |

## Instalação no Windows

### Requisitos

Antes de abrir o projeto, instale:

- Git para Windows
- Git LFS
- Unity Hub
- Unity 6000.3.13f1

### Clonar o projeto com Git LFS

Abra o **PowerShell** ou o **Git Bash** e execute os comandos abaixo.

```bash
git lfs install
```

```bash
git clone https://github.com/georgines/pickup-cockpit-vr.git
```

```bash
cd pickup-cockpit-vr
```

```bash
git lfs pull
```

> Evite baixar o projeto pelo botão **Download ZIP** do GitHub. Como o projeto usa arquivos grandes do Unity, o ideal é clonar com Git e baixar os arquivos reais com Git LFS.

### Se o projeto já foi clonado antes de instalar o Git LFS

Entre na pasta do projeto e execute:

```bash
git lfs install
```

```bash
git lfs pull
```

Depois disso, feche e abra novamente o Unity.

## Como abrir no Unity

1. Abra o **Unity Hub**.
2. Clique em **Add project from disk**.
3. Selecione a pasta `pickup-cockpit-vr`.
4. Aguarde a importação dos pacotes.
5. Abra a cena principal:

```text
Assets/Scenes/minha_cena.unity
```

## Como executar

1. Abra a cena principal no Unity.
2. Ative o **Meta XR Simulator**.
3. Clique em **Play** no Unity Editor.
4. Use o simulador para controlar a experiência VR.

## Controles no simulador VR

<p align="center">
  <img src="docs/img/comandos_teclado_simulador.png" alt="Menu VR com botão para dirigir a caminhonete" width="1024">
</p>

## Desenvolvimento do projeto

### Fluxo da interação principal

A interação principal acontece pelo menu VR flutuante. O usuário aponta para o botão **Dirigir Caminhonete** e confirma a ação. Depois disso, o projeto muda do modo de exploração para o modo motorista.

Ao entrar na cabine, o sistema posiciona o `OVRCameraRig` no ponto do assento, desativa a locomoção a pé, oculta elementos visuais desnecessários e ativa os comandos VR da caminhonete.

### Scripts principais

| Arquivo | Função |
|---|---|
| `Assets/menu/scripts/VRMotoristaCarro.cs` | Controla a entrada e saída do jogador na caminhonete em VR. Move o `OVRCameraRig` para o assento, desativa a locomoção a pé e ativa o modo de direção. |
| `Assets/menu/scripts/EntradaVR.cs` | Lê os comandos do Meta Quest usados no Meta XR Simulator. Mapeia aceleração, freio, direção, freio de mão, rádio e saída do carro. |
| `Assets/carro/scripts/controle/ControleCarro.cs` | Controla a física da caminhonete, usando `WheelCollider` para aceleração, freio, direção, rodas, estabilidade, RPM e áudio do motor. |
| `Assets/carro/scripts/camera/ControleCamera.cs` | Controla câmeras internas e externas usadas fora do modo VR. Ao entrar na caminhonete, as câmeras não-VR são desativadas. |
| `Assets/carro/scripts/tocador_som/TocadorSomCarro.cs` | Controla o rádio/música de fundo da caminhonete, permitindo ligar/desligar e trocar estação. |

### Organização da lógica

A física da caminhonete fica centralizada no `ControleCarro.cs`. Ele não depende diretamente de teclado, Xbox ou VR. O carro apenas recebe valores de entrada, como aceleração, freio e direção.

A entrada específica do Meta Quest fica no `EntradaVR.cs`. Essa separação deixa o controle do veículo mais organizado e facilita a manutenção do projeto.

O `VRMotoristaCarro.cs` faz a ponte entre a experiência VR e o carro: quando o jogador entra na cabine, ele troca o estado da cena para o modo de direção.

## Dificuldades e soluções

### Meta XR Simulator

#### Dificuldade

Durante o desenvolvimento, algumas alterações feitas no projeto não eram aplicadas corretamente apenas dando **Play** novamente no Unity. Em alguns momentos, foi necessário fechar e reabrir o projeto para que o Meta XR Simulator reconhecesse as mudanças.

#### Impacto no desenvolvimento

Isso tornou os testes mais lentos, principalmente durante os ajustes dos controles VR e da posição do jogador dentro da cabine.

#### Solução adotada

A solução foi testar em ciclos menores e reiniciar o projeto quando o simulador não atualizava corretamente o comportamento esperado.

### Câmera VR dentro do cockpit

#### Dificuldade

Uma das maiores dificuldades foi posicionar a câmera do Meta Quest dentro do cockpit da caminhonete. Em alguns testes, a renderização da carroceria atrapalhava a visão, como se a câmera estivesse atravessando os polígonos do carro.

#### Impacto no desenvolvimento

Esse problema prejudicava a sensação de estar sentado corretamente dentro da cabine e causava conflitos visuais durante os testes em VR.

#### Solução adotada

A solução foi separar o carro e a câmera em camadas diferentes no Unity. Com isso, ficou possível controlar melhor quais partes do veículo deveriam ser renderizadas pela câmera VR, reduzindo os conflitos visuais dentro da cabine.

### Separação entre entrada VR e física do carro

#### Dificuldade

O carro precisava ser controlado em VR sem deixar a lógica da física dependente diretamente do Meta Quest.

#### Solução adotada

A lógica da caminhonete ficou no `ControleCarro.cs`, enquanto os comandos do Meta Quest ficaram no `EntradaVR.cs`. Assim, o carro continua tendo uma lógica central e recebe os comandos VR de forma separada.

### Arquivos grandes do Unity

#### Dificuldade

Projetos Unity costumam usar arquivos grandes, como modelos, texturas, áudios e assets importados. Se esses arquivos não forem baixados corretamente, o projeto pode abrir com itens ausentes.

#### Solução adotada

O repositório foi preparado para uso com **Git LFS**, garantindo o download correto dos arquivos grandes necessários para abrir a cena.

## Limitações conhecidas

- A movimentação pode apresentar uma visão em formato circular durante o uso do simulador. Esse comportamento está relacionado aos recursos de conforto em VR.
- O projeto foi testado com foco no **Meta XR Simulator**.
- Os controles de teclado e Xbox não fazem parte da experiência principal documentada.
- Algumas melhorias visuais e de usabilidade ainda precisam ser feitas.
- A experiência representa uma prova de conceito acadêmica, não uma versão final de produto.

## Melhorias futuras

- Ajustar melhor a posição do jogador dentro da cabine.
- Melhorar o acabamento visual do menu VR.
- Testar e validar a experiência em um headset Meta Quest físico.
- Melhorar os feedbacks visuais e sonoros durante a direção.
- Refinar a condução da caminhonete.
- Criar uma experiência mais completa de showroom automotivo.

## Estrutura principal

```text
Assets/
├── Scenes/
│   └── minha_cena.unity
├── menu/
│   └── scripts/
│       ├── EntradaVR.cs
│       └── VRMotoristaCarro.cs
├── carro/
│   └── scripts/
│       ├── camera/
│       │   └── ControleCamera.cs
│       ├── controle/
│       │   └── ControleCarro.cs
│       └── tocador_som/
│           └── TocadorSomCarro.cs
└── cenario/
    └── assets do ambiente
```

## Status do projeto

Projeto acadêmico desenvolvido para estudo de Realidade Virtual, Unity, Meta Quest, OpenXR e fundamentos de experiências imersivas no Metaverso.

---

## Pacotes e assets utilizados

### Pacotes Unity (Package Manager)

| Pacote | Versão | Link |
|---|---|---|
| Meta XR All-in-One SDK | 201.0.0 | [Asset Store](https://assetstore.unity.com/packages/tools/integration/meta-xr-all-in-one-sdk-269657) |
| Universal Render Pipeline (URP) | 17.3.0 | [Documentação](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.3/manual/) |
| Input System | 1.19.0 | [Documentação](https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.inputsystem.html) |
| OpenXR Plugin | 1.16.1 | [Documentação](https://docs.unity3d.com/Packages/com.unity.xr.openxr@1.16/manual/index.html) |

### Assets da Unity Asset Store

| Asset | Uso no projeto | Link |
|---|---|---|
| Low-Poly Simple Nature Pack (JustCreate) | Árvores, arbustos, rochas e flores do cenário | [Asset Store](https://assetstore.unity.com/packages/3d/environments/landscapes/low-poly-simple-nature-pack-162153) |
| FREE Skybox Extended Shader (BOXOPHOBIC) | Skybox do ambiente externo | [Asset Store](https://assetstore.unity.com/packages/vfx/shaders/free-skybox-extended-shader-107400) |
| Drivable-Free Low Poly Cars (AWBMEGAMES) | Modelo 3D da caminhonete | [Asset Store](https://assetstore.unity.com/packages/3d/vehicles/drivable-free-low-poly-cars-327427) |
| Vehicle - Essentials (Nox_Sound) | Sons de buzina, freio de mão e outros efeitos do veículo | [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/transportation/vehicle-essentials-194951) |
| i6 German - Free Engine Sound Pack (Skril Studio) | Sons de motor da caminhonete | [Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/transportation/i6-german-free-engine-sound-pack-106037) |
| Trilha sonora do rádio | Música de fundo da estação de rádio da caminhonete | [YouTube](https://www.youtube.com/watch?v=XBYKkWSeo94) |
