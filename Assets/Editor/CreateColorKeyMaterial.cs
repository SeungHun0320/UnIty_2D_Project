using UnityEngine;
using UnityEditor;

// Custom/Sprite_ColorKey 셰이더 머티리얼을 생성하고 ShieldAuraEffect 프리팹에 할당합니다.
public class CreateColorKeyMaterial
{
    public static void Execute()
    {
        string matPath = "Assets/Materials/FX_ColorKey.mat";
        Material mat   = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        if (mat == null)
        {
            Shader shader = Shader.Find("Custom/Sprite_ColorKey");
            if (shader == null)
            {
                Debug.LogError("[CreateColorKeyMaterial] 'Custom/Sprite_ColorKey' 셰이더를 찾을 수 없습니다. 컴파일 에러가 없는지 확인해주세요.");
                return;
            }

            mat             = new Material(shader);
            mat.color       = Color.white;
            mat.SetFloat("_KeyThreshold", 0.08f); // 검정 판별 임계값

            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CreateColorKeyMaterial] FX_ColorKey.mat 생성 완료.");
        }

        // ShieldAuraEffect 프리팹 SpriteRenderer에 할당
        string prefabPath = "Assets/Prefabs/Skills/ShieldAuraEffect.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("[CreateColorKeyMaterial] ShieldAuraEffect.prefab을 찾을 수 없습니다."); return; }

        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null)       { Debug.LogError("[CreateColorKeyMaterial] SpriteRenderer가 없습니다."); return; }

        sr.sharedMaterial = mat;
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        Debug.Log("[CreateColorKeyMaterial] ShieldAuraEffect 프리팹에 FX_ColorKey 머티리얼 할당 완료!");
    }
}
