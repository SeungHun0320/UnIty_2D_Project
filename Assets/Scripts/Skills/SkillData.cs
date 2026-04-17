using System.Collections;
using UnityEngine;

// 스킬 발동 방식을 정의합니다.
// Instant: 키를 누르는 즉시 발동 / Hold: 일정 시간 누르고 있어야 발동
public enum SkillActivationType { Instant, Hold }

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

    [Header("Activation")]
    [Tooltip("발동 방식: Instant(즉시) / Hold(길게 누르기)")]
    public SkillActivationType activationType = SkillActivationType.Instant;
    [Tooltip("Hold 타입일 때 발동에 필요한 누르기 지속 시간(초)")]
    [Min(0f)] public float holdDuration = 1f;

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

    // 히트박스 활성화/비활성화 공통 루틴입니다. hitbox를 직접 보유한 서브클래스에서 호출합니다.
    // 애니메이션 재생 속도(CurrentTrackTimeScale)로 타이밍을 자동 보정합니다.
    protected IEnumerator ActivateHitbox(SkillContext ctx, HitboxConfig hitbox)
    {
        if (hitbox == null) yield break;
        float s = ctx.Anim?.CurrentTrackTimeScale ?? 1f;
        yield return new UnityEngine.WaitForSeconds(hitbox.delay / s);
        ctx.Hitbox?.Activate();
        yield return new UnityEngine.WaitForSeconds(hitbox.duration / s);
        ctx.Hitbox?.Deactivate();
    }
}
