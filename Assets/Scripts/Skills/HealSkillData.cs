using System.Collections;
using UnityEngine;

// 소울을 소모해 체력 1칸을 회복하는 힐 스킬입니다.
// activationType = Hold, holdDuration으로 차징 시간을 조절합니다.
[CreateAssetMenu(fileName = "HealSkill", menuName = "Game/Skills/HealSkill")]
public class HealSkillData : SkillData
{
    [Header("Animation")]
    [Tooltip("재생할 애니메이션 키입니다. 비워두면 애니메이션 없이 즉시 회복합니다.")]
    public string animationKey = "";

    [Header("Heal")]
    [Tooltip("한 번에 회복할 체력 양입니다. 1 = 마스크 1칸")]
    [Min(1f)] public float healAmount = 1f;

    // 이미 체력이 최대라면 힐 불가 — SkillData.CanActivate 오버라이드로 Controller를 수정하지 않습니다.
    public override bool CanActivate(ICharacterStats stats)
        => stats == null || stats.CurrentHealth < stats.MaxHealth;

    public override IEnumerator Execute(SkillContext ctx)
    {
        if (!string.IsNullOrEmpty(animationKey))
            ctx.Anim?.PlayAnimation(animationKey);

        if (ctx.Stats != null)
            ctx.Stats.Heal(healAmount);

        yield break;
    }
}
