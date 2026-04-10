using UnityEngine;

// 스킬 슬롯과 쿨다운을 관리합니다. (SRP)
// 슬롯에 SkillData SO를 할당하면 코드 수정 없이 스킬 구성을 바꿀 수 있습니다.
[RequireComponent(typeof(PlayerStateMachine))]
public class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private SkillData[] slots;

    private float[] _cooldownTimers;
    private PlayerStateMachine _sm;

    private void Awake()
    {
        _sm = GetComponent<PlayerStateMachine>();
        _cooldownTimers = new float[slots != null ? slots.Length : 0];
    }

    private void Update()
    {
        for (int i = 0; i < _cooldownTimers.Length; i++)
        {
            if (_cooldownTimers[i] > 0f)
                _cooldownTimers[i] -= Time.deltaTime;
        }
    }

    // 슬롯 사용을 시도합니다. 쿨다운 중이거나 슬롯이 비어있으면 false를 반환합니다.
    public bool TryUse(int slotIndex)
    {
        if (slots == null || slotIndex >= slots.Length) return false;
        if (slots[slotIndex] == null) return false;
        if (_cooldownTimers[slotIndex] > 0f) return false;

        _cooldownTimers[slotIndex] = slots[slotIndex].cooldown;
        _sm.OnSkillInput(slots[slotIndex]);
        return true;
    }

    // UI 연동용: 0 ~ 1 범위의 쿨다운 잔여 비율을 반환합니다.
    public float GetCooldownRatio(int slotIndex)
    {
        if (slots == null || slotIndex >= slots.Length) return 0f;
        if (slots[slotIndex] == null || slots[slotIndex].cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_cooldownTimers[slotIndex] / slots[slotIndex].cooldown);
    }
}
