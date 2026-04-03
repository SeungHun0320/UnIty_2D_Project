using Spine;
using Spine.Unity;
using UnityEngine;

public class SpineAnimationDriver : MonoBehaviour
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

    [Header("Jump Fall Freeze")]
    [Tooltip("velocity.y 가 이 값 이하일 때 점프 애니메이션 마지막 프레임을 유지합니다.")]
    [SerializeField] private float fallVelocityThreshold = -0.1f;

    private bool _lockMove;
    private bool _isMoving;
    private bool _isJumping;
    private TrackEntry _jumpEntry;

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
        if (moving == _isMoving) return;
        _isMoving = moving;
        if (_lockMove) return;
        if (_isMoving)
        {
            if (!CanPlay(moveAnimation)) return;
            skeletonAnimation.AnimationState.SetAnimation(0, moveAnimation, true);
        }
        else { PlayIdle(); }
    }

    public void LockMoveAnimations() { _lockMove = true; }
    public void UnlockMoveAnimations() { _lockMove = false; }

    public void PlayJump()
    {
        if (!CanPlay(jumpAnimation)) return;
        _lockMove = true;
        _isJumping = true;
        _jumpEntry = skeletonAnimation.AnimationState.SetAnimation(0, jumpAnimation, false);
        if (_jumpEntry != null)
            _jumpEntry.Complete += OnJumpDone;
    }

    private void OnJumpDone(TrackEntry e)
    {
        if (!_isJumping) return;
        e.TimeScale = 0f;
        e.TrackTime = e.AnimationEnd;
    }

    public void NotifyVelocityY(float vy)
    {
        if (!_isJumping || _jumpEntry == null) return;
        if (vy <= fallVelocityThreshold)
        {
            _jumpEntry.TimeScale = 0f;
            _jumpEntry.TrackTime = _jumpEntry.AnimationEnd;
        }
        else if (_jumpEntry.TimeScale == 0f)
            _jumpEntry.TimeScale = 1f;
    }

    public void NotifyLanded()
    {
        if (!_isJumping) return;
        _isJumping = false;
        _jumpEntry = null;
        _lockMove = false;
        string next = _isMoving ? moveAnimation : idleAnimation;
        if (CanPlay(next))
            skeletonAnimation.AnimationState.SetAnimation(0, next, true);
    }

    public void PlayAttack()
    {
        if (!CanPlay(attackAnimation)) return;
        skeletonAnimation.AnimationState.SetAnimation(0, attackAnimation, false);
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
