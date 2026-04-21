using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Title 씬을 올바른 경로에 저장합니다.
public class SaveTitleScene
{
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Title") { Debug.LogWarning("[SaveTitleScene] Title 씬이 활성화되어 있지 않습니다."); return; }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Title.unity");
        Debug.Log("[SaveTitleScene] Assets/Scenes/Title.unity 저장 완료");
    }
}
