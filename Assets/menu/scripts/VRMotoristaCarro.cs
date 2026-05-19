using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Vehicle
{
    /// <summary>
    /// Gerencia a entrada e saída do jogador no carro em VR.
    ///
    /// O OVRCameraRig continua usando o tracking normal do headset; o script só
    /// move o root do rig para acompanhar o ponto de assento do carro.
    /// </summary>
    public class VRMotoristaCarro : MonoBehaviour
    {
        [Header("VR")]
        [Tooltip("Raiz do OVRCameraRig.")]
        public OVRCameraRig cameraRig;

        [Tooltip("Duracao do fade de tela no VR antes e depois de entrar ou sair do carro.")]
        public float duracaoFadeVR = 0.2f;

        [Header("Carro")]
        [Tooltip("EntradaVR do carro.")]
        public EntradaVR entradaVR;

        [Tooltip("ControleCarro do carro. Se vazio, é detectado automaticamente no Awake().")]
        public ControleCarro controleCarro;

        [Header("Cameras nao-VR")]
        [Tooltip("Cameras Unity do carro. Desativadas ao entrar no carro.")]
        public Camera[] camerasNaoVR;

        [Header("Controllers")]
        [Tooltip("GameObjects dos controllers visiveis. Somem ao entrar no carro.")]
        public GameObject[] gameObjectsControllers;

        [Header("Locomocao a pe")]
        [Tooltip("Sistema de locomocao a pe. E desativado ao entrar no carro e reativado ao sair.")]
        public GameObject locomotionAPe;

        [Tooltip("Componentes de locomocao/conforto que devem ser desativados explicitamente ao dirigir, como tunneling e turn providers.")]
        public Behaviour[] componentesDesativarAoDirigir;

        [Header("Posicao FORA do carro")]
        [Tooltip("Ponto de referencia para a posicao inicial fora do carro. Se vazio, usa a origem da cena.")]
        public Transform pontoFora;

        [Tooltip("Offset em relacao ao ponto de referencia fora do carro.")]
        public Vector3 offsetFora = Vector3.zero;

        [Tooltip("Rotacao fora do carro, em graus.")]
        public Vector3 rotacaoFora = Vector3.zero;

        [Tooltip("Ao sair do carro, respawna o veiculo no ponto inicial e reposiciona o jogador fora do carro.")]
        public bool resetarCenarioAoSair = true;

        [Header("Posicao DENTRO do carro")]
        [Tooltip("Ponto do assento que o root do rig deve acompanhar enquanto o jogador dirige.")]
        public Transform pontoAssento;

        [Tooltip("Offset em relacao ao ponto do assento.")]
        public Vector3 offsetDentro = Vector3.zero;

        [Tooltip("Folga extra para afastar a cabeca do painel, teto, vidro ou banco.")]
        public Vector3 offsetSegurancaCabeca = Vector3.zero;

        [Tooltip("Rotacao dentro do carro, em graus.")]
        public Vector3 rotacaoDentro = Vector3.zero;

        [Header("Cockpit")]
        [Tooltip("Objetos internos do cockpit que podem ser ocultados enquanto o jogador dirige.")]
        public GameObject[] objetosInternosOcultarAoDirigir;

        private bool _estaDirigindo;
        private List<MonoBehaviour> _entradasOriginais;
        private readonly List<Collider> _collidersRigDesativados = new List<Collider>();
        private readonly List<CharacterController> _ccRigDesativados = new List<CharacterController>();
        private readonly List<Renderer> _renderersOcultados = new List<Renderer>();
        private readonly List<Canvas> _canvasesOcultados = new List<Canvas>();
        private readonly List<Behaviour> _componentesDesativadosAoDirigir = new List<Behaviour>();

        private GameObject _canvasMensagem;
        private Text _textoMensagem;
        private float _tempoOcultarMensagem;
        private Vector3 _posicaoInicialRig;
        private Quaternion _rotacaoInicialRig;
        private bool _poseInicialRigCapturada;
        private bool _transicaoEmAndamento;
        private Quaternion _compensacaoYawEntradaNoCarro = Quaternion.identity;
        private bool _usarCompensacaoYawEntradaNoCarro;
        private Vector3 _posicaoOlhosAntesDeEntrarNoCarro;
        private Quaternion _rotacaoOlhosAntesDeEntrarNoCarro = Quaternion.identity;
        private bool _poseForaCapturadaAntesDeEntrarNoCarro;

        private void Awake()
        {
            if (cameraRig == null)
                Debug.LogError("[VRMotoristaCarro] CameraRig nao atribuido.", this);
            if (entradaVR == null)
                Debug.LogError("[VRMotoristaCarro] EntradaVR nao atribuida.", this);

            if (controleCarro == null)
                controleCarro = GetComponent<ControleCarro>();

            if (cameraRig != null)
            {
                _posicaoInicialRig = cameraRig.transform.position;
                _rotacaoInicialRig = cameraRig.transform.rotation;
                _poseInicialRigCapturada = true;
            }

            CriarCanvasVR();
        }

        private void Start()
        {
            CriarCanvasVR();
            AplicarPosicaoFora();
        }

        private void Update()
        {
            if (_estaDirigindo && entradaVR != null && entradaVR.ConsumirSolicitacaoSair())
                SairDoCarro();

            if (_canvasMensagem != null &&
                _canvasMensagem.activeSelf &&
                Time.time >= _tempoOcultarMensagem)
            {
                _canvasMensagem.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (_estaDirigindo)
                AplicarPosicaoDentro();
        }

        public void EntrarNoCarro()
        {
            if (_estaDirigindo || _transicaoEmAndamento || cameraRig == null || entradaVR == null)
                return;

            StartCoroutine(ExecutarTransicaoComFade(EntrarNoCarroSemFade));
        }

        public void SairDoCarro()
        {
            if (!_estaDirigindo || _transicaoEmAndamento || cameraRig == null)
                return;

            StartCoroutine(ExecutarTransicaoComFade(SairDoCarroSemFade));
        }

        public void AplicarPosicaoFora()
        {
            if (cameraRig == null)
                return;

            bool usarPontoFora = pontoFora != null;
            bool usarOffsetManual = offsetFora != Vector3.zero || rotacaoFora != Vector3.zero;

            Vector3 posicao;
            Quaternion rotacao;

            if (usarPontoFora)
            {
                posicao = pontoFora.position + pontoFora.TransformDirection(offsetFora);
                rotacao = pontoFora.rotation * Quaternion.Euler(rotacaoFora);
            }
            else if (usarOffsetManual)
            {
                posicao = offsetFora;
                rotacao = Quaternion.Euler(rotacaoFora);
            }
            else if (_poseInicialRigCapturada)
            {
                posicao = _posicaoInicialRig;
                rotacao = _rotacaoInicialRig;
            }
            else
            {
                posicao = cameraRig.transform.position;
                rotacao = cameraRig.transform.rotation;
            }

            AplicarPoseCompensandoOlhos(posicao, rotacao);
        }

        public void AplicarPosicaoDentro()
        {
            if (cameraRig == null)
                return;

            Transform referencia = pontoAssento != null ? pontoAssento : transform;
            Vector3 posicao = referencia.position + referencia.TransformDirection(offsetDentro + offsetSegurancaCabeca);
            Quaternion rotacao = referencia.rotation * Quaternion.Euler(rotacaoDentro);

            if (_usarCompensacaoYawEntradaNoCarro)
                rotacao *= _compensacaoYawEntradaNoCarro;

            AplicarPoseCompensandoOlhos(posicao, rotacao);
        }

        private void AplicarPoseCompensandoOlhos(Vector3 posicaoOlhos, Quaternion rotacaoRig)
        {
            if (cameraRig == null)
                return;

            cameraRig.transform.rotation = rotacaoRig;

            Transform eyeAnchor = cameraRig.centerEyeAnchor;
            if (eyeAnchor == null)
            {
                cameraRig.transform.position = posicaoOlhos;
                return;
            }

            Vector3 deslocamentoOlhos = eyeAnchor.position - cameraRig.transform.position;
            cameraRig.transform.position = posicaoOlhos - deslocamentoOlhos;
        }

        private void DesativarCollidersDoRig()
        {
            _collidersRigDesativados.Clear();
            _ccRigDesativados.Clear();

            if (cameraRig == null)
                return;

            foreach (var col in cameraRig.GetComponentsInChildren<Collider>(true))
            {
                if (col != null && col.enabled)
                {
                    _collidersRigDesativados.Add(col);
                    col.enabled = false;
                }
            }

            foreach (var cc in cameraRig.GetComponentsInChildren<CharacterController>(true))
            {
                if (cc != null && cc.enabled)
                {
                    _ccRigDesativados.Add(cc);
                    cc.enabled = false;
                }
            }
        }

        private void ReativarCollidersDoRig()
        {
            foreach (var col in _collidersRigDesativados)
            {
                if (col != null)
                    col.enabled = true;
            }

            foreach (var cc in _ccRigDesativados)
            {
                if (cc != null)
                    cc.enabled = true;
            }

            _collidersRigDesativados.Clear();
            _ccRigDesativados.Clear();
        }

        private void OcultarObjetoSemDesativar(GameObject alvo)
        {
            if (alvo == null)
                return;

            foreach (var renderer in alvo.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.enabled)
                {
                    _renderersOcultados.Add(renderer);
                    renderer.enabled = false;
                }
            }

            foreach (var canvas in alvo.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas != null && canvas.enabled)
                {
                    _canvasesOcultados.Add(canvas);
                    canvas.enabled = false;
                }
            }
        }

        private void RestaurarObjetosOcultados()
        {
            foreach (var renderer in _renderersOcultados)
            {
                if (renderer != null)
                    renderer.enabled = true;
            }

            foreach (var canvas in _canvasesOcultados)
            {
                if (canvas != null)
                    canvas.enabled = true;
            }

            _renderersOcultados.Clear();
            _canvasesOcultados.Clear();
        }

        private void DesativarComponentesAoDirigir()
        {
            _componentesDesativadosAoDirigir.Clear();

            foreach (var componente in componentesDesativarAoDirigir)
            {
                if (componente == null || !componente.enabled)
                    continue;

                _componentesDesativadosAoDirigir.Add(componente);
                componente.enabled = false;
            }
        }

        private void ReativarComponentesAoDirigir()
        {
            foreach (var componente in _componentesDesativadosAoDirigir)
            {
                if (componente != null)
                    componente.enabled = true;
            }

            _componentesDesativadosAoDirigir.Clear();
        }

        private void CriarCanvasVR()
        {
            if (cameraRig == null)
                return;

            Transform eyeAnchor = cameraRig.centerEyeAnchor;
            if (eyeAnchor == null)
                return;

            if (_canvasMensagem != null)
            {
                AtualizarAncoraCanvasVR(eyeAnchor);
                return;
            }

            _canvasMensagem = new GameObject("CanvasMensagemVR");
            _canvasMensagem.transform.SetParent(eyeAnchor, false);
            _canvasMensagem.transform.localScale = Vector3.one * 0.005f;

            Canvas canvas = _canvasMensagem.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;

            RectTransform rt = _canvasMensagem.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800f, 120f);

            GameObject fundo = new GameObject("Fundo");
            fundo.transform.SetParent(_canvasMensagem.transform, false);
            Image img = fundo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);

            RectTransform rtFundo = fundo.GetComponent<RectTransform>();
            rtFundo.anchorMin = Vector2.zero;
            rtFundo.anchorMax = Vector2.one;
            rtFundo.offsetMin = Vector2.zero;
            rtFundo.offsetMax = Vector2.zero;

            GameObject textoGO = new GameObject("Texto");
            textoGO.transform.SetParent(_canvasMensagem.transform, false);
            _textoMensagem = textoGO.AddComponent<Text>();
            _textoMensagem.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _textoMensagem.fontSize = 72;
            _textoMensagem.fontStyle = FontStyle.Bold;
            _textoMensagem.color = new Color(1f, 0.9f, 0.1f);
            _textoMensagem.alignment = TextAnchor.MiddleCenter;

            RectTransform rtTexto = textoGO.GetComponent<RectTransform>();
            rtTexto.anchorMin = Vector2.zero;
            rtTexto.anchorMax = Vector2.one;
            rtTexto.offsetMin = Vector2.zero;
            rtTexto.offsetMax = Vector2.zero;

            AtualizarAncoraCanvasVR(eyeAnchor);
            _canvasMensagem.SetActive(false);
        }

        private void AtualizarAncoraCanvasVR(Transform eyeAnchor)
        {
            if (_canvasMensagem == null || eyeAnchor == null)
                return;

            _canvasMensagem.transform.SetParent(eyeAnchor, false);
            _canvasMensagem.transform.localPosition = new Vector3(0f, 0f, 2f);
            _canvasMensagem.transform.localRotation = Quaternion.identity;
        }

        private void ExibirMensagem(string mensagem, float duracao = 2f)
        {
            if (_canvasMensagem == null || _textoMensagem == null)
                CriarCanvasVR();

            if (_canvasMensagem == null || _textoMensagem == null)
                return;

            _textoMensagem.text = mensagem;
            _canvasMensagem.SetActive(true);
            _tempoOcultarMensagem = Time.time + duracao;
        }

        private IEnumerator ExecutarTransicaoComFade(System.Action transicao)
        {
            _transicaoEmAndamento = true;

            OVRScreenFade screenFade = null;

            try
            {
                screenFade = GarantirOVRScreenFade();
                if (screenFade == null)
                {
                    transicao?.Invoke();
                    yield break;
                }

                screenFade.fadeTime = Mathf.Max(0.01f, duracaoFadeVR);

                // Garante que um OVRScreenFade criado dinamicamente inicialize seus componentes internos.
                if (OVRScreenFade.instance == null)
                    yield return null;

                screenFade.FadeOut();
                yield return new WaitForSeconds(screenFade.fadeTime);

                transicao?.Invoke();

                screenFade.FadeIn();
                yield return new WaitForSeconds(screenFade.fadeTime);
            }
            finally
            {
                _transicaoEmAndamento = false;
            }
        }

        private void EntrarNoCarroSemFade()
        {
            CapturarPoseForaAntesDeEntrarNoCarro();

            foreach (var cam in camerasNaoVR)
            {
                if (cam != null)
                    cam.enabled = false;
            }

            foreach (var go in gameObjectsControllers)
            {
                if (go != null)
                    OcultarObjetoSemDesativar(go);
            }

            foreach (var go in objetosInternosOcultarAoDirigir)
            {
                if (go != null)
                    OcultarObjetoSemDesativar(go);
            }

            DesativarComponentesAoDirigir();

            if (locomotionAPe != null)
                locomotionAPe.SetActive(false);

            if (OVRManager.instance != null)
                OVRManager.instance.usePositionTracking = false;

            _compensacaoYawEntradaNoCarro = CalcularCompensacaoYawEntrada();
            _usarCompensacaoYawEntradaNoCarro = true;

            if (controleCarro != null)
            {
                controleCarro.LiberarFreioMaoInicial();

                var rb = controleCarro.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                _entradasOriginais = new List<MonoBehaviour>(controleCarro.entradas);
                controleCarro.entradas.Clear();
                controleCarro.entradas.Add(entradaVR);
            }

            DesativarCollidersDoRig();
            AplicarPosicaoDentro();

            entradaVR.Ativar();
            _estaDirigindo = true;
            ExibirMensagem("DENTRO DO CARRO");
        }

        private void SairDoCarroSemFade()
        {
            entradaVR?.Desativar();
            _usarCompensacaoYawEntradaNoCarro = false;
            _compensacaoYawEntradaNoCarro = Quaternion.identity;

            if (controleCarro != null)
            {
                controleCarro.ReativarFreioMaoInicial();

                if (_entradasOriginais != null)
                {
                    controleCarro.entradas.Clear();
                    controleCarro.entradas.AddRange(_entradasOriginais);
                    _entradasOriginais = null;
                }
            }

            foreach (var cam in camerasNaoVR)
            {
                if (cam != null)
                    cam.enabled = true;
            }

            if (locomotionAPe != null)
                locomotionAPe.SetActive(true);

            ReativarComponentesAoDirigir();

            if (OVRManager.instance != null)
                OVRManager.instance.usePositionTracking = true;

            _estaDirigindo = false;
            ReativarCollidersDoRig();
            RestaurarObjetosOcultados();

            if (resetarCenarioAoSair && controleCarro != null)
                controleCarro.ExecutarRespawn();

            RestaurarPoseForaDepoisDeSairDoCarro();
            ExibirMensagem("FORA DO CARRO");
        }

        private OVRScreenFade GarantirOVRScreenFade()
        {
            if (OVRScreenFade.instance != null)
                return OVRScreenFade.instance;

            OVRScreenFade existente = FindFirstObjectByType<OVRScreenFade>(FindObjectsInactive.Include);
            if (existente != null)
                return existente;

            if (cameraRig == null)
                return null;

            Transform ancoraFade = cameraRig.centerEyeAnchor != null ? cameraRig.centerEyeAnchor : cameraRig.transform;
            var fadeGo = new GameObject("OVRScreenFade_Auto");
            fadeGo.transform.SetParent(ancoraFade, false);
            fadeGo.transform.localPosition = Vector3.zero;
            fadeGo.transform.localRotation = Quaternion.identity;

            var fade = fadeGo.AddComponent<OVRScreenFade>();
            fade.fadeOnStart = false;
            fade.fadeTime = Mathf.Max(0.01f, duracaoFadeVR);
            return fade;
        }

        private Quaternion CalcularCompensacaoYawEntrada()
        {
            if (cameraRig == null || cameraRig.centerEyeAnchor == null)
                return Quaternion.identity;

            Vector3 forwardLocal = cameraRig.centerEyeAnchor.localRotation * Vector3.forward;
            forwardLocal.y = 0f;

            if (forwardLocal.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.Inverse(Quaternion.LookRotation(forwardLocal.normalized, Vector3.up));
        }

        private void CapturarPoseForaAntesDeEntrarNoCarro()
        {
            if (cameraRig == null)
                return;

            Transform eyeAnchor = cameraRig.centerEyeAnchor;
            if (eyeAnchor != null)
            {
                _posicaoOlhosAntesDeEntrarNoCarro = eyeAnchor.position;
                _rotacaoOlhosAntesDeEntrarNoCarro = eyeAnchor.rotation;
                _poseForaCapturadaAntesDeEntrarNoCarro = true;
                return;
            }

            _posicaoOlhosAntesDeEntrarNoCarro = cameraRig.transform.position;
            _rotacaoOlhosAntesDeEntrarNoCarro = cameraRig.transform.rotation;
            _poseForaCapturadaAntesDeEntrarNoCarro = true;
        }

        private void RestaurarPoseForaDepoisDeSairDoCarro()
        {
            if (!_poseForaCapturadaAntesDeEntrarNoCarro || cameraRig == null)
            {
                AplicarPosicaoFora();
                return;
            }

            Quaternion rotacaoRig = _rotacaoOlhosAntesDeEntrarNoCarro;
            Transform eyeAnchor = cameraRig.centerEyeAnchor;
            if (eyeAnchor != null)
                rotacaoRig = _rotacaoOlhosAntesDeEntrarNoCarro * Quaternion.Inverse(eyeAnchor.localRotation);

            AplicarPoseCompensandoOlhos(_posicaoOlhosAntesDeEntrarNoCarro, rotacaoRig);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (pontoFora != null)
            {
                Vector3 posFora = pontoFora.position + pontoFora.TransformDirection(offsetFora);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(posFora, 0.15f);
                Gizmos.DrawLine(pontoFora.position, posFora);
                UnityEditor.Handles.color = Color.yellow;
                UnityEditor.Handles.Label(posFora + Vector3.up * 0.25f, "Fora do Carro");
            }

            Transform referenciaDentro = pontoAssento != null ? pontoAssento : transform;
            Vector3 posDentro = referenciaDentro.position + referenciaDentro.TransformDirection(offsetDentro + offsetSegurancaCabeca);
            Quaternion rotDentro = referenciaDentro.rotation * Quaternion.Euler(rotacaoDentro);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(posDentro, 0.15f);
            Gizmos.DrawLine(referenciaDentro.position, posDentro);
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.ArrowHandleCap(0, posDentro, rotDentro, 0.4f, EventType.Repaint);
            UnityEditor.Handles.Label(posDentro + Vector3.up * 0.25f, "Assento VR");
        }
#endif
    }
}
