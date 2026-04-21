using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Bootstrap 씬을 올바른 경로에 저장합니다.
public class SaveBootstrapScene
{
    public static void Execute()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.name != "Bootstrap")
        {
            Debug.LogWarning("[SaveBootstrapScene] Bootstrap 씬이 활성화되어 있지 않습니다.");
            return;
        }
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Bootstrap.unity");
        Debug.Log("[SaveBootstrapScene] Assets/Scenes/Bootstrap.unity 저장 완료");
    }
}
