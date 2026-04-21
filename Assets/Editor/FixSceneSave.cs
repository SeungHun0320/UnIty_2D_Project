using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bootstrap 씬을 올바른 경로(Assets/Scenes/)로 저장합니다.
public class FixSceneSave
{
    public static void Execute()
    {
        // Bootstrap 씬 열고 올바른 경로로 저장
        var bootstrapScene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Additive);
        EditorSceneManager.SaveScene(bootstrapScene, "Assets/Scenes/Bootstrap.unity");
        EditorSceneManager.CloseScene(bootstrapScene, false);
        Debug.Log("[FixSceneSave] Bootstrap 씬 → Assets/Scenes/Bootstrap.unity 저장 완료");
        AssetDatabase.Refresh();
    }
}
