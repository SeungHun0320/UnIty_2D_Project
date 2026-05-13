using UnityEngine;

// SpineInputController / MobileInputController 공통 로직을 담는 추상 베이스 클래스입니다.
// 컴포넌트 참조, 이벤트 구독, 물리 틱, 이동 입력 적용을 통합합니다.
[RequireComponent(typeof(PlayerAnimationDriver))]
[RequireComponent(typeof(PlayerMover))]
[RequireComponent(typeof(PlayerStateMachine))]
public abstract class InputControllerBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected PlayerAnimationDriver animationDriverComponent;
    [SerializeField] protected PlayerStateMachine    playerStateMachine;
    [SerializeField] protected PlayerMover           playerMover;
    [SerializeField] protected PlayerSkillController skillController;

    protected Vector2 _moveInput;

    protected virtual void Awake()
    {
        animationDriverComponent ??= GetComponent<PlayerAnimationDriver>();
        playerStateMachine       ??= GetComponent<PlayerStateMachine>();
        playerMover              ??= GetComponent<PlayerMover>();
        skillController          ??= GetComponent<PlayerSkillController>();

        if (playerMover              != null) playerMover.OnLanded                       += HandleLanded;
        if (animationDriverComponent != null) animationDriverComponent.OnActionComplete  += HandleAttackComplete;
    }

    protected virtual void OnDestroy()
    {
        if (playerMover              != null) playerMover.OnLanded                       -= HandleLanded;
        if (animationDriverComponent != null) animationDriverComponent.OnActionComplete  -= HandleAttackComplete;
    }

    protected virtual void FixedUpdate() => playerMover?.FixedTick(_moveInput);

    protected void HandleLanded()
    {
        animationDriverComponent?.NotifyLanded();
        playerStateMachine?.OnLanded();
    }

    protected void HandleAttackComplete() => playerStateMachine?.OnAttackComplete();

    // GameOver/StageClear/Paused 상태에서 입력을 차단합니다.
    protected bool IsInputBlocked()
    {
        var s = GameInstance.Instance?.CurrentGameState;
        return s == GameState.GameOver || s == GameState.StageClear || s == GameState.Paused;
    }

    // _moveInput을 상태머신과 물리에 전달합니다.
    protected void ApplyMoveInput()
    {
        playerStateMachine?.OnMoveInput(_moveInput, playerMover != null ? playerMover.MovingThreshold : 0.05f);
        playerMover?.FlipByInput(_moveInput.x);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        animationDriverComponent ??= GetComponent<PlayerAnimationDriver>();
        playerStateMachine       ??= GetComponent<PlayerStateMachine>();
        playerMover              ??= GetComponent<PlayerMover>();
        skillController          ??= GetComponent<PlayerSkillController>();
    }
#endif
}
