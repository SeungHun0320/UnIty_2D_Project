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

// 스킬 실행 상태입니다. CurrentSkill.Execute()를 실행하며 타입을 알 필요가 없습니다.
public class SkillState : IPlayerState
{
    private Coroutine _routine;

    public void Enter(PlayerStateMachine sm)
    {
        _routine = sm.StartCoroutine(sm.CurrentSkill.Execute(sm.SkillContext));
    }

    public void Exit(PlayerStateMachine sm)
    {
        if (_routine != null)
        {
            sm.StopCoroutine(_routine);
            _routine = null;
        }
        sm.SkillContext.Hitbox?.Deactivate();
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
        sm.SkillContext.Hitbox?.Deactivate();
    }
    public void Exit(PlayerStateMachine sm) { }
    // 사망 후 모든 입력 무시
    public void OnMoveInput(PlayerStateMachine sm, Vector2 move, float threshold) { }
}

// ---------- 상태머신 ----------

public enum PlayerState { Idle, Moving, Skill, Jumping, Hit, Dead }

[RequireComponent(typeof(PlayerAnimationDriver))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriverComponent;
    [SerializeField] private PlayerAttackHitbox attackHitboxComponent;
    [SerializeField] private PlayerMover playerMoverComponent;
    [SerializeField] private PlayerStats playerStatsComponent;

    // 현재 실행 중인 스킬 데이터입니다. SkillState가 Execute() 호출 시 사용합니다.
    public SkillData CurrentSkill { get; private set; }

    // 스킬 실행에 필요한 참조 묶음입니다. Awake에서 한 번 생성됩니다.
    public SkillContext SkillContext { get; private set; }

    // 상태 싱글턴 인스턴스 (할당 최소화)
    public readonly IdleState    IdleState    = new();
    public readonly MovingState  MovingState  = new();
    public readonly SkillState   SkillState   = new();
    public readonly JumpingState JumpingState = new();
    public readonly HitState     HitState     = new();
    public readonly DeadState    DeadState    = new();

    private IPlayerState _currentState;

    public IAnimationDriver AnimationDriver { get; private set; }
    public PlayerState CurrentState { get; private set; }
    public Vector2 LastMoveInput { get; set; }
    public float MovingThreshold { get; private set; }

    private void Awake()
    {
        animationDriverComponent ??= GetComponent<SpineAnimationDriver>();
        playerMoverComponent     ??= GetComponent<PlayerMover>();
        playerStatsComponent     ??= GetComponent<PlayerStats>();

        AnimationDriver = animationDriverComponent;

        SkillContext = new SkillContext
        {
            Hitbox  = attackHitboxComponent,
            Anim    = animationDriverComponent,
            Mover   = playerMoverComponent,
            Stats   = playerStatsComponent,
            Origin  = transform,
        };

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
        if (LastMoveInput.sqrMagnitude > MovingThreshold * MovingThreshold)
            ChangeState(MovingState);
        else
            ChangeState(IdleState);
    }

    // PlayerSkillController가 쿨다운 검증 후 호출합니다.
    public void OnSkillInput(SkillData skill)
    {
        if (CurrentState == PlayerState.Dead) return;
        CurrentSkill = skill;
        ChangeState(SkillState);
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
        if      (newState is IdleState)    CurrentState = PlayerState.Idle;
        else if (newState is MovingState)  CurrentState = PlayerState.Moving;
        else if (newState is SkillState)   CurrentState = PlayerState.Skill;
        else if (newState is JumpingState) CurrentState = PlayerState.Jumping;
        else if (newState is HitState)     CurrentState = PlayerState.Hit;
        else if (newState is DeadState)    CurrentState = PlayerState.Dead;

        _currentState.Enter(this);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<SpineAnimationDriver>();
        if (playerMoverComponent == null)
            playerMoverComponent = GetComponent<PlayerMover>();
        if (playerStatsComponent == null)
            playerStatsComponent = GetComponent<PlayerStats>();
    }
#endif
}
