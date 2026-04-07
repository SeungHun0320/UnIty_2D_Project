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
    public void Enter(PlayerStateMachine sm) => sm.AnimationDriver?.PlayAttack();
    public void Exit(PlayerStateMachine sm) { }
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

// ---------- 상태머신 ----------

public enum PlayerState { Idle, Moving, Attacking, Jumping }
[RequireComponent(typeof(SpineAnimationDriver))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriverComponent;

    // 상태 싱글턴 인스턴스 (할당 최소화)
    public readonly IdleState IdleState = new IdleState();
    public readonly MovingState MovingState = new MovingState();
    public readonly AttackingState AttackingState = new AttackingState();
    public readonly JumpingState JumpingState = new JumpingState();

    private IPlayerState _currentState;

    public IAnimationDriver AnimationDriver { get; private set; }
    public PlayerState CurrentState { get; private set; }
    public Vector2 LastMoveInput { get; set; }
    public float MovingThreshold { get; private set; }

    private void Awake()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<SpineAnimationDriver>();
        AnimationDriver = animationDriverComponent;

        ChangeState(IdleState);
    }

    public void OnMoveInput(Vector2 move, float movingThreshold)
    {
        MovingThreshold = movingThreshold;
        _currentState?.OnMoveInput(this, move, movingThreshold);
    }

    public void OnJumpInput()
    {
        ChangeState(JumpingState);
    }

    public void OnLanded()
    {
        // 착지 시점의 이동 입력에 따라 Idle/Moving으로 복귀
        if (LastMoveInput.sqrMagnitude > MovingThreshold * MovingThreshold)
            ChangeState(MovingState);
        else
            ChangeState(IdleState);
    }

    public void OnAttackInput()
    {
        ChangeState(AttackingState);
    }

    // SpineAnimationDriver.OnAttackComplete 이벤트를 SpineInputController가 전달합니다.
    public void OnAttackComplete()
    {
        ChangeState(IdleState);
    }

    public void ChangeState(IPlayerState newState)
    {
        _currentState?.Exit(this);
        _currentState = newState;

        // 열거형 동기화 (인스펙터 표시용)
        if (newState is IdleState) CurrentState = PlayerState.Idle;
        else if (newState is MovingState) CurrentState = PlayerState.Moving;
        else if (newState is AttackingState) CurrentState = PlayerState.Attacking;
        else if (newState is JumpingState) CurrentState = PlayerState.Jumping;

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
