using UnityEngine;

// 스킬 슬롯, 쿨다운, 소울 소모, Instant/Hold/TapOrHold 발동 방식을 관리합니다. (SRP)
// tapSkill + holdSkill 둘 다 세팅된 슬롯은 자동으로 TapOrHold 모드로 동작합니다.
[RequireComponent(typeof(PlayerStateMachine))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerSkillController : MonoBehaviour
{
    // Inspector에서 슬롯별 스킬을 설정합니다.
    // skill만 세팅 → Instant 또는 Hold (SkillData.activationType 따름)
    // tapSkill + holdSkill 세팅 → TapOrHold 모드 (holdThreshold로 판별)
    [System.Serializable]
    public class SlotConfig
    {
        [Tooltip("단독 스킬 (Instant 또는 Hold). TapOrHold 모드면 비워두세요.")]
        public SkillData skill;

        [Header("TapOrHold 모드 (아래 둘 다 세팅 시 활성화)")]
        [Tooltip("짧게 누를 때 실행할 스킬 (Instant 권장)")]
        public SkillData tapSkill;
        [Tooltip("길게 누를 때 실행할 스킬 (Hold 권장)")]
        public SkillData holdSkill;
        [Tooltip("탭/홀드 판별 기준 시간(초). 이 시간 이내 릴리즈면 탭, 초과하면 홀드.")]
        [Min(0.05f)] public float holdThreshold = 0.4f;
        [Tooltip("true면 키를 누르고 있는 동안 holdSkill을 반복 실행합니다.")]
        public bool repeatsWhileHeld = false;
        [Tooltip("반복 실행 간격(초). repeatsWhileHeld가 true일 때만 사용합니다.")]
        [Min(0.05f)] public float repeatInterval = 0.3f;

        public bool IsTapOrHold => tapSkill != null && holdSkill != null;
    }

    // 슬롯별 런타임 상태 (SO 오염 방지 — SlotConfig에 넣지 않습니다)
    private struct SlotState
    {
        public float cooldownTimer;
        public float holdTimer;
        public bool  isHolding;
        public bool  holdFired;    // TapOrHold: holdSkill이 이미 실행됐으면 릴리즈 시 tap 차단
        public int   repeatCount;  // 반복 실행 누적 횟수 (중복 방지용)
    }

    [SerializeField] private SlotConfig[] slots;

    [Header("Input Buffer")]
    [Tooltip("공격 중 입력을 이 시간(초) 동안 기억했다가 공격 완료 직후 자동 실행합니다.")]
    [SerializeField, Min(0f)] private float bufferWindow = 0.15f;

    // 버퍼에 저장할 입력 — 슬롯 인덱스 + 입력 시각
    private struct BufferedInput
    {
        public int   slotIndex;
        public float timestamp;
    }

    private BufferedInput?      _buffer;
    private SlotState[]         _states;
    private PlayerStateMachine  _sm;
    private PlayerStats         _stats;

    private void Awake()
    {
        _sm    = GetComponent<PlayerStateMachine>();
        _stats = GetComponent<PlayerStats>();

        int count = slots != null ? slots.Length : 0;
        _states = new SlotState[count];
    }

    private void Update()
    {
        // 버퍼 유효시간 만료 시 자동 폐기
        if (_buffer.HasValue && Time.time - _buffer.Value.timestamp > bufferWindow)
            _buffer = null;

        for (int i = 0; i < _states.Length; i++)
        {
            if (_states[i].cooldownTimer > 0f)
                _states[i].cooldownTimer -= Time.deltaTime;

            if (!_states[i].isHolding) continue;

            _states[i].holdTimer += Time.deltaTime;

            SlotConfig cfg = slots[i];

            if (cfg.IsTapOrHold)
            {
                if (!_states[i].holdFired && _states[i].holdTimer >= cfg.holdThreshold)
                {
                    // 최초 holdThreshold 도달 → holdSkill 실행
                    _states[i].holdFired = true;
                    ExecuteSkill(i, cfg.holdSkill);
                }
                else if (_states[i].holdFired && cfg.repeatsWhileHeld)
                {
                    // 반복 모드: repeatInterval마다 재실행
                    // holdTimer를 holdThreshold 기준으로 초과분을 누적해 간격을 측정합니다.
                    float elapsed = _states[i].holdTimer - cfg.holdThreshold;
                    int   fires   = Mathf.FloorToInt(elapsed / cfg.repeatInterval);
                    // 이미 실행한 횟수보다 많아졌을 때만 실행 (중복 방지)
                    if (fires > _states[i].repeatCount)
                    {
                        _states[i].repeatCount = fires;
                        ExecuteSkill(i, cfg.holdSkill);
                    }
                }
            }
            else if (cfg.skill != null && cfg.skill.activationType == SkillActivationType.Hold)
            {
                // 기존 Hold 단독 슬롯
                if (_states[i].holdTimer >= cfg.skill.holdDuration)
                {
                    _states[i].isHolding = false;
                    _states[i].holdTimer  = 0f;
                    ExecuteSkill(i, cfg.skill);
                }
            }
        }
    }

    // ── 입력 진입점 (SpineInputController에서 호출) ───────────────────────────

    // 키를 누르는 순간
    public void OnSlotPressed(int i)
    {
        if (!IsValid(i)) return;
        SlotConfig cfg = slots[i];

        if (cfg.IsTapOrHold)
        {
            if (!CanUse(i, cfg.tapSkill) && !CanUse(i, cfg.holdSkill)) return;
            _states[i].isHolding  = true;
            _states[i].holdTimer  = 0f;
            _states[i].holdFired  = false;
        }
        else if (cfg.skill != null)
        {
            if (cfg.skill.activationType == SkillActivationType.Hold)
            {
                if (!CanUse(i, cfg.skill)) return;
                _states[i].isHolding = true;
                _states[i].holdTimer  = 0f;
            }
            // Instant는 OnSlotPerformed에서 처리
        }
    }

    // 키를 떼는 순간
    public void OnSlotReleased(int i)
    {
        if (i < 0 || i >= _states.Length) return;
        SlotConfig cfg = slots[i];

        if (cfg.IsTapOrHold)
        {
            // holdSkill이 아직 실행 안 됐으면 tap 실행
            if (_states[i].isHolding && !_states[i].holdFired)
                ExecuteSkill(i, cfg.tapSkill);
        }

        _states[i].isHolding   = false;
        _states[i].holdTimer   = 0f;
        _states[i].holdFired   = false;
        _states[i].repeatCount = 0;
    }

    // Instant 슬롯 전용 — 키 performed 시 호출
    public void OnSlotPerformed(int i)
    {
        if (!IsValid(i)) return;
        SlotConfig cfg = slots[i];

        // TapOrHold / Hold 슬롯은 Pressed/Released로 처리하므로 여기선 무시
        if (cfg.IsTapOrHold) return;
        if (cfg.skill == null || cfg.skill.activationType != SkillActivationType.Instant) return;

        ExecuteSkill(i, cfg.skill);
    }

    // ── UI 연동 ───────────────────────────────────────────────────────────────

    public float GetCooldownRatio(int i)
    {
        if (!IsValid(i)) return 0f;
        SlotConfig cfg = slots[i];
        SkillData  sd  = cfg.IsTapOrHold ? cfg.tapSkill : cfg.skill;
        if (sd == null || sd.cooldown <= 0f) return 0f;
        return Mathf.Clamp01(_states[i].cooldownTimer / sd.cooldown);
    }

    public float GetHoldRatio(int i)
    {
        if (!IsValid(i)) return 0f;
        SlotConfig cfg = slots[i];
        if (cfg.IsTapOrHold)
            return Mathf.Clamp01(_states[i].holdTimer / cfg.holdThreshold);
        if (cfg.skill == null || cfg.skill.holdDuration <= 0f) return 0f;
        return Mathf.Clamp01(_states[i].holdTimer / cfg.skill.holdDuration);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private void ExecuteSkill(int i, SkillData sd)
    {
        if (sd == null) return;

        // 공격 중이면 버퍼에 저장하고 완료 후 재시도
        if (_sm.CurrentState == PlayerState.Skill)
        {
            _buffer = new BufferedInput { slotIndex = i, timestamp = Time.time };
            return;
        }

        if (!CanUse(i, sd)) return;

        _states[i].cooldownTimer = sd.cooldown;
        _stats.UseSoul(sd.soulCost);
        _sm.OnSkillInput(sd);
    }

    // 공격 완료 시 SkillState.Exit()에서 호출됩니다. 버퍼 소비를 시도합니다.
    public void OnSkillFinished()
    {
        if (!_buffer.HasValue) return;
        if (Time.time - _buffer.Value.timestamp > bufferWindow) { _buffer = null; return; }

        int buffered = _buffer.Value.slotIndex;
        _buffer = null;

        if (!IsValid(buffered)) return;
        SlotConfig cfg = slots[buffered];

        // TapOrHold 슬롯은 tap(짧게)으로 재실행합니다.
        SkillData sd = cfg.IsTapOrHold ? cfg.tapSkill : cfg.skill;
        ExecuteSkill(buffered, sd);
    }

    private bool CanUse(int i, SkillData sd)
    {
        if (sd == null)                                             return false;
        if (_sm.CurrentState == PlayerState.Dead)                  return false;
        if (_states[i].cooldownTimer > 0f)                         return false;
        if (_stats.CurrentSoul < sd.soulCost)                      return false;
        if (!sd.CanActivate(_stats))                               return false;
        return true;
    }

    private bool IsValid(int i) =>
        slots != null && i >= 0 && i < slots.Length && slots[i] != null;
}
