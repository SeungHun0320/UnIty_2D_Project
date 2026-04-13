using UnityEngine;
using UnityEditor;

// Additive 블렌딩 머티리얼을 생성하고 ShieldAuraEffect 프리팹에 할당합니다.
public class CreateAdditiveMaterial
{
    public static void Execute()
    {
        // 1. Additive 블렌딩 머티리얼 생성 (URP/Built-in 공용)
        string matPath = "Assets/Materials/FX_Additive.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            // URP → Sprites/Default, Built-in → Sprites/Default 순으로 시도
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null)
            {
                Debug.LogError("[CreateAdditiveMaterial] 적합한 쉐이더를 찾을 수 없습니다.");
                return;
            }
            mat = new Material(shader);
            mat.color = Color.white;

            // Additive 블렌딩: SrcFactor=One, DstFactor=One
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);

            // Materials 폴더 없으면 생성
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CreateAdditiveMaterial] FX_Additive.mat 생성 완료.");
        }

        // 2. ShieldAuraEffect 프리팹의 SpriteRenderer에 머티리얼 할당
        string prefabPath = "Assets/Prefabs/Skills/ShieldAuraEffect.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError("[CreateAdditiveMaterial] ShieldAuraEffect.prefab을 찾을 수 없습니다.");
            return;
        }

        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("[CreateAdditiveMaterial] SpriteRenderer를 찾을 수 없습니다.");
            return;
        }

        sr.sharedMaterial = mat;
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[CreateAdditiveMaterial] ShieldAuraEffect 프리팹에 FX_Additive 머티리얼 할당 완료!");
    }
}
