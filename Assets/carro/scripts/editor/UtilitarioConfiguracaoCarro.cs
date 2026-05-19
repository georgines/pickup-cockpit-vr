using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vehicle;

/// <summary>
/// Utilitário de configuração do carro.
/// Selecione o GameObject do carro na Hierarchy e acesse:
/// Tools → Veículo → Configurar Carro Selecionado
/// </summary>
public static class UtilitarioConfiguracaoCarro
{
    [MenuItem("Tools/Veículo/Configurar Carro Selecionado")]
    public static void ConfigurarCarroSelecionado()
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("[UtilitarioConfiguracaoCarro] Selecione um GameObject de carro na Hierarchy antes de executar.");
            return;
        }

        GameObject carro = Selection.activeGameObject;

        // ── Rigidbody ─────────────────────────────────────────────────────────
        Rigidbody rb = carro.GetComponent<Rigidbody>() ?? carro.AddComponent<Rigidbody>();
        rb.mass                    = 1200f;
        rb.linearDamping           = 0.02f;
        rb.angularDamping          = 1.25f;
        rb.interpolation           = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode  = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass            = new Vector3(0f, -0.45f, 0f);
        rb.maxAngularVelocity      = 4.5f;

        // ── Collider do corpo ─────────────────────────────────────────────────
        BoxCollider colisaoCarro = carro.GetComponent<BoxCollider>() ?? carro.AddComponent<BoxCollider>();
        colisaoCarro.center = new Vector3(0f, 0.85f, 0f);
        colisaoCarro.size   = new Vector3(2f, 1.4f, 4.3f);

        // ── Detecção de rodas ─────────────────────────────────────────────────
        Transform rodaDE = EncontrarRoda(carro.transform, new[] { "fl", "front_left", "front left", "roda_esquerda_frontal", "dianteira esquerda", "roda dianteira esquerda" });
        Transform rodaDD = EncontrarRoda(carro.transform, new[] { "fr", "front_right", "front right", "roda_direita_frontal", "dianteira direita", "roda dianteira direita" });
        Transform rodaTE = EncontrarRoda(carro.transform, new[] { "bl", "rl", "rear_left", "rear left", "roda_esquerda_traseira", "traseira esquerda", "roda traseira esquerda" });
        Transform rodaTD = EncontrarRoda(carro.transform, new[] { "br", "rr", "rear_right", "rear right", "roda_direita_traseira", "traseira direita", "roda traseira direita" });

        if (rodaDE == null || rodaDD == null || rodaTE == null || rodaTD == null)
            InferirRodasPorPosicao(carro.transform, ref rodaDE, ref rodaDD, ref rodaTE, ref rodaTD);

        GarantirRodasUnicas(carro.transform, ref rodaDE, ref rodaDD, ref rodaTE, ref rodaTD);

        if (rodaDE == null || rodaDD == null || rodaTE == null || rodaTD == null)
        {
            Debug.LogError("[UtilitarioConfiguracaoCarro] Não foi possível encontrar as 4 rodas automaticamente.\nRenomeie as rodas com frente/traseira/esquerda/direita ou configure manualmente.");
            return;
        }

        Debug.Log($"[UtilitarioConfiguracaoCarro] Rodas detectadas: DE={rodaDE.name}, DD={rodaDD.name}, TE={rodaTE.name}, TD={rodaTD.name}");

        // ── WheelColliders ────────────────────────────────────────────────────
        WheelCollider wcDE = CriarOuObterWheelCollider(carro.transform, "CR_RodaDianteiraEsquerda", rodaDE.position);
        WheelCollider wcDD = CriarOuObterWheelCollider(carro.transform, "CR_RodaDianteiraDireita",  rodaDD.position);
        WheelCollider wcTE = CriarOuObterWheelCollider(carro.transform, "CR_RodaTraseiraEsquerda",  rodaTE.position);
        WheelCollider wcTD = CriarOuObterWheelCollider(carro.transform, "CR_RodaTraseiraDireita",   rodaTD.position);

        ConfigurarWheelCollider(wcDE, eixoDirecao: true);
        ConfigurarWheelCollider(wcDD, eixoDirecao: true);
        ConfigurarWheelCollider(wcTE, eixoDirecao: false);
        ConfigurarWheelCollider(wcTD, eixoDirecao: false);

        // ── ControleCarro ─────────────────────────────────────────────────────
        ControleCarro controleCarro = carro.GetComponent<ControleCarro>() ?? carro.AddComponent<ControleCarro>();
        controleCarro.colisorRodaDianteiraEsquerda = wcDE;
        controleCarro.colisorRodaDianteiraDireita  = wcDD;
        controleCarro.colisorRodaTraseiraEsquerda  = wcTE;
        controleCarro.colisorRodaTraseiraDireita   = wcTD;
        controleCarro.malhaRodaDianteiraEsquerda   = rodaDE;
        controleCarro.malhaRodaDianteiraDireita    = rodaDD;
        controleCarro.malhaRodaTraseiraEsquerda    = rodaTE;
        controleCarro.malhaRodaTraseiraDireita     = rodaTD;

        if (controleCarro.volanteDirecao == null)
            controleCarro.volanteDirecao = BuscarVolante(carro.transform);

        // ── Entradas ──────────────────────────────────────────────────────────
        EntradaTeclado entradaTeclado = carro.GetComponent<EntradaTeclado>() ?? carro.AddComponent<EntradaTeclado>();
        EntradaXbox    entradaXbox   = carro.GetComponent<EntradaXbox>()    ?? carro.AddComponent<EntradaXbox>();

        // Garante que as entradas estão na lista do controlador
        if (!controleCarro.entradas.Contains(entradaTeclado))
            controleCarro.entradas.Add(entradaTeclado);
        if (!controleCarro.entradas.Contains(entradaXbox))
            controleCarro.entradas.Add(entradaXbox);

        // ── Áudio ─────────────────────────────────────────────────────────────
        ConfigurarAudioColisao(carro, controleCarro);
        ConfigurarAudioMotor(carro, controleCarro);
        ConfigurarAudioBuzina(carro, controleCarro);

        // ── RespawnCarro ──────────────────────────────────────────────────────
        RespawnCarro respawn = carro.GetComponent<RespawnCarro>() ?? carro.AddComponent<RespawnCarro>();
        respawn.SalvarPontoSpawn();

        EditorUtility.SetDirty(carro);
        Debug.Log("[UtilitarioConfiguracaoCarro] Carro configurado com sucesso!\nControles: W/S acelerar/frear, A/D direção, Espaço freio de mão, R respawn, Setas câmera, Q alternar câmera.");
    }

    // ── Detecção de rodas ─────────────────────────────────────────────────────

    private static Transform EncontrarRoda(Transform raiz, IEnumerable<string> tokens)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t == raiz) continue;
            string nome = t.name.ToLowerInvariant();
            if (EhObjetoColisor(nome)) continue;

            foreach (string token in tokens)
            {
                string tok = token.ToLowerInvariant();
                if (!nome.Contains(tok)) continue;
                bool tokenCurto = tok.Length <= 2;
                if (tokenCurto && !EhNomeDeRoda(nome)) continue;
                if (EhNomeDeRoda(nome) || !tokenCurto) return t;
            }
        }
        return null;
    }

    private static void InferirRodasPorPosicao(
        Transform raiz,
        ref Transform rodaDE, ref Transform rodaDD,
        ref Transform rodaTE, ref Transform rodaTD)
    {
        var candidatas = raiz
            .GetComponentsInChildren<Transform>(true)
            .Where(t => !EhObjetoColisor(t.name.ToLowerInvariant()))
            .Where(EhCandidataRoda)
            .OrderByDescending(t => raiz.InverseTransformPoint(t.position).z)
            .ToList();

        if (candidatas.Count < 4) return;

        var dianteiras = candidatas.Take(2).OrderBy(t => raiz.InverseTransformPoint(t.position).x).ToList();
        var traseiras  = candidatas.Skip(2).Take(2).OrderBy(t => raiz.InverseTransformPoint(t.position).x).ToList();

        if (dianteiras.Count == 2) { rodaDE ??= dianteiras[0]; rodaDD ??= dianteiras[1]; }
        if (traseiras.Count  == 2) { rodaTE ??= traseiras[0];  rodaTD ??= traseiras[1];  }
    }

    private static void GarantirRodasUnicas(
        Transform raiz,
        ref Transform rodaDE, ref Transform rodaDD,
        ref Transform rodaTE, ref Transform rodaTD)
    {
        var usadas = new HashSet<Transform>();
        if (rodaDE != null) usadas.Add(rodaDE);
        if (rodaDD != null && !usadas.Add(rodaDD)) rodaDD = null;
        if (rodaTE != null && !usadas.Add(rodaTE)) rodaTE = null;
        if (rodaTD != null && !usadas.Add(rodaTD)) rodaTD = null;

        if (rodaDE != null && rodaDD != null && rodaTE != null && rodaTD != null) return;

        var restantes = raiz
            .GetComponentsInChildren<Transform>(true)
            .Where(t => !EhObjetoColisor(t.name.ToLowerInvariant()))
            .Where(EhCandidataRoda)
            .Where(t => !usadas.Contains(t))
            .OrderByDescending(t => raiz.InverseTransformPoint(t.position).z)
            .ToList();

        PreencherRodaFaltante(raiz, ref rodaDE, restantes, frente: true,  esquerda: true);
        PreencherRodaFaltante(raiz, ref rodaDD, restantes, frente: true,  esquerda: false);
        PreencherRodaFaltante(raiz, ref rodaTE, restantes, frente: false, esquerda: true);
        PreencherRodaFaltante(raiz, ref rodaTD, restantes, frente: false, esquerda: false);
    }

    private static void PreencherRodaFaltante(Transform raiz, ref Transform destino, List<Transform> candidatas, bool frente, bool esquerda)
    {
        if (destino != null) return;
        Transform escolhida = candidatas
            .OrderByDescending(t => frente ? raiz.InverseTransformPoint(t.position).z : -raiz.InverseTransformPoint(t.position).z)
            .ThenBy(t => esquerda ? raiz.InverseTransformPoint(t.position).x : -raiz.InverseTransformPoint(t.position).x)
            .FirstOrDefault();
        if (escolhida == null) return;
        destino = escolhida;
        candidatas.Remove(escolhida);
    }

    private static bool EhCandidataRoda(Transform t)
    {
        string nome = t.name.ToLowerInvariant();
        return EhNomeDeRoda(nome) &&
               (t.GetComponent<MeshRenderer>() != null || t.GetComponentInChildren<MeshRenderer>(true) != null);
    }

    private static bool EhObjetoColisor(string nome) => nome.StartsWith("wc_") || nome.StartsWith("cr_");

    private static bool EhNomeDeRoda(string nome) =>
        nome.Contains("roda") || nome.Contains("wheel") || nome.Contains("tire") || nome.Contains("pneu");

    // ── WheelCollider ─────────────────────────────────────────────────────────

    private static WheelCollider CriarOuObterWheelCollider(Transform raiz, string nomeObjeto, Vector3 posicao)
    {
        Transform encontrado = raiz.Find(nomeObjeto);
        GameObject go;

        if (encontrado == null)
        {
            go = new GameObject(nomeObjeto);
            go.transform.SetParent(raiz);
        }
        else
        {
            go = encontrado.gameObject;
        }

        go.transform.position = posicao;
        return go.GetComponent<WheelCollider>() ?? go.AddComponent<WheelCollider>();
    }

    private static void ConfigurarWheelCollider(WheelCollider wc, bool eixoDirecao)
    {
        wc.radius            = 0.47f;
        wc.suspensionDistance = 0.2f;

        JointSpring mola = wc.suspensionSpring;
        mola.spring         = 30000f;
        mola.damper         = 4500f;
        mola.targetPosition = 0.5f;
        wc.suspensionSpring = mola;

        WheelFrictionCurve friccaoFrente = wc.forwardFriction;
        friccaoFrente.extremumSlip   = 0.4f;
        friccaoFrente.extremumValue  = 1f;
        friccaoFrente.asymptoteSlip  = 0.8f;
        friccaoFrente.asymptoteValue = 0.75f;
        friccaoFrente.stiffness      = 1.6f;
        wc.forwardFriction           = friccaoFrente;

        WheelFrictionCurve friccaoLateral = wc.sidewaysFriction;
        friccaoLateral.extremumSlip   = 0.2f;
        friccaoLateral.extremumValue  = 1f;
        friccaoLateral.asymptoteSlip  = 0.5f;
        friccaoLateral.asymptoteValue = 0.8f;
        friccaoLateral.stiffness      = eixoDirecao ? 1.8f : 2f;
        wc.sidewaysFriction           = friccaoLateral;
    }

    // ── Áudio ─────────────────────────────────────────────────────────────────

    private static void ConfigurarAudioColisao(GameObject carro, ControleCarro controleCarro)
    {
        if (controleCarro.fonteAudioColisao == null)
        {
            AudioSource fonte = carro.GetComponent<AudioSource>() ?? carro.AddComponent<AudioSource>();
            fonte.playOnAwake  = false;
            fonte.spatialBlend = 1f;
            fonte.rolloffMode  = AudioRolloffMode.Logarithmic;
            fonte.minDistance  = 3f;
            fonte.maxDistance  = 35f;
            fonte.dopplerLevel = 0f;
            controleCarro.fonteAudioColisao = fonte;
        }

        if (controleCarro.audioColisaoPunch == null)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Free Pack/Heavy Object Impact 10.wav");
            if (clip != null) controleCarro.audioColisaoPunch = clip;
        }
    }

    private static void ConfigurarAudioMotor(GameObject carro, ControleCarro controleCarro)
    {
        if (controleCarro.fonteAudioMotorLigado == null)
        {
            AudioSource fonte = carro.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            controleCarro.fonteAudioMotorLigado = fonte;
        }

        if (controleCarro.fonteAudioMotorAceleracao == null)
        {
            AudioSource fonte = carro.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            controleCarro.fonteAudioMotorAceleracao = fonte;
        }

        if (controleCarro.fonteAudioFreio == null)
        {
            AudioSource fonte = carro.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            controleCarro.fonteAudioFreio = fonte;
        }

        controleCarro.audioFreioSuave = null;

        if (controleCarro.audioMotorLigadoLoop == null)
            controleCarro.audioMotorLigadoLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Vehicle_Essentials/Vehicle_Van/Vehicle_Van_Engine/Vehicle_Van_Idle_Exterior_Rear_Loop_01.wav");

        if (controleCarro.audioMotorAceleracaoLoop == null)
            controleCarro.audioMotorAceleracaoLoop = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Vehicle_Essentials/Vehicle_Van/Vehicle_Van_Drive/Vehicle_Van_Drive_Exterior_Loop_01.wav");

        if (controleCarro.audioFreioMao == null)
            controleCarro.audioFreioMao = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Vehicle_Essentials/Vehicle_Car/Vehicle_Car_Handbrake/Vehicle_Car_HandBrake_04.wav");
    }

    private static void ConfigurarAudioBuzina(GameObject carro, ControleCarro controleCarro)
    {
        if (controleCarro.fonteAudioBuzina == null)
        {
            AudioSource fonte = carro.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            controleCarro.fonteAudioBuzina = fonte;
        }

        if (controleCarro.audioBuzina == null)
            controleCarro.audioBuzina = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Vehicle_Essentials/Vehicle_Car/Vehicle_Car_Horns/Vehicle_Car_Horn_Exterior.wav");
    }

    // ── Volante ───────────────────────────────────────────────────────────────

    private static Transform BuscarVolante(Transform raiz)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            string nome = t.name.ToLowerInvariant();
            if (nome.Contains("volante") || nome.Contains("direcao") || nome.Contains("deracao") || nome.Contains("steer"))
                return t;
        }
        return null;
    }
}
