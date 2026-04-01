using Spine;
using Spine.Unity;
using UnityEngine;

public class SpineAnimationDriver : MonoBehaviour
{
    // 기본 세팅: SkeletonAnimation 하나만 연결하면 상태 전환을 사용할 수 있습니다.
    [Header("References")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimation = "idle";
    [SerializeField] private string moveAnimation = "run";
    [SerializeField] private string jumpAnimation = "jump";
    [SerializeField] private string attackAnimation = "attack";

    [Header("Mix")]
    [SerializeField, Min(0f)] private float defaultMixDuration = 0.1f;
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    // 이동/Idle 계열 애니메이션이 변경되지 않도록 잠그는 플래그입니다.
    // 점프 같은 일시적인 애니메이션이 재생되는 동안 사용합니다.
    private bool _lockMoveAnimations;
    private bool _isMoving;

    public SkeletonAnimation SkeletonAnimation => skeletonAnimation;

    private void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation == null)
        {
            Debug.LogError("[SpineAnimationDriver] SkeletonAnimation reference is missing.", this);
            enabled = false;
            return;
        }

        ApplyDefaultMix();
        PlayIdle(forceRestart: true);
    }

    public void PlayIdle(bool forceRestart = false)
    {
        if (_lockMoveAnimations) return;
        if (!CanPlay(idleAnimation)) return;
        if (!forceRestart && IsCurrent(idleAnimation)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        _isMoving = false;
    }

    public void SetMoving(bool moving)
    {
        if (_lockMoveAnimations) return;
        if (moving == _isMoving) return;
        _isMoving = moving;

        if (_isMoving)
        {
            if (!CanPlay(moveAnimation)) return;
            skeletonAnimation.AnimationState.SetAnimation(0, moveAnimation, true);
        }
        else
        {
            PlayIdle();
        }
    }

    // 이동/Idle 애니메이션 변경을 잠그는 헬퍼입니다.
    public void LockMoveAnimations()
    {
        _lockMoveAnimations = true;
    }

    // 이동/Idle 애니메이션 변경 잠금을 해제하는 헬퍼입니다.
    public void UnlockMoveAnimations()
    {
        _lockMoveAnimations = false;
    }

    public void PlayJump()
    {
        // 점프 애니메이션이 없으면 아무 것도 하지 않습니다.
        if (!CanPlay(jumpAnimation)) return;

        LockMoveAnimations();

        var jumpEntry = skeletonAnimation.AnimationState.SetAnimation(0, jumpAnimation, false);

        // 점프 이후에는 현재 이동 상태에 따라 Idle 또는 Move로 돌아갑니다.
        string next = _isMoving ? moveAnimation : idleAnimation;
        if (CanPlay(next))
            skeletonAnimation.AnimationState.AddAnimation(0, next, true, 0f);

        // 점프 애니메이션이 끝나면 잠금을 해제합니다.
        if (jumpEntry != null)
        {
            jumpEntry.Complete += _ => { UnlockMoveAnimations(); };
        }
    }

    public void PlayAttack()
    {
        if (!CanPlay(attackAnimation)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, attackAnimation, false);

        if (CanPlay(idleAnimation))
            skeletonAnimation.AnimationState.AddAnimation(0, idleAnimation, true, attackToIdleDelay);
    }

    private bool CanPlay(string animationName)
    {
        if (string.IsNullOrWhiteSpace(animationName))
            return false;

        SkeletonData skeletonData = skeletonAnimation.Skeleton?.Data;
        if (skeletonData == null)
            return false;

        Spine.Animation anim = skeletonData.FindAnimation(animationName);
        if (anim != null) return true;

        Debug.LogWarning($"[SpineAnimationDriver] Animation '{animationName}' not found.", this);
        return false;
    }

    private bool IsCurrent(string animationName)
    {
        TrackEntry current = skeletonAnimation.AnimationState.GetCurrent(0);
        return current != null && current.Animation != null && current.Animation.Name == animationName;
    }

    private void ApplyDefaultMix()
    {
        AnimationStateData stateData = skeletonAnimation.AnimationState != null
            ? skeletonAnimation.AnimationState.Data
            : null;
        if (stateData == null) return;

        stateData.DefaultMix = defaultMixDuration;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }
#endif
}
