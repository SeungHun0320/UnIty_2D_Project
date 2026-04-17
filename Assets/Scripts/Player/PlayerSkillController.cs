using UnityEngine;

// 스킬 슬롯, 쿨다운, 소울 소모, 발동 방식(Instant/Hold)을 관리합니다. (SRP)
// SpineInputController가 OnSlotPerformed/Pressed/Released를 호출하면
// 슬롯의 activationType에 따라 즉시 실행하거나 hold 타이머를 처리합니다.
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerSkillController : MonoBehaviour
{
    // 슬롯별 상태를 하나의 구조체로 묶어 배열 인덱스 동기화 오류를 방지합니다.
    private struct SkillSlot
    {
        public SkillData data;
        public float     cooldownTimer;
        public float     holdTimer;
        public bool      holdActive;
    }

    [SerializeField] private SkillData[] slots;

    private SkillSlot[]        _slots;
    private PlayerStateMachine _sm;
    private PlayerStats        _stats;

    private void Awake()
    {
        _sm    = GetComponent<PlayerStateMachine>();
        _stats = GetComponent<PlayerStats>();

        int count = slots != null ? slots.Length : 0;
        _slots = new SkillSlot[count];
        for (int i = 0; i < count; i++)
            _slots[i].data = slots[i];
    }

    private void Update()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].cooldownTimer > 0f)
                _slots[i].cooldownTimer -= Time.deltaTime;

            // Hold 타이머 틱 — 눌리는 동안 누적, holdDuration 초과 시 발동
            if (!_slots[i].holdActive) continue;

            _slots[i].holdTimer += Time.deltaTime;

            if (_slots[i].data != null && _slots[i].holdTimer >= _slots[i].data.holdDuration)
            {
                _slots[i].holdActive = false;
                _slots[i].holdTimer  = 0f;
                ExecuteSlot(i);
            }
        }
    }

    // ── 입력 진입점 (SpineInputController에서 호출) ───────────────────────────

    // Instant 슬롯: 키를 누르는 순간 발동
    public void OnSlotPerformed(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return;
        if (_slots[slotIndex].data.activationType != SkillActivationType.Instant) return;
        ExecuteSlot(slotIndex);
    }

    // Hold 슬롯: 키를 누르기 시작할 때 타이머 시작
    public void OnSlotPressed(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return;
        if (_slots[slotIndex].data.activationType != SkillActivationType.Hold) return;
        if (!CanUse(slotIndex)) return;

        _slots[slotIndex].holdActive = true;
        _slots[slotIndex].holdTimer  = 0f;
    }

    // Hold 슬롯: 키를 떼면 차징 취소
    public void OnSlotReleased(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Length) return;
        _slots[slotIndex].holdActive = false;
        _slots[slotIndex].holdTimer  = 0f;
    }

    // ── UI 연동 ───────────────────────────────────────────────────────────────

    // 0 ~ 1 범위의 쿨다운 잔여 비율을 반환합니다.
    public float GetCooldownRatio(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return 0f;
        if (_slots[slotIndex].data.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_slots[slotIndex].cooldownTimer / _slots[slotIndex].data.cooldown);
    }

    // Hold 슬롯의 차징 진행도를 0 ~ 1로 반환합니다. (UI 피드백용)
    public float GetHoldRatio(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return 0f;
        if (_slots[slotIndex].data.holdDuration <= 0f) return 0f;
        return Mathf.Clamp01(_slots[slotIndex].holdTimer / _slots[slotIndex].data.holdDuration);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    // 슬롯 실행: 쿨다운 소모 + 소울 소모 + 상태머신에 전달
    private void ExecuteSlot(int slotIndex)
    {
        if (!CanUse(slotIndex)) return;

        _slots[slotIndex].cooldownTimer = _slots[slotIndex].data.cooldown;
        _stats.UseSoul(_slots[slotIndex].data.soulCost);
        _sm.OnSkillInput(_slots[slotIndex].data);
    }

    // 발동 가능 여부 검사: 사망 / 쿨다운 / 소울 부족 / 스킬 자체 조건
    private bool CanUse(int slotIndex)
    {
        if (_sm.CurrentState == PlayerState.Dead)                    return false;
        if (_slots[slotIndex].cooldownTimer > 0f)                    return false;
        if (_stats.CurrentSoul < _slots[slotIndex].data.soulCost)    return false;

        // 스킬 자체 조건 — HealSkillData 등이 SkillData.CanActivate()를 오버라이드합니다.
        if (!_slots[slotIndex].data.CanActivate(_stats))             return false;

        return true;
    }

    private bool IsSlotValid(int slotIndex)
    {
        return _slots != null
            && slotIndex >= 0
            && slotIndex < _slots.Length
            && _slots[slotIndex].data != null;
    }
}
