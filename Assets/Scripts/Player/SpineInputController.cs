using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력을 받아 PlayerMover(물리)와 PlayerSkillController(스킬)에 위임합니다. (SRP)
// skillInputActions 배열이 슬롯과 1:1 대응합니다 — Inspector에서 키를 추가하면 슬롯도 늘어납니다.
// 각 슬롯의 activationType(Instant/Hold)에 따라 started/canceled/performed를 자동으로 분기합니다.
public class SpineInputController : InputControllerBase
{
    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [Tooltip("슬롯 0, 1, 2… 순서대로 대응합니다. 슬롯 수만큼 추가하세요.")]
    [SerializeField] private InputActionProperty[] skillInputActions;
    [SerializeField] private InputActionProperty jumpAction;

    private InputAction   _runtimeMoveAction;
    private InputAction[] _runtimeSkillActions;
    private InputAction   _runtimeJumpAction;

    // 람다 핸들러 레퍼런스 저장 — OnDisable에서 -= 해제에 필요합니다.
    private System.Action<InputAction.CallbackContext>[] _skillStartedHandlers;
    private System.Action<InputAction.CallbackContext>[] _skillCanceledHandlers;
    private System.Action<InputAction.CallbackContext>[] _skillPerformedHandlers;

    protected override void Awake()
    {
        base.Awake();
        EnsureActions();
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
        if (IsInputBlocked()) { _moveInput = Vector2.zero; return; }

        InputAction move = GetMoveAction();
        _moveInput = move != null ? move.ReadValue<Vector2>() : Vector2.zero;
        ApplyMoveInput();
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
        _runtimeSkillActions = new InputAction[Mathf.Max(count, PlayerSkillController.DefaultSlotCount)];

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
        action.AddCompositeBinding("2DVector")
            .With("Up",    "<Keyboard>/upArrow")
            .With("Down",  "<Keyboard>/downArrow")
            .With("Left",  "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        action.AddBinding("<Gamepad>/leftStick");
        return action;
    }

    // SlotAttack → X, SlotSpecial → A, SlotDash → C (예약)
    private static InputAction CreateDefaultSkillAction(int slotIndex)
    {
        var action = new InputAction(name: $"Skill_{slotIndex}", type: InputActionType.Button);
        switch (slotIndex)
        {
            case PlayerSkillController.SlotAttack:
                action.AddBinding("<Keyboard>/x");
                action.AddBinding("<Gamepad>/buttonSouth");
                break;
            case PlayerSkillController.SlotSpecial:
                action.AddBinding("<Keyboard>/a");
                action.AddBinding("<Gamepad>/buttonWest");
                break;
            case PlayerSkillController.SlotDash:
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
    protected override void OnValidate() => base.OnValidate();
#endif
}
