using UnityEngine;
using UnityEditor;

public class SetupHealSkill
{
    public static void Execute()
    {
        // 1. HealSkill SO 생성
        string soPath = "Assets/Resources/Skills/HealSkill.asset";
        System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources/Skills");
        AssetDatabase.Refresh();

        HealSkillData healSO = AssetDatabase.LoadAssetAtPath<HealSkillData>(soPath);
        if (healSO == null)
        {
            healSO = ScriptableObject.CreateInstance<HealSkillData>();
            AssetDatabase.CreateAsset(healSO, soPath);
        }

        // SO 값 설정
        healSO.activationType = SkillActivationType.Hold;
        healSO.holdDuration   = 1.5f;
        healSO.soulCost       = 33;
        healSO.cooldown       = 3f;
        healSO.healAmount     = 1f;
        EditorUtility.SetDirty(healSO);

        // 2. 씬의 Player 오브젝트에서 PlayerSkillController 찾기
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[SetupHealSkill] 씬에서 Player 오브젝트를 찾지 못했습니다.");
            return;
        }

        PlayerSkillController ctrl = player.GetComponent<PlayerSkillController>();
        if (ctrl == null)
        {
            Debug.LogError("[SetupHealSkill] PlayerSkillController 컴포넌트를 찾지 못했습니다.");
            return;
        }

        // 3. slots 배열을 SerializedObject로 수정 (private 필드 접근)
        SerializedObject so = new SerializedObject(ctrl);
        SerializedProperty slots = so.FindProperty("slots");

        // 기존 슬롯이 2개 미만이면 보존, 3개로 확장
        if (slots.arraySize < 3)
            slots.arraySize = 3;

        // slot[2]에 HealSkill SO 할당
        slots.GetArrayElementAtIndex(2).objectReferenceValue = healSO;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);

        AssetDatabase.SaveAssets();
        Debug.Log("[SetupHealSkill] 완료 — HealSkill.asset 생성 및 slots[2] 할당 성공");
    }
}
