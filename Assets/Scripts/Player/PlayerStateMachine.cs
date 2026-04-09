using System.Collections;
using UnityEngine;
// 플레이어 상태를 나타내는 인터페이스입니다. (OCP)
// 새 상태를 추가할 때 기존 코드를 수정하지 않고 새 클래스를 추가하기만 합니다.
public interface IPlayerState
{
    void Enter(PlayerStateMachine sm);
    void Exit(PlayerStateMachine sm);
    void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold);
}

// ---------- 상태 구현 ----------

public class IdleState : IPlayerState
{
    public void Enter(PlayerStateMachine sm) => sm.AnimationDriver?.SetMoving(false);
    public void Exit(PlayerStateMachine sm) { }
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold)
    {
        if (move.sqrMagnitude > threshold * threshold)
            sm.ChangeState(sm.MovingState);
    }
}

public class MovingState : IPlayerState
{
    public void Enter(PlayerStateMachine sm) => sm.AnimationDriver?.SetMoving(true);
    public void Exit(PlayerStateMachine sm) { }
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold)
    {
        if (move.sqrMagnitude <= threshold * threshold)
            sm.ChangeState(sm.IdleState);
    }
}

public class AttackingState : IPlayerState
{
    private Coroutine _hitboxCoroutine;

    public void Enter(PlayerStateMachine sm)
    {
        sm.AnimationDriver?.PlayAttack();
        _hitboxCoroutine = sm.StartCoroutine(HitboxRoutine(sm));
    }

    public void Exit(PlayerStateMachine sm)
    {
        if (_hitboxCoroutine != null)
        {
            sm.StopCoroutine(_hitboxCoroutine);
            _hitboxCoroutine = null;
        }
        sm.AttackHitbox?.Deactivate();
    }

    // 딜레이 후 히트박스를 활성화하고 지정 시간 후 비활성화합니다.
    private IEnumerator HitboxRoutine(PlayerStateMachine sm)
    {
        yield return new WaitForSeconds(sm.AttackHitboxDelay);
        sm.AttackHitbox?.Activate();
        yield return new WaitForSeconds(sm.AttackHitboxDuration);
        sm.AttackHitbox?.Deactivate();
    }

    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold) { }
}

public class JumpingState : IPlayerState
{
    public void Enter(PlayerStateMachine sm) => sm.AnimationDriver?.PlayJump();
    public void Exit(PlayerStateMachine sm) { }
    // 점프 중 이동 입력은 물리(PlayerMover)가 처리 - 상태 전환만 막음
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold)
    {
        sm.LastMoveInput = move;
    }
}

public class HitState : IPlayerState
{
    public void Enter(PlayerStateMachine sm) => sm.AnimationDriver?.PlayHit();
    public void Exit(PlayerStateMachine sm) { }
    // 피격 중 이동 입력은 물리(PlayerMover/넉백)가 처리 - 상태 전환만 막음
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold)
    {
        sm.LastMoveInput = move;
    }
}

public class DeadState : IPlayerState
{
    public void Enter(PlayerStateMachine sm)
    {
        sm.AnimationDriver?.PlayDead();
        sm.AttackHitbox?.Deactivate();
    }
    public void Exit(PlayerStateMachine sm) { }
    // 사망 후 모든 입력 무시
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold) { }
}

// ---------- 상태머신 ----------

public enum PlayerState { Idle, Moving, Attacking, Jumping, Hit, Dead }
[RequireComponent(typeof(PlayerAnimationDriver))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriverComponent;
    [SerializeField] private PlayerAttackHitbox attackHitboxComponent;

    [Header("Attack Timing")]
    [SerializeField, Min(0f)] private float attackHitboxDelay    = 0.8f;
    [SerializeField, Min(0f)] private float attackHitboxDuration = 0.4f;  // 0.8 ~ 1.2초
    public float AttackHitboxDelay    => attackHitboxDelay;
    public float AttackHitboxDuration => attackHitboxDuration;
    public IAttackHitbox AttackHitbox { get; private set; }

    // 상태 싱글턴 인스턴스 (할당 최소화)
    public readonly IdleState IdleState = new();
    public readonly MovingState MovingState = new();
    public readonly AttackingState AttackingState = new();
    public readonly JumpingState JumpingState = new();
    public readonly HitState HitState = new();
    public readonly DeadState DeadState = new();

    private IPlayerState _currentState;

    public IAnimationDriver AnimationDriver { get; private set; }
    public PlayerState CurrentState { get; private set; }
    public Vector2 LastMoveInput { get; set; }
    public float MovingThreshold { get; private set; }

    private void Awake()
    {
        animationDriverComponent ??= GetComponent<SpineAnimationDriver>();
        AnimationDriver = animationDriverComponent;
        AttackHitbox = attackHitboxComponent;

        ChangeState(IdleState);
    }

    public void OnMoveInput(Vector2 move, float movingThreshold)
    {
        MovingThreshold = movingThreshold;
        _currentState?.OnMoveInput(this, move, movingThreshold);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDeadEvent);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawnEvent);
    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDeadEvent);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawnEvent);
    }
    private void OnPlayerDeadEvent(PlayerDeadEvent _) => OnDead();
    private void OnPlayerRespawnEvent(PlayerRespawnEvent _)
    {
        animationDriverComponent?.ResetState();
        ChangeState(IdleState);
    }

    public void OnJumpInput()
    {
        if (CurrentState == PlayerState.Dead) return;
        ChangeState(JumpingState);
    }

    public void OnLanded()
    {
        if (CurrentState == PlayerState.Dead) return;
        // 착지 시점의 이동 입력에 따라 Idle/Moving으로 복귀
        if (LastMoveInput.sqrMagnitude > MovingThreshold * MovingThreshold)
            ChangeState(MovingState);
        else
            ChangeState(IdleState);
    }

    public void OnAttackInput()
    {
        if (CurrentState == PlayerState.Dead) return;
        ChangeState(AttackingState);
    }

    // SpineAnimationDriver.OnAttackComplete 이벤트를 SpineInputController가 전달합니다.
    public void OnAttackComplete()
    {
        if (CurrentState == PlayerState.Dead) return;
        ChangeState(IdleState);
    }

    // PlayerHitReceiver가 피격 시 호출합니다.
    public void OnHit()
    {
        if (CurrentState == PlayerState.Dead) return;
        ChangeState(HitState);
    }

    // PlayerDeadEvent 수신 시 사망 상태로 전환합니다.
    public void OnDead() => ChangeState(DeadState);

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;

        // 열거형 동기화 (인스펙터 표시용)
        if (newState is IdleState) CurrentState = PlayerState.Idle;
        else if (newState is MovingState) CurrentState = PlayerState.Moving;
        else if (newState is AttackingState) CurrentState = PlayerState.Attacking;
        else if (newState is JumpingState) CurrentState = PlayerState.Jumping;
        else if (newState is HitState) CurrentState = PlayerState.Hit;
        else if (newState is DeadState) CurrentState = PlayerState.Dead;

        _currentState.Enter(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<SpineAnimationDriver>();
    }
#endif
}
