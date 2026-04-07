using Spine.Unity;
using UnityEngine;

public class SpineAnimationDriver : MonoBehaviour, IAnimationDriver
{
    [Header("References")]
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [Header("Animation Names")]
    [SerializeField] private string idleAnimation = "idle";
    [SerializeField] private string moveAnimation = "run";
    [SerializeField] private string jumpAnimation = "jump";
    [SerializeField] private string attackAnimation = "attack";

    [Header("Mix")]
    [SerializeField, Min(0f)] private float defaultMixDuration = 0.1f;
    [SerializeField, Min(0f)] private float jumpToIdleMix = 0.05f;
    [SerializeField, Min(0f)] private float jumpToMoveMix = 0.05f;
    [SerializeField, Min(0f)] private float attackToIdleDelay = 0f;

    public event System.Action OnAttackComplete;

    private bool _lockMove;
    private bool _isMoving;
    private bool _isJumping;

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
        BuildMixTable();
        PlayIdle(forceRestart: true);
    }

    public void PlayIdle(bool forceRestart = false)
    {
        if (_lockMove) return;
        if (!CanPlay(idleAnimation)) return;
        if (!forceRestart && IsCurrent(idleAnimation)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        _isMoving = false;
    }

    public void SetMoving(bool moving)
    {
        if (moving == _isMoving) 
            return;

        _isMoving = moving;

        if (_lockMove) return;
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

    public void LockMoveAnimations() { _lockMove = true; }
    public void UnlockMoveAnimations() { _lockMove = false; }

    public void PlayJump()
    {
        if (!CanPlay(jumpAnimation)) return;
        _lockMove = true;
        _isJumping = true;
        skeletonAnimation.AnimationState.SetAnimation(0, jumpAnimation, false);
    }

    public void NotifyLanded()
    {
        _lockMove = false;
        if (!_isJumping) return;
        _isJumping = false;
        string next = _isMoving ? moveAnimation : idleAnimation;
        if (CanPlay(next))
            skeletonAnimation.AnimationState.SetAnimation(0, next, true);
    }

    public void PlayAttack()
    {
        if (!CanPlay(attackAnimation)) return;
        var entry = skeletonAnimation.AnimationState.SetAnimation(0, attackAnimation, false);
        entry.Complete += _ => OnAttackComplete?.Invoke();
        if (CanPlay(idleAnimation))
            skeletonAnimation.AnimationState.AddAnimation(0, idleAnimation, true, attackToIdleDelay);
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

    private void BuildMixTable()
    {
        var sd = skeletonAnimation.AnimationState?.Data;
        if (sd == null) return;
        sd.DefaultMix = defaultMixDuration;
        float jtIdle = jumpToIdleMix > 0f ? jumpToIdleMix : defaultMixDuration;
        float jtMove = jumpToMoveMix > 0f ? jumpToMoveMix : defaultMixDuration;
        if (!string.IsNullOrWhiteSpace(jumpAnimation))
        {
            if (!string.IsNullOrWhiteSpace(idleAnimation)) sd.SetMix(jumpAnimation, idleAnimation, jtIdle);
            if (!string.IsNullOrWhiteSpace(moveAnimation)) sd.SetMix(jumpAnimation, moveAnimation, jtMove);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();
    }
#endif
}
