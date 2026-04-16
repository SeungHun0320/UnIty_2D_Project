using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 할로우 나이트 스타일 마스크(체력) UI.
/// 슬롯 생성·체력 상태 관리만 담당하며, 시각 상태는 MaskSlot에 위임합니다.
/// </summary>
public class MaskUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Full 루프 애니메이션 설정 (스프라이트, 회전, 주기 등)")]
    [SerializeField] private MaskFullConfig fullConfig;
    [Tooltip("Hit 시퀀스 애니메이션 설정 (스프라이트, 스케일 등)")]
    [SerializeField] private MaskHitConfig  hitConfig;
    [SerializeField] private Sprite         emptySprite;

    [Header("속도")]
    [SerializeField] private float fullSpeed = 1f;
    [SerializeField] private float hitSpeed  = 1f;

    [Header("슬롯 크기")]
    [SerializeField] private int   maxMasks           = 5;
    [SerializeField] private bool  useFixedSlotSize   = true;
    [SerializeField] private float slotWidth          = 55f;
    [SerializeField] private float slotHeight         = 70f;
    [SerializeField] private bool  preserveSpriteAspect = true;

    private readonly List<MaskSlot> _slots = new List<MaskSlot>();
    private int _currentHealth;
    private int _maxHealth;

    // ── 유니티 이벤트 ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_maxHealth   <= 0) _maxHealth   = Mathf.Max(1, maxMasks);
        if (_currentHealth <= 0) _currentHealth = _maxHealth;
        EnsureSlotCount();
        ApplySizing();
    }

    private void Start()
    {
        RefreshAllSlots();
    }

    private void OnValidate()
    {
        slotWidth  = Mathf.Max(1f, slotWidth);
        slotHeight = Mathf.Max(1f, slotHeight);
        fullSpeed  = Mathf.Max(0.01f, fullSpeed);
        hitSpeed   = Mathf.Max(0.01f, hitSpeed);
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    public void SetMaxHealth(int maxHealth)
    {
        _maxHealth     = Mathf.Max(1, maxHealth);
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        EnsureSlotCount();
        RefreshAllSlots();
    }

    public void SetHealth(int currentHealth, int previousHealth = -1)
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        bool tookDamage = previousHealth >= 0 && currentHealth < previousHealth;

        _currentHealth = currentHealth;

        if (tookDamage)
        {
            int hitIndex = currentHealth;   // 막 빈 슬롯 인덱스
            if (hitIndex < _slots.Count)
                _slots[hitIndex].PlayHit(hitSpeed, onDone: RefreshAllSlots);
        }

        RefreshAllSlots();
    }

    public void Damage(int amount = 1)
    {
        int prev = _currentHealth;
        SetHealth(_currentHealth - Mathf.Max(1, amount), prev);
    }

    public void Heal(int amount = 1)
    {
        int prev = _currentHealth;
        SetHealth(_currentHealth + Mathf.Max(1, amount), prev);
    }

    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth()     => _maxHealth;

    // ── 내부 로직 ─────────────────────────────────────────────────────────────

    private void EnsureSlotCount()
    {
        while (_slots.Count < _maxHealth)
        {
            int idx = _slots.Count;

            var slotGo = new GameObject("Mask_" + idx);
            slotGo.transform.SetParent(transform, false);
            slotGo.AddComponent<RectTransform>();
            var le = slotGo.AddComponent<LayoutElement>();
            le.flexibleWidth = le.flexibleHeight = 0f;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

            var icon = iconGo.AddComponent<Image>();
            icon.sprite        = emptySprite;
            icon.preserveAspect = preserveSpriteAspect;

            var slot = slotGo.AddComponent<MaskSlot>();
            slot.Setup(icon, emptySprite, fullConfig, hitConfig);
            _slots.Add(slot);
        }

        for (int i = 0; i < _slots.Count; i++)
            _slots[i].gameObject.SetActive(i < _maxHealth);
    }

    private void RefreshAllSlots()
    {
        ApplySizing();
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i >= _maxHealth) { _slots[i].gameObject.SetActive(false); continue; }
            _slots[i].gameObject.SetActive(true);

            if (_slots[i].IsPlayingHit) continue;   // Hit 재생 중인 슬롯은 건드리지 않음

            if (i < _currentHealth)
                _slots[i].PlayFull(fullSpeed);
            else
                _slots[i].PlayEmpty();
        }
    }

    private void ApplySizing()
    {
        float w = slotWidth, h = slotHeight;

        if (!useFixedSlotSize)
        {
            w = h = 0f;
            void Check(Sprite s)
            {
                if (s == null) return;
                if (s.rect.width  > w) w = s.rect.width;
                if (s.rect.height > h) h = s.rect.height;
            }
            if (fullConfig?.sprites != null) foreach (var s in fullConfig.sprites) Check(s);
            Check(emptySprite);
            w = Mathf.Max(1f, w); h = Mathf.Max(1f, h);
        }

        foreach (var slot in _slots)
        {
            if (slot == null) 
                continue;
            var rt = slot.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(w, h);
                var layoutEl = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                layoutEl.preferredWidth  = w; layoutEl.preferredHeight = h;
                layoutEl.flexibleWidth   = 0f; layoutEl.flexibleHeight  = 0f;
            }
            var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.preserveAspect = preserveSpriteAspect;
        }
    }
}
