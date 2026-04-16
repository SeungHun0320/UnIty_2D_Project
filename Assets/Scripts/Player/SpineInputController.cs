using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력을 받아 PlayerMover(물리)와 PlayerSkillController(스킬)에 위임합니다. (SRP)
// 이 클래스는 입력 처리만 담당합니다.
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
    [SerializeField] private InputActionProperty attackAction;
    [SerializeField] private InputActionProperty skillAction;
    [SerializeField] private InputActionProperty jumpAction;

    private IAnimationDriver animationDriver;

    private InputAction _runtimeMoveAction;
    private InputAction _runtimeAttackAction;
    private InputAction _runtimeSkillAction;
    private InputAction _runtimeJumpAction;

    private Vector2 _moveInput;
    private bool _waitingForRespawn;
    private bool _waitingForRestart;
    private InputAction _runtimeRespawnAction;
    private InputAction _runtimeRestartAction;

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

        _runtimeRespawnAction = new InputAction(name: "Respawn", type: InputActionType.Button);
        _runtimeRespawnAction.AddBinding("<Keyboard>/e");
        _runtimeRespawnAction.AddBinding("<Gamepad>/buttonEast");

        _runtimeRestartAction = new InputAction(name: "Restart", type: InputActionType.Button);
        _runtimeRestartAction.AddBinding("<Keyboard>/r");
        _runtimeRestartAction.AddBinding("<Gamepad>/start");

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
        InputAction move = GetMoveAction();
        if (move != null) move.Enable();

        InputAction attack = GetAttackAction();
        if (attack != null) { attack.performed += OnAttackPerformed; attack.Enable(); }

        InputAction skill = GetSkillAction();
        if (skill != null) { skill.performed += OnSkillPerformed; skill.Enable(); }

        InputAction jump = GetJumpAction();
        if (jump != null) { jump.performed += OnJumpPerformed; jump.Enable(); }

        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<StageClearEvent>(OnStageClear);
    }

    private void OnDisable()
    {
        InputAction move = GetMoveAction();
        if (move != null) move.Disable();

        InputAction attack = GetAttackAction();
        if (attack != null) { attack.performed -= OnAttackPerformed; attack.Disable(); }

        InputAction skill = GetSkillAction();
        if (skill != null) { skill.performed -= OnSkillPerformed; skill.Disable(); }

        InputAction jump = GetJumpAction();
        if (jump != null) { jump.performed -= OnJumpPerformed; jump.Disable(); }

        if (_runtimeRespawnAction != null)
        {
            _runtimeRespawnAction.performed -= OnRespawnPerformed;
            _runtimeRespawnAction.Disable();
        }

        if (_runtimeRestartAction != null)
        {
            _runtimeRestartAction.performed -= OnRestartPerformed;
            _runtimeRestartAction.Disable();
        }

        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<StageClearEvent>(OnStageClear);
    }

    // 사망 시 일반 입력을 차단하고 E키 리스폰 대기 모드로 전환합니다.
    private void OnPlayerDead(PlayerDeadEvent _)
    {
        if (_waitingForRespawn) return;
        _waitingForRespawn = true;
        _runtimeRespawnAction.performed += OnRespawnPerformed;
        _runtimeRespawnAction.Enable();
    }

    private void OnRespawnPerformed(InputAction.CallbackContext _)
    {
        if (!_waitingForRespawn) return;
        GameManager.Instance?.RespawnPlayer();
    }

    private void OnPlayerRespawn(PlayerRespawnEvent _)
    {
        _waitingForRespawn = false;
        _runtimeRespawnAction.performed -= OnRespawnPerformed;
        _runtimeRespawnAction.Disable();
    }

    // 스테이지 클리어 시 R키 재시작 대기 모드로 전환합니다.
    private void OnStageClear(StageClearEvent _)
    {
        if (_waitingForRestart) return;
        _waitingForRestart = true;
        _runtimeRestartAction.performed += OnRestartPerformed;
        _runtimeRestartAction.Enable();
    }

    private void OnRestartPerformed(InputAction.CallbackContext _)
    {
        if (!_waitingForRestart) return;
        _waitingForRestart = false;
        _runtimeRestartAction.performed -= OnRestartPerformed;
        _runtimeRestartAction.Disable();
        GameManager.Instance?.RestartStage();
    }

    private void Update()
    {
        if (playerStateMachine == null) return;
        if (_waitingForRespawn) { _moveInput = Vector2.zero; return; }

        InputAction action = GetMoveAction();
        _moveInput = action != null ? action.ReadValue<Vector2>() : Vector2.zero;

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

    // J키 - 슬롯 0 (기본 공격)
    private void OnAttackPerformed(InputAction.CallbackContext _)
    {
        skillController?.TryUse(0);
    }

    // K키 - 슬롯 1 (스킬)
    private void OnSkillPerformed(InputAction.CallbackContext _)
    {
        skillController?.TryUse(1);
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        if (playerMover == null) return;
        bool groundedNow = playerMover.IsGrounded || playerMover.CheckGroundedImmediate();
        if (!groundedNow && !playerMover.CanJump) return;

        playerStateMachine?.OnJumpInput();
        playerMover.Jump();
    }

    private void EnsureActions()
    {
        if (!HasUsableBindings(moveAction.action))
            _runtimeMoveAction = CreateDefaultMoveAction();

        if (!HasUsableBindings(attackAction.action))
            _runtimeAttackAction = CreateDefaultAttackAction();

        if (!HasUsableBindings(skillAction.action))
            _runtimeSkillAction = CreateDefaultSkillAction();

        if (!HasUsableBindings(jumpAction.action))
            _runtimeJumpAction = CreateDefaultJumpAction();
    }

    private InputAction GetMoveAction() =>
        HasUsableBindings(moveAction.action) ? moveAction.action : _runtimeMoveAction;

    private InputAction GetAttackAction() =>
        HasUsableBindings(attackAction.action) ? attackAction.action : _runtimeAttackAction;

    private InputAction GetSkillAction() =>
        HasUsableBindings(skillAction.action) ? skillAction.action : _runtimeSkillAction;

    private InputAction GetJumpAction() =>
        HasUsableBindings(jumpAction.action) ? jumpAction.action : _runtimeJumpAction;

    private static bool HasUsableBindings(InputAction action) =>
        action != null && action.bindings.Count > 0;

    private static InputAction CreateDefaultMoveAction()
    {
        var action = new InputAction(name: "Move", type: InputActionType.Value);
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        action.AddBinding("<Gamepad>/leftStick");
        return action;
    }

    private static InputAction CreateDefaultAttackAction()
    {
        var action = new InputAction(name: "Attack", type: InputActionType.Button);
        action.AddBinding("<Keyboard>/j");
        action.AddBinding("<Mouse>/leftButton");
        action.AddBinding("<Gamepad>/buttonSouth");
        return action;
    }

    private static InputAction CreateDefaultSkillAction()
    {
        var action = new InputAction(name: "Skill", type: InputActionType.Button);
        action.AddBinding("<Keyboard>/k");
        action.AddBinding("<Gamepad>/buttonWest");
        return action;
    }

    private static InputAction CreateDefaultJumpAction()
    {
        var action = new InputAction(name: "Jump", type: InputActionType.Button);
        action.AddBinding("<Keyboard>/space");
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
