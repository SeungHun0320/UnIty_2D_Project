using System;
using UnityEngine;

// 몬스터 전용 Spine 애니메이션 드라이버입니다.
public class EnemyAnimationDriver : SpineAnimationDriver
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    public override event Action OnActionComplete;

    public override void PlayAttack()
    {
        if (_isDead) return;
        if (!CanPlay(Animations.Attack)) return;
        var entry = SkeletonAnimation.AnimationState.SetAnimation(0, Animations.Attack, false);
        entry.Complete += _ => OnActionComplete?.Invoke();
        if (CanPlay(Animations.Idle))
            SkeletonAnimation.AnimationState.AddAnimation(0, Animations.Idle, true, attackToIdleDelay);
    }

    // 몬스터는 현재 스킬 없음 — 추후 확장 시 구현합니다.
    public override void PlayAnimation(string animationKey) { }
}
