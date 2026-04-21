using UnityEditor;
using UnityEngine;

// Title 씬의 LoadPanel을 초기 비활성화 상태로 설정합니다.
public class SetTitleLoadPanelInactive
{
    public static void Execute()
    {
        var loadPanel = GameObject.Find("Canvas/LoadPanel");
        if (loadPanel == null) { Debug.LogWarning("[Setup] LoadPanel을 찾지 못했습니다."); return; }
        loadPanel.SetActive(false);
        EditorUtility.SetDirty(loadPanel);
        Debug.Log("[Setup] LoadPanel 비활성화 완료");
    }
}
