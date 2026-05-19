using UnityEngine;
using UnityEngine.InputSystem;

namespace Vehicle
{
    /// <summary>
    /// Fonte de input via gamepad (Xbox / DualShock) para o ControleCarro e CameraSeguimentoSimples.
    /// Requer o pacote New Input System instalado.
    /// Todos os botões são configuráveis no Inspector.
    /// Adicione no mesmo GameObject do carro e arraste:
    ///   - na lista "Entradas" do ControleCarro
    ///   - no campo "Entrada" do CameraSeguimentoSimples
    ///
    /// Mapeamento padrão (Xbox):
    ///   RT (Gatilho Direito)         → acelerar
    ///   LT (Gatilho Esquerdo)        → frear / ré
    ///   LS (Analógico Esquerdo X)    → direção
    ///   RS (Analógico Direito)       → câmera
    ///   LB (Ombro Esquerdo)          → freio de mão
    ///   R3 (Clique Analógico Dir)    → buzina
    ///   X / □ (Botão Oeste)          → respawn
    ///   Y / △ (Botão Norte)          → alternar câmera
    ///   Start / Menu                 → ligar/desligar rádio
    ///   Select / View                → pular estação
    /// </summary>
    public class EntradaXbox : MonoBehaviour, IEntradaCarro, IEntradaCamera
    {
        // ── Eixos analógicos ──────────────────────────────────────────────────

        [Header("Eixos Analógicos")]
        [Tooltip("Eixo para acelerar (0..1)")]
        public EixoGamepad eixoAcelerador = EixoGamepad.GatilhoDireito;

        [Tooltip("Eixo para frear / ré (0..1)")]
        public EixoGamepad eixoFreio = EixoGamepad.GatilhoEsquerdo;

        [Tooltip("Eixo para direção do carro (usa X)")]
        public EixoGamepad2D eixoDirecao = EixoGamepad2D.AnalogicoEsquerdo;

        [Tooltip("Soma o DPad ao eixo de direção")]
        public bool usarDPadNaDirecao = true;

        [Tooltip("Eixo para mover a câmera")]
        public EixoGamepad2D eixoCamera = EixoGamepad2D.AnalogicoDir;

        // ── Botões ────────────────────────────────────────────────────────────

        [Header("Botões")]
        [Tooltip("Botão para freio de mão (segurado)")]
        public BotaoGamepad botaoFreioMao = BotaoGamepad.OmbroEsquerdo;

        [Tooltip("Botão para buzina (clique)")]
        public BotaoGamepad botaoBuzina = BotaoGamepad.CliqueAnalogicoDir;

        [Tooltip("Botão para respawn (clique)")]
        public BotaoGamepad botaoRespawn = BotaoGamepad.BotaoOeste;

        [Tooltip("Botão para alternar câmera externa/interna (clique)")]
        public BotaoGamepad botaoAlternarCamera = BotaoGamepad.BotaoNorte;

        [Tooltip("Botão para ligar/desligar rádio (clique)")]
        public BotaoGamepad botaoAlternarRadio = BotaoGamepad.Start;

        [Tooltip("Botão para pular estação (clique)")]
        public BotaoGamepad botaoPularEstacao = BotaoGamepad.Select;

        // ── IEntradaCarro ──────────────────────────────────────────────

        public float Aceleracao    { get; private set; }
        public float Freio         { get; private set; }
        public float Direcao       { get; private set; }
        public bool  FreioMao      { get; private set; }
        public bool  Buzina        { get; private set; }
        public bool  Respawn       { get; private set; }
        public bool  AlternarRadio { get; private set; }
        public bool  PularEstacao  { get; private set; }

        // ── IEntradaCamera ────────────────────────────────────────────────────

        public Vector2 Olhar        { get; private set; }
        public bool    AlternarModo { get; private set; }

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Update()
        {
            if (Gamepad.current == null) { Zerar(); return; }
            var gp = Gamepad.current;

            // Carro
            Aceleracao = LerEixo(gp, eixoAcelerador);
            Freio      = LerEixo(gp, eixoFreio);

            float direcaoEixo = LerEixo2D(gp, eixoDirecao).x;
            float direcaoDPad = usarDPadNaDirecao ? gp.dpad.ReadValue().x : 0f;
            Direcao       = Mathf.Clamp(direcaoEixo + direcaoDPad, -1f, 1f);

            FreioMao      = LerBotaoSegurado(gp, botaoFreioMao);
            Buzina        = LerBotaoClique(gp, botaoBuzina);
            Respawn       = LerBotaoClique(gp, botaoRespawn);
            AlternarRadio = LerBotaoClique(gp, botaoAlternarRadio);
            PularEstacao  = LerBotaoClique(gp, botaoPularEstacao);

            // Câmera
            Olhar        = LerEixo2D(gp, eixoCamera);
            AlternarModo = LerBotaoClique(gp, botaoAlternarCamera);
        }

        // ── Helpers de leitura ────────────────────────────────────────────────

        private static float LerEixo(Gamepad gp, EixoGamepad eixo) => eixo switch
        {
            EixoGamepad.GatilhoDireito  => gp.rightTrigger.ReadValue(),
            EixoGamepad.GatilhoEsquerdo => gp.leftTrigger.ReadValue(),
            _                           => 0f
        };

        private static Vector2 LerEixo2D(Gamepad gp, EixoGamepad2D eixo) => eixo switch
        {
            EixoGamepad2D.AnalogicoEsquerdo => gp.leftStick.ReadValue(),
            EixoGamepad2D.AnalogicoDir      => gp.rightStick.ReadValue(),
            _                               => Vector2.zero
        };

        private static bool LerBotaoSegurado(Gamepad gp, BotaoGamepad botao) => botao switch
        {
            BotaoGamepad.OmbroEsquerdo      => gp.leftShoulder.isPressed,
            BotaoGamepad.OmbroDireito       => gp.rightShoulder.isPressed,
            BotaoGamepad.CliqueAnalogicoEsq => gp.leftStickButton.isPressed,
            BotaoGamepad.CliqueAnalogicoDir => gp.rightStickButton.isPressed,
            BotaoGamepad.BotaoNorte         => gp.buttonNorth.isPressed,
            BotaoGamepad.BotaoSul           => gp.buttonSouth.isPressed,
            BotaoGamepad.BotaoLeste         => gp.buttonEast.isPressed,
            BotaoGamepad.BotaoOeste         => gp.buttonWest.isPressed,
            BotaoGamepad.Start              => gp.startButton.isPressed,
            BotaoGamepad.Select             => gp.selectButton.isPressed,
            _                               => false
        };

        private static bool LerBotaoClique(Gamepad gp, BotaoGamepad botao) => botao switch
        {
            BotaoGamepad.OmbroEsquerdo      => gp.leftShoulder.wasPressedThisFrame,
            BotaoGamepad.OmbroDireito       => gp.rightShoulder.wasPressedThisFrame,
            BotaoGamepad.CliqueAnalogicoEsq => gp.leftStickButton.wasPressedThisFrame,
            BotaoGamepad.CliqueAnalogicoDir => gp.rightStickButton.wasPressedThisFrame,
            BotaoGamepad.BotaoNorte         => gp.buttonNorth.wasPressedThisFrame,
            BotaoGamepad.BotaoSul           => gp.buttonSouth.wasPressedThisFrame,
            BotaoGamepad.BotaoLeste         => gp.buttonEast.wasPressedThisFrame,
            BotaoGamepad.BotaoOeste         => gp.buttonWest.wasPressedThisFrame,
            BotaoGamepad.Start              => gp.startButton.wasPressedThisFrame,
            BotaoGamepad.Select             => gp.selectButton.wasPressedThisFrame,
            _                               => false
        };

        private void Zerar()
        {
            Aceleracao = 0f; Freio = 0f; Direcao = 0f;
            FreioMao = false; Buzina = false; Respawn = false;
            AlternarRadio = false; PularEstacao = false;
            Olhar = Vector2.zero; AlternarModo = false;
        }
    }

    // ── Enums em português ────────────────────────────────────────────────────

    public enum EixoGamepad
    {
        [InspectorName("RT (Gatilho Direito)")]
        GatilhoDireito,
        [InspectorName("LT (Gatilho Esquerdo)")]
        GatilhoEsquerdo,
    }

    public enum EixoGamepad2D
    {
        [InspectorName("LS (Analógico Esquerdo)")]
        AnalogicoEsquerdo,
        [InspectorName("RS (Analógico Direito)")]
        AnalogicoDir,
    }

    public enum BotaoGamepad
    {
        [InspectorName("LB (Ombro Esquerdo)")]
        OmbroEsquerdo,
        [InspectorName("RB (Ombro Direito)")]
        OmbroDireito,
        [InspectorName("L3 (Clique Analógico Esquerdo)")]
        CliqueAnalogicoEsq,
        [InspectorName("R3 (Clique Analógico Direito)")]
        CliqueAnalogicoDir,
        [InspectorName("Y / △ (Botão Norte)")]
        BotaoNorte,
        [InspectorName("A / × (Botão Sul)")]
        BotaoSul,
        [InspectorName("B / ○ (Botão Leste)")]
        BotaoLeste,
        [InspectorName("X / □ (Botão Oeste)")]
        BotaoOeste,
        [InspectorName("Start / Menu / Options")]
        Start,
        [InspectorName("Select / View / Share")]
        Select,
    }
}
