using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class SoulUI : MonoBehaviour
{
    // 파도/셰이더 기반 작업 정리: 기본 게이지(세로 높이)만 사용합니다.
    // 규칙: 현재 소울량에 따라 세로 높이로 표시합니다.
    // 규칙: 원형 클리핑은 셰이더로 처리합니다.
    [Header("References")]
    [SerializeField] private Image vesselImage;
    [SerializeField] private Image fillAnimImage;
    [SerializeField] private RectTransform fillViewport;
    [SerializeField] private Image fillMaskShapeImage;
    [SerializeField] private bool preserveFillAspect = false;

    [Header("Sprites")]
    [SerializeField] private Sprite vesselSprite;
    [SerializeField] private Sprite fullFillSprite;
    [Tooltip("non-full 상태에서 표시할 Fill 스프라이트(일단 기존 scrollSprite 사용).")]
    [SerializeField] private Sprite scrollSprite;
    // 하위 호환용(에디터 툴/기존 씬 직렬화 깨짐 방지). 런타임에서는 사용하지 않습니다.
    [HideInInspector, SerializeField] private Sprite chargingLoopSprite;
    [HideInInspector, SerializeField] private Sprite[] chargingFillFrames;

    [Header("Soul Value")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;

    [Header("Gauge Layout")]
    [Tooltip("0이면 자동 계산, 0보다 크면 이 값(px)을 최대 높이로 사용합니다.")]
    [SerializeField, Min(0f)] private float fullViewportHeightOverride = 0f;
    [Tooltip("true면 fillAnimImage를 현재 높이에 맞춰 원본 비율로 같이 스케일합니다.")]
    [SerializeField] private bool scaleFillImageByViewportHeight = false;
    [Tooltip("true면 FillMaskShape(FullMask)의 알파를 사용해 FlowAnim을 마스킹합니다.")]
    [SerializeField] private bool useAlphaMask = true;
    [Tooltip("true면 위치/앵커를 코드가 자동으로 맞춥니다. false면 사용자가 씬에서 조정한 위치를 유지합니다.")]
    [SerializeField] private bool autoLayout = false;
    [Tooltip("true면 셰이더에서 원형으로 클리핑합니다.")]
    [SerializeField] private bool useShaderCircleClip = true;
    [SerializeField] private Shader circleClipShader;
    [SerializeField] private Vector2 circleCenterUv = new Vector2(0.5f, 0.5f);
    [SerializeField, Range(0f, 1f)] private float circleRadiusUv = 0.5f;
    [SerializeField, Range(0f, 0.1f)] private float circleEdgeSoftness = 0.01f;

    private bool _maskLayoutInitialized;
    private bool _fillLayoutInitialized;
    private Material _runtimeCircleMat;

    private static readonly int CircleCenterId = Shader.PropertyToID("_CircleCenter");
    private static readonly int CircleRadiusId = Shader.PropertyToID("_CircleRadius");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int FillId = Shader.PropertyToID("_Fill");
    private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int MaskUVRectId = Shader.PropertyToID("_MaskUVRect");

    [Header("디버그 (플레이 중에만 동작)")]
    [Tooltip("플레이 중 F1/F2로 소울을 증감합니다.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("F1/F2 한 번에 증감할 소울 양")]
    [SerializeField] private int debugStep = 10;

    private float _fullViewportHeight = -1f;
    private DebugKeyHandler _debugKeyHandler;
    private string _lastSpriteBindLog;
    private string _lastShaderStateLog;

    private void Awake()
    {
        if (!HasRequiredReferences())
            TryAutoResolveReferences();

        if (!HasRequiredReferences())
        {
            enabled = false;
            return;
        }

        maxSoul = Mathf.Max(1, maxSoul);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);

        EnsureViewportMask();
        EnsureShaderCircleClip();
        // 셰이더 원형 클리핑을 쓰면 Mask 기반 클리핑은 끕니다(FullSoul이 계속 보이는 문제 방지)
        if (useShaderCircleClip)
            useAlphaMask = false;
        EnsureAlphaMask();
        DisableMaskShapeRenderer();
        ApplyStaticSprites();
        EnsureViewportAnchors();
        CacheFullViewportHeight();
        Refresh(immediate: true);

        _debugKeyHandler = new DebugKeyHandler(this);
    }

    private void Update()
    {
        _debugKeyHandler?.Tick(enableDebugKeys, debugStep);
    }

    private IEnumerator Start()
    {
        // UI 레이아웃이 안정화된 다음 최대 높이를 다시 캐시합니다.
        yield return null;
        CacheFullViewportHeight();
        Refresh(immediate: true);
    }

    private bool HasRequiredReferences()
    {
        return fillAnimImage != null && fillViewport != null;
    }

    private void TryAutoResolveReferences()
    {
        if (fillViewport == null)
        {
            Transform viewportTf = transform.Find("FillViewport");
            if (viewportTf != null)
                fillViewport = viewportTf as RectTransform;
            if (fillViewport == null)
                fillViewport = GetComponentInChildren<RectTransform>(true);
        }

        if (fillAnimImage == null)
        {
            Transform flowTf = transform.Find("FillViewport/FillMaskShape/FlowAnim");
            if (flowTf != null)
                fillAnimImage = flowTf.GetComponent<Image>();
            if (fillAnimImage == null)
                fillAnimImage = GetComponentInChildren<Image>(true);
        }

        if (fillMaskShapeImage == null)
        {
            Transform maskTf = transform.Find("FillViewport/FillMaskShape");
            if (maskTf != null)
                fillMaskShapeImage = maskTf.GetComponent<Image>();
        }
    }

    private void EnsureViewportMask()
    {
        // 규칙: (옵션 B) FillViewport를 조절하지 않고 fillAnimImage 자체 높이를 조절합니다.
        // 따라서 FillViewport의 RectMask2D에 의존하지 않습니다.
    }

    private void EnsureShaderCircleClip()
    {
        if (!useShaderCircleClip) return;
        if (fillAnimImage == null) return;

        if (_runtimeCircleMat == null)
        {
            if (circleClipShader == null)
                circleClipShader = Shader.Find("UI/SoulCircleClip");
            if (circleClipShader == null) return;
            _runtimeCircleMat = new Material(circleClipShader);
        }

        // full 상태는 원본 그대로(사용자 요구) -> non-full에서만 머티리얼 적용
        if (currentSoul < maxSoul && fillAnimImage.material != _runtimeCircleMat)
            fillAnimImage.material = _runtimeCircleMat;

        _runtimeCircleMat.SetVector(CircleCenterId, new Vector4(circleCenterUv.x, circleCenterUv.y, 0f, 0f));
        _runtimeCircleMat.SetFloat(CircleRadiusId, circleRadiusUv);
        _runtimeCircleMat.SetFloat(EdgeSoftnessId, circleEdgeSoftness);
        _runtimeCircleMat.SetFloat(FillId, 1f);

        if (fullFillSprite != null && fullFillSprite.texture != null)
        {
            Texture tex = fullFillSprite.texture;
            Rect r = fullFillSprite.textureRect;
            Vector4 uvRect = new Vector4(
                r.x / tex.width,
                r.y / tex.height,
                r.width / tex.width,
                r.height / tex.height
            );
            _runtimeCircleMat.SetTexture(MaskTexId, tex);
            _runtimeCircleMat.SetVector(MaskUVRectId, uvRect);
        }

        LogShaderState("EnsureShaderCircleClip");
    }

    private void EnsureAlphaMask()
    {
        // 규칙: FullMask(원형)로 바깥쪽을 클리핑합니다.
        if (!useAlphaMask) return;

        if (fillMaskShapeImage == null)
        {
            Transform maskTf = transform.Find("FillViewport/FillMaskShape");
            if (maskTf != null)
                fillMaskShapeImage = maskTf.GetComponent<Image>();
        }

        if (fillMaskShapeImage == null) return;

        // Mask는 Image.enabled가 켜져 있어야 동작합니다.
        fillMaskShapeImage.enabled = true;

        // 원형 클리핑을 위해 FullSoul(원형) 스프라이트를 마스크 이미지로 사용합니다.
        if (fullFillSprite != null && fillMaskShapeImage.sprite != fullFillSprite)
            fillMaskShapeImage.sprite = fullFillSprite;

        Mask mask = fillMaskShapeImage.GetComponent<Mask>();
        if (mask == null)
            mask = fillMaskShapeImage.gameObject.AddComponent<Mask>();

        mask.showMaskGraphic = false;

        // 사용자 배치 우선: MaskShape의 RectTransform 크기/위치는 코드에서 변경하지 않습니다.
    }

    private void DisableMaskShapeRenderer()
    {
        // 규칙: FillMaskShape에 FullSoul 스프라이트가 렌더링되면 0에서도 "풀처럼" 보일 수 있으므로 렌더링을 끕니다.
        if (useAlphaMask)
        {
            // 마스킹 모드에서는 Mask가 동작해야 하므로 Image는 켜고(showMaskGraphic=false) 화면에는 안 보이게 합니다.
            return;
        }

        if (fillMaskShapeImage == null)
        {
            Transform maskTf = transform.Find("FillViewport/FillMaskShape");
            if (maskTf != null)
                fillMaskShapeImage = maskTf.GetComponent<Image>();
        }

        if (fillMaskShapeImage == null) return;
        if (fillMaskShapeImage.enabled)
            fillMaskShapeImage.enabled = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxSoul = Mathf.Max(1, maxSoul);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);

        // 기본 게이지 모드에서는 에디터에서 강제 갱신/머티리얼 바인딩을 하지 않습니다.
        // (RectTransform/SendMessage 경고 및 상태 꼬임 방지)
        CacheFullViewportHeight();
        if (!Application.isPlaying)
        {
            TryAutoResolveReferences();
            EnsureAlphaMask();
            DisableMaskShapeRenderer();
            ApplyStaticSprites();
        }
    }
#endif

    private void ApplyStaticSprites()
    {
        // 요구사항: VesselImage는 코드에서 건드리지 않고 수동 세팅을 유지합니다.
        if (fillAnimImage != null)
        {
            fillAnimImage.type = Image.Type.Simple;
            fillAnimImage.preserveAspect = preserveFillAspect;
        }
    }

    private void EnsureViewportAnchors()
    {
        if (fillViewport == null) return;
        // 사용자 배치 우선: FillViewport의 앵커/오프셋은 코드에서 변경하지 않습니다.
    }

    private void CacheFullViewportHeight()
    {
        if (fillViewport == null) return;

        if (fullViewportHeightOverride > 0.001f)
        {
            _fullViewportHeight = fullViewportHeightOverride;
            return;
        }

        float h = 0f;

        // 1) 보통 의도한 최대 높이가 여기에 들어있습니다(예: (0, SoulGaugeSize)).
        if (fillViewport.sizeDelta.y > 1f)
            h = fillViewport.sizeDelta.y;

        // 2) 레이아웃 완료 후에는 rect.height가 의미 있는 값이 됩니다.
        if (h <= 1f && fillViewport.rect.height > 1f)
            h = fillViewport.rect.height;

        // 3) 그래도 안 되면 부모 패널 높이를 사용합니다.
        if (h <= 1f)
        {
            var parentRt = fillViewport.parent as RectTransform;
            if (parentRt != null && parentRt.rect.height > 1f)
                h = parentRt.rect.height;
        }

        if (h > 1f)
            _fullViewportHeight = h;
    }

    public void SetSoul(int soul)
    {
        bool wasFull = currentSoul >= maxSoul;
        currentSoul = Mathf.Clamp(soul, 0, maxSoul);
        bool isFull = currentSoul >= maxSoul;

        // full 경계 진입/이탈 시에는 즉시 전환을 강제해
        // FullSoul <-> ChargingLoop 스프라이트가 한 프레임도 지연되지 않게 합니다.
        Refresh(immediate: wasFull != isFull);
    }

    public void SetMaxSoul(int max)
    {
        bool wasFull = currentSoul >= maxSoul;
        maxSoul = Mathf.Max(1, max);
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
        bool isFull = currentSoul >= maxSoul;
        Refresh(immediate: wasFull != isFull);
    }

    public int GetSoul() => currentSoul;
    public int GetMaxSoul() => maxSoul;

    private void Refresh(bool immediate)
    {
        EnsureShaderCircleClip();
        EnsureAlphaMask();
        DisableMaskShapeRenderer();
        ApplyViewportHeight();

        UpdateVisualState(immediate);
    }

    private void ApplyViewportHeight()
    {
        if (fillViewport == null || fillAnimImage == null) return;

        EnsureViewportAnchors();

        if (_fullViewportHeight <= 0.001f)
            CacheFullViewportHeight();

        if (_fullViewportHeight <= 0.001f)
            return;

        float ratio = Mathf.Clamp01((float)currentSoul / maxSoul);
        ApplyFillImageHeight(ratio);
    }

    // RectTransform 크기 변경 대신 Image.fillAmount만 사용합니다.
    private void ApplyFillImageHeight(float fillRatio)
    {
        if (fillAnimImage == null) return;

        fillAnimImage.type = Image.Type.Filled;
        fillAnimImage.fillMethod = Image.FillMethod.Vertical;
        fillAnimImage.fillOrigin = (int)Image.OriginVertical.Bottom;
        fillAnimImage.fillAmount = Mathf.Clamp01(fillRatio);
    }

    private void ApplyFillImageSizeByHeight(float targetHeight)
    {
        if (fillAnimImage == null) return;
        if (targetHeight <= 0.001f) return;

        Sprite refSprite = scrollSprite != null ? scrollSprite : fillAnimImage.sprite;
        if (refSprite == null) return;

        float spriteW = refSprite.rect.width;
        float spriteH = refSprite.rect.height;
        if (spriteH <= 0.001f) return;

        float aspect = spriteW / spriteH;

        RectTransform rt = fillAnimImage.rectTransform;
        if (autoLayout && !_fillLayoutInitialized)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            _fillLayoutInitialized = true;
        }

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetHeight * aspect);
    }

    private void UpdateVisualState(bool immediate)
    {
        if (fillAnimImage == null) return;

        bool isEmpty = currentSoul <= 0;
        bool isFull = currentSoul >= maxSoul;

        if (isEmpty)
        {
            fillAnimImage.enabled = false;
            return;
        }

        fillAnimImage.enabled = true;

        if (isFull)
        {
            if (fullFillSprite != null)
            {
                fillAnimImage.sprite = fullFillSprite;
                LogSpriteBinding("full/fullFillSprite", fullFillSprite);
            }
            // full은 원본 그대로(요구사항). 셰이더 머티리얼 제거.
            if (useShaderCircleClip && fillAnimImage.material != null)
                fillAnimImage.material = null;
            LogShaderState("UpdateVisualState/full");
            return;
        }

        // non-full: scrollSprite를 Fill 이미지로 사용합니다.
        if (scrollSprite != null && fillAnimImage.sprite != scrollSprite)
        {
            fillAnimImage.sprite = scrollSprite;
            LogSpriteBinding("charging/scrollSprite", scrollSprite);
        }

        if (useShaderCircleClip)
            EnsureShaderCircleClip();

        fillAnimImage.preserveAspect = preserveFillAspect;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogSpriteBinding(string source, Sprite sprite)
    {
        string name = sprite != null ? sprite.name : "<null>";
        string tex = (sprite != null && sprite.texture != null) ? sprite.texture.name : "<null>";
        int w = sprite != null ? Mathf.RoundToInt(sprite.rect.width) : 0;
        int h = sprite != null ? Mathf.RoundToInt(sprite.rect.height) : 0;
        string msg = $"[SoulUI] Sprite bind source={source}, sprite={name}, tex={tex}, rect={w}x{h}";
        if (_lastSpriteBindLog == msg) return;
        _lastSpriteBindLog = msg;
        Debug.Log(msg);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogShaderState(string source)
    {
        if (!Application.isPlaying) return;
        if (fillAnimImage == null) return;

        bool matApplied = _runtimeCircleMat != null && fillAnimImage.material == _runtimeCircleMat;
        string shaderName = _runtimeCircleMat != null && _runtimeCircleMat.shader != null ? _runtimeCircleMat.shader.name : "<null>";
        string maskTexName = "<null>";
        Vector4 maskUvRect = Vector4.zero;

        if (_runtimeCircleMat != null)
        {
            if (_runtimeCircleMat.HasProperty(MaskTexId))
            {
                Texture t = _runtimeCircleMat.GetTexture(MaskTexId);
                maskTexName = t != null ? t.name : "<null>";
            }
            if (_runtimeCircleMat.HasProperty(MaskUVRectId))
                maskUvRect = _runtimeCircleMat.GetVector(MaskUVRectId);
        }

        string msg =
            $"[SoulUI] ShaderState src={source}, applied={matApplied}, shader={shaderName}, maskTex={maskTexName}, maskUV=({maskUvRect.x:F3},{maskUvRect.y:F3},{maskUvRect.z:F3},{maskUvRect.w:F3})";

        if (_lastShaderStateLog == msg) return;
        _lastShaderStateLog = msg;
        Debug.Log(msg);
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