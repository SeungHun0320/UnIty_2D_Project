using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Title 씬 TitleManager 참조 연결 상태를 검증합니다.
public class VerifyTitleSetup
{
    public static void Execute()
    {
        var tmGo = GameObject.Find("TitleManager");
        if (tmGo == null) { Debug.LogError("[Verify] TitleManager 오브젝트 없음"); return; }

        var tm = tmGo.GetComponent<TitleManager>();
        if (tm == null) { Debug.LogError("[Verify] TitleManager 컴포넌트 없음"); return; }

        var so = new SerializedObject(tm);
        var mainPanel  = so.FindProperty("mainPanel").objectReferenceValue;
        var loadPanel  = so.FindProperty("loadPanel").objectReferenceValue;
        var slotTexts  = so.FindProperty("slotTexts");

        Debug.Log($"[Verify] mainPanel: {(mainPanel != null ? mainPanel.name : "NULL")}");
        Debug.Log($"[Verify] loadPanel: {(loadPanel != null ? loadPanel.name : "NULL")}");
        Debug.Log($"[Verify] slotTexts.arraySize: {slotTexts.arraySize}");
        for (int i = 0; i < slotTexts.arraySize; i++)
        {
            var elem = slotTexts.GetArrayElementAtIndex(i).objectReferenceValue;
            Debug.Log($"[Verify] slotTexts[{i}]: {(elem != null ? elem.name : "NULL")}");
        }
    }
}
