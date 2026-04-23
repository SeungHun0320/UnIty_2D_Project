using UnityEngine;

// 모바일 가상 버튼 입력을 받아 PlayerMover / PlayerSkillController에 위임합니다. (SRP)
// InputRouter가 플랫폼에 따라 이 컴포넌트를 활성화합니다.
// SpineInputController와 동일한 하위 API를 호출하므로 게임 로직 수정이 필요 없습니다.
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerMover))]
public class MobileInputController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationDriver animationDriverComponent;
    [SerializeField] private PlayerStateMachine    playerStateMachine;
    [SerializeField] private PlayerMover           playerMover;
    [SerializeField] private PlayerSkillController skillController;

    private Vector2 _moveInput;
    private bool    _leftPressed;
    private bool    _rightPressed;
    private bool    _waitingForRespawn;

    // ── 유니티 이벤트 ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (animationDriverComponent == null)
            animationDriverComponent = GetComponent<PlayerAnimationDriver>();
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
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);

        // 비활성화 시 이동 입력 초기화
        _leftPressed = _rightPressed = false;
        _moveInput   = Vector2.zero;
    }

    private void Update()
    {
        if (_waitingForRespawn) { _moveInput = Vector2.zero; return; }

        playerStateMachine?.OnMoveInput(_moveInput, playerMover != null ? playerMover.MovingThreshold : 0.05f);
        playerMover?.FlipByInput(_moveInput.x);
    }

    private void FixedUpdate()
    {
        playerMover?.FixedTick(_moveInput);
    }

    // ── 이동 버튼 (MobileButton Inspector에서 연결) ───────────────────────────

    public void OnLeftDown()
    {
        _leftPressed  = true;
        _moveInput.x  = -1f;
    }

    public void OnLeftUp()
    {
        _leftPressed = false;
        _moveInput.x = _rightPressed ? 1f : 0f;
    }

    public void OnRightDown()
    {
        _rightPressed = true;
        _moveInput.x  = 1f;
    }

    public void OnRightUp()
    {
        _rightPressed = false;
        _moveInput.x  = _leftPressed ? -1f : 0f;
    }

    // ── 점프 버튼 ─────────────────────────────────────────────────────────────

    public void OnJumpDown()
    {
        if (playerMover == null) return;
        bool grounded = playerMover.IsGrounded || playerMover.CheckGroundedImmediate();
        if (!grounded && !playerMover.CanJump) return;

        playerStateMachine?.OnJumpInput();
        playerMover.Jump();
    }

    // ── 스킬 버튼 (슬롯 인덱스 직접 지정) ────────────────────────────────────

    // Instant 슬롯 (공격/스킬): MobileButton.onPress에 연결
    public void OnSlot0Down() => skillController?.OnSlotPerformed(0);
    public void OnSlot1Down() => skillController?.OnSlotPerformed(1);

    // Hold 슬롯 (힐): MobileButton.onPress / onRelease에 각각 연결
    public void OnSlot2Down()   => skillController?.OnSlotPressed(2);
    public void OnSlot2Up()     => skillController?.OnSlotReleased(2);

    // ── 내부 이벤트 핸들러 ────────────────────────────────────────────────────

    private void HandleLanded()
    {
        animationDriverComponent?.NotifyLanded();
        playerStateMachine?.OnLanded();
    }

    private void HandleAttackComplete()
    {
        playerStateMachine?.OnAttackComplete();
    }

    private void OnPlayerDead(PlayerDeadEvent _)
    {
        if (_waitingForRespawn) return;
        _waitingForRespawn = true;
        // 모바일에서는 E키 대신 화면 탭으로 리스폰 — 별도 리스폰 버튼 연결 가능
    }

    // 리스폰 버튼에서 호출합니다.
    public void OnRespawn()
    {
        if (!_waitingForRespawn) return;
        GameManager.Instance?.RespawnPlayer();
    }

    private void OnPlayerRespawn(PlayerRespawnEvent _)
    {
        _waitingForRespawn = false;
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
