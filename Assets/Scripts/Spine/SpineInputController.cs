using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력(이동/점프/공격)을 처리하는 컨트롤러입니다.
// 간단한 중력/점프 로직을 사용하며, y=0을 바닥으로 취급합니다.
[RequireComponent(typeof(SpineAnimationDriver))]
public class SpineInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriver;
    [SerializeField] private PlayerStateMachine playerStateMachine;

    [Header("Input Actions (Input System)")]
    [SerializeField] private InputActionProperty moveAction;
    [SerializeField] private InputActionProperty attackAction;
    [SerializeField] private InputActionProperty jumpAction;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float movingThreshold = 0.05f;
    [SerializeField] private bool flipByMoveX = true;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpForce = 7f;
    [SerializeField, Min(0f)] private float gravity = 20f;

    private InputAction _runtimeMoveAction;
    private InputAction _runtimeAttackAction;
    private InputAction _runtimeJumpAction;

    private bool _isGrounded;
    private float _verticalVelocity;

    private void Awake()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        // 이 컨트롤러는 직접 transform을 움직이므로,
        // 존재하는 Rigidbody2D가 있다면 물리 시뮬레이션은 끕니다.
        var rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null)
        {
            rb2D.simulated = false;
        }

        EnsureActions();
    }

    private void OnEnable()
    {
        InputAction move = GetMoveAction();
        if (move != null)
            move.Enable();

        InputAction attack = GetAttackAction();
        if (attack != null)
        {
            attack.performed += OnAttackPerformed;
            attack.Enable();
        }

        InputAction jump = GetJumpAction();
        if (jump != null)
        {
            jump.performed += OnJumpPerformed;
            jump.Enable();
        }
    }

    private void OnDisable()
    {
        InputAction attack = GetAttackAction();
        if (attack != null)
            attack.performed -= OnAttackPerformed;

        InputAction jump = GetJumpAction();
        if (jump != null)
            jump.performed -= OnJumpPerformed;
    }

    private void Update()
    {
        if (playerStateMachine == null) return;

        Vector2 move = Vector2.zero;
        InputAction action = GetMoveAction();
        if (action != null)
            move = action.ReadValue<Vector2>();

        // y=0을 바닥으로 취급하여 점프 가능 여부를 판정합니다.
        _isGrounded = transform.position.y <= 0.0001f && _verticalVelocity <= 0f;

        // 입력 벡터를 상태머신으로 전달하여 애니메이션 상태를 관리합니다.
        playerStateMachine.OnMoveInput(move, movingThreshold);

        bool isMoving = Mathf.Abs(move.x) > movingThreshold;

        // 수평 이동 (transform 기반)
        if (isMoving)
        {
            Vector3 delta = new Vector3(move.x, 0f, 0f) * (moveSpeed * Time.deltaTime);
            transform.position += delta;
        }

        if (flipByMoveX && Mathf.Abs(move.x) > 0.001f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(move.x);
            transform.localScale = scale;
        }

        // 간단한 중력/점프 적용
        if (!_isGrounded)
        {
            _verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 pos = transform.position;
        pos.y += _verticalVelocity * Time.deltaTime;

        // y=0 아래로는 내려가지 않도록 클램프
        if (pos.y <= 0f)
        {
            pos.y = 0f;
            _verticalVelocity = 0f;
            _isGrounded = true;
        }

        transform.position = pos;
    }

    private void OnAttackPerformed(InputAction.CallbackContext _)
    {
        if (playerStateMachine == null) return;
        playerStateMachine.OnAttackInput();
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        if (!_isGrounded) return;

        // 점프 시작 시 점프 애니메이션을 재생합니다.
        if (animationDriver != null)
            animationDriver.PlayJump();

        // 기존 수직 속도를 덮어쓰고 위로 점프합니다.
        _verticalVelocity = jumpForce;
    }

    private void EnsureActions()
    {
        // 씬에 "빈 InputAction"이 직렬화된 경우(바인딩 0개) 기본 바인딩으로 폴백합니다.
        if (!HasUsableBindings(moveAction.action))
            _runtimeMoveAction = CreateDefaultMoveAction();

        if (!HasUsableBindings(attackAction.action))
            _runtimeAttackAction = CreateDefaultAttackAction();

        if (!HasUsableBindings(jumpAction.action))
            _runtimeJumpAction = CreateDefaultJumpAction();
    }

    private InputAction GetMoveAction()
    {
        return HasUsableBindings(moveAction.action) ? moveAction.action : _runtimeMoveAction;
    }

    private InputAction GetAttackAction()
    {
        return HasUsableBindings(attackAction.action) ? attackAction.action : _runtimeAttackAction;
    }

    private InputAction GetJumpAction()
    {
        return HasUsableBindings(jumpAction.action) ? jumpAction.action : _runtimeJumpAction;
    }

    private static bool HasUsableBindings(InputAction action)
    {
        if (action == null) return false;
        // InputAction.bindings는 null이 아닌 값 타입(읽기 전용 배열)이라 null 체크가 컴파일 에러를 유발할 수 있습니다.
        return action.bindings.Count > 0;
    }

    private static InputAction CreateDefaultMoveAction()
    {
        var action = new InputAction(name: "Move", type: InputActionType.Value);
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        action.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
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
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();
    }
#endif
}
