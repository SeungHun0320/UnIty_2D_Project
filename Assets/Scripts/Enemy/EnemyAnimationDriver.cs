using System;
using UnityEngine;

// 몬스터 전용 Spine 애니메이션 드라이버입니다.
public class EnemyAnimationDriver : SpineAnimationDriver
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    public override event Action OnAttackComplete;

    public override void PlayAttack()
    {
        if (_isDead) return;
        if (!CanPlay(Animations.Attack)) return;
        var entry = SkeletonAnimation.AnimationState.SetAnimation(0, Animations.Attack, false);
        entry.Complete += _ => OnAttackComplete?.Invoke();
        if (CanPlay(Animations.Idle))
            SkeletonAnimation.AnimationState.AddAnimation(0, Animations.Idle, true, attackToIdleDelay);
    }
}
