using UnityEditor;

public class SetupBuildScenes
{
    public static void Execute()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Stage1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Stage2.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
    }
}
