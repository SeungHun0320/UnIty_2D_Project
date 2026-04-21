using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// TitleManager의 slotTexts 배열을 에디터에서 일괄 연결합니다.
public class SetupTitleSlotTexts
{
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Title")
        {
            Debug.LogWarning("[SetupTitleSlotTexts] Title 씬이 활성화되어 있지 않습니다.");
            return;
        }

        var tmGo = GameObject.Find("TitleManager");
        if (tmGo == null) { Debug.LogWarning("[SetupTitleSlotTexts] TitleManager를 찾지 못했습니다."); return; }

        var tm = tmGo.GetComponent<TitleManager>();
        if (tm == null) { Debug.LogWarning("[SetupTitleSlotTexts] TitleManager 컴포넌트가 없습니다."); return; }

        var slot0Text = GameObject.Find("Canvas/LoadPanel/Slot0Button/Text")?.GetComponent<Text>();
        var slot1Text = GameObject.Find("Canvas/LoadPanel/Slot1Button/Text")?.GetComponent<Text>();
        var slot2Text = GameObject.Find("Canvas/LoadPanel/Slot2Button/Text")?.GetComponent<Text>();
        var slot3Text = GameObject.Find("Canvas/LoadPanel/Slot3Button/Text")?.GetComponent<Text>();

        var so   = new SerializedObject(tm);
        var prop = so.FindProperty("slotTexts");
        prop.arraySize = 4;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = slot0Text;
        prop.GetArrayElementAtIndex(1).objectReferenceValue = slot1Text;
        prop.GetArrayElementAtIndex(2).objectReferenceValue = slot2Text;
        prop.GetArrayElementAtIndex(3).objectReferenceValue = slot3Text;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(tmGo);
        Debug.Log("[SetupTitleSlotTexts] slotTexts 연결 완료");
    }
}
