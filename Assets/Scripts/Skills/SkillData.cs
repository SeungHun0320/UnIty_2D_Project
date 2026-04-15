using System.Collections;
using UnityEngine;

// 스킬 데이터의 추상 베이스 ScriptableObject입니다. (OCP)
// 새 스킬은 이 클래스를 상속해 Execute()만 구현하면 됩니다.
// SkillState는 타입을 몰라도 Execute()만 호출하므로 기존 코드를 수정하지 않습니다.
public abstract class SkillData : ScriptableObject
{
    [Header("Skill Settings")]
    [Min(0f)] public float cooldown = 0f;

    [Header("Soul")]
    [Tooltip("스킬 사용 시 소모할 소울입니다. 0이면 소울 소모 없음.")]
    [Min(0)] public int soulCost = 0;

    [Header("Hitbox")]
    public HitboxConfig hitbox = new();

    [Header("Effects")]
    public SkillEffect[] effects;

    // 서브클래스가 스킬 행동을 직접 구현합니다.
    public abstract IEnumerator Execute(SkillContext ctx);

    // 등록된 모든 이펙트를 재생합니다. 서브클래스의 Execute()에서 호출합니다.
    protected void PlayEffects(SkillContext ctx)
    {
        if (effects == null) return;
        foreach (var effect in effects)
            effect?.Play(ctx);
    }

    // 히트박스 활성화/비활성화 공통 루틴입니다. 서브클래스에서 재사용합니다.
    // 애니메이션 재생 속도(CurrentTrackTimeScale)로 타이밍을 자동 보정합니다.
    protected IEnumerator ActivateHitbox(SkillContext ctx)
    {
        float s = ctx.Anim?.CurrentTrackTimeScale ?? 1f;
        yield return new UnityEngine.WaitForSeconds(hitbox.delay / s);
        ctx.Hitbox?.Activate();
        yield return new UnityEngine.WaitForSeconds(hitbox.duration / s);
        ctx.Hitbox?.Deactivate();
    }
}
