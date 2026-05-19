using UnityEngine;
using UnityEngine.InputSystem;

namespace Vehicle
{
    /// <summary>
    /// Fonte de input via teclado para o ControleCarro e CameraSeguimentoSimples.
    /// Requer o pacote New Input System instalado.
    /// Adicione no mesmo GameObject do carro e arraste:
    ///   - na lista "Entradas" do ControleCarro
    ///   - no campo "Entrada" do CameraSeguimentoSimples
    /// </summary>
    public class EntradaTeclado : MonoBehaviour, IEntradaCarro, IEntradaCamera
    {
        [Header("Aceleração / Freio")]
        public Key teclaAcelerador = Key.W;
        public Key teclaFreio      = Key.S;
        public Key teclaRe         = Key.X;

        [Header("Direção")]
        public Key teclaDireita  = Key.D;
        public Key teclaEsquerda = Key.A;

        [Header("Funções")]
        public Key teclaFreioMao = Key.Space;
        public Key teclaBuzina   = Key.H;
        public Key teclaRespawn  = Key.R;

        [Header("Rádio")]
        public Key teclaAlternarRadio = Key.M;
        public Key teclaPularEstacao  = Key.N;

        [Header("Câmera")]
        public Key teclaCameraEsquerda = Key.LeftArrow;
        public Key teclaCameraDireita  = Key.RightArrow;
        public Key teclaCameraCima     = Key.UpArrow;
        public Key teclaCameraBaixo    = Key.DownArrow;
        public Key teclaAlternarCamera = Key.Q;

        // ── IEntradaCarro ──────────────────────────────────────────────

        public float Aceleracao    => PressionadaFloat(teclaAcelerador);
        public float Freio         => Mathf.Max(PressionadaFloat(teclaFreio), PressionadaFloat(teclaRe));
        public float Direcao       => PressionadaFloat(teclaDireita) - PressionadaFloat(teclaEsquerda);
        public bool  FreioMao      => Pressionada(teclaFreioMao);
        public bool  Buzina        => PressionadaDown(teclaBuzina);
        public bool  Respawn       => PressionadaDown(teclaRespawn);
        public bool  AlternarRadio => PressionadaDown(teclaAlternarRadio);
        public bool  PularEstacao  => PressionadaDown(teclaPularEstacao);

        // ── IEntradaCamera ────────────────────────────────────────────────────

        public Vector2 Olhar
        {
            get
            {
                if (Keyboard.current == null) return Vector2.zero;
                float h = (Keyboard.current[teclaCameraDireita].isPressed  ? 1f : 0f)
                        - (Keyboard.current[teclaCameraEsquerda].isPressed ? 1f : 0f);
                float v = (Keyboard.current[teclaCameraCima].isPressed     ? 1f : 0f)
                        - (Keyboard.current[teclaCameraBaixo].isPressed    ? 1f : 0f);
                return new Vector2(h, v);
            }
        }

        public bool AlternarModo => PressionadaDown(teclaAlternarCamera);

        // ── Helpers ───────────────────────────────────────────────────────────

        private static float PressionadaFloat(Key tecla) => Pressionada(tecla) ? 1f : 0f;

        private static bool Pressionada(Key tecla)
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current[tecla].isPressed;
        }

        private static bool PressionadaDown(Key tecla)
        {
            if (Keyboard.current == null) return false;
            return Keyboard.current[tecla].wasPressedThisFrame;
        }
    }
}
