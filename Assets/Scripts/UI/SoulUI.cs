using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SoulUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image vesselImage;
    [SerializeField] private Image fillAnimImage;
    [SerializeField] private RectTransform fillViewport;

    [Header("Sprites")]
    [SerializeField] private Sprite vesselSprite;
    [SerializeField] private Sprite fullFillSprite;
    [SerializeField] private Sprite[] chargingFillFrames;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float chargingFrameDuration = 0.06f;
    [SerializeField] private bool animateWhenCharging = true;

    [Header("Soul Value")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;

    [Header("디버그 (플레이 중에만 동작)")]
    [Tooltip("플레이 중 F1/F2로 소울을 증감합니다.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("F1/F2 한 번에 증감할 소울 양")]
    [SerializeField] private int debugStep = 10;

    private Coroutine _animRoutine;
    private int _frameIndex;
    private float _fullViewportHeight = -1f;

    private DebugKeyHandler _debugKeyHandler;

    private void Awake()
    {
        maxSoul = Mathf.Max(1, maxSoul);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);

        ApplyStaticSprites();
        CacheFullViewportHeight();
        Refresh(immediate: true);

        _debugKeyHandler = new DebugKeyHandler(this);
    }

    private void Update()
    {
        _debugKeyHandler?.Tick(enableDebugKeys, debugStep);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxSoul = Mathf.Max(1, maxSoul);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);

        ApplyStaticSprites();

        if (!Application.isPlaying)
        {
            CacheFullViewportHeight();
            Refresh(immediate: true);
        }
    }
#endif

    private void OnDisable()
    {
        StopChargingAnimation();
    }

    private void ApplyStaticSprites()
    {
        if (vesselImage != null && vesselSprite != null)
            vesselImage.sprite = vesselSprite;

        if (fillAnimImage != null)
            fillAnimImage.type = Image.Type.Simple;
    }

    private void CacheFullViewportHeight()
    {
        if (fillViewport == null) return;

        float h = fillViewport.rect.height;
        if (h <= 0.001f)
            h = fillViewport.sizeDelta.y;

        if (h > 0.001f)
            _fullViewportHeight = h;
    }

    public void SetSoul(int soul)
    {
        currentSoul = Mathf.Clamp(soul, 0, maxSoul);
        Refresh(immediate: false);
    }

    public void SetMaxSoul(int max)
    {
        maxSoul = Mathf.Max(1, max);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
        Refresh(immediate: false);
    }

    public int GetSoul() => currentSoul;
    public int GetMaxSoul() => maxSoul;

    private void Refresh(bool immediate)
    {
        ApplyViewportHeight();
        UpdateVisualState(immediate);
    }

    private void ApplyViewportHeight()
    {
        if (fillViewport == null) return;

        if (_fullViewportHeight <= 0.001f)
            CacheFullViewportHeight();

        if (_fullViewportHeight <= 0.001f)
            return;

        float ratio = Mathf.Clamp01((float)currentSoul / maxSoul);

        Vector2 size = fillViewport.sizeDelta;
        size.y = _fullViewportHeight * ratio;
        fillViewport.sizeDelta = size;
    }

    private void UpdateVisualState(bool immediate)
    {
        if (fillAnimImage == null)
            return;

        bool isEmpty = currentSoul <= 0;
        bool isFull = currentSoul >= maxSoul;
        bool canAnimate = animateWhenCharging &&
                          chargingFillFrames != null &&
                          chargingFillFrames.Length > 0;

        if (isEmpty)
        {
            StopChargingAnimation();

            if (canAnimate && chargingFillFrames[0] != null)
                fillAnimImage.sprite = chargingFillFrames[0];
            else if (fullFillSprite != null)
                fillAnimImage.sprite = fullFillSprite;

            return;
        }

        if (isFull)
        {
            StopChargingAnimation();

            if (fullFillSprite != null)
                fillAnimImage.sprite = fullFillSprite;

            return;
        }

        // charging state
        if (canAnimate)
        {
            if (immediate)
            {
                _frameIndex = 0;
                if (chargingFillFrames[0] != null)
                    fillAnimImage.sprite = chargingFillFrames[0];
            }

            if (_animRoutine == null)
                _animRoutine = StartCoroutine(ChargingAnimationLoop());
        }
        else
        {
            StopChargingAnimation();

            if (fullFillSprite != null)
                fillAnimImage.sprite = fullFillSprite;
        }
    }

    private IEnumerator ChargingAnimationLoop()
    {
        _frameIndex = 0;

        if (chargingFillFrames != null &&
            chargingFillFrames.Length > 0 &&
            chargingFillFrames[0] != null)
        {
            fillAnimImage.sprite = chargingFillFrames[0];
        }

        while (true)
        {
            if (currentSoul <= 0 || currentSoul >= maxSoul)
            {
                _animRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(chargingFrameDuration);

            if (chargingFillFrames == null || chargingFillFrames.Length == 0)
                continue;

            _frameIndex = (_frameIndex + 1) % chargingFillFrames.Length;

            Sprite next = chargingFillFrames[_frameIndex];
            if (next != null)
                fillAnimImage.sprite = next;
        }
    }

    private void StopChargingAnimation()
    {
        if (_animRoutine != null)
        {
            StopCoroutine(_animRoutine);
            _animRoutine = null;
        }
    }

    private sealed class DebugKeyHandler
    {
        private readonly SoulUI _owner;

        public DebugKeyHandler(SoulUI owner) => _owner = owner;

        public void Tick(bool enabled, int step)
        {
            if (!enabled) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                _owner.SetSoul(_owner.GetSoul() + step);
#if UNITY_EDITOR
                Debug.Log($"[SoulUI] F1 +{step} => {_owner.GetSoul()}/{_owner.GetMaxSoul()}");
#endif
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                _owner.SetSoul(_owner.GetSoul() - step);
#if UNITY_EDITOR
                Debug.Log($"[SoulUI] F2 -{step} => {_owner.GetSoul()}/{_owner.GetMaxSoul()}");
#endif
            }
        }
    }
}