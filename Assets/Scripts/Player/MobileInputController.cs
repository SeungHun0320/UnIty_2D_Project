using UnityEngine;

// 모바일 가상 버튼 입력을 받아 PlayerMover / PlayerSkillController에 위임합니다. (SRP)
// InputRouter가 플랫폼에 따라 이 컴포넌트를 활성화합니다.
public class MobileInputController : InputControllerBase
{
    private bool _leftPressed;
    private bool _rightPressed;
    private bool _waitingForRespawn;

    // ── 유니티 이벤트 ─────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
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

        _leftPressed = _rightPressed = false;
        _moveInput   = Vector2.zero;
    }

    private void Update()
    {
        if (_waitingForRespawn || IsInputBlocked()) { _moveInput = Vector2.zero; return; }
        ApplyMoveInput();
    }

    // ── 이동 버튼 (MobileButton Inspector에서 연결) ───────────────────────────

    public void OnLeftDown()
    {
        _leftPressed = true;
        _moveInput.x = -1f;
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
    public void OnSlot0Down() => skillController?.OnSlotPerformed(PlayerSkillController.SlotAttack);
    public void OnSlot1Down() => skillController?.OnSlotPerformed(PlayerSkillController.SlotSpecial);

    // Hold 슬롯 (힐): MobileButton.onPress / onRelease에 각각 연결
    public void OnSlot2Down() => skillController?.OnSlotPressed(PlayerSkillController.SlotDash);
    public void OnSlot2Up()   => skillController?.OnSlotReleased(PlayerSkillController.SlotDash);

    // ── 내부 이벤트 핸들러 ────────────────────────────────────────────────────

    private void OnPlayerDead(PlayerDeadEvent _)
    {
        if (_waitingForRespawn) return;
        _waitingForRespawn = true;
    }

    private void OnPlayerRespawn(PlayerRespawnEvent _) => _waitingForRespawn = false;

    // ── 외부 호출 ─────────────────────────────────────────────────────────────

    // 리스폰 버튼에서 호출합니다.
    public void OnRespawn()
    {
        if (!_waitingForRespawn) return;
        GameInstance.Instance?.RespawnPlayer();
    }

    // 일시정지 버튼에서 호출합니다.
    public void OnPauseClicked() => GameInstance.Instance?.TogglePause();

    // InputRouter가 모바일 환경에서 호출합니다.
    public void WireButtons(Transform mobileUIRoot)
    {
        var btnMap = new System.Collections.Generic.Dictionary<string, MobileButton>();
        foreach (var btn in mobileUIRoot.GetComponentsInChildren<MobileButton>(true))
            btnMap[btn.name] = btn;

        Wire("BtnLeft",   OnLeftDown,  OnLeftUp);
        Wire("BtnRight",  OnRightDown, OnRightUp);
        Wire("BtnJump",   OnJumpDown,  null);
        Wire("BtnAttack", OnSlot0Down, null);
        Wire("BtnSkill",  OnSlot1Down, null);
        Wire("BtnHeal",   OnSlot2Down, OnSlot2Up);

        void Wire(string btnName, UnityEngine.Events.UnityAction press, UnityEngine.Events.UnityAction release)
        {
            if (!btnMap.TryGetValue(btnName, out var btn))
            { Debug.LogWarning($"[MobileInputController] {btnName} 버튼을 찾지 못했습니다."); return; }
            if (press   != null) btn.onPress.AddListener(press);
            if (release != null) btn.onRelease.AddListener(release);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate() => base.OnValidate();
#endif
}
