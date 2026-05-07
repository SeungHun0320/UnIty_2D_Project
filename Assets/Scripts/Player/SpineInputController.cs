using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력을 받아 PlayerMover(물리)와 PlayerSkillController(스킬)에 위임합니다. (SRP)
// skillInputActions 배열이 슬롯과 1:1 대응합니다 — Inspector에서 키를 추가하면 슬롯도 늘어납니다.
// 각 슬롯의 activationType(Instant/Hold)에 따라 started/canceled/performed를 자동으로 분기합니다.
[RequireComponent(typeof(PlayerAnimationDriver))]
[RequireComponent(typeof(PlayerMover))]
public class SpineInputController : MonoBehaviour
{
    [Header("References")]
    // abstract 타입 직렬화 시 Unity 6 빌드에서 역직렬화 실패 → 구체 타입으로 선언합니다.
    [SerializeField] private PlayerAnimationDriver animationDriverComponent;
    [SerializeField] private PlayerStateMachine playerStateMachine;
    [SerializeField] private PlayerMover playerMover;
    [SerializeField] private PlayerSkillController skillController;

    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [Tooltip("슬롯 0, 1, 2… 순서대로 대응합니다. 슬롯 수만큼 추가하세요.")]
    [SerializeField] private InputActionProperty[] skillInputActions;
    [SerializeField] private InputActionProperty jumpAction;

    private IAnimationDriver animationDriver;

    private InputAction   _runtimeMoveAction;
    private InputAction[] _runtimeSkillActions;
    private InputAction   _runtimeJumpAction;

    // 람다 핸들러 레퍼런스 저장 — OnDisable에서 -= 해제에 필요합니다.
    private System.Action<InputAction.CallbackContext>[] _skillStartedHandlers;
    private System.Action<InputAction.CallbackContext>[] _skillCanceledHandlers;
    private System.Action<InputAction.CallbackContext>[] _skillPerformedHandlers;

    private Vector2 _moveInput;

    private void Awake()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<PlayerAnimationDriver>();
        animationDriver = animationDriverComponent;

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();

        if (skillController == null)
            skillController = GetComponent<PlayerSkillController>();

        if (playerMover != null)
            playerMover.OnLanded += HandleLanded;
        if (animationDriverComponent != null)
            animationDriverComponent.OnActionComplete += HandleAttackComplete;

        EnsureActions();
    }

    private void OnDestroy()
    {
        if (playerMover != null)
            playerMover.OnLanded -= HandleLanded;
        if (animationDriverComponent != null)
            animationDriverComponent.OnActionComplete -= HandleAttackComplete;
    }

    private void OnEnable()
    {
        GetMoveAction()?.Enable();

        // 슬롯별로 3종 이벤트 등록 — 핸들러를 배열에 저장해 OnDisable에서 해제합니다.
        int skillCount = GetSkillActionCount();
        _skillStartedHandlers   = new System.Action<InputAction.CallbackContext>[skillCount];
        _skillCanceledHandlers  = new System.Action<InputAction.CallbackContext>[skillCount];
        _skillPerformedHandlers = new System.Action<InputAction.CallbackContext>[skillCount];

        for (int i = 0; i < skillCount; i++)
        {
            int captured = i;   // 클로저 캡처 방지
            InputAction action = GetSkillAction(i);
            if (action == null) continue;

            _skillStartedHandlers[i]   = _ => skillController?.OnSlotPressed(captured);
            _skillCanceledHandlers[i]  = _ => skillController?.OnSlotReleased(captured);
            _skillPerformedHandlers[i] = _ => skillController?.OnSlotPerformed(captured);

            action.started   += _skillStartedHandlers[i];
            action.canceled  += _skillCanceledHandlers[i];
            action.performed += _skillPerformedHandlers[i];
            action.Enable();
        }

        InputAction jump = GetJumpAction();
        if (jump != null) { jump.performed += OnJumpPerformed; jump.Enable(); }
    }

    private void OnDisable()
    {
        GetMoveAction()?.Disable();

        for (int i = 0; i < GetSkillActionCount(); i++)
        {
            InputAction action = GetSkillAction(i);
            if (action == null) continue;

            if (_skillStartedHandlers?[i]   != null) action.started   -= _skillStartedHandlers[i];
            if (_skillCanceledHandlers?[i]  != null) action.canceled  -= _skillCanceledHandlers[i];
            if (_skillPerformedHandlers?[i] != null) action.performed -= _skillPerformedHandlers[i];
            action.Disable();
        }
        _skillStartedHandlers   = null;
        _skillCanceledHandlers  = null;
        _skillPerformedHandlers = null;

        InputAction jumpOff = GetJumpAction();
        if (jumpOff != null) { jumpOff.performed -= OnJumpPerformed; jumpOff.Disable(); }
    }

    private void Update()
    {
        if (playerStateMachine == null) return;

        // 사망·클리어·일시정지 상태에서는 이동 입력을 차단합니다.
        var state = GameInstance.Instance?.CurrentGameState;
        if (state == GameState.GameOver
         || state == GameState.StageClear
         || state == GameState.Paused)
        {
            _moveInput = Vector2.zero;
            return;
        }

        InputAction move = GetMoveAction();
        _moveInput = move != null ? move.ReadValue<Vector2>() : Vector2.zero;

        playerStateMachine.OnMoveInput(_moveInput, playerMover != null ? playerMover.MovingThreshold : 0.05f);
        playerMover?.FlipByInput(_moveInput.x);
    }

    private void FixedUpdate()
    {
        playerMover?.FixedTick(_moveInput);
    }

    private void HandleLanded()
    {
        animationDriver?.NotifyLanded();
        playerStateMachine?.OnLanded();
    }

    private void HandleAttackComplete()
    {
        playerStateMachine?.OnAttackComplete();
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        if (playerMover == null) return;
        bool groundedNow = playerMover.IsGrounded || playerMover.CheckGroundedImmediate();
        if (!groundedNow && !playerMover.CanJump) return;

        playerStateMachine?.OnJumpInput();
        playerMover.Jump();
    }

    // ── 액션 해석 헬퍼 ───────────────────────────────────────────────────────

    private void EnsureActions()
    {
        if (!HasUsableBindings(moveAction.action))
            _runtimeMoveAction = CreateDefaultMoveAction();

        int count = skillInputActions != null ? skillInputActions.Length : 0;
        _runtimeSkillActions = new InputAction[Mathf.Max(count, 3)];

        for (int i = 0; i < _runtimeSkillActions.Length; i++)
        {
            bool hasBinding = i < count && HasUsableBindings(skillInputActions[i].action);
            if (!hasBinding)
                _runtimeSkillActions[i] = CreateDefaultSkillAction(i);
        }

        if (!HasUsableBindings(jumpAction.action))
            _runtimeJumpAction = CreateDefaultJumpAction();
    }

    private InputAction GetMoveAction() =>
        HasUsableBindings(moveAction.action) ? moveAction.action : _runtimeMoveAction;

    private InputAction GetSkillAction(int index)
    {
        bool hasBinding = skillInputActions != null
                       && index < skillInputActions.Length
                       && HasUsableBindings(skillInputActions[index].action);

        if (hasBinding) return skillInputActions[index].action;
        if (_runtimeSkillActions != null && index < _runtimeSkillActions.Length)
            return _runtimeSkillActions[index];
        return null;
    }

    private int GetSkillActionCount()
    {
        int inspector = skillInputActions != null ? skillInputActions.Length : 0;
        int runtime   = _runtimeSkillActions != null ? _runtimeSkillActions.Length : 0;
        return Mathf.Max(inspector, runtime);
    }

    private InputAction GetJumpAction() =>
        HasUsableBindings(jumpAction.action) ? jumpAction.action : _runtimeJumpAction;

    private static bool HasUsableBindings(InputAction action) =>
        action != null && action.bindings.Count > 0;

    // ── 기본 바인딩 ──────────────────────────────────────────────────────────

    private static InputAction CreateDefaultMoveAction()
    {
        var action = new InputAction(name: "Move", type: InputActionType.Value);
        // 방향키만 사용합니다. WASD는 제거했습니다.
        action.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        action.AddBinding("<Gamepad>/leftStick");
        return action;
    }

    // 슬롯 인덱스에 따라 기본 키를 할당합니다.
    // slot 0 → X (기본 공격 Instant)
    // slot 1 → A (TapOrHold: 짧게=총알, 길게=체력회복)
    // slot 2 → C (예약: 대시)
    private static InputAction CreateDefaultSkillAction(int slotIndex)
    {
        var action = new InputAction(name: $"Skill_{slotIndex}", type: InputActionType.Button);
        switch (slotIndex)
        {
            case 0:
                action.AddBinding("<Keyboard>/x");
                action.AddBinding("<Gamepad>/buttonSouth");
                break;
            case 1:
                action.AddBinding("<Keyboard>/a");
                action.AddBinding("<Gamepad>/buttonWest");
                break;
            case 2:
                // 대시용 예약 — 나중에 구현
                action.AddBinding("<Keyboard>/c");
                action.AddBinding("<Gamepad>/buttonNorth");
                break;
            default:
                // 슬롯 3 이상은 키 없음 — Inspector에서 직접 할당하세요.
                break;
        }
        return action;
    }

    private static InputAction CreateDefaultJumpAction()
    {
        var action = new InputAction(name: "Jump", type: InputActionType.Button);
        action.AddBinding("<Keyboard>/space");
        action.AddBinding("<Keyboard>/z");
        action.AddBinding("<Gamepad>/buttonSouth");
        return action;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<PlayerAnimationDriver>();

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();

        if (skillController == null)
            skillController = GetComponent<PlayerSkillController>();
    }
#endif
}
