using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SetupBootstrap
{
    public static void Execute()
    {
        // 1. Stage1 어디티브로 로드 (디스크 상태 그대로, 저장 안 함)
        var stage1Scene = EditorSceneManager.OpenScene("Assets/Scenes/Stage1.unity", OpenSceneMode.Additive);

        // 2. Bootstrap 씬 생성 (어디티브, Single 아님)
        var bootstrapScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // 3. Stage1에서 이동할 루트 오브젝트
        string[] toMove = { "GameManager", "Canvas", "EventSystem", "Player" };
        var roots = stage1Scene.GetRootGameObjects();

        foreach (var objName in toMove)
        {
            var go = roots.FirstOrDefault(r => r.name == objName);
            if (go != null)
            {
                SceneManager.MoveGameObjectToScene(go, bootstrapScene);
                Debug.Log($"[Bootstrap] '{objName}' 이동 완료");
            }
            else
            {
                Debug.LogWarning($"[Bootstrap] '{objName}' Stage1에서 찾지 못했습니다.");
            }
        }

        // 4. GameManager 오브젝트에 BootstrapLoader 추가
        var gmObj = bootstrapScene.GetRootGameObjects().FirstOrDefault(r => r.name == "GameManager");
        if (gmObj != null)
        {
            if (gmObj.GetComponent<BootstrapLoader>() == null)
            {
                var loader = gmObj.AddComponent<BootstrapLoader>();
                var field = typeof(BootstrapLoader).GetField("firstSceneName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(loader, "Stage1");
                Debug.Log("[Bootstrap] BootstrapLoader 추가 완료 (firstSceneName = Stage1)");
            }
        }
        else
        {
            Debug.LogWarning("[Bootstrap] GameManager 오브젝트를 찾지 못했습니다.");
        }

        // 5. Bootstrap 씬 저장
        EditorSceneManager.SaveScene(bootstrapScene, "Assets/Scenes/Bootstrap.unity");
        Debug.Log("[Bootstrap] Assets/Scenes/Bootstrap.unity 저장 완료");

        // 6. Stage1 저장 없이 닫기 (Stage1.unity 파일 변경 없음)
        EditorSceneManager.CloseScene(stage1Scene, true);
        Debug.Log("[Bootstrap] Stage1 닫기 완료 (디스크 변경 없음)");

        // 7. Build Settings 업데이트
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Stage1.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Stage2.unity", true),
        };
        Debug.Log("[Bootstrap] Build Settings 업데이트 완료");

        AssetDatabase.Refresh();
        Debug.Log("[Bootstrap] 완료! 이제 Stage1과 Stage2에서 GameManager/Canvas/EventSystem/Player를 직접 삭제해주세요.");
    }
}
