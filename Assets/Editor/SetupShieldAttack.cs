using UnityEngine;
using UnityEditor;

public class SetupShieldAttack
{
    public static void Execute()
    {
        // 1. ShieldAttack.asset 생성
        string assetPath = "Assets/Data/Skills/ShieldAttack.asset";
        ShieldAttackData existing = AssetDatabase.LoadAssetAtPath<ShieldAttackData>(assetPath);
        if (existing == null)
        {
            ShieldAttackData data = ScriptableObject.CreateInstance<ShieldAttackData>();
            data.cooldown          = 3f;
            data.hitbox.delay      = 0.3f;
            data.hitbox.duration   = 0f;
            data.damageMultiplier  = 1.5f;
            data.animationKey      = "shield_attack";
            data.spawnOffset       = new Vector2(0.5f, 0f);

            // 투사체 프리팹 연결
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Skills/Projectile.prefab");
            data.projectilePrefab = prefab;

            AssetDatabase.CreateAsset(data, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SetupShieldAttack] ShieldAttack.asset 생성 완료.");
        }
        else
        {
            Debug.Log("[SetupShieldAttack] ShieldAttack.asset 이미 존재합니다.");
        }

        // 2. PlayerSkillController Slots[1]에 할당
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[SetupShieldAttack] Player를 찾을 수 없습니다."); return; }

        PlayerSkillController controller = player.GetComponent<PlayerSkillController>();
        if (controller == null) { Debug.LogError("[SetupShieldAttack] PlayerSkillController가 없습니다."); return; }

        ShieldAttackData shieldAttack = AssetDatabase.LoadAssetAtPath<ShieldAttackData>(assetPath);
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty slots = so.FindProperty("slots");
        slots.arraySize = 2;
        slots.GetArrayElementAtIndex(1).objectReferenceValue = shieldAttack;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(player.scene);
        Debug.Log("[SetupShieldAttack] Slots[1] 할당 완료!");
    }
}
