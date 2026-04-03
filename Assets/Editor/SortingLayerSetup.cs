
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class SortingLayerSetup
{
    [MenuItem("Tools/Setup Sorting Layers")]
    public static void Run()
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("m_SortingLayers");
        string[] names = { "Background", "Ground", "Deco", "Object", "Character", "UI" };

        foreach (var n in names)
        {
            bool found = false;
            for (int i = 0; i < layersProp.arraySize; i++)
                if (layersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n)
                { found = true; break; }

            if (!found)
            {
                layersProp.InsertArrayElementAtIndex(layersProp.arraySize);
                var e = layersProp.GetArrayElementAtIndex(layersProp.arraySize - 1);
                e.FindPropertyRelative("name").stringValue = n;
                e.FindPropertyRelative("uniqueID").intValue = n.GetHashCode();
                Debug.Log("[SortingLayerSetup] Added: " + n);
            }
        }
        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        // Tilemaps
        var bgGo = GameObject.Find("BG_Tilemap");
        if (bgGo != null) { var r = bgGo.GetComponent<TilemapRenderer>(); if (r != null) { r.sortingLayerName = "Background"; r.sortingOrder = 0; EditorUtility.SetDirty(r); } }

        var grGo = GameObject.Find("GroundTilemap");
        if (grGo != null) { var r = grGo.GetComponent<TilemapRenderer>(); if (r != null) { r.sortingLayerName = "Ground"; r.sortingOrder = 0; EditorUtility.SetDirty(r); } }

        var dcGo = GameObject.Find("DecoTileMap");
        if (dcGo != null) { var r = dcGo.GetComponent<TilemapRenderer>(); if (r != null) { r.sortingLayerName = "Deco"; r.sortingOrder = 0; EditorUtility.SetDirty(r); } }

        // Characters
        var plGo = GameObject.Find("Player");
        if (plGo != null) { var r = plGo.GetComponent<MeshRenderer>(); if (r != null) { r.sortingLayerName = "Character"; r.sortingOrder = 0; EditorUtility.SetDirty(r); } }

        var moGo = GameObject.Find("Monster");
        if (moGo != null) { var r = moGo.GetComponent<MeshRenderer>(); if (r != null) { r.sortingLayerName = "Character"; r.sortingOrder = -1; EditorUtility.SetDirty(r); } }

        // SpriteRenderers -> Object
        foreach (var sr in GameObject.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            sr.sortingLayerName = "Object";
            sr.sortingOrder = 0;
            EditorUtility.SetDirty(sr);
            Debug.Log("[SortingLayerSetup] SpriteRenderer: " + sr.gameObject.name + " -> Object");
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[SortingLayerSetup] 완료!");
        EditorUtility.DisplayDialog("완료", "Sorting Layer 설정 완료!", "OK");
    }
}
#endif
