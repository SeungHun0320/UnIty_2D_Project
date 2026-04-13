using UnityEngine;
using UnityEditor;
using System.IO;

// atlas0_300.png에서 Vengeful Spirit_Aura_* 스프라이트를 추출하고
// 검정(밝기 임계값 이하) 픽셀을 투명하게 처리한 개별 PNG로 저장합니다.
public class ExtractAuraSprites
{
    private const string AtlasPath      = "Assets/Resources/atlas0_300.png";
    private const string OutputFolder   = "Assets/Sprites/Skills/ShieldAura";
    private const float  BlackThreshold = 0.05f; // 이 밝기 이하 픽셀 → 투명

    public static void Execute()
    {
        // 1. Read/Write 활성화
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
        if (atlas == null) { Debug.LogError("[ExtractAura] 아틀라스를 찾을 수 없습니다."); return; }

        // 2. 출력 폴더 생성
        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/Sprites/Skills"))
            AssetDatabase.CreateFolder("Assets/Sprites", "Skills");
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Sprites/Skills", "ShieldAura");

        // 3. Aura 스프라이트 추출
        Object[] all = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
        int count = 0;
        foreach (Object obj in all)
        {
            Sprite sprite = obj as Sprite;
            if (sprite == null || !sprite.name.Contains("Aura")) continue;

            Rect rect = sprite.textureRect;
            int x = (int)rect.x, y = (int)rect.y, w = (int)rect.width, h = (int)rect.height;

            // 픽셀 추출
            Color[] pixels = atlas.GetPixels(x, y, w, h);

            // 검정 픽셀 투명화
            for (int i = 0; i < pixels.Length; i++)
            {
                float brightness = pixels[i].r * 0.299f + pixels[i].g * 0.587f + pixels[i].b * 0.114f;
                if (brightness < BlackThreshold)
                    pixels[i].a = 0f;
            }

            // 새 텍스처에 쓰기
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();

            // PNG 저장
            byte[] png = tex.EncodeToPNG();
            string filePath = $"{OutputFolder}/{sprite.name}.png";
            File.WriteAllBytes(Application.dataPath.Replace("Assets", "") + filePath, png);
            count++;
        }

        AssetDatabase.Refresh();

        // 4. Read/Write 원상 복구
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        Debug.Log($"[ExtractAura] {count}개 스프라이트 추출 완료 → {OutputFolder}");
    }
}
