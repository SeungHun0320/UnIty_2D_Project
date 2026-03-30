using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BuildSoulFlowStripTexture
{
    private const string OutputDir = "Assets/Generated/Soul";

    [MenuItem("Tools/Hollow Knight HUD/Build Soul Flow Strip From Frames", false, 4)]
    public static void BuildFromSelectedSoulUI()
    {
        SoulUI soulUI = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<SoulUI>()
            : null;

        if (soulUI == null)
            soulUI = Object.FindAnyObjectByType<SoulUI>(FindObjectsInactive.Include);

        if (soulUI == null)
        {
            EditorUtility.DisplayDialog("Soul Flow Strip", "씬에서 SoulUI를 찾지 못했습니다.", "확인");
            return;
        }

        SerializedObject so = new SerializedObject(soulUI);
        SerializedProperty framesProp = so.FindProperty("chargingFillFrames");
        if (framesProp == null || framesProp.arraySize <= 0)
        {
            EditorUtility.DisplayDialog("Soul Flow Strip", "chargingFillFrames가 비어 있습니다.", "확인");
            return;
        }

        var frames = new List<Sprite>();
        for (int i = 0; i < framesProp.arraySize; i++)
        {
            Sprite sp = framesProp.GetArrayElementAtIndex(i).objectReferenceValue as Sprite;
            if (sp != null) frames.Add(sp);
        }

        if (frames.Count == 0)
        {
            EditorUtility.DisplayDialog("Soul Flow Strip", "유효한 프레임 스프라이트가 없습니다.", "확인");
            return;
        }

        int totalW = 0;
        int maxH = 0;
        foreach (var sp in frames)
        {
            totalW += Mathf.RoundToInt(sp.rect.width);
            maxH = Mathf.Max(maxH, Mathf.RoundToInt(sp.rect.height));
        }

        if (totalW <= 0 || maxH <= 0)
        {
            EditorUtility.DisplayDialog("Soul Flow Strip", "프레임 크기가 유효하지 않습니다.", "확인");
            return;
        }

        Texture2D strip = new Texture2D(totalW, maxH, TextureFormat.RGBA32, false);
        strip.wrapMode = TextureWrapMode.Repeat;
        strip.filterMode = FilterMode.Bilinear;

        Color[] clear = new Color[totalW * maxH];
        for (int i = 0; i < clear.Length; i++) clear[i] = new Color(0, 0, 0, 0);
        strip.SetPixels(clear);

        int xOffset = 0;
        foreach (var sp in frames)
        {
            Rect r = sp.rect;
            int sx = Mathf.RoundToInt(r.x);
            int sy = Mathf.RoundToInt(r.y);
            int sw = Mathf.RoundToInt(r.width);
            int sh = Mathf.RoundToInt(r.height);

            try
            {
                Color[] src = sp.texture.GetPixels(sx, sy, sw, sh);
                strip.SetPixels(xOffset, 0, sw, sh, src);
            }
            catch
            {
                EditorUtility.DisplayDialog(
                    "Soul Flow Strip",
                    $"프레임 '{sp.name}' 픽셀 읽기에 실패했습니다.\nTexture Import에서 Read/Write Enabled를 켜주세요.",
                    "확인"
                );
                return;
            }

            xOffset += sw;
        }

        strip.Apply(false, false);

        Directory.CreateDirectory(OutputDir);
        string fileName = $"soul_flow_strip_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        string assetPath = $"{OutputDir}/{fileName}";
        File.WriteAllBytes(assetPath, strip.EncodeToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        // Importer 설정
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        // 생성된 strip를 FlowAnim(Image)에 바로 연결
        var flowImage = so.FindProperty("fillAnimImage")?.objectReferenceValue as Image;
        var loopSpriteProp = so.FindProperty("chargingLoopSprite");
        var stripSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (flowImage != null && stripSprite != null)
        {
            Undo.RecordObject(flowImage, "Assign Soul Flow Strip Sprite");
            flowImage.sprite = stripSprite;
            EditorUtility.SetDirty(flowImage);
        }
        if (loopSpriteProp != null && stripSprite != null)
        {
            loopSpriteProp.objectReferenceValue = stripSprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(soulUI);
        Debug.Log($"[Soul Flow Strip] 생성 완료: {assetPath} ({totalW}x{maxH}, frames={frames.Count})");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
    }
}
