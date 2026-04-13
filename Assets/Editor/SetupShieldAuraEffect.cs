using UnityEngine;
using UnityEditor;

public class SetupShieldAuraEffect
{
    public static void Execute()
    {
        // 1. SpriteEffect SO 생성
        string effectPath = "Assets/Data/Skills/ShieldAuraEffect.asset";
        SpriteEffect effect = AssetDatabase.LoadAssetAtPath<SpriteEffect>(effectPath);
        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<SpriteEffect>();
            effect.effectPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Skills/ShieldAuraEffect.prefab");
            effect.destroyAfter  = 0.75f;
            effect.offset        = Vector2.zero;
            effect.followPlayer  = false;
            AssetDatabase.CreateAsset(effect, effectPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupShieldAuraEffect] ShieldAuraEffect.asset 생성 완료.");
        }

        // 2. ShieldAttack.asset의 Effects[0]에 할당
        string skillPath = "Assets/Data/Skills/ShieldAttack.asset";
        ShieldAttackData skill = AssetDatabase.LoadAssetAtPath<ShieldAttackData>(skillPath);
        if (skill == null) { Debug.LogError("[SetupShieldAuraEffect] ShieldAttack.asset을 찾을 수 없습니다."); return; }

        SerializedObject so    = new SerializedObject(skill);
        SerializedProperty fx  = so.FindProperty("effects");
        fx.arraySize = 1;
        fx.GetArrayElementAtIndex(0).objectReferenceValue = effect;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(skill);
        Debug.Log("[SetupShieldAuraEffect] ShieldAttack effects[0] 연결 완료!");
    }
}
