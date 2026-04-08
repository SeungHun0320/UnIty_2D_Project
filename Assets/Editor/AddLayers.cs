using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AddLayers
{
    public static void Execute()
    {
        string[] layersToAdd = new string[]
        {
            "Player",
            "Enemy",
            "PlayerHurtbox",
            "PlayerAttackHitbox",
            "EnemyHurtbox",
            "EnemyAttackHitbox"
        };

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // 현재 등록된 레이어 목록
        var existing = new HashSet<string>();
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            string val = layersProp.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrEmpty(val)) existing.Add(val);
        }

        int added = 0;
        // 유저 레이어는 8번 슬롯부터 시작 (0~7은 빌트인)
        for (int i = 8; i < layersProp.arraySize; i++)
        {
            SerializedProperty slot = layersProp.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(slot.stringValue)) continue;

            // 빈 슬롯에 아직 추가 안 된 레이어를 채웁니다
            while (added < layersToAdd.Length && existing.Contains(layersToAdd[added]))
                added++;

            if (added >= layersToAdd.Length) break;

            slot.stringValue = layersToAdd[added];
            Debug.Log($"[AddLayers] 레이어 추가: '{layersToAdd[added]}' → slot {i}");
            added++;
        }

        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Debug.Log($"[AddLayers] 완료. {added}개 레이어 처리됨.");
    }
}
