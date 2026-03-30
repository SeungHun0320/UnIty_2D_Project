using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// 할로우 나이트 스타일 마스크(체력) UI.
/// 마스크 아이콘 배열로 체력을 표시합니다.
/// - 최대 체력 = 마스크 개수
/// - 피격 시 해당 마스크가 빈칸으로 변경
/// </summary>
public class MaskUI : MonoBehaviour
{
    [Header("마스크 스프라이트")]
    [Tooltip("가득 찬 마스크 1개만 쓸 때 여기에 지정. maskFullSprites가 있으면 무시.")]
    [SerializeField] private Sprite maskFull;
    [Tooltip("체력칸 한 개마다 돌릴 스프라이트 (예: 6프레임). 시간에 따라 0→1→…→5→0 반복.")]
    [SerializeField] private Sprite[] maskFullSprites;
    [Tooltip("스프라이트 한 프레임당 시간(초). 0이면 애니 끔.")]
    [SerializeField] private float spriteFrameDuration = 0.12f;
    [Header("스프라이트 애니메이션(주기 모드)")]
    [Tooltip("켜면 스프라이트가 계속 돌지 않고, 일정 주기마다 잠깐만 재생됩니다.")]
    [SerializeField] private bool animatePeriodically = false;
    [Tooltip("몇 초마다 한 번 애니메이션을 시작할지")]
    [SerializeField] private float animationIntervalSeconds = 2f;
    [Tooltip("한 번 시작하면 몇 초 동안만 프레임이 도는지. 0이면 한 바퀴(프레임 수 * frameDuration)만큼 재생.")]
    [SerializeField] private float animationBurstSeconds = 0f;
    [Tooltip("maskFullSprites[i]마다 적용할 회전(도). X,Y,Z 순서. 아틀라스 보정용.")]
    [SerializeField] private Vector3[] maskFullRotations;
    [SerializeField] private Sprite maskEmpty;     // 빈 마스크
    [SerializeField] private Sprite maskCracked;   // 피격 순간 연출용(선택)

    [Header("마스크 이미지 (자동 할당 또는 수동)")]
    // 마스크 "아이콘" 이미지들(슬롯 자식). 슬롯 방식이면 Mask_i/Icon 의 Image가 들어감.
    [SerializeField] private List<Image> maskImages = new List<Image>();

    [Header("설정")]
    [Tooltip("최대 체력(마스크) 기본값. SetMaxHealth로 런타임 변경 가능.")]
    [SerializeField] private int maxMasks = 5;
    [Tooltip("피격 순간 cracked 스프라이트를 1프레임이라도 보여줄지")]
    [SerializeField] private bool useCrackedSprite = false;
    [Tooltip("스프라이트 원본 비율(가로/세로) 유지")]
    [SerializeField] private bool preserveSpriteAspect = true;
    [Tooltip("슬롯(칸) 크기 고정 여부. 리소스가 제각각이면 Fixed 권장.")]
    [SerializeField] private bool useFixedSlotSize = true;
    [Tooltip("슬롯(칸) 가로(px). 55x70 계열이면 55 권장.")]
    [SerializeField] private float slotWidth = 55f;
    [Tooltip("슬롯(칸) 세로(px). 55x70 계열이면 70 권장.")]
    [SerializeField] private float slotHeight = 70f;

    [Header("디버그 (플레이 중에만 동작)")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("F3: 데미지, F4: 회복, F5: 최대+1")]
    [SerializeField] private int debugStep = 1;

    private int _currentHealth;
    private int _maxHealth;
    private int _lastDamagedMaskIndex = -1;
    private float _spriteCycleTime;  // 6프레임 돌리기용
    private float _burstRemaining;
    private Coroutine _spriteAnimationRoutine;
    private DebugKeyHandler _debugKeyHandler;

    private void Awake()
    {
        // 자동 수집: 슬롯(자식 Mask_*) 안의 아이콘 Image만 모읍니다.
        CollectMaskIcons();

        if (_maxHealth <= 0) _maxHealth = Mathf.Max(1, maxMasks);
        if (_currentHealth <= 0) _currentHealth = _maxHealth;

        EnsureMaskCount();
        ApplySizing();
        RefreshAllMasks();

        StartSpriteAnimationRoutine();
        _debugKeyHandler = new DebugKeyHandler(this);
    }

    private void OnValidate()
    {
        slotWidth = Mathf.Max(1f, slotWidth);
        slotHeight = Mathf.Max(1f, slotHeight);
        animationIntervalSeconds = Mathf.Max(0.05f, animationIntervalSeconds);
        animationBurstSeconds = Mathf.Max(0f, animationBurstSeconds);
        spriteFrameDuration = Mathf.Max(0f, spriteFrameDuration);
        if (!Application.isPlaying)
        {
            CollectMaskIcons();
            ApplySizing();
        }
        else
        {
            // 에디터에서 값 바꿀 때도 즉시 반영되도록 애니메이션 루틴 재시작
            StartSpriteAnimationRoutine();
        }
    }

    private void OnDisable()
    {
        StopSpriteAnimationRoutine();
    }

    private void StartSpriteAnimationRoutine()
    {
        StopSpriteAnimationRoutine();

        if (maskFullSprites == null || maskFullSprites.Length <= 0) return;
        if (spriteFrameDuration <= 0f) return;

        // 초기 값은 시각적으로 0프레임부터 시작하는 느낌으로 맞춤
        _spriteCycleTime = 0f;
        _burstRemaining = 0f;

        // 현재 체력/스프라이트 상태 반영(사이즈 변경 없이 스프라이트만)
        RefreshAllMasks(applySizing: false);

        _spriteAnimationRoutine = StartCoroutine(
            animatePeriodically ? AnimateSpritePeriodically() : AnimateSpriteContinuously()
        );
    }

    private void StopSpriteAnimationRoutine()
    {
        if (_spriteAnimationRoutine != null)
        {
            StopCoroutine(_spriteAnimationRoutine);
            _spriteAnimationRoutine = null;
        }
    }

    private IEnumerator AnimateSpriteContinuously()
    {
        int len = maskFullSprites.Length;
        float cycleLen = spriteFrameDuration * len;
        while (true)
        {
            yield return new WaitForSeconds(spriteFrameDuration);
            _spriteCycleTime += spriteFrameDuration;
            if (_spriteCycleTime >= cycleLen)
                _spriteCycleTime -= cycleLen;

            RefreshAllMasks(applySizing: false); // 스프라이트만 돌림
        }
    }

    private IEnumerator AnimateSpritePeriodically()
    {
        int len = maskFullSprites.Length;
        float cycleLen = spriteFrameDuration * len;

        while (true)
        {
            // interval 동안은 "정지 상태(프레임 0 고정)"로 보이게 하려면 여기서 RefreshAllMasks(false) 호출을 추가해도 됩니다.
            yield return new WaitForSeconds(animationIntervalSeconds);

            float burstTime = animationBurstSeconds > 0f ? animationBurstSeconds : cycleLen;
            _burstRemaining = burstTime;
            _spriteCycleTime = 0f;
            RefreshAllMasks(applySizing: false);

            while (_burstRemaining > 0f)
            {
                float wait = Mathf.Min(spriteFrameDuration, _burstRemaining);
                yield return new WaitForSeconds(wait);
                _burstRemaining -= wait;

                _spriteCycleTime += wait;
                if (_spriteCycleTime >= cycleLen)
                    _spriteCycleTime -= cycleLen;

                RefreshAllMasks(applySizing: false);
            }

            // 끝나면 첫 프레임으로 돌아가 고정
            _spriteCycleTime = 0f;
            _burstRemaining = 0f;
            RefreshAllMasks(applySizing: false);
        }
    }

    /// <summary> 최대 체력 설정 (마스크 개수 = maxHealth) </summary>
    public void SetMaxHealth(int maxHealth)
    {
        _maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = Mathf.Clamp(_currentHealth, 0, _maxHealth);
        EnsureMaskCount();
        RefreshAllMasks();
    }

    /// <summary> 현재 체력 설정. previousHealth 전달 시(피격) cracked 연출에 사용. </summary>
    public void SetHealth(int currentHealth, int previousHealth = -1)
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);
        bool tookDamage = previousHealth >= 0 && currentHealth < previousHealth;
        if (tookDamage)
            _lastDamagedMaskIndex = currentHealth; // 방금 빈칸이 된 마스크 인덱스
        _currentHealth = currentHealth;
        RefreshAllMasks();

        if (tookDamage)
        {
            var gameHUD = GetComponentInParent<GameHUD>();
            if (gameHUD != null)
                gameHUD.PlayHitFlash(_lastDamagedMaskIndex);
        }
    }

    public int GetCurrentHealth() => _currentHealth;
    public int GetMaxHealth() => _maxHealth;

    public void Damage(int amount = 1)
    {
        amount = Mathf.Max(1, amount);
        int prev = _currentHealth;
        SetHealth(_currentHealth - amount, prev);
    }

    public void Heal(int amount = 1)
    {
        amount = Mathf.Max(1, amount);
        SetHealth(_currentHealth + amount, _currentHealth);
        _lastDamagedMaskIndex = -1;
    }

    private void Update()
    {
        _debugKeyHandler?.Tick(enableDebugKeys, debugStep);
    }

    private sealed class DebugKeyHandler
    {
        private readonly MaskUI _owner;

        public DebugKeyHandler(MaskUI owner)
        {
            _owner = owner;
        }

        public void Tick(bool enabled, int step)
        {
            if (!enabled) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                _owner.Damage(step);
#if UNITY_EDITOR
                Debug.Log($"[MaskUI] 데미지: {_owner.GetCurrentHealth()}/{_owner.GetMaxHealth()}");
#endif
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                _owner.Heal(step);
#if UNITY_EDITOR
                Debug.Log($"[MaskUI] 회복: {_owner.GetCurrentHealth()}/{_owner.GetMaxHealth()}");
#endif
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                _owner.SetMaxHealth(_owner.GetMaxHealth() + 1);
                _owner.Heal(1);
#if UNITY_EDITOR
                Debug.Log($"[MaskUI] 최대 체력 증가: {_owner.GetCurrentHealth()}/{_owner.GetMaxHealth()}");
#endif
            }
        }
    }

    private void CollectMaskIcons()
    {
        if (maskImages == null) maskImages = new List<Image>();
        maskImages.Clear();

        // 패널 직계 자식 중 Mask_*를 슬롯으로 보고, 그 자식 Icon Image를 수집
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform slot = transform.GetChild(i);
            if (!slot.name.StartsWith("Mask_")) continue;

            // 슬롯 방식: Mask_0/Icon(Image)
            Image icon = null;
            for (int c = 0; c < slot.childCount; c++)
            {
                var child = slot.GetChild(c);
                var img = child.GetComponent<Image>();
                if (img != null)
                {
                    icon = img;
                    break;
                }
            }

            // 예전 방식(직접 Image가 슬롯인 경우) fallback: 슬롯 자체에 Image가 있으면 그걸 사용
            if (icon == null)
                icon = slot.GetComponent<Image>();

            if (icon != null)
                maskImages.Add(icon);
        }
    }

    private void EnsureMaskCount()
    {
        int need = _maxHealth;
        while (maskImages.Count < need)
        {
            int idx = maskImages.Count;

            // 슬롯(컨테이너)
            var slotGo = new GameObject("Mask_" + idx);
            slotGo.transform.SetParent(transform, false);
            slotGo.AddComponent<RectTransform>();

            // LayoutGroup이 슬롯 크기를 그대로 쓰도록 고정
            var le = slotGo.AddComponent<LayoutElement>();
            le.flexibleWidth = 0f;
            le.flexibleHeight = 0f;

            // 실제 표시 아이콘(Image) - 슬롯 안에서만 preserveAspect 처리
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(slotGo.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            var iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = GetFullSpriteForIndex();
            iconImg.preserveAspect = preserveSpriteAspect;

            maskImages.Add(iconImg);
        }
        for (int i = 0; i < maskImages.Count; i++)
            maskImages[i].gameObject.SetActive(i < need);
    }

    /// <summary> 현재 애니 프레임 인덱스 (0 ~ maskFullSprites.Length-1). </summary>
    private int GetCurrentFrameIndex()
    {
        if (maskFullSprites == null || maskFullSprites.Length == 0) return 0;
        return Mathf.FloorToInt(_spriteCycleTime / Mathf.Max(0.001f, spriteFrameDuration)) % maskFullSprites.Length;
    }

    /// <summary> 지금 시점에 "가득 찬" 칸에 쓸 스프라이트. 6프레임 시간에 따라 0→1→…→5→0. </summary>
    private Sprite GetFullSpriteForIndex()
    {
        if (maskFullSprites != null && maskFullSprites.Length > 0)
        {
            int frame = GetCurrentFrameIndex();
            var s = maskFullSprites[frame];
            if (s != null) return s;
        }
        return maskFull;
    }

    /// <summary> 해당 프레임에 적용할 회전(도). X,Y,Z. maskFullRotations 있으면 사용. </summary>
    private Vector3 GetRotationForFrame(int frame)
    {
        if (maskFullRotations != null && frame >= 0 && frame < maskFullRotations.Length)
            return maskFullRotations[frame];
        return Vector3.zero;
    }

    private void ApplySizing()
    {
        float maxW = useFixedSlotSize ? slotWidth : 0f;
        float maxH = useFixedSlotSize ? slotHeight : 0f;

        // Auto 모드일 때만 스프라이트들에서 최대 rect를 계산
        if (!useFixedSlotSize)
        {
            void Consider(Sprite s)
            {
                if (s == null) return;
                var r = s.rect;
                if (r.width > maxW) maxW = r.width;
                if (r.height > maxH) maxH = r.height;
            }

            if (maskFullSprites != null)
                for (int i = 0; i < maskFullSprites.Length; i++) Consider(maskFullSprites[i]);
            Consider(maskFull);
            Consider(maskEmpty);
            Consider(maskCracked);

            maxW = Mathf.Max(1f, maxW);
            maxH = Mathf.Max(1f, maxH);
        }

        Vector2 slotSize = new Vector2(maxW, maxH);

        for (int i = 0; i < maskImages.Count; i++)
        {
            var img = maskImages[i];
            if (img == null) continue;

            // 슬롯 방식: 슬롯(부모)의 크기를 고정하고, 아이콘은 슬롯 안에서 preserveAspect
            img.preserveAspect = preserveSpriteAspect;

            var slot = img.transform.parent != null ? img.transform.parent.GetComponent<RectTransform>() : null;
            if (slot == null) slot = img.rectTransform; // 예전 방식 fallback

            if (slot != null)
            {
                slot.sizeDelta = slotSize;
                var le = slot.GetComponent<LayoutElement>();
                if (le == null) le = slot.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = maxW;
                le.preferredHeight = maxH;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;
            }
        }
    }

    private void RefreshAllMasks(bool applySizing = true)
    {
        if (applySizing) ApplySizing();
        int frame = GetCurrentFrameIndex();
        Vector2 fixedSlotSize = new Vector2(slotWidth, slotHeight);

        for (int i = 0; i < maskImages.Count; i++)
        {
            if (i >= _maxHealth) continue;
            Image img = maskImages[i];
            RectTransform rt = img.rectTransform;
            if (i < _currentHealth)
            {
                img.sprite = GetFullSpriteForIndex();
                if (rt != null)
                    rt.localEulerAngles = GetRotationForFrame(frame);
            }
            else
            {
                img.sprite = (useCrackedSprite && i == _lastDamagedMaskIndex && maskCracked) ? maskCracked : maskEmpty;
                if (rt != null)
                    rt.localEulerAngles = Vector3.zero;
            }
            img.enabled = true;

            // 회전 적용 시 레이아웃이 슬롯을 키우는 걸 막기 위해 슬롯 크기 재고정 (회전 쓸 때만)
            if (useFixedSlotSize && maskFullRotations != null && maskFullRotations.Length > 0 && rt != null && rt.parent != null)
            {
                var slot = rt.parent.GetComponent<RectTransform>();
                if (slot != null)
                {
                    slot.sizeDelta = fixedSlotSize;
                    var le = slot.GetComponent<LayoutElement>();
                    if (le != null) { le.preferredWidth = slotWidth; le.preferredHeight = slotHeight; }
                }
            }
        }
    }
}
