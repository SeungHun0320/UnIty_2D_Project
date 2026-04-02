using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 입력(이동/점프/공격)을 처리하는 컨트롤러입니다.
// 한국어: 타일맵 콜라이더와 안정적으로 충돌하도록 Rigidbody2D 기반으로 이동합니다.
// 한국어: 바닥 판정은 콜라이더 캐스트로 처리해 지터/침투에 강하게 합니다.
[RequireComponent(typeof(SpineAnimationDriver))]
public class SpineInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriver;
    [SerializeField] private PlayerStateMachine playerStateMachine;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D bodyCollider;

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
    [SerializeField, Min(0f)] private float gravity = 3f; // 한국어: Rigidbody2D.gravityScale로 사용(기존 직렬화 유지)

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0; // 한국어: 충돌(바닥)로 취급할 레이어
    [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.08f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.08f; // 한국어: 바닥에서 살짝 떨어져도 점프 허용

    private InputAction _runtimeMoveAction;
    private InputAction _runtimeAttackAction;
    private InputAction _runtimeJumpAction;

    private bool _isGrounded;
    private Vector2 _moveInput;
    private float _lastGroundedTime;

    private void Awake()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        if (playerStateMachine == null)
            playerStateMachine = GetComponent<PlayerStateMachine>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();

        // 한국어: 플레이어에 Collider2D가 없으면 충돌 자체가 불가능하므로 기본 콜라이더를 자동 생성합니다.
        if (bodyCollider == null)
        {
            var capsule = GetComponent<CapsuleCollider2D>();
            if (capsule == null)
                capsule = gameObject.AddComponent<CapsuleCollider2D>();

            capsule.isTrigger = false;
            capsule.direction = CapsuleDirection2D.Vertical;
            bodyCollider = capsule;
        }

        EnsureActions();

        // 한국어: 물리 이동 전제 기본값(프로젝트 설정을 해치지 않는 선)
        if (rb != null)
        {
            rb.gravityScale = gravity;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // 한국어: 고중력/고속 낙하 시 바닥 관통(터널링) 방지
        }
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

        _moveInput = move;

        // 입력 벡터를 상태머신으로 전달하여 애니메이션 상태를 관리합니다.
        playerStateMachine.OnMoveInput(move, movingThreshold);

        if (flipByMoveX && Mathf.Abs(move.x) > 0.001f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(move.x);
            transform.localScale = scale;
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        _isGrounded = IsGroundedByCast();
        if (_isGrounded)
            _lastGroundedTime = Time.time;

        Vector2 v = rb.linearVelocity;
        float targetX = Mathf.Abs(_moveInput.x) > movingThreshold ? (_moveInput.x * moveSpeed) : 0f;
        v.x = targetX;
        rb.linearVelocity = v;
    }

    private bool IsGroundedByCast()
    {
        // 한국어: 콜라이더 외곽에서 아래로 살짝 캐스트하여 TilemapCollider2D에서도 안정적으로 접지 판정
        if (bodyCollider == null)
            return false;

        Bounds b = bodyCollider.bounds;
        Vector2 origin = b.center;
        Vector2 size = new Vector2(b.size.x * 0.92f, b.size.y);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundProbeDistance, groundLayers);
        return hit.collider != null && hit.collider != bodyCollider;
    }

    private void OnAttackPerformed(InputAction.CallbackContext _)
    {
        if (playerStateMachine == null) return;
        playerStateMachine.OnAttackInput();
    }

    private void OnJumpPerformed(InputAction.CallbackContext _)
    {
        // 한국어: 입력 이벤트가 FixedUpdate보다 먼저 들어오면 _isGrounded가 갱신되기 전일 수 있어 즉시 재확인합니다.
        bool groundedNow = _isGrounded || IsGroundedByCast();
        bool withinCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
        if (!groundedNow && !withinCoyote) return;

        // 점프 시작 시 점프 애니메이션을 재생합니다.
        if (animationDriver != null)
            animationDriver.PlayJump();

        // 한국어: 물리 기반 점프(수직 속도만 교체)
        if (rb != null)
        {
            Vector2 v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;
        }
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

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (bodyCollider == null)
            bodyCollider = GetComponent<Collider2D>();
    }
#endif
}
