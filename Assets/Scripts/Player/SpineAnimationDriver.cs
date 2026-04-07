using Spine;
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

    [Header("Jump Fall Freeze")]
    [Tooltip("velocity.y 가 이 값 이하일 때 점프 애니메이션 마지막 프레임을 유지합니다.")]
    [SerializeField] private float fallVelocityThreshold = -0.1f;

    public event System.Action OnAttackComplete;

    private bool _lockMove;
    private bool _isMoving;
    private bool _isJumping;
    private bool _jumpAnimDone;
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
        if (_lockMove)
        {
            Debug.Log("[PlayIdle] _lockMove is true, returning");
            return;
        }
        if (!CanPlay(idleAnimation))
        {
            Debug.Log("[PlayIdle] Cannot play idle animation");
            return;
        }
        if (!forceRestart && IsCurrent(idleAnimation))
        {
            Debug.Log("[PlayIdle] Already playing idle, skipping");
            return;
        }
        Debug.Log("[PlayIdle] Setting idle animation");
        skeletonAnimation.AnimationState.SetAnimation(0, idleAnimation, true);
        _isMoving = false;
    }

    public void SetMoving(bool moving)
    {
        if (moving == _isMoving) return;
        _isMoving = moving;
        if (_lockMove)
        {
            Debug.Log($"[SetMoving] _lockMove is true, returning. moving={moving}");
            return;
        }
        if (_isMoving)
        {
            if (!CanPlay(moveAnimation)) return;
            Debug.Log("[SetMoving] Playing move animation");
            skeletonAnimation.AnimationState.SetAnimation(0, moveAnimation, true);
        }
        else
        {
            Debug.Log("[SetMoving] Calling PlayIdle()");
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
        _jumpAnimDone = false;
        _jumpEntry = skeletonAnimation.AnimationState.SetAnimation(0, jumpAnimation, false);
        if (_jumpEntry != null)
        {
            // TrackEnd=MaxValue: Complete 이후 Spine이 entry를 자동 제거하는 것을 방지
            // loop=false 유지: Spine이 재생 시간을 min(trackTime, AnimationEnd)로 클램프
            //                  → 완료 후 자동으로 마지막 프레임 고정
            _jumpEntry.TrackEnd = float.MaxValue;
            _jumpEntry.Complete += OnJumpDone;
        }
    }

    private void OnJumpDone(TrackEntry e)
    {
        if (!_isJumping) return;
        _jumpAnimDone = true;
        e.TimeScale = 0f;
        // TrackEnd=MaxValue이므로 entry가 살아있고, loop=false이므로 AnimationEnd에서 클램프됨
    }

    public void NotifyVelocityY(float vy)
    {
        if (!_isJumping || _jumpEntry == null) return;
        if (vy <= fallVelocityThreshold)
        {
            // 낙하 감지: 마지막 프레임으로 이동 후 정지
            _jumpEntry.TrackTime = _jumpEntry.AnimationEnd;
            _jumpEntry.TimeScale = 0f;
        }
        else if (!_jumpAnimDone && _jumpEntry.TimeScale == 0f)
        {
            // 완료 전에만 재개 (완료 후에는 마지막 프레임 고정 유지)
            _jumpEntry.TimeScale = 1f;
        }
    }

    public void NotifyLanded()
    {
        _lockMove = false;
        if (!_isJumping) return;
        _isJumping = false;
        _jumpAnimDone = false;
        _jumpEntry = null;
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
