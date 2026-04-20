using UnityEditor;
using UnityEngine;

public class SetupSkillSlots
{
    public static void Execute()
    {
        // SO 로드
        var basicAttack  = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Data/Skills/BasicAttack.asset");
        var shieldAttack = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Data/Skills/ShieldAttack.asset");
        var healSkill    = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Data/Skills/HealSkill.asset");

        if (basicAttack == null || shieldAttack == null || healSkill == null)
        {
            Debug.LogError("[SetupSkillSlots] SO 로드 실패. 경로를 확인하세요.");
            return;
        }

        // Player의 PlayerSkillController 찾기
        var go = GameObject.Find("Player");
        if (go == null) { Debug.LogError("[SetupSkillSlots] Player GameObject를 찾을 수 없습니다."); return; }

        var ctrl = go.GetComponent<PlayerSkillController>();
        if (ctrl == null) { Debug.LogError("[SetupSkillSlots] PlayerSkillController를 찾을 수 없습니다."); return; }

        var so = new SerializedObject(ctrl);
        var slotsProp = so.FindProperty("slots");

        // 슬롯 3개로 맞추기
        slotsProp.arraySize = 3;

        // Slot 0: X키 — 기본 공격 (Instant 단독)
        var slot0 = slotsProp.GetArrayElementAtIndex(0);
        slot0.FindPropertyRelative("skill").objectReferenceValue     = basicAttack;
        slot0.FindPropertyRelative("tapSkill").objectReferenceValue  = null;
        slot0.FindPropertyRelative("holdSkill").objectReferenceValue = null;

        // Slot 1: A키 — TapOrHold (짧게=총알, 길게=힐)
        var slot1 = slotsProp.GetArrayElementAtIndex(1);
        slot1.FindPropertyRelative("skill").objectReferenceValue          = null;
        slot1.FindPropertyRelative("tapSkill").objectReferenceValue       = shieldAttack;
        slot1.FindPropertyRelative("holdSkill").objectReferenceValue      = healSkill;
        slot1.FindPropertyRelative("holdThreshold").floatValue            = 0.4f;

        // Slot 2: C키 — 대시 예약 (비어있음)
        var slot2 = slotsProp.GetArrayElementAtIndex(2);
        slot2.FindPropertyRelative("skill").objectReferenceValue     = null;
        slot2.FindPropertyRelative("tapSkill").objectReferenceValue  = null;
        slot2.FindPropertyRelative("holdSkill").objectReferenceValue = null;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(ctrl);

        Debug.Log("[SetupSkillSlots] 슬롯 설정 완료 — Slot0:BasicAttack / Slot1:TapOrHold(Shield+Heal) / Slot2:예약");
    }
}
