using UnityEngine;
using UnityEditor;
using TMPro;

public class FixMobileUI
{
    public static void Execute()
    {
        // 이동 버튼 레이블 수정 (◀▶ → < >)
        FixLabel("Canvas/MobileUI/BtnLeft",  "< ");
        FixLabel("Canvas/MobileUI/BtnRight", " >");

        // MobileUI 기본 비활성화 (InputRouter.Awake에서 플랫폼 감지 후 활성화)
        GameObject mobileRoot = GameObject.Find("Canvas/MobileUI");
        if (mobileRoot != null)
        {
            mobileRoot.SetActive(false);
            EditorUtility.SetDirty(mobileRoot);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FixMobileUI] 완료");
    }

    private static void FixLabel(string path, string text)
    {
        GameObject go = GameObject.Find(path + "/Label");
        if (go == null) return;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = text;
            EditorUtility.SetDirty(tmp);
        }
    }
}
