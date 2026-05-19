using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class EncontraAssetsNaoUsados
{
    [MenuItem("Tools/Encontrar Assets Nao Usados")]
    public static void Executar()
    {
        // 1. Coleta todas as cenas do projeto
        string[] todasCenas = AssetDatabase.FindAssets("t:Scene")
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .ToArray();

        Debug.Log($"[NaoUsados] Cenas encontradas: {todasCenas.Length}");
        foreach (var c in todasCenas) Debug.Log($"  Cena: {c}");

        // 2. Coleta todas as dependências das cenas (recursivo)
        HashSet<string> usados = new HashSet<string>(
            AssetDatabase.GetDependencies(todasCenas, recursive: true)
        );

        // Adiciona as próprias cenas como usadas
        foreach (var c in todasCenas) usados.Add(c);

        // 3. Coleta todos os assets do projeto (exceto Editor-only e packages)
        string[] todosGuids = AssetDatabase.FindAssets("", new[] { "Assets" });
        var todosPaths = todosGuids
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => !string.IsNullOrEmpty(p) && !AssetDatabase.IsValidFolder(p))
            .Distinct()
            .ToList();

        Debug.Log($"[NaoUsados] Total de assets: {todosPaths.Count}");
        Debug.Log($"[NaoUsados] Assets usados (dependências): {usados.Count}");

        // 4. Subtrai usados
        var naoUsados = todosPaths
            .Where(p => !usados.Contains(p))
            .OrderBy(p => p)
            .ToList();

        Debug.Log($"[NaoUsados] Assets NÃO usados: {naoUsados.Count}");

        // 5. Serializa resultado como JSON simples para leitura externa
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[");
        for (int i = 0; i < naoUsados.Count; i++)
        {
            string virgula = i < naoUsados.Count - 1 ? "," : "";
            sb.AppendLine($"  \"{naoUsados[i].Replace("\\", "/")}\"{virgula}");
        }
        sb.AppendLine("]");

        string saida = Path.Combine(
            Application.dataPath.Replace("Assets", ""),
            "assets_nao_usados_raw.json"
        );
        File.WriteAllText(saida, sb.ToString(), System.Text.Encoding.UTF8);
        Debug.Log($"[NaoUsados] Resultado salvo em: {saida}");
        EditorUtility.DisplayDialog("Assets Não Usados",
            $"Análise concluída!\n\nTotal de assets: {todosPaths.Count}\nUsados: {usados.Count}\nNão usados: {naoUsados.Count}\n\nResultado: assets_nao_usados_raw.json",
            "OK");
    }
}
