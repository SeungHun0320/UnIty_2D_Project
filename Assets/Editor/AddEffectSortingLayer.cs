using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AddEffectSortingLayer
{
    public static void Execute()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var sortingLayers = tagManager.FindProperty("m_SortingLayers");

        // 이미 존재하면 스킵
        for (int i = 0; i < sortingLayers.arraySize; i++)
        {
            if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Effect")
            {
                Debug.Log("[AddEffectSortingLayer] Effect 레이어가 이미 존재합니다.");
                return;
            }
        }

        // Character 다음 인덱스 찾기
        int insertAt = sortingLayers.arraySize;
        for (int i = 0; i < sortingLayers.arraySize; i++)
        {
            if (sortingLayers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == "Character")
            {
                insertAt = i + 1;
                break;
            }
        }

        sortingLayers.InsertArrayElementAtIndex(insertAt);
        var newLayer = sortingLayers.GetArrayElementAtIndex(insertAt);
        newLayer.FindPropertyRelative("name").stringValue = "Effect";
        newLayer.FindPropertyRelative("uniqueID").intValue = "Effect".GetHashCode();

        tagManager.ApplyModifiedProperties();
        Debug.Log($"[AddEffectSortingLayer] Effect 레이어 추가 완료 (index={insertAt})");
    }
}
