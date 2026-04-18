using UnityEditor;
using UnityEngine;

public class SetupMonsterDropTable
{
    public static void Execute()
    {
        string monsterPath = "Assets/Prefabs/Characters/Monster.prefab";
        string geoPrefabPath = "Assets/Prefabs/Items/GeoPickup.prefab";

        var geoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(geoPrefabPath);
        if (geoPrefab == null) { Debug.LogError("GeoPickup.prefab 없음"); return; }

        var pickup = geoPrefab.GetComponent<ItemPickupBase>();
        if (pickup == null) { Debug.LogError("GeoPickup에 ItemPickupBase 없음"); return; }

        using var scope = new PrefabUtility.EditPrefabContentsScope(monsterPath);
        var root = scope.prefabContentsRoot;

        var dropper = root.GetComponent<ItemDropper>();
        if (dropper == null) { Debug.LogError("Monster에 ItemDropper 없음"); return; }

        var so = new SerializedObject(dropper);
        var table = so.FindProperty("dropTable");

        table.ClearArray();
        table.InsertArrayElementAtIndex(0);
        var entry = table.GetArrayElementAtIndex(0);
        entry.FindPropertyRelative("prefab").objectReferenceValue = pickup;
        entry.FindPropertyRelative("chance").floatValue = 1f;
        entry.FindPropertyRelative("countRange").vector2IntValue = new Vector2Int(2, 3);

        so.ApplyModifiedProperties();
        Debug.Log("[SetupMonsterDropTable] 드랍 테이블 설정 완료 (GeoPickup x2~3, 100%)");
    }
}
