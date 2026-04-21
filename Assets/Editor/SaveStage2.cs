using UnityEditor;
using UnityEditor.SceneManagement;

public class SaveStage2
{
    public static void Execute()
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Stage2.unity");
        AssetDatabase.Refresh();
    }
}
