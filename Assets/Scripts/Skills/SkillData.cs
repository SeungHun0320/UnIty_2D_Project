using System.Collections;
using UnityEngine;

// 스킬 데이터의 추상 베이스 ScriptableObject입니다. (OCP)
// 새 스킬은 이 클래스를 상속해 Execute()만 구현하면 됩니다.
// SkillState는 타입을 몰라도 Execute()만 호출하므로 기존 코드를 수정하지 않습니다.
public abstract class SkillData : ScriptableObject
{
    [Header("Skill Settings")]
    [Min(0f)] public float cooldown = 0f;

    [Header("Hitbox")]
    public HitboxConfig hitbox = new();

    // 서브클래스가 스킬 행동을 직접 구현합니다.
    public abstract IEnumerator Execute(SkillContext ctx);

    // 히트박스 활성화/비활성화 공통 루틴입니다. 서브클래스에서 재사용합니다.
    protected IEnumerator ActivateHitbox(SkillContext ctx)
    {
        yield return new UnityEngine.WaitForSeconds(hitbox.delay);
        ctx.Hitbox?.Activate();
        yield return new UnityEngine.WaitForSeconds(hitbox.duration);
        ctx.Hitbox?.Deactivate();
    }
}
