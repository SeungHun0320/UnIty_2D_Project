using System.Collections;
using UnityEngine;

// 기본 공격 스킬입니다. 공격 애니메이션 재생 후 히트박스 타이밍만 처리합니다.
[CreateAssetMenu(fileName = "BasicAttack", menuName = "Game/Skills/BasicAttack")]
public class BasicAttackData : SkillData
{
    [Header("Hitbox")]
    public HitboxConfig hitbox = new();

    public override IEnumerator Execute(SkillContext ctx)
    {
        ctx.Anim?.PlayAttack();
        yield return ActivateHitbox(ctx, hitbox);
    }
}
