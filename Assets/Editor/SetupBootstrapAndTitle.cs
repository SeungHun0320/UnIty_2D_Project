using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupBootstrapAndTitle
{
    public static void Execute()
    {
        SetupBootstrapScene();
        SetupTitleScene();
        UpdateBuildSettings();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] 완료! Bootstrap에 SaveManager 추가 및 Title 씬 생성됨.");
    }

    // ── Bootstrap: SaveManager 추가 + playerPrefab 연결 ───────────────────────
    private static void SetupBootstrapScene()
    {
        var bootstrapScene = EditorSceneManager.OpenScene("Assets/Scenes/Bootstrap.unity", OpenSceneMode.Additive);

        var gmObj = FindInScene(bootstrapScene, "GameManager");
        if (gmObj == null) { Debug.LogWarning("[Setup] Bootstrap에서 GameManager를 찾지 못했습니다."); }
        else
        {
            // SaveManager 추가
            if (gmObj.GetComponent<SaveManager>() == null)
            {
                gmObj.AddComponent<SaveManager>();
                Debug.Log("[Setup] SaveManager 추가 완료");
            }

            // playerPrefab 연결
            var gm = gmObj.GetComponent<GameManager>();
            if (gm != null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Characters/Player.prefab");
                if (prefab != null)
                {
                    var so = new SerializedObject(gm);
                    var prop = so.FindProperty("playerPrefab");
                    if (prop != null)
                    {
                        prop.objectReferenceValue = prefab;
                        so.ApplyModifiedProperties();
                        Debug.Log("[Setup] playerPrefab 연결 완료");
                    }
                }
                else Debug.LogWarning("[Setup] Assets/Prefabs/Characters/Player.prefab을 찾지 못했습니다.");
            }
        }

        EditorSceneManager.SaveScene(bootstrapScene, "Assets/Scenes/Bootstrap.unity");
        EditorSceneManager.CloseScene(bootstrapScene, true);
        Debug.Log("[Setup] Bootstrap 저장 완료");
    }

    // ── Title 씬 생성 ─────────────────────────────────────────────────────────
    private static void SetupTitleScene()
    {
        var titleScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // 기본 카메라
        var camGo  = new GameObject("Main Camera");
        SceneManager.MoveGameObjectToScene(camGo, titleScene);
        var cam    = camGo.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        cam.tag = "MainCamera";

        // TitleManager 오브젝트
        var tmGo = new GameObject("TitleManager");
        SceneManager.MoveGameObjectToScene(tmGo, titleScene);
        tmGo.AddComponent<TitleManager>();

        EditorSceneManager.SaveScene(titleScene, "Assets/Scenes/Title.unity");
        EditorSceneManager.CloseScene(titleScene, true);
        Debug.Log("[Setup] Title 씬 생성 완료 (Assets/Scenes/Title.unity)");
    }

    // ── Build Settings ────────────────────────────────────────────────────────
    private static void UpdateBuildSettings()
    {
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Title.unity",     true),
            new EditorBuildSettingsScene("Assets/Scenes/Stage1.unity",    true),
            new EditorBuildSettingsScene("Assets/Scenes/Stage2.unity",    true),
        };
        Debug.Log("[Setup] Build Settings 업데이트 완료");
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects())
            if (go.name == name) return go;
        return null;
    }
}
