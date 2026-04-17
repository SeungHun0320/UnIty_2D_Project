using UnityEngine;
using UnityEditor;

// Spine 텍스처의 임포터 설정 경고를 수정합니다.
public class FixSpineTextures
{
    public static void Execute()
    {
        FixKnight();
        FixRust();
        AssetDatabase.SaveAssets();
        Debug.Log("[FixSpineTextures] 완료");
    }

    // Knight.png: sRGB 비활성화 + MipMap 비활성화 (Spine 스프라이트 시트에 불필요)
    private static void FixKnight()
    {
        string path = "Assets/Spine/Spine2D Knight Character Animation Pack/Exports/json_export/Knight.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[FixSpineTextures] Knight.png 임포터를 찾지 못했습니다."); return; }

        bool changed = false;
        if (ti.sRGBTexture)         { ti.sRGBTexture        = false; changed = true; }
        if (ti.mipmapEnabled)       { ti.mipmapEnabled       = false; changed = true; }
        if (ti.alphaIsTransparency) { ti.alphaIsTransparency = false; changed = true; }

        if (changed)
        {
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            Debug.Log("[FixSpineTextures] Knight.png 수정됨 (sRGB=false, MipMap=false)");
        }
        else
        {
            Debug.Log("[FixSpineTextures] Knight.png 이미 올바름");
        }
    }

    // 2D_Character_Rust.png: Alpha Is Transparency 비활성화
    private static void FixRust()
    {
        string path = "Assets/Resources/Characters/2D_Character_Rust/Spine/2D_Character_Rust.png";
        TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) { Debug.LogWarning("[FixSpineTextures] 2D_Character_Rust.png 임포터를 찾지 못했습니다."); return; }

        bool changed = false;
        if (ti.alphaIsTransparency) { ti.alphaIsTransparency = false; changed = true; }
        if (ti.mipmapEnabled)       { ti.mipmapEnabled       = false; changed = true; }

        if (changed)
        {
            EditorUtility.SetDirty(ti);
            ti.SaveAndReimport();
            Debug.Log("[FixSpineTextures] 2D_Character_Rust.png 수정됨 (AlphaIsTransparency=false)");
        }
        else
        {
            Debug.Log("[FixSpineTextures] 2D_Character_Rust.png 이미 올바름");
        }
    }
}
