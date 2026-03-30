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
    [SerializeField] private string attackAnimation = "attack";

    [Header("Mix")]
    [SerializeField, Min(0f)] private float defaultMixDuration = 0.1f;
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

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
        if (!CanPlay(idleAnimation)) return;
        if (!forceRestart && IsCurrent(idleAnimation)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        _isMoving = false;
    }

    public void SetMoving(bool moving)
    {
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
