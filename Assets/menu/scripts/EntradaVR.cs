using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Vehicle
{
    /// <summary>
    /// Fonte de input via controllers Meta Quest para o ControleCarro.
    /// Só fica ativa quando VRMotoristaCarro chamar Ativar().
    ///
    /// Mapeamento padrão (Quest):
    ///   Thumbstick Direito Y+      → acelerar
    ///   Thumbstick Direito Y-      → frear / ré
    ///   Thumbstick Direito X       → direção
    ///   Botão X                    → freio de mão (segurado)
    ///   Gatilho Indicador Direito  → trocar estação (clique)
    ///   Grip Direito               → ligar/desligar rádio (clique)
    ///   Botão Menu                 → sair do carro
    ///
    /// Câmera:
    ///   Headset (movimento da cabeça) → controla a câmera (tracking nativo OVR)
    ///
    /// Calibração do assento:
    ///   Feita no Editor via os campos "Offset Assento" e "Rotacao Assento"
    ///   no componente VRMotoristaCarro — sem necessidade de entrar em Play Mode.
    /// </summary>
    public class EntradaVR : MonoBehaviour, IEntradaCarro, IEntradaCamera
    {
        // ── Controles do carro ────────────────────────────────────────────────

        [Header("Aceleração / Freio / Direção")]
        [Tooltip("Thumbstick usado para acelerar (Y+), frear (Y-) e dirigir (X).")]
        public EixoVR2D eixoConducao = EixoVR2D.ThumbstickDireito;

        [Header("Freio de Mão")]
        [Tooltip("Botão segurado para acionar o freio de mão.")]
        public BotaoVR botaoFreioMao = BotaoVR.X;

        [Header("Buzina")]
        public BotaoVR botaoBuzina = BotaoVR.B;

        [Header("Respawn")]
        public BotaoVR botaoRespawn = BotaoVR.A;

        [Header("Rádio")]
        [Tooltip("Gatilho para trocar estação (clique por borda de subida).")]
        public EixoVR eixoPularEstacao = EixoVR.GatilhoIndicadorDireito;

        [Tooltip("Grip para ligar ou desligar o rádio (clique por borda de subida).")]
        public EixoVR eixoAlternarRadio = EixoVR.GatilhoMaoDireito;

        [Tooltip("Limiar do gatilho/grip para ser reconhecido como clique.")]
        [Range(0.5f, 0.95f)]
        public float limiarGatilho = 0.7f;

        [Header("Sair do Carro")]
        [Tooltip("Botão para sair do carro.")]
        public BotaoVR botaoSair = BotaoVR.Menu;

        // ── Estado ────────────────────────────────────────────────────────────

        private bool _ativo;

        // Detecção de clique nos gatilhos analógicos (borda de subida)
        private bool _gatilhoPularAnterior;
        private bool _gatilhoRadioAnterior;
        private bool _clicouPularEstacao;
        private bool _clicouAlternarRadio;
        private bool _menuAnterior;
        private static readonly List<InputDevice> _dispositivosXR = new List<InputDevice>();

        public bool SolicitouSair { get; private set; }
        public bool DetectouSairNesteFrame { get; private set; }

        // ── Ativação ──────────────────────────────────────────────────────────

        public void Ativar()   => _ativo = true;
        public void Desativar() => _ativo = false;
        public bool ConsumirSolicitacaoSair()
        {
            bool solicitou = SolicitouSair;
            SolicitouSair = false;
            return solicitou;
        }

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            OVRInput.Update();

            DetectouSairNesteFrame = false;
            _clicouPularEstacao  = false;
            _clicouAlternarRadio = false;

            // Borda de subida nos gatilhos analógicos (independe de _ativo)
            bool pularAtual = LerEixo(eixoPularEstacao)  >= limiarGatilho;
            bool radioAtual = LerEixo(eixoAlternarRadio) >= limiarGatilho;
            bool cliqueMenuAtual = LerCliqueMenuAtual();
            bool menuAtual = LerBotaoMenuAtual();

            _clicouPularEstacao  = pularAtual && !_gatilhoPularAnterior;
            _clicouAlternarRadio = radioAtual && !_gatilhoRadioAnterior;

            _gatilhoPularAnterior = pularAtual;
            _gatilhoRadioAnterior = radioAtual;

            if (!_ativo)
            {
                _menuAnterior = menuAtual;
                return;
            }

            if (LerClique(botaoSair, menuAtual, cliqueMenuAtual))
            {
                SolicitouSair = true;
                DetectouSairNesteFrame = true;
                Debug.Log("[EntradaVR] Comando de sair do carro detectado.", this);
            }

            _menuAnterior = menuAtual;
        }

        private void FixedUpdate()
        {
            OVRInput.FixedUpdate();
        }

        // ── IEntradaCarro ─────────────────────────────────────────────────────

        public float Aceleracao    => _ativo ? Mathf.Max(0f,  LerEixo2D(eixoConducao).y) : 0f;
        public float Freio         => _ativo ? Mathf.Max(0f, -LerEixo2D(eixoConducao).y) : 0f;
        public float Direcao       => _ativo ? LerEixo2D(eixoConducao).x                 : 0f;
        public bool  FreioMao      => _ativo && LerSegurado(botaoFreioMao);
        public bool  Buzina        => _ativo && LerClique(botaoBuzina);
        public bool  Respawn       => _ativo && LerClique(botaoRespawn);
        public bool  AlternarRadio => _ativo && _clicouAlternarRadio;
        public bool  PularEstacao  => _ativo && _clicouPularEstacao;

        // ── IEntradaCamera ────────────────────────────────────────────────────

        public Vector2 Olhar        => Vector2.zero;
        public bool    AlternarModo => false;

        // ── Helpers de leitura ────────────────────────────────────────────────

        private static float LerEixo(EixoVR eixo) => eixo switch
        {
            EixoVR.GatilhoIndicadorDireito  => OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger, OVRInput.Controller.RTouch),
            EixoVR.GatilhoIndicadorEsquerdo => OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger, OVRInput.Controller.LTouch),
            EixoVR.GatilhoMaoDireito        => OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger, OVRInput.Controller.RTouch),
            EixoVR.GatilhoMaoEsquerdo       => OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger, OVRInput.Controller.LTouch),
            _                               => 0f
        };

        private static Vector2 LerEixo2D(EixoVR2D eixo) => eixo switch
        {
            EixoVR2D.ThumbstickEsquerdo => OVRInput.Get(OVRInput.RawAxis2D.LThumbstick, OVRInput.Controller.LTouch),
            EixoVR2D.ThumbstickDireito  => OVRInput.Get(OVRInput.RawAxis2D.RThumbstick, OVRInput.Controller.RTouch),
            _                           => Vector2.zero
        };

        private bool LerSegurado(BotaoVR botao) => botao switch
        {
            BotaoVR.A                       => OVRInput.Get(OVRInput.RawButton.A, OVRInput.Controller.RTouch),
            BotaoVR.B                       => OVRInput.Get(OVRInput.RawButton.B, OVRInput.Controller.RTouch),
            BotaoVR.X                       => OVRInput.Get(OVRInput.RawButton.X, OVRInput.Controller.LTouch),
            BotaoVR.Y                       => OVRInput.Get(OVRInput.RawButton.Y, OVRInput.Controller.LTouch),
            BotaoVR.CliqueThumbstickDireito => OVRInput.Get(OVRInput.RawButton.RThumbstick, OVRInput.Controller.RTouch),
            BotaoVR.CliqueThumbstickEsq     => OVRInput.Get(OVRInput.RawButton.LThumbstick, OVRInput.Controller.LTouch),
            BotaoVR.Menu                    => LerBotaoMenuAtual(),
            _                               => false
        };

        private bool LerClique(BotaoVR botao, bool menuAtual = false, bool cliqueMenuAtual = false) => botao switch
        {
            BotaoVR.A                       => OVRInput.GetDown(OVRInput.RawButton.A, OVRInput.Controller.RTouch),
            BotaoVR.B                       => OVRInput.GetDown(OVRInput.RawButton.B, OVRInput.Controller.RTouch),
            BotaoVR.X                       => OVRInput.GetDown(OVRInput.RawButton.X, OVRInput.Controller.LTouch),
            BotaoVR.Y                       => OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch),
            BotaoVR.CliqueThumbstickDireito => OVRInput.GetDown(OVRInput.RawButton.RThumbstick, OVRInput.Controller.RTouch),
            BotaoVR.CliqueThumbstickEsq     => OVRInput.GetDown(OVRInput.RawButton.LThumbstick, OVRInput.Controller.LTouch),
            BotaoVR.Menu                    => cliqueMenuAtual || (menuAtual && !_menuAnterior),
            _                               => false
        };

        private static bool LerCliqueMenuAtual()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start) ||
                OVRInput.GetDown(OVRInput.RawButton.Start) ||
                OVRInput.GetDown(OVRInput.RawButton.Start, OVRInput.Controller.Active) ||
                OVRInput.GetDown(OVRInput.RawButton.Start, OVRInput.Controller.LTouch))
            {
                return true;
            }

            return ExisteDispositivoComMenuPressionado();
        }

        private static bool LerBotaoMenuAtual()
        {
            if (OVRInput.Get(OVRInput.Button.Start) ||
                OVRInput.Get(OVRInput.RawButton.Start) ||
                OVRInput.Get(OVRInput.RawButton.Start, OVRInput.Controller.Active) ||
                OVRInput.Get(OVRInput.RawButton.Start, OVRInput.Controller.LTouch))
            {
                return true;
            }

            return ExisteDispositivoComMenuPressionado();
        }

        private static bool ExisteDispositivoComMenuPressionado()
        {
            _dispositivosXR.Clear();
            InputDevices.GetDevices(_dispositivosXR);

            for (int i = 0; i < _dispositivosXR.Count; i++)
            {
                InputDevice dispositivo = _dispositivosXR[i];
                if (!dispositivo.isValid)
                    continue;

                if (dispositivo.TryGetFeatureValue(CommonUsages.menuButton, out bool menuPressionado) &&
                    menuPressionado)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // ── Enums em português ────────────────────────────────────────────────────

    public enum EixoVR
    {
        [InspectorName("Gatilho Indicador Direito (RT)")]
        GatilhoIndicadorDireito,
        [InspectorName("Gatilho Indicador Esquerdo (LT)")]
        GatilhoIndicadorEsquerdo,
        [InspectorName("Gatilho de Mão Direito (Grip Dir)")]
        GatilhoMaoDireito,
        [InspectorName("Gatilho de Mão Esquerdo (Grip Esq)")]
        GatilhoMaoEsquerdo,
    }

    public enum EixoVR2D
    {
        [InspectorName("Thumbstick Esquerdo")]
        ThumbstickEsquerdo,
        [InspectorName("Thumbstick Direito")]
        ThumbstickDireito,
    }

    public enum BotaoVR
    {
        [InspectorName("A (Controller Direito)")]
        A,
        [InspectorName("B (Controller Direito)")]
        B,
        [InspectorName("X (Controller Esquerdo)")]
        X,
        [InspectorName("Y (Controller Esquerdo)")]
        Y,
        [InspectorName("Clique Thumbstick Direito (R3)")]
        CliqueThumbstickDireito,
        [InspectorName("Clique Thumbstick Esquerdo (L3)")]
        CliqueThumbstickEsq,
        [InspectorName("Menu (Botão Esquerdo)")]
        Menu,
    }
}
