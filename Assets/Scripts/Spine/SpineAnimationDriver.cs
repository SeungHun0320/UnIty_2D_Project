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
    [Tooltip("기본 애니메이션 전환 시 사용할 믹스 시간입니다.")]
    [SerializeField, Min(0f)] private float defaultMixDuration = 0.1f;
    [Tooltip("점프 → Idle 전환 시 사용할 믹스 시간입니다. (0이면 defaultMix 사용)")]
    [SerializeField, Min(0f)] private float jumpToIdleMix = 0.05f;
    [Tooltip("점프 → 이동 전환 시 사용할 믹스 시간입니다. (0이면 defaultMix 사용)")]
    [SerializeField, Min(0f)] private float jumpToMoveMix = 0.05f;
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
        ApplyCustomMixes();
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
        // 잠겨 있어도 현재 이동 여부(_isMoving)는 갱신합니다.
        // 이렇게 해야 점프 도중에 방향키를 떼면, 착지 후 Idle로 자연스럽게 돌아갈 수 있습니다.
        if (moving == _isMoving) return;
        _isMoving = moving;

        if (_lockMoveAnimations) return;

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

        // 점프 애니메이션이 끝나면, 그 시점의 이동 여부(_isMoving)에 따라
        // Idle 또는 Move로 자연스럽게 전환하고 잠금을 해제합니다.
        if (jumpEntry != null)
        {
            jumpEntry.Complete += _ =>
            {
                UnlockMoveAnimations();

                string next = _isMoving ? moveAnimation : idleAnimation;
                if (CanPlay(next))
                    skeletonAnimation.AnimationState.SetAnimation(0, next, true);
            };
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

    // 애니메이션 쌍별 믹스 시간을 설정합니다.
    private void ApplyCustomMixes()
    {
        AnimationStateData stateData = skeletonAnimation.AnimationState != null
            ? skeletonAnimation.AnimationState.Data
            : null;
        if (stateData == null) return;

        float jtIdle = jumpToIdleMix > 0f ? jumpToIdleMix : defaultMixDuration;
        float jtMove = jumpToMoveMix > 0f ? jumpToMoveMix : defaultMixDuration;

        // 점프가 끝나고 Idle/Move로 돌아갈 때의 블렌드를 부드럽게 제어합니다.
        if (!string.IsNullOrWhiteSpace(jumpAnimation) && !string.IsNullOrWhiteSpace(idleAnimation))
            stateData.SetMix(jumpAnimation, idleAnimation, jtIdle);
        if (!string.IsNullOrWhiteSpace(jumpAnimation) && !string.IsNullOrWhiteSpace(moveAnimation))
            stateData.SetMix(jumpAnimation, moveAnimation, jtMove);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }
#endif
}
