using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Vehicle
{
    /// <summary>
    /// Toca uma música de fundo em loop com fade de entrada e saída.
    /// Não conhece nenhuma entrada — quem chama os métodos públicos é o
    /// ControleCarro (via IEntradaCarro) ou outro sistema externo.
    ///
    /// - AlternarMute()   → liga/desliga o som (a música continua tocando internamente)
    /// - PularEstacao()   → pula para um ponto aleatório da música
    /// </summary>
    public class TocadorSomCarro : MonoBehaviour, IRadioCarro
    {
        public AudioSource fonteMusicaFundo;
        public AudioClip   musicaFundo;
        public float volumeMusicaFundo  = 0.42f;
        public float duracaoFadeEntrada = 2.5f;
        public float duracaoFadeSaida   = 2.5f;

        private Coroutine  rotinaMusica;
        private bool       musicaMutada;
        private string     mensagemMusica      = string.Empty;
        private float      tempoMensagemMusica;
        private Texture2D  fundoMensagem;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()  => GarantirMusicaFundo();

        private void Start()
        {
            if (Application.isPlaying)
                IniciarMusicaFundo();
        }

        // ── API pública ───────────────────────────────────────────────────────

        /// <summary>Liga ou desliga o som. A música continua tocando internamente.</summary>
        public void LigarEDesligarRadio()
        {
            if (fonteMusicaFundo == null) return;
            musicaMutada             = !musicaMutada;
            fonteMusicaFundo.mute    = musicaMutada;
            mensagemMusica           = musicaMutada ? "RADIO DESLIGADO" : "RADIO LIGADO";
            tempoMensagemMusica      = Time.time + 2f;
            Debug.Log(mensagemMusica + ".");
        }

        /// <summary>Pula para um ponto aleatório da música atual.</summary>
        public void PularEstacao()
        {
            if (fonteMusicaFundo == null || musicaFundo == null) return;
            if (!fonteMusicaFundo.isPlaying) fonteMusicaFundo.Play();
            float margem        = Mathf.Max(0.1f, duracaoFadeSaida + 0.1f);
            float inicioMax     = Mathf.Max(0f, musicaFundo.length - margem);
            fonteMusicaFundo.time = inicioMax > 0.01f ? Random.Range(0f, inicioMax) : 0f;
            mensagemMusica      = "PROXIMA ESTACAO";
            tempoMensagemMusica = Time.time + 2f;
            Debug.Log($"[TocadorSomCarro] PularEstacao chamado. Mensagem definida até: {tempoMensagemMusica:F2}");
        }

        // ── Internos ──────────────────────────────────────────────────────────

        private void GarantirMusicaFundo()
        {
            if (fonteMusicaFundo == null)
            {
                Transform existente   = transform.Find("musica_fundo");
                GameObject obj        = existente != null ? existente.gameObject : new GameObject("musica_fundo");
                if (obj.transform.parent == null)
                    obj.transform.SetParent(transform, worldPositionStays: false);
                fonteMusicaFundo = obj.GetComponent<AudioSource>() ?? obj.AddComponent<AudioSource>();
            }

            if (musicaFundo == null)
            {
#if UNITY_EDITOR
                musicaFundo = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/core/audio/dependencias/gta-vice-cite.mp3");
#endif
            }

            if (fonteMusicaFundo != null)
            {
                fonteMusicaFundo.playOnAwake  = false;
                fonteMusicaFundo.loop         = false;
                fonteMusicaFundo.spatialBlend = 0f;
                fonteMusicaFundo.clip         = musicaFundo;
            }
        }

        private void IniciarMusicaFundo()
        {
            if (fonteMusicaFundo == null || musicaFundo == null) return;
            if (rotinaMusica != null) StopCoroutine(rotinaMusica);
            rotinaMusica = StartCoroutine(RotinaMusicaFundo());
        }

        private IEnumerator RotinaMusicaFundo()
        {
            bool primeiraExecucao = true;
            while (true)
            {
                fonteMusicaFundo.clip   = musicaFundo;
                fonteMusicaFundo.volume = 0f;

                if (primeiraExecucao)
                {
                    float margem    = Mathf.Max(0.1f, duracaoFadeSaida + 0.1f);
                    float inicioMax = Mathf.Max(0f, musicaFundo.length - margem);
                    fonteMusicaFundo.time = inicioMax > 0.01f ? Random.Range(0f, inicioMax) : 0f;
                    primeiraExecucao      = false;
                }
                else
                {
                    fonteMusicaFundo.time = 0f;
                }

                fonteMusicaFundo.Play();
                yield return FadeVolume(fonteMusicaFundo, 0f, volumeMusicaFundo, Mathf.Max(0.01f, duracaoFadeEntrada));

                float janelaSaida   = Mathf.Max(0.01f, duracaoFadeSaida);
                float tempoRestante = Mathf.Max(0f, musicaFundo.length - janelaSaida);
                if (tempoRestante > 0f)
                    yield return new WaitForSeconds(tempoRestante);

                yield return FadeVolume(fonteMusicaFundo, fonteMusicaFundo.volume, 0f, janelaSaida);
                fonteMusicaFundo.Stop();
            }
        }

        private static IEnumerator FadeVolume(AudioSource fonte, float origem, float destino, float duracao)
        {
            float tempo = 0f;
            while (tempo < duracao)
            {
                tempo        += Time.deltaTime;
                fonte.volume  = Mathf.Lerp(origem, destino, Mathf.Clamp01(tempo / duracao));
                yield return null;
            }
            fonte.volume = destino;
        }

        // ── HUD ──────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (string.IsNullOrEmpty(mensagemMusica) || Time.time > tempoMensagemMusica) return;

            const float largura = 540f;
            const float altura  = 70f;
            Rect area = new Rect((Screen.width - largura) * 0.5f, 24f, largura, altura);

            GarantirFundoMensagem();
            GUIStyle caixa = new GUIStyle(GUI.skin.box) { border = new RectOffset(2, 2, 2, 2) };
            caixa.normal.background = fundoMensagem;
            GUI.Box(area, GUIContent.none, caixa);

            GUIStyle texto = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize  = 30
            };

            texto.normal.textColor = Color.black;
            GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), mensagemMusica, texto);
            GUI.Label(new Rect(area.x - 1f, area.y - 1f, area.width, area.height), mensagemMusica, texto);
            GUI.Label(new Rect(area.x + 1f, area.y - 1f, area.width, area.height), mensagemMusica, texto);
            GUI.Label(new Rect(area.x - 1f, area.y + 1f, area.width, area.height), mensagemMusica, texto);
            texto.normal.textColor = new Color(1f, 0.9f, 0.1f, 1f);
            GUI.Label(area, mensagemMusica, texto);
        }

        private void GarantirFundoMensagem()
        {
            if (fundoMensagem != null) return;
            fundoMensagem = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            fundoMensagem.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.45f));
            fundoMensagem.Apply();
        }
    }
}
