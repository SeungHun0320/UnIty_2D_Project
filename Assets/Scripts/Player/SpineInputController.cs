using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력을 받아 PlayerMover(물리)와 PlayerStateMachine(상태)에 위임합니다. (SRP)
// 이 클래스는 입력 처리만 담당합니다.
[RequireComponent(typeof(SpineAnimationDriver))]
[RequireComponent(typeof(PlayerMover))]
public class SpineInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriverComponent;
    [SerializeField] private PlayerStateMachine playerStateMachine;
    [SerializeField] private PlayerMover playerMover;

    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private InputActionProperty attackAction;
    [SerializeField] private InputActionProperty jumpAction;

    private IAnimationDriver animationDriver;

    private InputAction _runtimeMoveAction;
    private InputAction _runtimeAttackAction;
    private InputAction _runtimeJumpAction;

    private Vector2 _moveInput;

    private void Awake()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<SpineAnimationDriver>();
        animationDriver = animationDriverComponent;

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();

        playerMover.OnLanded += HandleLanded;
        animationDriverComponent.OnAttackComplete += HandleAttackComplete;

        EnsureActions();
    }

    private void OnDestroy()
    {
        if (playerMover != null)
            playerMover.OnLanded -= HandleLanded;
        if (animationDriverComponent != null)
            animationDriverComponent.OnAttackComplete -= HandleAttackComplete;
    }

    private void OnEnable()
    {
        InputAction move = GetMoveAction();
        if (move != null) move.Enable();

        InputAction attack = GetAttackAction();
        if (attack != null) { attack.performed += OnAttackPerformed; attack.Enable(); }

        InputAction jump = GetJumpAction();
        if (jump != null) { jump.performed += OnJumpPerformed; jump.Enable(); }

        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    private void OnDisable()
    {
        InputAction attack = GetAttackAction();
        if (attack != null) attack.performed -= OnAttackPerformed;

        InputAction jump = GetJumpAction();
        if (jump != null) jump.performed -= OnJumpPerformed;

        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
    }

    // 사망 시 이 컴포넌트를 비활성화해 모든 입력을 차단합니다.
    private void OnPlayerDead(PlayerDeadEvent _) => enabled = false;

    private void Update()
    {
        if (playerStateMachine == null) return;

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

    private void OnAttackPerformed(InputAction.CallbackContext _)
    {
        playerStateMachine?.OnAttackInput();
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        if (playerMover == null) return;
        bool groundedNow = playerMover.IsGrounded || playerMover.CheckGroundedImmediate();
        if (!groundedNow && !playerMover.CanJump) return;

        // JumpingState.Enter → PlayJump 호출
        playerStateMachine?.OnJumpInput();
        playerMover.Jump();
    }

    private void EnsureActions()
    {
        if (!HasUsableBindings(moveAction.action))
            _runtimeMoveAction = CreateDefaultMoveAction();

        if (!HasUsableBindings(attackAction.action))
            _runtimeAttackAction = CreateDefaultAttackAction();

        if (!HasUsableBindings(jumpAction.action))
            _runtimeJumpAction = CreateDefaultJumpAction();
    }

    private InputAction GetMoveAction() =>
        HasUsableBindings(moveAction.action) ? moveAction.action : _runtimeMoveAction;

    private InputAction GetAttackAction() =>
        HasUsableBindings(attackAction.action) ? attackAction.action : _runtimeAttackAction;

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
            animationDriverComponent = GetComponent<SpineAnimationDriver>();

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        if (playerMover == null)
            playerMover = GetComponent<PlayerMover>();
    }
#endif
}
