using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveStage1
{
    public static void Execute()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Stage1.unity");
        AssetDatabase.Refresh();
    }
}
