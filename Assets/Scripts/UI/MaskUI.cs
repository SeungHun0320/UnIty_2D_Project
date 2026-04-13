using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 할로우 나이트 스타일 마스크(체력) UI.
/// 각 Mask_* 슬롯의 Animator가 스프라이트/회전/피격 애니메이션을 처리합니다.
/// SetupMaskAnimator 에디터 스크립트로 Animator를 먼저 생성해야 합니다.
/// </summary>
public class MaskUI : MonoBehaviour
{
    [Header("Animator (SetupMaskAnimator로 생성 후 여기에 드래그)")]
    [Tooltip("MaskSlot.controller — 새 슬롯 생성 시 자동으로 할당됩니다.")]
    [SerializeField] private RuntimeAnimatorController maskAnimatorController;

    [Header("마스크 스프라이트 (SetupMaskAnimator 클립 생성용 데이터)")]
    [Tooltip("체력칸 한 개마다 돌릴 스프라이트. SetupMaskAnimator가 읽어서 MaskFull 클립을 생성합니다.")]
    [SerializeField] private Sprite[] maskFullSprites;
    [SerializeField] private Sprite maskEmpty;

    [Header("마스크 이미지 (자동 할당 또는 수동)")]
    [SerializeField] private List<Image> maskImages = new List<Image>();

    [Header("스프라이트 애니메이션 주기")]
    [Tooltip("켜면 한 사이클 재생 후 일정 시간 대기했다가 다시 재생합니다.")]
    [SerializeField] private bool animatePeriodically = false;
    [Tooltip("주기 모드에서 다음 재생까지 대기 시간(초).")]
    [SerializeField] private float animationIntervalSeconds = 2f;
    [Tooltip("슬롯 Animator 재생 속도 (1 = 정상, 0.5 = 절반 속도). Full/Hit/Empty 모두 적용됩니다.")]
    [SerializeField] private float animatorSpeed = 1f;

    [Header("설정")]
    [SerializeField] private int maxMasks = 5;
    [SerializeField] private bool preserveSpriteAspect = true;
    [SerializeField] private bool useFixedSlotSize = true;
    [SerializeField] private float slotWidth  = 55f;
    [SerializeField] private float slotHeight = 70f;

    private int _currentHealth;
    private int _maxHealth;
    private int _lastDamagedMaskIndex = -1;

    // 각 Mask_* 슬롯의 Animator 캐시
    private List<Animator> _slotAnimators = new List<Animator>();

    private void Awake()
    {
        CollectMaskIcons();

        if (_maxHealth <= 0) _maxHealth = Mathf.Max(1, maxMasks);
        if (_currentHealth <= 0) _currentHealth = _maxHealth;

        EnsureMaskCount();
        ApplySizing();
    }

    private IEnumerator Start()
    {
        // Animator 초기화 대기 후 상태 적용
        yield return null;
        ApplyAnimatorSpeed();
        RefreshAllMasks();
        if (animatePeriodically)
            StartCoroutine(AnimateSlotsPeriodically());
    }

    private void OnValidate()
    {
        slotWidth  = Mathf.Max(1f, slotWidth);
        slotHeight = Mathf.Max(1f, slotHeight);
        animatorSpeed = Mathf.Max(0.01f, animatorSpeed);
        if (!Application.isPlaying)
        {
            CollectMaskIcons();
            ApplySizing();
        }
        else
        {
            // 플레이 중 Inspector에서 값을 바꾸면 즉시 반영
            ApplyAnimatorSpeed();
        }
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    public void SetMaxHealth(int maxHealth)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        EnsureMaskCount();
        RefreshAllMasks();
    }

    public void SetHealth(int currentHealth, int previousHealth = -1)
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        bool tookDamage = previousHealth >= 0 && currentHealth < previousHealth;

        if (tookDamage)
        {
            _lastDamagedMaskIndex = currentHealth;
            // Full 상태일 때 먼저 Hit 트리거를 쏩니다.
            // animatePeriodically가 speed=0으로 멈춰놨을 수 있으므로 먼저 속도를 복원합니다.
            var hitAnim = GetSlotAnimator(_lastDamagedMaskIndex);
            if (hitAnim != null)
            {
                hitAnim.speed = animatorSpeed;
                hitAnim.SetTrigger("Hit");
            }
        }

        _currentHealth = currentHealth;
        RefreshAllMasks();
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

    private void CollectMaskIcons()
    {
        if (maskImages == null) maskImages = new List<Image>();
        maskImages.Clear();
        _slotAnimators.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slot = transform.GetChild(i);
            if (!slot.name.StartsWith("Mask_")) continue;

            Image icon = null;
            for (int c = 0; c < slot.childCount; c++)
            {
                var img = slot.GetChild(c).GetComponent<Image>();
                if (img != null) { icon = img; break; }
            }
            if (icon == null) icon = slot.GetComponent<Image>();
            if (icon == null) continue;

            maskImages.Add(icon);
            _slotAnimators.Add(slot.GetComponent<Animator>());
        }
    }

    private void EnsureMaskCount()
    {
        int need = _maxHealth;
        while (maskImages.Count < need)
        {
            int idx = maskImages.Count;

            var slotGo = new GameObject("Mask_" + idx);
            slotGo.transform.SetParent(transform, false);
            slotGo.AddComponent<RectTransform>();
            var le = slotGo.AddComponent<LayoutElement>();
            le.flexibleWidth = le.flexibleHeight = 0f;

            var iconGo  = new GameObject("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt  = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = maskEmpty;
            iconImg.preserveAspect = preserveSpriteAspect;

            maskImages.Add(iconImg);

            // Controller가 있으면 새 슬롯에도 즉시 Animator를 붙입니다.
            if (maskAnimatorController != null)
            {
                var anim = slotGo.AddComponent<Animator>();
                anim.runtimeAnimatorController = maskAnimatorController;
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;
                _slotAnimators.Add(anim);
            }
            else
            {
                _slotAnimators.Add(null);
            }
        }

        for (int i = 0; i < maskImages.Count; i++)
            maskImages[i].gameObject.SetActive(i < need);
    }

    private void RefreshAllMasks()
    {
        ApplySizing();
        int damagedThisCall = _lastDamagedMaskIndex;
        _lastDamagedMaskIndex = -1; // 이번 호출에서만 skip 조건 적용

        for (int i = 0; i < maskImages.Count; i++)
        {
            if (i >= _maxHealth)
            {
                maskImages[i].transform.parent.gameObject.SetActive(false);
                continue;
            }

            maskImages[i].transform.parent.gameObject.SetActive(true);
            Animator anim = GetSlotAnimator(i);

            if (anim != null)
            {
                // Animator가 있으면 상태 전환 (스프라이트/회전은 클립이 처리)
                if (i < _currentHealth)
                {
                    var info = anim.GetCurrentAnimatorStateInfo(0);
                    if (!info.IsName("Full") && !info.IsName("Hit"))
                    {
                        anim.ResetTrigger("Hit"); // 힐 시 남은 Hit 트리거 제거
                        anim.Play("Full", 0, 0f);
                    }
                }
                else
                {
                    var hitInfo = anim.GetCurrentAnimatorStateInfo(0);
                    bool hitPending = (i == damagedThisCall);  // 같은 프레임, 트리거 미처리
                    bool hitPlaying = hitInfo.IsName("Hit");   // 이미 Hit 클립 재생 중
                    if (!hitPending && !hitPlaying)
                        anim.Play("Empty", 0, 0f);
                }

                maskImages[i].enabled = true;
            }
            else
            {
                // Animator 미설정 폴백: 코드로 스프라이트 직접 설정
                maskImages[i].sprite = i >= _currentHealth ? maskEmpty : null;
                maskImages[i].enabled = true;
            }
        }
    }

    private void ApplySizing()
    {
        float maxW = useFixedSlotSize ? slotWidth  : 0f;
        float maxH = useFixedSlotSize ? slotHeight : 0f;

        if (!useFixedSlotSize)
        {
            void Consider(Sprite s) { if (s == null) return; if (s.rect.width > maxW) maxW = s.rect.width; if (s.rect.height > maxH) maxH = s.rect.height; }
            if (maskFullSprites != null) foreach (var s in maskFullSprites) Consider(s);
            Consider(maskEmpty);
            maxW = Mathf.Max(1f, maxW); maxH = Mathf.Max(1f, maxH);
        }

        var slotSize = new Vector2(maxW, maxH);
        foreach (var img in maskImages)
        {
            if (img == null) continue;
            img.preserveAspect = preserveSpriteAspect;
            var slot = img.transform.parent?.GetComponent<RectTransform>() ?? img.rectTransform;
            if (slot != null)
            {
                slot.sizeDelta = slotSize;
                var layoutEl = slot.GetComponent<LayoutElement>() ?? slot.gameObject.AddComponent<LayoutElement>();
                layoutEl.preferredWidth = maxW; layoutEl.preferredHeight = maxH;
                layoutEl.flexibleWidth  = 0f;   layoutEl.flexibleHeight  = 0f;
            }
        }
    }

    // 한 사이클 재생 후 intervalSeconds 대기를 반복하는 주기적 애니메이션입니다.
    private IEnumerator AnimateSlotsPeriodically()
    {
        // 클립 길이를 Animator에서 직접 읽습니다 (spriteFrameDuration 불필요)
        var refAnim = GetSlotAnimator(0);
        float cycleTime = refAnim != null
            ? refAnim.GetCurrentAnimatorStateInfo(0).length / Mathf.Max(animatorSpeed, 0.01f)
            : 1f;
        while (true)
        {
            // 전체 Full 슬롯 애니메이션 재시작
            for (int i = 0; i < _currentHealth && i < _maxHealth; i++)
                GetSlotAnimator(i)?.Play("Full", 0, 0f);

            // 한 사이클 재생
            yield return new WaitForSeconds(cycleTime);

            // 첫 프레임에서 정지
            for (int i = 0; i < _currentHealth && i < _maxHealth; i++)
            {
                var anim = GetSlotAnimator(i);
                if (anim == null) continue;
                anim.Play("Full", 0, 0f);
                anim.speed = 0f;
            }

            // 대기
            yield return new WaitForSeconds(animationIntervalSeconds);

            // 재개 (animatorSpeed 기준으로 복원)
            for (int i = 0; i < _currentHealth && i < _maxHealth; i++)
            {
                var anim = GetSlotAnimator(i);
                if (anim != null) anim.speed = animatorSpeed;
            }
        }
    }

    // 모든 슬롯 Animator에 animatorSpeed 적용
    private void ApplyAnimatorSpeed()
    {
        for (int i = 0; i < maskImages.Count; i++)
        {
            var anim = GetSlotAnimator(i);
            if (anim != null) anim.speed = animatorSpeed;
        }
    }

    // 슬롯 인덱스로 유효한 Animator 반환 (Controller 없는 Animator는 null 처리)
    private Animator GetSlotAnimator(int index)
    {
        if (index < 0 || index >= maskImages.Count) return null;

        if (_slotAnimators.Count > index && _slotAnimators[index] != null)
        {
            var cached = _slotAnimators[index];
            return cached.runtimeAnimatorController != null ? cached : null;
        }

        var img  = maskImages[index];
        if (img == null) return null;
        var slot = (img.transform.parent != null && img.transform.parent != transform)
                   ? img.transform.parent : img.transform;
        var anim = slot.GetComponent<Animator>();

        // Controller 없는 Animator는 무효 처리
        if (anim != null && anim.runtimeAnimatorController == null) anim = null;
        if (_slotAnimators.Count > index) _slotAnimators[index] = anim;
        return anim;
    }
}
