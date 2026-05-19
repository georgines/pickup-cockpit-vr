using UnityEngine;
using UnityEditor;

namespace Vehicle
{
    /// <summary>
    /// Editor para VRMotoristaCarro com modos de calibração interativos.
    ///
    /// FUNCIONA EM EDIT MODE:
    ///   Não precisa entrar em Play. Clica no botão de modo, edita os campos,
    ///   e o cameraRig é reposicionado em tempo real. Suporta Undo (Ctrl+Z).
    /// </summary>
    [CustomEditor(typeof(VRMotoristaCarro))]
    public class VRMotoristoCarroEditor : Editor
    {
        private enum ModoPreview { Nenhum, Fora, Dentro }
        private ModoPreview _modo = ModoPreview.Nenhum;

        // Cache de SerializedProperties — evita o bug de "atribuição direta
        // sobrescrita por ApplyModifiedProperties".
        private SerializedProperty _propOffsetFora;
        private SerializedProperty _propRotacaoFora;
        private SerializedProperty _propOffsetDentro;
        private SerializedProperty _propRotacaoDentro;

        private void OnEnable()
        {
            _propOffsetFora    = serializedObject.FindProperty("offsetFora");
            _propRotacaoFora   = serializedObject.FindProperty("rotacaoFora");
            _propOffsetDentro  = serializedObject.FindProperty("offsetDentro");
            _propRotacaoDentro = serializedObject.FindProperty("rotacaoDentro");
        }

        private void OnDisable()
        {
            // Sair do modo calibração ao desselecionar o objeto.
            _modo = ModoPreview.Nenhum;
        }

        public override void OnInspectorGUI()
        {
            var alvo = (VRMotoristaCarro)target;
            serializedObject.Update();

            // ── Campos padrão (exceto os de calibração) ──────────────────────
            DesenharCamposPadrao();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Calibração do Assento", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // ── Botão FORA + campos ──────────────────────────────────────────
            DesenharBotaoModo(
                ModoPreview.Fora,
                "🌍  Ajustar posição FORA do carro",
                "✔  Ajustando FORA do carro (clique para sair)",
                new Color(0.85f, 0.85f, 0.3f),
                new Color(1f, 0.85f, 0.2f));

            if (_modo == ModoPreview.Fora)
            {
                DesenharCamposCalibracao(
                    alvo,
                    _propOffsetFora, _propRotacaoFora,
                    "Fora",
                    alvo.AplicarPosicaoFora);
            }

            EditorGUILayout.Space(4);

            // ── Botão DENTRO + campos ────────────────────────────────────────
            DesenharBotaoModo(
                ModoPreview.Dentro,
                "🚗  Ajustar posição DENTRO do carro (Assento)",
                "✔  Ajustando DENTRO do carro (clique para sair)",
                new Color(0.3f, 0.7f, 0.35f),
                new Color(0.2f, 1f, 0.4f));

            if (_modo == ModoPreview.Dentro)
            {
                DesenharCamposCalibracao(
                    alvo,
                    _propOffsetDentro, _propRotacaoDentro,
                    "Dentro",
                    alvo.AplicarPosicaoDentro);
            }

            EditorGUILayout.Space(6);
            DesenharHelpBox();

            // Aplica todas as mudanças pendentes do serializedObject
            // (já fizemos ApplyModifiedProperties dentro de DesenharCamposCalibracao
            //  para callbacks imediatos, mas garantimos por segurança).
            serializedObject.ApplyModifiedProperties();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private void DesenharBotaoModo(
            ModoPreview modo,
            string labelInativo,
            string labelAtivo,
            Color corInativa,
            Color corAtiva)
        {
            GUI.backgroundColor = _modo == modo ? corAtiva : corInativa;
            string label = _modo == modo ? labelAtivo : labelInativo;

            if (GUILayout.Button(label, GUILayout.Height(36)))
            {
                AplicarModo(_modo == modo ? ModoPreview.Nenhum : modo);
            }

            GUI.backgroundColor = Color.white;
        }

        private void DesenharCamposCalibracao(
            VRMotoristaCarro alvo,
            SerializedProperty propOffset,
            SerializedProperty propRotacao,
            string sufixo,
            System.Action aplicar)
        {
            EditorGUILayout.Space(4);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(propOffset,  new GUIContent("Offset "  + sufixo));
            EditorGUILayout.PropertyField(propRotacao, new GUIContent("Rotação " + sufixo));

            if (EditorGUI.EndChangeCheck())
            {
                // Ordem CRÍTICA:
                // 1) Aplica os SerializedProperty no objeto real
                // 2) Registra Undo do cameraRig que vai ser movido
                // 3) Chama AplicarPosicaoFora/Dentro (lê os novos valores)
                // 4) Marca dirty para a Scene salvar
                serializedObject.ApplyModifiedProperties();

                if (alvo.cameraRig != null)
                {
                    Undo.RecordObject(alvo.cameraRig.transform, "Calibrar Posição " + sufixo);
                    aplicar();
                    EditorUtility.SetDirty(alvo.cameraRig.transform);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "CameraRig não atribuído — não posso reposicionar.",
                        MessageType.Warning);
                }

                EditorUtility.SetDirty(alvo);
                SceneView.RepaintAll();

                // Re-sincroniza para a próxima iteração do GUI.
                serializedObject.Update();
            }
        }

        private void DesenharHelpBox()
        {
            switch (_modo)
            {
                case ModoPreview.Nenhum:
                    EditorGUILayout.HelpBox(
                        "Clique em um dos botões para entrar no modo de ajuste.\n" +
                        "Os campos Offset/Rotação só aparecem dentro do modo ativo.\n" +
                        "Mudanças são aplicadas em tempo real ao CameraRig.",
                        MessageType.Info);
                    break;

                case ModoPreview.Fora:
                    EditorGUILayout.HelpBox(
                        "MODO ATIVO: FORA DO CARRO — gizmo amarelo na Scene View.\n" +
                        "Clique e arraste a legenda X / Y / Z para ajustar.\n" +
                        "Undo (Ctrl+Z) disponível.",
                        MessageType.Warning);
                    break;

                case ModoPreview.Dentro:
                    EditorGUILayout.HelpBox(
                        "MODO ATIVO: DENTRO DO CARRO — gizmo ciano na Scene View.\n" +
                        "Posicione o ponto onde a CABEÇA do motorista deve ficar.\n" +
                        "Clique e arraste a legenda X / Y / Z para ajustar.",
                        MessageType.Warning);
                    break;
            }
        }

        private void DesenharCamposPadrao()
        {
            // Itera todas as SerializedProperties exceto m_Script e os campos
            // de calibração (que são desenhados manualmente nos modos).
            var prop = serializedObject.GetIterator();
            prop.NextVisible(true); // pular m_Script

            while (prop.NextVisible(false))
            {
                if (prop.name == "offsetFora"   ||
                    prop.name == "rotacaoFora"  ||
                    prop.name == "offsetDentro" ||
                    prop.name == "rotacaoDentro")
                    continue;

                EditorGUILayout.PropertyField(prop, true);
            }
        }

        private void AplicarModo(ModoPreview modo)
        {
            var alvo = (VRMotoristaCarro)target;
            if (alvo == null) return;

            _modo = modo;

            if (alvo.cameraRig == null) return;

            // Ao entrar num modo, reposiciona imediatamente para mostrar
            // visualmente onde estamos calibrando.
            Undo.RecordObject(alvo.cameraRig.transform, "Preview Posição VR");

            switch (modo)
            {
                case ModoPreview.Fora:   alvo.AplicarPosicaoFora();   break;
                case ModoPreview.Dentro: alvo.AplicarPosicaoDentro(); break;
            }

            EditorUtility.SetDirty(alvo.cameraRig.transform);
            SceneView.RepaintAll();
        }
    }
}
