using UnityEngine;

// 스킬 슬롯, 쿨다운, 소울 소모, 발동 방식(Instant/Hold)을 관리합니다. (SRP)
// SpineInputController가 OnSlotPerformed/Pressed/Released를 호출하면
// 슬롯의 activationType에 따라 즉시 실행하거나 hold 타이머를 처리합니다.
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private SkillData[] slots;

    private float[]            _cooldownTimers;
    private float[]            _holdTimers;
    private bool[]             _holdActive;       // 해당 슬롯의 키가 현재 눌려있는지
    private PlayerStateMachine _sm;
    private PlayerStats        _stats;

    private void Awake()
    {
        _sm    = GetComponent<PlayerStateMachine>();
        _stats = GetComponent<PlayerStats>();

        int count = slots != null ? slots.Length : 0;
        _cooldownTimers = new float[count];
        _holdTimers     = new float[count];
        _holdActive     = new bool[count];
    }

    private void Update()
    {
        for (int i = 0; i < _cooldownTimers.Length; i++)
        {
            if (_cooldownTimers[i] > 0f)
                _cooldownTimers[i] -= Time.deltaTime;
        }

        // Hold 타이머 틱 — 눌리는 동안 누적, holdDuration 초과 시 발동
        for (int i = 0; i < _holdActive.Length; i++)
        {
            if (!_holdActive[i]) continue;

            _holdTimers[i] += Time.deltaTime;

            if (slots[i] != null && _holdTimers[i] >= slots[i].holdDuration)
            {
                _holdActive[i]  = false;
                _holdTimers[i]  = 0f;
                ExecuteSlot(i);
            }
        }
    }

    // ── 입력 진입점 (SpineInputController에서 호출) ───────────────────────────

    // Instant 슬롯: 키를 누르는 순간 발동
    public void OnSlotPerformed(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return;
        if (slots[slotIndex].activationType != SkillActivationType.Instant) return;
        ExecuteSlot(slotIndex);
    }

    // Hold 슬롯: 키를 누르기 시작할 때 타이머 시작
    public void OnSlotPressed(int slotIndex)
    {
        if (!IsSlotValid(slotIndex)) return;
        if (slots[slotIndex].activationType != SkillActivationType.Hold) return;
        if (!CanUse(slotIndex)) return;

        _holdActive[slotIndex] = true;
        _holdTimers[slotIndex] = 0f;
    }

    // Hold 슬롯: 키를 떼면 차징 취소
    public void OnSlotReleased(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _holdActive.Length) return;
        _holdActive[slotIndex] = false;
        _holdTimers[slotIndex] = 0f;
    }

    // ── UI 연동 ───────────────────────────────────────────────────────────────

    // 0 ~ 1 범위의 쿨다운 잔여 비율을 반환합니다.
    public float GetCooldownRatio(int slotIndex)
    {
        if (slots == null || slotIndex >= slots.Length) return 0f;
        if (slots[slotIndex] == null || slots[slotIndex].cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimers[slotIndex] / slots[slotIndex].cooldown);
    }

    // Hold 슬롯의 차징 진행도를 0 ~ 1로 반환합니다. (UI 피드백용)
    public float GetHoldRatio(int slotIndex)
    {
        if (slots == null || slotIndex >= slots.Length) return 0f;
        if (slots[slotIndex] == null || slots[slotIndex].holdDuration <= 0f) return 0f;
        return Mathf.Clamp01(_holdTimers[slotIndex] / slots[slotIndex].holdDuration);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    // 슬롯 실행: 쿨다운 소모 + 소울 소모 + 상태머신에 전달
    private void ExecuteSlot(int slotIndex)
    {
        if (!CanUse(slotIndex)) return;

        _cooldownTimers[slotIndex] = slots[slotIndex].cooldown;
        _stats.UseSoul(slots[slotIndex].soulCost);
        _sm.OnSkillInput(slots[slotIndex]);
    }

    // 발동 가능 여부 검사: 사망 / 쿨다운 / 소울 부족 / 최대 체력(힐 한정)
    private bool CanUse(int slotIndex)
    {
        if (_sm.CurrentState == PlayerState.Dead)          return false;
        if (_cooldownTimers[slotIndex] > 0f)               return false;
        if (_stats.CurrentSoul < slots[slotIndex].soulCost) return false;

        // 힐 스킬: 이미 체력이 최대면 소모 없이 차단
        if (slots[slotIndex] is HealSkillData && _stats.CurrentHealth >= _stats.MaxHealth)
            return false;

        return true;
    }

    private bool IsSlotValid(int slotIndex)
    {
        return slots != null
            && slotIndex >= 0
            && slotIndex < slots.Length
            && slots[slotIndex] != null;
    }
}
