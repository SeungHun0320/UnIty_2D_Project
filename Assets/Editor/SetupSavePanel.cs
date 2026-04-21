using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// SavePanel·PausePanel·UIManager 참조를 일괄 연결합니다.
public class SetupSavePanel
{
    public static void Execute()
    {
        // ── SavePanel 참조 연결 ────────────────────────────────────────────────
        var savePanelGo = GameObject.Find("Canvas/Panels/SavePanel");
        if (savePanelGo == null) { Debug.LogError("[Setup] SavePanel을 찾지 못했습니다."); return; }
        var savePanel = savePanelGo.GetComponent<SavePanel>();
        if (savePanel == null) { Debug.LogError("[Setup] SavePanel 컴포넌트 없음"); return; }

        var so = new SerializedObject(savePanel);

        // 슬롯 버튼 참조
        SetButton(so, "slot1Button", "Canvas/Panels/SavePanel/Slot1Button");
        SetButton(so, "slot2Button", "Canvas/Panels/SavePanel/Slot2Button");
        SetButton(so, "slot3Button", "Canvas/Panels/SavePanel/Slot3Button");
        SetButton(so, "backButton",  "Canvas/Panels/SavePanel/BackButton");

        // 슬롯 텍스트 배열 (index 0=Slot1, 1=Slot2, 2=Slot3)
        var slotTextsProp = so.FindProperty("slotTexts");
        slotTextsProp.arraySize = 3;
        slotTextsProp.GetArrayElementAtIndex(0).objectReferenceValue =
            GameObject.Find("Canvas/Panels/SavePanel/Slot1Button/Text")?.GetComponent<Text>();
        slotTextsProp.GetArrayElementAtIndex(1).objectReferenceValue =
            GameObject.Find("Canvas/Panels/SavePanel/Slot2Button/Text")?.GetComponent<Text>();
        slotTextsProp.GetArrayElementAtIndex(2).objectReferenceValue =
            GameObject.Find("Canvas/Panels/SavePanel/Slot3Button/Text")?.GetComponent<Text>();
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(savePanelGo);
        Debug.Log("[Setup] SavePanel 참조 연결 완료");

        // ── PausePanel saveButton 연결 ─────────────────────────────────────────
        var pausePanelGo = GameObject.Find("Canvas/Panels/PausePanel");
        if (pausePanelGo == null) { Debug.LogError("[Setup] PausePanel을 찾지 못했습니다."); return; }
        var pausePanel = pausePanelGo.GetComponent<PausePanel>();
        if (pausePanel == null) { Debug.LogError("[Setup] PausePanel 컴포넌트 없음"); return; }

        var pso = new SerializedObject(pausePanel);
        SetButton(pso, "saveButton", "Canvas/Panels/PausePanel/SaveButton");
        pso.ApplyModifiedProperties();
        EditorUtility.SetDirty(pausePanelGo);
        Debug.Log("[Setup] PausePanel saveButton 연결 완료");

        // ── UIManager savePanel 연결 ───────────────────────────────────────────
        var gmGo = GameObject.Find("GameManager");
        if (gmGo == null) { Debug.LogError("[Setup] GameManager를 찾지 못했습니다."); return; }
        var uiManager = gmGo.GetComponent<UIManager>();
        if (uiManager == null) { Debug.LogError("[Setup] UIManager 컴포넌트 없음"); return; }

        var uso = new SerializedObject(uiManager);
        uso.FindProperty("savePanel").objectReferenceValue = savePanel;
        uso.ApplyModifiedProperties();
        EditorUtility.SetDirty(gmGo);
        Debug.Log("[Setup] UIManager savePanel 연결 완료");
    }

    private static void SetButton(SerializedObject so, string propName, string goPath)
    {
        var btn = GameObject.Find(goPath)?.GetComponent<Button>();
        if (btn == null) { Debug.LogWarning($"[Setup] {goPath} Button을 찾지 못했습니다."); return; }
        so.FindProperty(propName).objectReferenceValue = btn;
        so.ApplyModifiedProperties();
    }
}
