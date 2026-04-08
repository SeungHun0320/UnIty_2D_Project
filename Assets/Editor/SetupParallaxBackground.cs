using UnityEngine;
using UnityEditor;

public class SetupParallaxBackground
{
    public static void Execute()
    {
        // Map_Background 찾기
        GameObject mapBg = GameObject.Find("Map/Map_Background");
        if (mapBg == null) mapBg = GameObject.Find("Map_Background");
        if (mapBg == null) { Debug.LogError("[Setup] Map_Background를 찾을 수 없습니다."); return; }

        // 기존 자식 제거
        for (int i = mapBg.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(mapBg.transform.GetChild(i).gameObject);

        string basePath = "Assets/FREE Parallax Forest Background HQ/Parallax Forest Background/";
        string[] layerNames = new string[]
        {
            "Layer_01_Sky",
            "Layer_02_Forest",
            "Layer_03_Forest",
            "Layer_04_Forest",
            "Layer_05_Forest",
            "Layer_06_Particles",
            "Layer_07_Forest",
            "Layer_08_Particles",
            "Layer_09_Bushes",
            "Layer_10_Mist"
        };
        string[] spriteFiles = new string[]
        {
            "01_Sky.png", "02_Forest.png", "03_Forest.png", "04_Forest.png",
            "05_Forest.png", "06_Particles.png", "07_Forest.png", "08_Particles.png",
            "09_Bushes.png", "10_Mist.png"
        };
        float[] parallaxFactors = new float[]
        {
            0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.45f, 0.5f, 0.55f, 0.7f, 0.9f
        };

        // ParallaxBackground 컴포넌트 추가
        ParallaxBackground parallax = mapBg.GetComponent<ParallaxBackground>();
        if (parallax == null) parallax = Undo.AddComponent<ParallaxBackground>(mapBg);

        // SerializedObject로 layers 배열 설정
        SerializedObject so = new SerializedObject(parallax);
        SerializedProperty layersProp = so.FindProperty("layers");
        layersProp.arraySize = layerNames.Length;

        for (int i = 0; i < layerNames.Length; i++)
        {
            // 자식 오브젝트 생성
            GameObject child = new GameObject(layerNames[i]);
            Undo.RegisterCreatedObjectUndo(child, "Create Layer");
            child.transform.SetParent(mapBg.transform, false);
            child.transform.localPosition = Vector3.zero;

            // SpriteRenderer 추가 및 스프라이트 연결
            SpriteRenderer sr = Undo.AddComponent<SpriteRenderer>(child);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(basePath + spriteFiles[i]);
            if (sprite != null) sr.sprite = sprite;
            else Debug.LogWarning($"[Setup] 스프라이트를 찾을 수 없습니다: {spriteFiles[i]}");

            // Order in Layer 설정 (뒤에서 앞 순서)
            sr.sortingOrder = i;

            // layers 배열에 설정
            SerializedProperty element = layersProp.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("spriteRenderer").objectReferenceValue = sr;
            element.FindPropertyRelative("parallaxFactor").floatValue = parallaxFactors[i];
        }

        so.ApplyModifiedProperties();

        // Main Camera 연결
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            SerializedProperty camProp = so.FindProperty("targetCamera");
            so.Update();
            camProp = so.FindProperty("targetCamera");
            camProp.objectReferenceValue = mainCam;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(mapBg);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mapBg.scene);
        Debug.Log("[Setup] ParallaxBackground 설정 완료!");
    }
}
