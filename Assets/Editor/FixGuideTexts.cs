using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

// 씬에 저장된 한국어 GuideText를 영어로 교체합니다.
public class FixGuideTexts
{
    public static void Execute()
    {
        FixTMP("Canvas/Panels/DeathPanel/GuideText",      "Press E to Respawn");
        FixTMP("Canvas/Panels/StageClearPanel/GuideText", "");

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[FixGuideTexts] 완료 및 씬 저장됨");
    }

    private static void FixTMP(string path, string newText)
    {
        GameObject go = GameObject.Find(path);
        if (go == null)
        {
            // 비활성화 오브젝트도 검색
            go = FindInactiveObject(path);
        }
        if (go == null) { Debug.LogWarning($"[FixGuideTexts] {path} 를 찾지 못했습니다."); return; }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) { Debug.LogWarning($"[FixGuideTexts] {path} 에 TextMeshProUGUI 없음"); return; }

        tmp.text = newText;
        EditorUtility.SetDirty(tmp);
        Debug.Log($"[FixGuideTexts] {path} → \"{newText}\"");
    }

    private static GameObject FindInactiveObject(string path)
    {
        string[] parts = path.Split('/');
        Transform current = null;

        // 루트 오브젝트 찾기 (비활성 포함)
        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == parts[0]) { current = root.transform; break; }
        }
        if (current == null) return null;

        for (int i = 1; i < parts.Length; i++)
        {
            current = current.Find(parts[i]);
            if (current == null) return null;
        }
        return current.gameObject;
    }
}
