using UnityEngine;
using UnityEditor;

public class SetupSpawnPoint
{
    public static void Execute()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[SetupSpawnPoint] Player를 찾을 수 없습니다."); return; }

        Transform spawnPoint = player.transform.Find("ProjectileSpawnPoint");
        if (spawnPoint == null) { Debug.LogError("[SetupSpawnPoint] ProjectileSpawnPoint를 찾을 수 없습니다."); return; }

        PlayerStateMachine sm = player.GetComponent<PlayerStateMachine>();
        SerializedObject so = new SerializedObject(sm);
        so.FindProperty("projectileSpawnPoint").objectReferenceValue = spawnPoint;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[SetupSpawnPoint] projectileSpawnPoint 연결 완료!");
    }
}
