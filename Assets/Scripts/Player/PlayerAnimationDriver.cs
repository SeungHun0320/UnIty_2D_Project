using Spine.Unity;
using System;
using UnityEngine;

// 플레이어 전용 Spine 애니메이션 드라이버입니다.
public class PlayerAnimationDriver : SpineAnimationDriver
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    public override event Action OnActionComplete;

    public override void PlayAttack()
    {
        if (_isDead) return;
        if (!CanPlay(Animations.Attack)) return;
        var entry = SetAnim(0, Animations.Attack, false, Speeds.Attack);
        entry.Complete += _ => OnActionComplete?.Invoke();
        if (CanPlay(Animations.Idle))
            SkeletonAnimation.AnimationState.AddAnimation(0, Animations.Idle, true, attackToIdleDelay);
    }

    // 애니메이션 키를 직접 지정해 재생합니다. 스킬별로 다른 애니메이션을 쓸 때 사용합니다.
    public override void PlayAnimation(string animationKey)
    {
        if (_isDead) return;
        if (!CanPlay(animationKey)) return;
        _lockMove = true;
        // 스킬 전용 커스텀 키는 Attack 속도를 기본으로 사용합니다.
        var entry = SetAnim(0, animationKey, false, Speeds.Attack);
        entry.Complete += _ =>
        {
            _lockMove = false;
            OnActionComplete?.Invoke();
            string next = _isMoving ? Animations.Move : Animations.Idle;
            if (CanPlay(next))
                SetAnim(0, next, true, next == Animations.Move ? Speeds.Move : Speeds.Idle);
        };
    }
}
