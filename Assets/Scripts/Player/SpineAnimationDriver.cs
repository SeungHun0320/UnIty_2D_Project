using Spine.Unity;
using System;
using UnityEngine;

// 애니메이션 이름을 묶어 관리하는 값 타입입니다.
[Serializable]
public struct AnimationNames
{
    public string Idle;
    public string Move;
    public string Jump;
    public string Attack;
    public string Hit;
    public string Dead;
}

public class SpineAnimationDriver : MonoBehaviour, IAnimationDriver
{
    [Header("References")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField] private AnimationNames animations = new()
    {
        Idle   = "idle",
        Move   = "run",
        Jump   = "jump",
        Attack = "attack",
        Hit    = "hit",
        Dead   = "death",
    };

    [Header("Timing")]
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    public event Action OnAttackComplete;

    private bool _lockMove;
    private bool _isMoving;
    private bool _isJumping;
    private bool _isDead;

    public SkeletonAnimation SkeletonAnimation => skeletonAnimation;

    private void Awake()
    {
        skeletonAnimation ??= GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            Debug.LogError("[SpineAnimationDriver] SkeletonAnimation reference is missing.", this);
            enabled = false;
            return;
        }
        PlayIdle(forceRestart: true);
    }

    public void PlayIdle(bool forceRestart = false)
    {
        if (_isDead || _lockMove) return;
        if (!CanPlay(animations.Idle)) return;
        if (!forceRestart && IsCurrent(animations.Idle)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, animations.Idle, true);
        _isMoving = false;
    }

    public void SetMoving(bool moving)
    {
        if (moving == _isMoving) return;
        _isMoving = moving;

        if (_isDead || _lockMove) return;
        if (_isMoving)
        {
            if (!CanPlay(animations.Move)) return;
            skeletonAnimation.AnimationState.SetAnimation(0, animations.Move, true);
        }
        else
        {
            PlayIdle();
        }
    }

    public void LockMoveAnimations()   { _lockMove = true; }
    public void UnlockMoveAnimations() { _lockMove = false; }

    public void PlayJump()
    {
        if (_isDead) return;
        if (!CanPlay(animations.Jump)) return;
        _lockMove = true;
        _isJumping = true;
        skeletonAnimation.AnimationState.SetAnimation(0, animations.Jump, false);
    }

    public void NotifyLanded()
    {
        if (_isDead) return;
        _lockMove = false;
        if (!_isJumping) return;
        _isJumping = false;
        string next = _isMoving ? animations.Move : animations.Idle;
        if (CanPlay(next))
            skeletonAnimation.AnimationState.SetAnimation(0, next, true);
    }

    public void PlayAttack()
    {
        if (_isDead) return;
        if (!CanPlay(animations.Attack)) return;
        var entry = skeletonAnimation.AnimationState.SetAnimation(0, animations.Attack, false);
        entry.Complete += _ => OnAttackComplete?.Invoke();
        if (CanPlay(animations.Idle))
            skeletonAnimation.AnimationState.AddAnimation(0, animations.Idle, true, attackToIdleDelay);
    }

    public void PlayHit()
    {
        if (_isDead) return;
        if (!CanPlay(animations.Hit)) return;
        var entry = skeletonAnimation.AnimationState.SetAnimation(0, animations.Hit, false);
        // 히트 애니메이션 종료 후 idle로 복귀합니다.
        entry.Complete += _ =>
        {
            string next = _isMoving ? animations.Move : animations.Idle;
            if (CanPlay(next))
                skeletonAnimation.AnimationState.SetAnimation(0, next, true);
        };
    }

    // 사망 애니메이션을 재생합니다. 이후 모든 애니메이션 전환을 차단합니다.
    public void PlayDead()
    {
        if (_isDead) return;
        _isDead   = true;
        _lockMove = true;
        if (!CanPlay(animations.Dead)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, animations.Dead, false);
    }

    private bool CanPlay(string anim)
    {
        if (string.IsNullOrWhiteSpace(anim)) return false;
        var data = skeletonAnimation.Skeleton?.Data;
        if (data == null) return false;
        if (data.FindAnimation(anim) != null) return true;
        Debug.LogWarning($"[SpineAnimationDriver] Animation '{anim}' not found.", this);
        return false;
    }

    private bool IsCurrent(string anim)
    {
        var cur = skeletonAnimation.AnimationState.GetCurrent(0);
        return cur != null && cur.Animation?.Name == anim;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }
#endif
}
