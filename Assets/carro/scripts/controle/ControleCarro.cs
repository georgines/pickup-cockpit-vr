using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Vehicle
{
    /// <summary>
    /// Controla a física do carro. Não conhece teclado, gamepad nem VR.
    /// Recebe os valores de movimento via IEntradaCarro.
    /// Adicione quantos scripts de entrada quiser na lista — o maior valor vence.
    /// </summary>
    public class ControleCarro : MonoBehaviour
    {
        // ── Entradas ─────────────────────────────────────────────────────────

        [Header("Entradas")]
        [Tooltip("Adicione EntradaTeclado, EntradaXbox, EntradaVR ou qualquer outro " +
                 "script que implemente IEntradaCarro.")]
        public List<MonoBehaviour> entradas = new List<MonoBehaviour>();

        // ── Rádio ─────────────────────────────────────────────────────────────

        [Header("Rádio (opcional)")]
        [Tooltip("Arraste qualquer componente que implemente IRadioCarro (ex: TocadorSomCarro).")]
        public MonoBehaviour radio;

        // ── Colisores das Rodas ───────────────────────────────────────────────

        [Header("Colisores das Rodas")]
        [FormerlySerializedAs("frontLeftCollider")]
        public WheelCollider colisorRodaDianteiraEsquerda;
        [FormerlySerializedAs("frontRightCollider")]
        public WheelCollider colisorRodaDianteiraDireita;
        [FormerlySerializedAs("rearLeftCollider")]
        public WheelCollider colisorRodaTraseiraEsquerda;
        [FormerlySerializedAs("rearRightCollider")]
        public WheelCollider colisorRodaTraseiraDireita;

        // ── Malhas das Rodas ──────────────────────────────────────────────────

        [Header("Malhas das Rodas")]
        [FormerlySerializedAs("frontLeftWheel")]
        public Transform malhaRodaDianteiraEsquerda;
        [FormerlySerializedAs("frontRightWheel")]
        public Transform malhaRodaDianteiraDireita;
        [FormerlySerializedAs("rearLeftWheel")]
        public Transform malhaRodaTraseiraEsquerda;
        [FormerlySerializedAs("rearRightWheel")]
        public Transform malhaRodaTraseiraDireita;
        [FormerlySerializedAs("steeringWheel")]
        public Transform volanteDirecao;

        // ── Direção e Tração ──────────────────────────────────────────────────

        [Header("Direcao e Tracao")]
        [FormerlySerializedAs("motorTorque")]
        public float torqueMotor = 1400f;
        [FormerlySerializedAs("brakeTorque")]
        public float torqueFreio = 2800f;
        [FormerlySerializedAs("maxSteerAngle")]
        public float anguloMaximoDirecao = 30f;
        [FormerlySerializedAs("maxSpeedKmh")]
        public float velocidadeMaximaKmh = 120f;
        [FormerlySerializedAs("allWheelDrive")]
        public bool tracaoNasQuatroRodas = true;
        [FormerlySerializedAs("suavizacaoDirecao")]
        public float velocidadeSuavizacaoDirecao = 4f;
        [FormerlySerializedAs("fatorDirecaoAltaVelocidade")]
        public float fatorDirecaoAltaVelocidade = 0.35f;
        [FormerlySerializedAs("fatorTorqueEmCurva")]
        public float fatorTorqueEmCurva = 0.85f;
        [FormerlySerializedAs("fatorFreioSuave")]
        public float fatorFreioSuave = 1f;
        [FormerlySerializedAs("anguloMaximoVolante")]
        public float anguloMaximoVolante = 135f;
        [FormerlySerializedAs("suavizacaoVolante")]
        public float suavizacaoVolante = 10f;
        [FormerlySerializedAs("eixoRotacaoVolante")]
        public Vector3 eixoRotacaoVolante = new Vector3(0f, 0f, 1f);

        // ── Estabilidade ──────────────────────────────────────────────────────

        [Header("Estabilidade")]
        [FormerlySerializedAs("centerOfMassYOffset")]
        public float deslocamentoCentroMassaY = -0.45f;
        [FormerlySerializedAs("downforce")]
        public float forcaParaBaixo = 80f;
        [FormerlySerializedAs("forcaEstabilizacaoLateral")]
        public float forcaEstabilizacaoLateral = 3.5f;
        [FormerlySerializedAs("forcaEstabilizacaoFrontal")]
        public float forcaEstabilizacaoFrontal = 2.5f;
        [FormerlySerializedAs("velocidadeAngularMaxima")]
        public float velocidadeAngularMaxima = 6f;
        [FormerlySerializedAs("alinharRotacaoAoIniciar")]
        public bool alinharRotacaoAoIniciar = true;
        [FormerlySerializedAs("aderenciaLateral")]
        public float aderenciaLateral = 2.2f;
        [FormerlySerializedAs("aderenciaLateralAltaVelocidade")]
        public float aderenciaLateralAltaVelocidade = 3.4f;

        // ── Motor ─────────────────────────────────────────────────────────────

        [Header("Motor")]
        [FormerlySerializedAs("idleRpm")]
        public float rpmMarchaLenta = 850f;
        [FormerlySerializedAs("maxRpm")]
        public float rpmMaximo = 6800f;
        [FormerlySerializedAs("rpmResponse")]
        public float respostaRpm = 4.5f;

        // ── Áudio de Colisão ──────────────────────────────────────────────────

        [Header("Audio de Colisao")]
        [FormerlySerializedAs("audioSourceColisao")]
        public AudioSource fonteAudioColisao;
        [FormerlySerializedAs("audioColisaoPunch")]
        public AudioClip audioColisaoPunch;
        [FormerlySerializedAs("forcaMinimaColisaoAudio")]
        public float forcaMinimaColisaoAudio = 5f;
        [FormerlySerializedAs("intervaloAudioColisao")]
        public float intervaloAudioColisao = 0.12f;
        [FormerlySerializedAs("volumeAudioColisao")]
        public float volumeAudioColisao = 0.9f;

        // ── Áudio de Freio ────────────────────────────────────────────────────

        [Header("Audio de Freio")]
        [FormerlySerializedAs("fonteAudioFreio")]
        public AudioSource fonteAudioFreio;
        [FormerlySerializedAs("audioFreioSuave")]
        public AudioClip audioFreioSuave;
        [FormerlySerializedAs("audioFreioMao")]
        public AudioClip audioFreioMao;
        [FormerlySerializedAs("intervaloAudioFreioSuave")]
        public float intervaloAudioFreioSuave = 0.2f;
        [FormerlySerializedAs("volumeFreioSuaveMinimo")]
        public float volumeFreioSuaveMinimo = 0.15f;
        [FormerlySerializedAs("volumeFreioSuaveMaximo")]
        public float volumeFreioSuaveMaximo = 0.65f;
        [FormerlySerializedAs("volumeFreioMao")]
        public float volumeFreioMao = 0.8f;

        // ── Áudio de Buzina ───────────────────────────────────────────────────

        [Header("Audio de Buzina")]
        [FormerlySerializedAs("fonteAudioBuzina")]
        public AudioSource fonteAudioBuzina;
        [FormerlySerializedAs("audioBuzina")]
        public AudioClip audioBuzina;
        [FormerlySerializedAs("volumeBuzina")]
        public float volumeBuzina = 0.9f;

        // ── Áudio de Motor ────────────────────────────────────────────────────

        [Header("Audio de Motor (Pickup)")]
        [FormerlySerializedAs("fonteAudioMotorLigado")]
        public AudioSource fonteAudioMotorLigado;
        [FormerlySerializedAs("fonteAudioMotorAceleracao")]
        public AudioSource fonteAudioMotorAceleracao;
        [FormerlySerializedAs("audioMotorLigadoLoop")]
        public AudioClip audioMotorLigadoLoop;
        [FormerlySerializedAs("audioMotorAceleracaoLoop")]
        public AudioClip audioMotorAceleracaoLoop;
        [FormerlySerializedAs("volumeMotorLigado")]
        public float volumeMotorLigado = 0.55f;
        [FormerlySerializedAs("volumeMotorAceleracao")]
        public float volumeMotorAceleracao = 0.85f;
        [FormerlySerializedAs("suavizacaoAudioMotor")]
        public float suavizacaoAudioMotor = 3.5f;
        [FormerlySerializedAs("pitchMinimoMotor")]
        public float pitchMinimoMotor = 0.85f;
        [FormerlySerializedAs("pitchMaximoMotor")]
        public float pitchMaximoMotor = 1.25f;

        // ── Estado interno ────────────────────────────────────────────────────

        private Rigidbody rb;
        private float rpmAtual;
        private float entradaDirecaoSuavizada;
        private float ultimoTempoAudioColisao;
        private bool freioMaoEstavaAtivo;
        private bool freioMaoInicial = true; // carro inicia com freio de mão ativo

        /// <summary>Libera o freio de mão inicial. Chamado ao entrar no carro.</summary>
        public void LiberarFreioMaoInicial() => freioMaoInicial = false;

        /// <summary>Reativa o freio de mão inicial. Chamado ao sair do carro.</summary>
        public void ReativarFreioMaoInicial() => freioMaoInicial = true;
        private Quaternion rotacaoLocalInicialVolante = Quaternion.identity;
        private bool volanteInicializado;

        public float VelocidadeAtualKmh => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;
        public float RpmAtualMotor      => rpmAtual;

        // ── Unity ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            AutoCorrigirMapeamentoMalhas();

            if (alinharRotacaoAoIniciar)
            {
                float yaw = transform.eulerAngles.y;
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            if (rb != null)
            {
                rb.centerOfMass       = new Vector3(0f, deslocamentoCentroMassaY, 0f);
                rb.maxAngularVelocity = velocidadeAngularMaxima;
                rb.angularDamping     = Mathf.Max(rb.angularDamping, 1.25f);
                rb.linearVelocity     = Vector3.zero;
                rb.angularVelocity    = Vector3.zero;
            }

            rpmAtual = rpmMarchaLenta;
            GarantirAudioMotor();
            GarantirAudioFreio();
            GarantirAudioBuzina();
            InicializarVolante();
            AplicarFreioInicial();
        }

        private void AplicarFreioInicial()
        {
            if (!TodosColisoresAtribuidos()) return;
            colisorRodaDianteiraEsquerda.brakeTorque = torqueFreio;
            colisorRodaDianteiraDireita.brakeTorque  = torqueFreio;
            colisorRodaTraseiraEsquerda.brakeTorque  = torqueFreio;
            colisorRodaTraseiraDireita.brakeTorque   = torqueFreio;
        }

        private void OnCollisionEnter(Collision c) => TocarAudioColisao(c);
        private void OnCollisionStay(Collision c)  => TocarAudioColisao(c);

        private void OnValidate()
        {
            anguloMaximoVolante = Mathf.Clamp(anguloMaximoVolante, 0f, 135f);
            if (malhaRodaDianteiraEsquerda == transform) malhaRodaDianteiraEsquerda = null;
            if (malhaRodaDianteiraDireita  == transform) malhaRodaDianteiraDireita  = null;
            if (malhaRodaTraseiraEsquerda  == transform) malhaRodaTraseiraEsquerda  = null;
            if (malhaRodaTraseiraDireita   == transform) malhaRodaTraseiraDireita   = null;
            AutoCorrigirMapeamentoMalhas();
            if (!volanteInicializado && volanteDirecao != null)
            {
                rotacaoLocalInicialVolante = volanteDirecao.localRotation;
                volanteInicializado = true;
            }
        }

        private void Update()
        {
            // Buzina — wasPressedThisFrame precisa ser lido no Update
            if (ColetarBool(e => e.Buzina))
                AtualizarBuzina();

            // Respawn
            if (ColetarBool(e => e.Respawn))
                ExecutarRespawn();

            // Rádio
            if (radio is IRadioCarro radioCarro)
            {
                if (ColetarBool(e => e.AlternarRadio))
                    radioCarro.LigarEDesligarRadio();
                if (ColetarBool(e => e.PularEstacao))
                    radioCarro.PularEstacao();
            }
        }

        private void FixedUpdate()
        {
            if (!TodosColisoresAtribuidos()) return;

            if (rb != null && Time.timeSinceLevelLoad < 0.25f)
                rb.angularVelocity = Vector3.ClampMagnitude(rb.angularVelocity, 0.5f);

            // ── Coleta input (maior valor absoluto vence) ─────────────────────
            float entradaDirecao    = 0f;
            float entradaAceleracao = 0f;
            float entradaFreio      = 0f;
            bool  freioMao          = false;

            foreach (MonoBehaviour mb in entradas)
            {
                if (mb == null || !mb.isActiveAndEnabled) continue;
                if (mb is not IEntradaCarro e) continue;
                entradaDirecao    = MaiorAbsoluto(entradaDirecao, e.Direcao);
                entradaAceleracao = Mathf.Max(entradaAceleracao,  e.Aceleracao);
                entradaFreio      = Mathf.Max(entradaFreio,       e.Freio);
                freioMao          = freioMao || e.FreioMao;
            }

            // ── Lógica freio / ré ─────────────────────────────────────────────
            float entradaRe         = 0f;
            float entradaFreioSuave = entradaFreio;
            AplicarLogicaFreioERe(ref entradaRe, ref entradaFreioSuave);

            // ── Direção ───────────────────────────────────────────────────────
            float velocidadeKmh = VelocidadeAtualKmh;
            entradaDirecaoSuavizada = Mathf.MoveTowards(
                entradaDirecaoSuavizada, entradaDirecao,
                velocidadeSuavizacaoDirecao * Time.fixedDeltaTime);

            float tVelDir       = Mathf.InverseLerp(20f, velocidadeMaximaKmh, velocidadeKmh);
            float fatorDirecao  = Mathf.Lerp(1f, fatorDirecaoAltaVelocidade, tVelDir);
            float anguloDirecao = anguloMaximoDirecao * fatorDirecao * entradaDirecaoSuavizada;

            colisorRodaDianteiraEsquerda.steerAngle = anguloDirecao;
            colisorRodaDianteiraDireita.steerAngle  = anguloDirecao;

            // ── Tração ────────────────────────────────────────────────────────
            float comandoTracao   = Mathf.Clamp(entradaAceleracao - entradaRe, -1f, 1f);
            float penalidadeCurva = Mathf.Lerp(1f, fatorTorqueEmCurva, Mathf.Abs(entradaDirecaoSuavizada));
            float torque = Mathf.Abs(velocidadeKmh) < velocidadeMaximaKmh
                ? comandoTracao * torqueMotor * penalidadeCurva : 0f;

            colisorRodaTraseiraEsquerda.motorTorque  = torque;
            colisorRodaTraseiraDireita.motorTorque   = torque;
            colisorRodaDianteiraEsquerda.motorTorque = tracaoNasQuatroRodas ? torque * 0.35f : 0f;
            colisorRodaDianteiraDireita.motorTorque  = tracaoNasQuatroRodas ? torque * 0.35f : 0f;

            // ── Freio ─────────────────────────────────────────────────────────
            // Libera o freio de mão inicial assim que o jogador acelerar
            if (freioMaoInicial && entradaAceleracao > 0.01f)
                freioMaoInicial = false;

            float freioAplicado = Mathf.Clamp01(entradaFreioSuave) * torqueFreio * fatorFreioSuave;
            if (freioMao || freioMaoInicial) freioAplicado = torqueFreio;

            colisorRodaDianteiraEsquerda.brakeTorque = freioAplicado;
            colisorRodaDianteiraDireita.brakeTorque  = freioAplicado;
            colisorRodaTraseiraEsquerda.brakeTorque  = freioAplicado;
            colisorRodaTraseiraDireita.brakeTorque   = freioAplicado;

            // ── Física auxiliar ───────────────────────────────────────────────
            if (rb != null)
            {
                rb.AddForce(-transform.up * forcaParaBaixo * rb.linearVelocity.magnitude);
                AplicarAssistenciaEstabilidade();
                AplicarAderenciaLateral();
            }

            AtualizarRpm(comandoTracao, freioAplicado > 0.01f);
            AtualizarAudioMotor(Mathf.Abs(comandoTracao));
            AtualizarAudioFreio(freioMao);
            AtualizarVolante(anguloDirecao);

            AtualizarPoseRoda(colisorRodaDianteiraEsquerda, malhaRodaDianteiraEsquerda);
            AtualizarPoseRoda(colisorRodaDianteiraDireita,  malhaRodaDianteiraDireita);
            AtualizarPoseRoda(colisorRodaTraseiraEsquerda,  malhaRodaTraseiraEsquerda);
            AtualizarPoseRoda(colisorRodaTraseiraDireita,   malhaRodaTraseiraDireita);
        }

        // ── Respawn público ───────────────────────────────────────────────────

        /// <summary>
        /// Teleporta o carro para o ponto de spawn e zera a velocidade.
        /// Pode ser chamado por qualquer sistema externo.
        /// </summary>
        public void ExecutarRespawn()
        {
            RespawnCarro respawn = GetComponent<RespawnCarro>();
            if (respawn != null)
            {
                respawn.Respawnar();
                RestaurarEstadoAposRespawn();
                return;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.WakeUp();
            }

            RestaurarEstadoAposRespawn();
        }

        private void RestaurarEstadoAposRespawn()
        {
            freioMaoInicial = true;
            freioMaoEstavaAtivo = false;
            entradaDirecaoSuavizada = 0f;
            rpmAtual = rpmMarchaLenta;

            if (!TodosColisoresAtribuidos())
                return;

            colisorRodaDianteiraEsquerda.motorTorque = 0f;
            colisorRodaDianteiraDireita.motorTorque = 0f;
            colisorRodaTraseiraEsquerda.motorTorque = 0f;
            colisorRodaTraseiraDireita.motorTorque = 0f;

            colisorRodaDianteiraEsquerda.steerAngle = 0f;
            colisorRodaDianteiraDireita.steerAngle = 0f;

            colisorRodaDianteiraEsquerda.brakeTorque = torqueFreio;
            colisorRodaDianteiraDireita.brakeTorque = torqueFreio;
            colisorRodaTraseiraEsquerda.brakeTorque = torqueFreio;
            colisorRodaTraseiraDireita.brakeTorque = torqueFreio;
        }

        // ── Helpers de coleta ─────────────────────────────────────────────────

        private bool ColetarBool(System.Func<IEntradaCarro, bool> seletor)
        {
            foreach (MonoBehaviour mb in entradas)
            {
                if (mb == null || !mb.isActiveAndEnabled) continue;
                if (mb is IEntradaCarro e && seletor(e)) return true;
            }
            return false;
        }

        private static float MaiorAbsoluto(float a, float b) =>
            Mathf.Abs(a) >= Mathf.Abs(b) ? a : b;

        // ── Lógica freio / ré ─────────────────────────────────────────────────

        private void AplicarLogicaFreioERe(ref float entradaRe, ref float entradaFreioSuave)
        {
            float combinada = Mathf.Clamp01(entradaFreioSuave);
            if (combinada <= 0.001f || rb == null) { entradaFreioSuave = 0f; entradaRe = 0f; return; }
            float velFrente = transform.InverseTransformDirection(rb.linearVelocity).z;
            if (velFrente > 1.2f) { entradaFreioSuave = combinada; entradaRe = 0f; }
            else                  { entradaRe = combinada; entradaFreioSuave = 0f; }
        }

        // ── Física ────────────────────────────────────────────────────────────

        private void AplicarAssistenciaEstabilidade()
        {
            if (rb == null) return;
            Vector3 velAng    = transform.InverseTransformDirection(rb.angularVelocity);
            rb.AddRelativeTorque(new Vector3(-velAng.x * forcaEstabilizacaoFrontal, 0f, -velAng.z * forcaEstabilizacaoLateral), ForceMode.Acceleration);
            rb.maxAngularVelocity = velocidadeAngularMaxima;
        }

        private void AplicarAderenciaLateral()
        {
            if (rb == null) return;
            Vector3 velLocal = transform.InverseTransformDirection(rb.linearVelocity);
            float tVel       = Mathf.InverseLerp(10f, velocidadeMaximaKmh, VelocidadeAtualKmh);
            velLocal.x      *= Mathf.Clamp01(1f - Mathf.Lerp(aderenciaLateral, aderenciaLateralAltaVelocidade, tVel) * Time.fixedDeltaTime);
            rb.linearVelocity = transform.TransformDirection(velLocal);
            AjustarAderenciaRoda(colisorRodaDianteiraEsquerda, 2.3f, 2.0f);
            AjustarAderenciaRoda(colisorRodaDianteiraDireita,  2.3f, 2.0f);
            AjustarAderenciaRoda(colisorRodaTraseiraEsquerda,  2.5f, 2.2f);
            AjustarAderenciaRoda(colisorRodaTraseiraDireita,   2.5f, 2.2f);
        }

        private static void AjustarAderenciaRoda(WheelCollider c, float frente, float lateral)
        {
            if (c == null) return;
            WheelFrictionCurve ff = c.forwardFriction;  ff.stiffness = frente;  c.forwardFriction  = ff;
            WheelFrictionCurve fl = c.sidewaysFriction; fl.stiffness = lateral; c.sidewaysFriction = fl;
        }

        // ── RPM ───────────────────────────────────────────────────────────────

        private void AtualizarRpm(float aceleracao, bool freando)
        {
            float rpmRodas = (Mathf.Abs(colisorRodaTraseiraEsquerda.rpm) + Mathf.Abs(colisorRodaTraseiraDireita.rpm) +
                              Mathf.Abs(colisorRodaDianteiraEsquerda.rpm) + Mathf.Abs(colisorRodaDianteiraDireita.rpm)) * 0.25f;
            float fatorVel = Mathf.InverseLerp(0f, velocidadeMaximaKmh, VelocidadeAtualKmh);
            float alvoAce  = Mathf.Lerp(rpmMarchaLenta, rpmMaximo, Mathf.Clamp01(Mathf.Abs(aceleracao)));
            float rpmAlvo  = Mathf.Max(rpmMarchaLenta + rpmRodas * 18f, Mathf.Lerp(rpmMarchaLenta, alvoAce, fatorVel + 0.25f));
            if (freando && VelocidadeAtualKmh < 2f && Mathf.Abs(aceleracao) < 0.05f) rpmAlvo = rpmMarchaLenta;
            rpmAtual = Mathf.Clamp(Mathf.Lerp(rpmAtual, rpmAlvo, Time.fixedDeltaTime * respostaRpm), rpmMarchaLenta, rpmMaximo);
        }

        // ── Utilitários ───────────────────────────────────────────────────────

        private bool TodosColisoresAtribuidos() =>
            colisorRodaDianteiraEsquerda != null && colisorRodaDianteiraDireita != null &&
            colisorRodaTraseiraEsquerda  != null && colisorRodaTraseiraDireita  != null;

        private static void AtualizarPoseRoda(WheelCollider c, Transform m)
        {
            if (c == null || m == null) return;
            c.GetWorldPose(out Vector3 p, out Quaternion r);
            m.position = p; m.rotation = r;
        }

        private void InicializarVolante()
        {
            if (volanteDirecao == null) return;
            rotacaoLocalInicialVolante = volanteDirecao.localRotation;
            volanteInicializado = true;
        }

        private void AtualizarVolante(float angulo)
        {
            if (volanteDirecao == null) return;
            if (!volanteInicializado) InicializarVolante();
            float lim = Mathf.Clamp(anguloMaximoVolante, 0f, 135f);
            float t   = anguloMaximoDirecao > 0.01f ? angulo / anguloMaximoDirecao : 0f;
            Quaternion alvo = rotacaoLocalInicialVolante * Quaternion.AngleAxis(-Mathf.Clamp(t, -1f, 1f) * lim, eixoRotacaoVolante.normalized);
            volanteDirecao.localRotation = Quaternion.Slerp(volanteDirecao.localRotation, alvo, Time.fixedDeltaTime * suavizacaoVolante);
        }

        // ── Áudio ─────────────────────────────────────────────────────────────

        private void AtualizarBuzina()
        {
            if (fonteAudioBuzina == null || audioBuzina == null) return;
            fonteAudioBuzina.PlayOneShot(audioBuzina, volumeBuzina);
        }

        private void AtualizarAudioFreio(bool freioMaoAtivo)
        {
            if (fonteAudioFreio == null) return;
            if (freioMaoAtivo && !freioMaoEstavaAtivo && audioFreioMao != null)
                fonteAudioFreio.PlayOneShot(audioFreioMao, volumeFreioMao);
            freioMaoEstavaAtivo = freioMaoAtivo;
        }

        private void AtualizarAudioMotor(float intensidade)
        {
            if (fonteAudioMotorLigado == null || fonteAudioMotorAceleracao == null) return;
            if (audioMotorLigadoLoop     != null && !fonteAudioMotorLigado.isPlaying)    fonteAudioMotorLigado.Play();
            if (audioMotorAceleracaoLoop != null && !fonteAudioMotorAceleracao.isPlaying) fonteAudioMotorAceleracao.Play();
            float tRpm  = Mathf.InverseLerp(rpmMarchaLenta, rpmMaximo, RpmAtualMotor);
            float pitch = Mathf.Lerp(pitchMinimoMotor, pitchMaximoMotor, tRpm);
            fonteAudioMotorLigado.volume    = Mathf.Lerp(fonteAudioMotorLigado.volume,    Mathf.Lerp(volumeMotorLigado, volumeMotorLigado * 0.55f, tRpm), Time.fixedDeltaTime * suavizacaoAudioMotor);
            fonteAudioMotorAceleracao.volume = Mathf.Lerp(fonteAudioMotorAceleracao.volume, Mathf.Lerp(0.05f, volumeMotorAceleracao, Mathf.Clamp01(tRpm * 0.75f + intensidade * 0.7f)), Time.fixedDeltaTime * suavizacaoAudioMotor);
            fonteAudioMotorLigado.pitch     = Mathf.Lerp(fonteAudioMotorLigado.pitch,     Mathf.Clamp(pitch * 0.95f, pitchMinimoMotor, pitchMaximoMotor), Time.fixedDeltaTime * suavizacaoAudioMotor);
            fonteAudioMotorAceleracao.pitch  = Mathf.Lerp(fonteAudioMotorAceleracao.pitch,  pitch, Time.fixedDeltaTime * suavizacaoAudioMotor);
        }

        private void TocarAudioColisao(Collision colisao)
        {
            if (audioColisaoPunch == null || fonteAudioColisao == null || colisao == null) return;
            if (Time.time - ultimoTempoAudioColisao < intervaloAudioColisao) return;
            if (colisao.relativeVelocity.magnitude < forcaMinimaColisaoAudio) return;
            fonteAudioColisao.PlayOneShot(audioColisaoPunch, volumeAudioColisao);
            ultimoTempoAudioColisao = Time.time;
        }

        private void GarantirAudioMotor()
        {
            if (fonteAudioMotorLigado == null || fonteAudioMotorAceleracao == null)
            {
                foreach (AudioSource f in GetComponents<AudioSource>())
                {
                    if (f == null) continue;
                    if (fonteAudioMotorLigado    == null && f != fonteAudioColisao) fonteAudioMotorLigado    = f;
                    else if (fonteAudioMotorAceleracao == null && f != fonteAudioColisao && f != fonteAudioMotorLigado) fonteAudioMotorAceleracao = f;
                }
            }
            if (fonteAudioMotorLigado    == null) fonteAudioMotorLigado    = gameObject.AddComponent<AudioSource>();
            if (fonteAudioMotorAceleracao == null) fonteAudioMotorAceleracao = gameObject.AddComponent<AudioSource>();
            ConfigurarFonteMotor(fonteAudioMotorLigado,    audioMotorLigadoLoop);
            ConfigurarFonteMotor(fonteAudioMotorAceleracao, audioMotorAceleracaoLoop);
        }

        private static void ConfigurarFonteMotor(AudioSource f, AudioClip clip)
        {
            if (f == null) return;
            f.playOnAwake = false; f.loop = true; f.spatialBlend = 1f;
            f.rolloffMode = AudioRolloffMode.Logarithmic;
            f.minDistance = 4f; f.maxDistance = 45f; f.dopplerLevel = 0.05f; f.clip = clip;
        }

        private void GarantirAudioFreio()
        {
            if (fonteAudioFreio == null) fonteAudioFreio = gameObject.AddComponent<AudioSource>();
            fonteAudioFreio.playOnAwake = false; fonteAudioFreio.loop = false; fonteAudioFreio.spatialBlend = 1f;
            fonteAudioFreio.rolloffMode = AudioRolloffMode.Logarithmic;
            fonteAudioFreio.minDistance = 3f; fonteAudioFreio.maxDistance = 28f; fonteAudioFreio.dopplerLevel = 0.02f;
        }

        private void GarantirAudioBuzina()
        {
            if (fonteAudioBuzina == null) fonteAudioBuzina = gameObject.AddComponent<AudioSource>();
            fonteAudioBuzina.playOnAwake = false; fonteAudioBuzina.loop = false; fonteAudioBuzina.spatialBlend = 1f;
            fonteAudioBuzina.rolloffMode = AudioRolloffMode.Logarithmic;
            fonteAudioBuzina.minDistance = 4f; fonteAudioBuzina.maxDistance = 55f; fonteAudioBuzina.dopplerLevel = 0.04f;
        }

        // ── Auto-configuração de malhas ───────────────────────────────────────

        private void AutoCorrigirMapeamentoMalhas()
        {
            if (!TodosColisoresAtribuidos()) return;
            bool invalido =
                malhaRodaDianteiraEsquerda == null || malhaRodaDianteiraDireita == null ||
                malhaRodaTraseiraEsquerda  == null || malhaRodaTraseiraDireita  == null ||
                malhaRodaDianteiraEsquerda == transform || malhaRodaDianteiraDireita == transform ||
                malhaRodaTraseiraEsquerda  == transform || malhaRodaTraseiraDireita  == transform ||
                malhaRodaDianteiraEsquerda == malhaRodaDianteiraDireita ||
                malhaRodaTraseiraEsquerda  == malhaRodaTraseiraDireita  ||
                malhaRodaDianteiraEsquerda == malhaRodaTraseiraEsquerda ||
                malhaRodaDianteiraEsquerda == malhaRodaTraseiraDireita  ||
                malhaRodaDianteiraDireita  == malhaRodaTraseiraEsquerda ||
                malhaRodaDianteiraDireita  == malhaRodaTraseiraDireita;
            if (!invalido) return;

            Transform[] c = BuscarCandidatasMalhaRoda();
            if (c.Length < 4) return;
            if (malhaRodaDianteiraEsquerda == null) malhaRodaDianteiraEsquerda = EscolherMaisProxima(colisorRodaDianteiraEsquerda, c, null, null, null);
            if (malhaRodaDianteiraDireita  == null) malhaRodaDianteiraDireita  = EscolherMaisProxima(colisorRodaDianteiraDireita,  c, malhaRodaDianteiraEsquerda, null, null);
            if (malhaRodaTraseiraEsquerda  == null) malhaRodaTraseiraEsquerda  = EscolherMaisProxima(colisorRodaTraseiraEsquerda,  c, malhaRodaDianteiraEsquerda, malhaRodaDianteiraDireita, null);
            if (malhaRodaTraseiraDireita   == null) malhaRodaTraseiraDireita   = EscolherMaisProxima(colisorRodaTraseiraDireita,   c, malhaRodaDianteiraEsquerda, malhaRodaDianteiraDireita, malhaRodaTraseiraEsquerda);
            if (volanteDirecao == null)
            {
                volanteDirecao = BuscarVolanteDirecao();
                if (volanteDirecao != null) { rotacaoLocalInicialVolante = volanteDirecao.localRotation; volanteInicializado = true; }
            }
        }

        private Transform[] BuscarCandidatasMalhaRoda()
        {
            var lista = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t == transform) continue;
                string nome = t.name.ToLowerInvariant();
                if (!nome.Contains("roda") && !nome.Contains("wheel") && !nome.Contains("tire") && !nome.Contains("pneu")) continue;
                if (t.GetComponent<MeshRenderer>() == null && t.GetComponentInChildren<MeshRenderer>(true) == null) continue;
                lista.Add(t);
            }
            return lista.ToArray();
        }

        private static Transform EscolherMaisProxima(WheelCollider col, Transform[] cands, Transform u1, Transform u2, Transform u3)
        {
            if (col == null) return null;
            float menor = float.MaxValue; Transform melhor = null;
            foreach (Transform c in cands)
            {
                if (c == u1 || c == u2 || c == u3) continue;
                float d = (c.position - col.transform.position).sqrMagnitude;
                if (d < menor) { menor = d; melhor = c; }
            }
            return melhor;
        }

        private Transform BuscarVolanteDirecao()
        {
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string nome = t.name.ToLowerInvariant();
                if (nome.Contains("volante") || nome.Contains("direcao") || nome.Contains("deracao") || nome.Contains("steer"))
                    return t;
            }
            return null;
        }
    }
}
