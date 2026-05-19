using System.Collections.Generic;
using UnityEngine;

namespace Vehicle
{
    /// <summary>
    /// Registra no Console do Unity todas as entradas ativas do ControleCarro.
    /// Funciona com qualquer fonte que implemente IEntradaCarro — teclado, gamepad,
    /// VR ou qualquer implementação futura.
    ///
    /// Detecta adicionalmente IEntradaCamera e o pedido de sair do carro vindo da EntradaVR.
    ///
    /// SETUP:
    ///   1. Adicione este componente em qualquer GameObject da cena.
    ///   2. Arraste o ControleCarro para o campo "Controle Carro" no Inspector.
    ///
    /// FORMATO DOS LOGS:
    ///   [DiagnosticoEntrada] &lt;Fonte&gt; → &lt;Comando&gt; [detalhe]  (t=0.00s)
    /// </summary>
    [AddComponentMenu("Vehicle/Diagnóstico de Entrada do Carro")]
    public class DiagnosticoEntradaCarro : MonoBehaviour
    {
        // ── Referências ───────────────────────────────────────────────────────

        [Header("Referências")]
        [Tooltip("Arraste o ControleCarro aqui. Campo obrigatório.")]
        public ControleCarro controleCarro;

        // ── Opções de log ─────────────────────────────────────────────────────

        [Header("Comandos Discretos (IEntradaCarro)")]
        [Tooltip("Buzina, Respawn, AlternarRádio, PularEstacao e transições do FreioMao.")]
        public bool logComandosDiscretos = true;

        [Header("Eixos Analógicos (IEntradaCarro)")]
        [Tooltip("Aceleração, Freio e Direção quando acima do limiar.")]
        public bool logEixos = true;

        [Range(0.01f, 0.5f)]
        [Tooltip("Valor mínimo (absoluto) para um eixo gerar log.")]
        public float limiarEixo = 0.05f;

        [Range(0.1f, 5f)]
        [Tooltip("Cooldown em segundos entre logs repetidos do mesmo eixo. Evita spam.")]
        public float intervaloLogEixos = 0.5f;

        [Header("Câmera (IEntradaCamera)")]
        [Tooltip("AlternarModo e eixos Olhar em fontes que implementam IEntradaCamera.")]
        public bool logCamera = true;

        [Header("Extras VR (EntradaVR)")]
        [Tooltip("SolicitouSair da EntradaVR.")]
        public bool logExtrasVR = true;

        // ── Estado interno ────────────────────────────────────────────────────

        private IEntradaCarro[]  _entradas           = System.Array.Empty<IEntradaCarro>();
        private IEntradaCamera[] _cameras            = System.Array.Empty<IEntradaCamera>();
        private EntradaVR[]      _entradasVR         = System.Array.Empty<EntradaVR>();
        private string[]         _nomes              = System.Array.Empty<string>();
        private bool[]           _freioMaoAnterior   = System.Array.Empty<bool>();

        // [fonte][eixo]: 0=Acel  1=Freio  2=Dir  3=OlharX  4=OlharY
        private float[,] _ultimoLogEixo;

        // Eventos discretos do IEntradaCarro — FreioMao tratado à parte (flag contínua)
        private static readonly string[] NomesEventosCarro =
            { "Buzina", "Respawn", "AlternarRádio", "PularEstacao" };

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (controleCarro == null)
                Debug.LogError(
                    "[DiagnosticoEntradaCarro] ControleCarro não atribuído. " +
                    "Arraste o ControleCarro para o campo 'Controle Carro' no Inspector.", this);
        }

        private void Start()
        {
            AtualizarListaEntradas();
            LogarFontesEncontradas();
        }

        private void Update()
        {
            AtualizarListaEntradas();

            for (int i = 0; i < _entradas.Length; i++)
            {
                string nome = _nomes[i];

                if (logComandosDiscretos)
                    LogarComandosCarro(_entradas[i], nome, i);

                if (logEixos)
                    LogarEixosCarro(_entradas[i], nome, i);

                if (logCamera && _cameras[i] != null)
                    LogarCamera(_cameras[i], nome, i);

                if (logExtrasVR && _entradasVR[i] != null)
                    LogarExtrasVR(_entradasVR[i], nome, i);
            }
        }

        // ── Blocos de log ─────────────────────────────────────────────────────

        private void LogarComandosCarro(IEntradaCarro e, string fonte, int idx)
        {
            // Eventos de disparo único
            for (int j = 0; j < NomesEventosCarro.Length; j++)
                if (LerEventoCarro(e, j))
                    Log(fonte, NomesEventosCarro[j]);

            // FreioMao: loga apenas na transição ATIVADO / LIBERADO
            if (e.FreioMao && !_freioMaoAnterior[idx])
            {
                Log(fonte, "FreioMao", "ATIVADO");
                _freioMaoAnterior[idx] = true;
            }
            else if (!e.FreioMao && _freioMaoAnterior[idx])
            {
                Log(fonte, "FreioMao", "LIBERADO");
                _freioMaoAnterior[idx] = false;
            }
        }

        private void LogarEixosCarro(IEntradaCarro e, string fonte, int idx)
        {
            float agora = Time.time;
            TentarLogEixo(fonte, "Aceleração", e.Aceleracao, idx, 0, agora);
            TentarLogEixo(fonte, "Freio",      e.Freio,      idx, 1, agora);
            TentarLogEixo(fonte, "Direção",    e.Direcao,    idx, 2, agora);
        }

        private void LogarCamera(IEntradaCamera cam, string fonte, int idx)
        {
            if (cam.AlternarModo)
                Log(fonte, "Câmera.AlternarModo");

            float agora   = Time.time;
            Vector2 olhar = cam.Olhar;

            TentarLogEixo(fonte, "Câmera.OlharX", olhar.x, idx, 3, agora);
            TentarLogEixo(fonte, "Câmera.OlharY", olhar.y, idx, 4, agora);
        }

        private void LogarExtrasVR(EntradaVR vr, string fonte, int idx)
        {
            if (vr.DetectouSairNesteFrame)
                Log(fonte, "VR.SolicitouSair");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void TentarLogEixo(string fonte, string eixo, float valor,
                                    int idxFonte, int idxEixo, float agora)
        {
            if (Mathf.Abs(valor) < limiarEixo) return;
            if (agora - _ultimoLogEixo[idxFonte, idxEixo] < intervaloLogEixos) return;

            _ultimoLogEixo[idxFonte, idxEixo] = agora;
            Log(fonte, eixo, $"{valor:+0.00;-0.00;0.00}");
        }

        private static void Log(string fonte, string comando, string detalhe = "")
        {
            string det = string.IsNullOrEmpty(detalhe) ? string.Empty : $" [{detalhe}]";
            Debug.Log($"[DiagnosticoEntrada] {fonte} → {comando}{det}  (t={Time.time:F2}s)");
        }

        private static bool LerEventoCarro(IEntradaCarro e, int j) => j switch
        {
            0 => e.Buzina,
            1 => e.Respawn,
            2 => e.AlternarRadio,
            3 => e.PularEstacao,
            _ => false
        };

        private void LogarFontesEncontradas()
        {
            if (_entradas.Length == 0)
            {
                Debug.LogWarning(
                    "[DiagnosticoEntradaCarro] Nenhuma IEntradaCarro encontrada " +
                    "na lista de entradas do ControleCarro.", this);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[DiagnosticoEntradaCarro] {_entradas.Length} fonte(s) registrada(s):");

            for (int i = 0; i < _nomes.Length; i++)
            {
                string camera = _cameras[i]    != null ? " | IEntradaCamera" : string.Empty;
                string vr     = _entradasVR[i] != null ? " | ExtrasVR"       : string.Empty;
                sb.AppendLine($"  [{i}] {_nomes[i]}{camera}{vr}");
            }

            Debug.Log(sb.ToString(), this);
        }

        private void AtualizarListaEntradas()
        {
            if (controleCarro == null) return;

            var novas      = new List<IEntradaCarro>();
            var cameras    = new List<IEntradaCamera>();
            var entradasVR = new List<EntradaVR>();
            var nomes      = new List<string>();

            foreach (MonoBehaviour mb in controleCarro.entradas)
            {
                if (mb is not IEntradaCarro iec) continue;

                novas.Add(iec);
                nomes.Add(mb.GetType().Name);
                cameras.Add(mb as IEntradaCamera); // null se não implementar
                entradasVR.Add(mb as EntradaVR);   // null se não for EntradaVR
            }

            bool mesmaQuantidade = novas.Count == _entradas.Length;
            bool mesmasFontes = mesmaQuantidade;
            if (mesmasFontes)
            {
                for (int i = 0; i < novas.Count; i++)
                {
                    if (!ReferenceEquals(novas[i], _entradas[i]))
                    {
                        mesmasFontes = false;
                        break;
                    }
                }
            }

            if (mesmasFontes) return;

            _entradas           = novas.ToArray();
            _nomes              = nomes.ToArray();
            _cameras            = cameras.ToArray();
            _entradasVR         = entradasVR.ToArray();
            _freioMaoAnterior   = new bool[_entradas.Length];
            _ultimoLogEixo      = new float[_entradas.Length, 5];
            LogarFontesEncontradas();
        }
    }
}
