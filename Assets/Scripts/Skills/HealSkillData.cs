using System.Collections;
using UnityEngine;

// 소울을 소모해 체력 1칸을 회복하는 힐 스킬입니다.
// activationType = Hold, holdDuration으로 차징 시간을 조절합니다.
[CreateAssetMenu(fileName = "HealSkill", menuName = "Game/Skills/HealSkill")]
public class HealSkillData : SkillData
{
    [Header("Heal")]
    [Tooltip("한 번에 회복할 체력 양입니다. 1 = 마스크 1칸")]
    [Min(1f)] public float healAmount = 1f;

    public override IEnumerator Execute(SkillContext ctx)
    {
        if (ctx.Stats != null)
            ctx.Stats.Heal(healAmount);
        yield break;
    }
}
