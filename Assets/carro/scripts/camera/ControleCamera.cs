using UnityEngine;
using UnityEngine.Serialization;

namespace Vehicle
{
    [ExecuteAlways]
    public class ControleCamera : MonoBehaviour
    {
        [Header("Cameras Reais")]
        [FormerlySerializedAs("externalCamera")]
        public Camera cameraExterna;
        [FormerlySerializedAs("internalCamera")]
        public Camera cameraInterna;

        [FormerlySerializedAs("target")]
        public Transform alvo;
        [FormerlySerializedAs("offset")]
        public Vector3 deslocamento = new Vector3(0f, 4f, -8f);
        [FormerlySerializedAs("positionLerp")]
        public float suavizacaoPosicao = 3.2f;
        [FormerlySerializedAs("lookLerp")]
        public float suavizacaoOlhar = 4.2f;

        [Header("Modo Externo")]
        [FormerlySerializedAs("externalDistance")]
        public float distanciaExterna = 8f;
        [FormerlySerializedAs("sensibilidadeExterna")]
        public float sensibilidadeExterna = 85f;
        [FormerlySerializedAs("sensibilidadePitchExterno")]
        public float sensibilidadePitchExterno = 65f;
        [FormerlySerializedAs("alturaPivoExterno")]
        public float alturaPivoExterno = 1.2f;
        [FormerlySerializedAs("pitchExternoMinimo")]
        public float pitchExternoMinimo = -8f;
        [FormerlySerializedAs("pitchExternoMaximo")]
        public float pitchExternoMaximo = 30f;
        [FormerlySerializedAs("pitchExternoPadrao")]
        public float pitchExternoPadrao = 15f;
        [FormerlySerializedAs("retornoAutomaticoExterno")]
        public bool retornoAutomaticoExterno = true;
        [FormerlySerializedAs("velocidadeRetornoExterno")]
        public float velocidadeRetornoExterno = 1.15f;
        [FormerlySerializedAs("desanexarEmRuntimeSeFilhaDoAlvo")]
        public bool desanexarEmRuntimeSeFilhaDoAlvo = true;

        [Header("Modo Interno (Motorista)")]
        [FormerlySerializedAs("pontoInterno")]
        public Transform pontoCameraInterna;
        [FormerlySerializedAs("offsetInterno")]
        public Vector3 deslocamentoInterno = Vector3.zero;
        [FormerlySerializedAs("sensibilidadeInterna")]
        public float sensibilidadeInterna = 48f;
        [FormerlySerializedAs("limitePitch")]
        public float limitePitchInterno = 65f;
        [FormerlySerializedAs("retornoAutomaticoInterno")]
        public bool retornoAutomaticoInterno = true;
        [FormerlySerializedAs("velocidadeRetornoInterno")]
        public float velocidadeRetornoInterno = 1.1f;

        [Header("Retorno Automatico por Movimento")]
        [FormerlySerializedAs("velocidadeMinimaRetornoKmh")]
        public float velocidadeMinimaRetornoKmh = 2f;

        // ── Estado interno ────────────────────────────────────────────────────

        private bool  modoInterno;
        private float yawExterno;
        private float pitchExterno;
        private float yawInterno;
        private float pitchInterno;
        private float velocidadeYawExterno;
        private float velocidadePitchExterno;
        private float velocidadeYawInterno;
        private float velocidadePitchInterno;
        private Vector3 velocidadePosicaoExterna;
        private float velocidadeRotacaoExternaX;
        private float velocidadeRotacaoExternaY;
        private float velocidadeRotacaoExternaZ;
        private Vector3 velocidadePosicaoInterna;
        private float velocidadeRotacaoInternaX;
        private float velocidadeRotacaoInternaY;
        private float velocidadeRotacaoInternaZ;
        private Rigidbody rigidbodyAlvo;

        public bool ModoInternoAtivo => modoInterno;

        /// <summary>Alterna câmera via código externo (ex: VRMotoristaCarroSimples).</summary>
        public void AlternarModoVR()
        {
            modoInterno = !modoInterno;
            if (modoInterno) { yawInterno = 0f; pitchInterno = 0f; }
            AplicarEstadoCameras();
        }

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            AutoConfigurarReferencias();
            GarantirRigidbodyAlvo();
            VincularCameraInternaAoPonto();
            AplicarEstadoCameras();
        }

        private void OnValidate()
        {
            AutoConfigurarReferencias();
            GarantirRigidbodyAlvo();
            VincularCameraInternaAoPonto();
            AplicarEstadoCameras();
        }

        private void OnEnable()
        {
            // Reativa o AudioListener da câmera ativa ao reativar o script
            AplicarEstadoCameras();
        }

        private void OnDisable()
        {
            // Desativa ambos os AudioListeners quando o script for desativado (ex: modo VR)
            if (cameraExterna != null)
            {
                AudioListener l = cameraExterna.GetComponent<AudioListener>();
                if (l != null) l.enabled = false;
            }
            if (cameraInterna != null)
            {
                AudioListener l = cameraInterna.GetComponent<AudioListener>();
                if (l != null) l.enabled = false;
            }
        }

        private void Start()
        {
            DesanexarCameraExternaEmRuntimeSeNecessario();
            yawExterno   = alvo != null ? alvo.eulerAngles.y : transform.eulerAngles.y;
            pitchExterno = Mathf.Clamp(pitchExternoPadrao, pitchExternoMinimo, pitchExternoMaximo);
            AplicarTransformacaoExterna(imediato: true);
            AtualizarCameraInterna(Vector2.zero);
        }

        private void LateUpdate()
        {
            if (!GarantirAlvoDisponivel()) return;

            // Lê input de todas as entradas do alvo que implementem IEntradaCamera
            Vector2 olhar        = Vector2.zero;
            bool    alternarModo = false;

            if (alvo != null)
            {
                foreach (MonoBehaviour mb in alvo.GetComponents<MonoBehaviour>())
                {
                    if (mb is not IEntradaCamera e) continue;
                    // Maior valor absoluto vence para o olhar
                    if (Mathf.Abs(e.Olhar.x) > Mathf.Abs(olhar.x)) olhar.x = e.Olhar.x;
                    if (Mathf.Abs(e.Olhar.y) > Mathf.Abs(olhar.y)) olhar.y = e.Olhar.y;
                    alternarModo = alternarModo || e.AlternarModo;
                }
            }

            // Alterna modo câmera
            if (alternarModo)
            {
                modoInterno = !modoInterno;
                if (modoInterno) { yawInterno = 0f; pitchInterno = 0f; }
                AplicarEstadoCameras();
            }

            VincularCameraInternaAoPonto();

            if (!modoInterno)
                AtualizarCameraExterna(olhar);

            AtualizarCameraInterna(modoInterno ? olhar : Vector2.zero);
        }

        // ── Câmera Externa ────────────────────────────────────────────────────

        private void AtualizarCameraExterna(Vector2 olhar)
        {
            yawExterno   += olhar.x * sensibilidadeExterna      * Time.deltaTime;
            pitchExterno -= olhar.y * sensibilidadePitchExterno * Time.deltaTime;
            pitchExterno  = Mathf.Clamp(pitchExterno, pitchExternoMinimo, pitchExternoMaximo);

            if (retornoAutomaticoExterno && olhar.sqrMagnitude < 0.0001f && DeveRetornarAutomaticamente())
            {
                yawExterno   = Mathf.SmoothDampAngle(yawExterno, alvo.eulerAngles.y, ref velocidadeYawExterno, 1f / Mathf.Max(0.01f, velocidadeRetornoExterno));
                pitchExterno = Mathf.SmoothDamp(pitchExterno, Mathf.Clamp(pitchExternoPadrao, pitchExternoMinimo, pitchExternoMaximo), ref velocidadePitchExterno, 1f / Mathf.Max(0.01f, velocidadeRetornoExterno));
            }

            AplicarTransformacaoExterna(imediato: false);
        }

        private void AplicarTransformacaoExterna(bool imediato)
        {
            Vector3    pivo            = alvo.position + Vector3.up * alturaPivoExterno;
            Quaternion rotacaoOrbital  = Quaternion.Euler(pitchExterno, yawExterno, 0f);
            Vector3    posicaoDesejada = pivo + rotacaoOrbital * new Vector3(0f, 0f, -distanciaExterna);
            Quaternion rotacaoDesejada = Quaternion.LookRotation(pivo - posicaoDesejada, Vector3.up);

            if (cameraExterna == null) return;

            Transform transformExterno = cameraExterna.transform;
            bool cameraFilhaDoAlvo     = transformExterno.IsChildOf(alvo);
            Transform paiExterno       = transformExterno.parent;

            if (imediato)
            {
                if (!cameraFilhaDoAlvo || paiExterno == null)
                    transformExterno.SetPositionAndRotation(posicaoDesejada, rotacaoDesejada);
                else
                {
                    transformExterno.localPosition = paiExterno.InverseTransformPoint(posicaoDesejada);
                    transformExterno.localRotation = Quaternion.Inverse(paiExterno.rotation) * rotacaoDesejada;
                }
                return;
            }

            float tempPos = 1f / Mathf.Max(0.01f, suavizacaoPosicao);
            float tempRot = 1f / Mathf.Max(0.01f, suavizacaoOlhar);

            if (cameraFilhaDoAlvo && paiExterno != null)
            {
                Vector3    posLocal = paiExterno.InverseTransformPoint(posicaoDesejada);
                Quaternion rotLocal = Quaternion.Inverse(paiExterno.rotation) * rotacaoDesejada;
                transformExterno.localPosition = Vector3.SmoothDamp(transformExterno.localPosition, posLocal, ref velocidadePosicaoExterna, tempPos);
                Vector3 al = transformExterno.localEulerAngles, dl = rotLocal.eulerAngles;
                transformExterno.localRotation = Quaternion.Euler(
                    Mathf.SmoothDampAngle(al.x, dl.x, ref velocidadeRotacaoExternaX, tempRot),
                    Mathf.SmoothDampAngle(al.y, dl.y, ref velocidadeRotacaoExternaY, tempRot),
                    Mathf.SmoothDampAngle(al.z, dl.z, ref velocidadeRotacaoExternaZ, tempRot));
                return;
            }

            transformExterno.position = Vector3.SmoothDamp(transformExterno.position, posicaoDesejada, ref velocidadePosicaoExterna, tempPos);
            Vector3 am = transformExterno.eulerAngles, dm = rotacaoDesejada.eulerAngles;
            transformExterno.rotation = Quaternion.Euler(
                Mathf.SmoothDampAngle(am.x, dm.x, ref velocidadeRotacaoExternaX, tempRot),
                Mathf.SmoothDampAngle(am.y, dm.y, ref velocidadeRotacaoExternaY, tempRot),
                Mathf.SmoothDampAngle(am.z, dm.z, ref velocidadeRotacaoExternaZ, tempRot));
        }

        // ── Câmera Interna ────────────────────────────────────────────────────

        private void AtualizarCameraInterna(Vector2 olhar)
        {
            Transform ancora = pontoCameraInterna != null ? pontoCameraInterna : alvo;
            if (cameraInterna == null || ancora == null) return;

            Transform t = cameraInterna.transform;

            yawInterno   += olhar.x * sensibilidadeInterna * Time.deltaTime;
            pitchInterno -= olhar.y * sensibilidadeInterna * Time.deltaTime;
            pitchInterno  = Mathf.Clamp(pitchInterno, -limitePitchInterno, limitePitchInterno);

            if (retornoAutomaticoInterno && olhar.sqrMagnitude < 0.0001f && DeveRetornarAutomaticamente())
            {
                yawInterno   = Mathf.SmoothDamp(yawInterno,   0f, ref velocidadeYawInterno,   1f / Mathf.Max(0.01f, velocidadeRetornoInterno));
                pitchInterno = Mathf.SmoothDamp(pitchInterno, 0f, ref velocidadePitchInterno, 1f / Mathf.Max(0.01f, velocidadeRetornoInterno));
            }

            float tempPos = 1f / Mathf.Max(0.01f, suavizacaoPosicao + 0.8f);
            float tempRot = 1f / Mathf.Max(0.01f, suavizacaoOlhar   + 0.8f);

            t.localPosition = Vector3.SmoothDamp(t.localPosition, deslocamentoInterno, ref velocidadePosicaoInterna, tempPos);

            Quaternion rotDesejada = Quaternion.Euler(pitchInterno, yawInterno, 0f);
            Vector3 a = t.localEulerAngles, d = rotDesejada.eulerAngles;
            t.localRotation = Quaternion.Euler(
                Mathf.SmoothDampAngle(a.x, d.x, ref velocidadeRotacaoInternaX, tempRot),
                Mathf.SmoothDampAngle(a.y, d.y, ref velocidadeRotacaoInternaY, tempRot),
                Mathf.SmoothDampAngle(a.z, d.z, ref velocidadeRotacaoInternaZ, tempRot));
        }

        // ── Utilitários ───────────────────────────────────────────────────────

        private bool GarantirAlvoDisponivel()
        {
            if (alvo != null) return true;
            AutoConfigurarReferencias();
            GarantirRigidbodyAlvo();
            return alvo != null;
        }

        private void DesanexarCameraExternaEmRuntimeSeNecessario()
        {
            if (!Application.isPlaying || !desanexarEmRuntimeSeFilhaDoAlvo || cameraExterna == null || alvo == null) return;
            Transform t = cameraExterna.transform;
            if (t.IsChildOf(alvo)) t.SetParent(null, worldPositionStays: true);
        }

        private bool DeveRetornarAutomaticamente()
        {
            if (rigidbodyAlvo == null) GarantirRigidbodyAlvo();
            if (rigidbodyAlvo == null) return true;
            return rigidbodyAlvo.linearVelocity.magnitude * 3.6f >= Mathf.Max(0f, velocidadeMinimaRetornoKmh);
        }

        private void AplicarEstadoCameras()
        {
            if (cameraExterna != null)
            {
                cameraExterna.enabled = !modoInterno;
                AudioListener l = cameraExterna.GetComponent<AudioListener>();
                if (l != null) l.enabled = !modoInterno;
            }

            if (cameraInterna != null)
            {
                cameraInterna.enabled = modoInterno;
                AudioListener l = cameraInterna.GetComponent<AudioListener>()
                               ?? cameraInterna.gameObject.AddComponent<AudioListener>();
                l.enabled = modoInterno;
            }
        }

        private void VincularCameraInternaAoPonto()
        {
            if (cameraInterna == null || pontoCameraInterna == null) return;
            Transform t = cameraInterna.transform;
            if (t.parent != pontoCameraInterna) t.SetParent(pontoCameraInterna, worldPositionStays: false);
        }


        private void AutoConfigurarReferencias()
        {
            if (cameraExterna == null) cameraExterna = GetComponent<Camera>() ?? Camera.main;
            if (alvo != null) return;

            if (transform.parent != null)
            {
                ControleCarro c = transform.parent.GetComponentInChildren<ControleCarro>(true);
                if (c != null) { alvo = c.transform; return; }
            }

            ControleCarro cCena = FindFirstObjectByType<ControleCarro>(FindObjectsInactive.Include);
            if (cCena != null) { alvo = cCena.transform; return; }

            if (transform.parent != null)
            {
                Rigidbody rb = transform.parent.GetComponentInChildren<Rigidbody>(true);
                if (rb != null) alvo = rb.transform;
            }
        }

        private void GarantirRigidbodyAlvo()
        {
            if (alvo == null) { rigidbodyAlvo = null; return; }
            rigidbodyAlvo = alvo.GetComponent<Rigidbody>()
                         ?? alvo.GetComponentInParent<Rigidbody>()
                         ?? alvo.GetComponentInChildren<Rigidbody>();
        }
    }
}
