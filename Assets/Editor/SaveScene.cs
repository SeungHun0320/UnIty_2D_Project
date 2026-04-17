using UnityEditor;
using UnityEditor.SceneManagement;

// 현재 씬을 저장합니다.
public class SaveScene
{
    public static void Execute()
    {
        EditorSceneManager.SaveOpenScenes();
        UnityEngine.Debug.Log("[SaveScene] 씬 저장 완료");
    }
}
