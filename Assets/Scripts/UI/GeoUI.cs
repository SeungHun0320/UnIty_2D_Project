using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 할로우 나이트 스타일 지오(골드) UI. 왼쪽에 지오 아이콘, 오른쪽에 숫자 텍스트.
/// 획득 시 바로 반영하지 않고 아래 대기 텍스트에 쌓였다가 일정 시간 후 메인 지오에 반영.
/// </summary>
public class GeoUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("지오 아이콘 이미지 (판넬 왼쪽). 인스펙터에서 스프라이트 할당.")]
    [SerializeField] private Image geoImage;
    [Tooltip("지오 수량 텍스트 (판넬 오른쪽).")]
    [SerializeField] private Text geoText;
    [Tooltip("대기 중인 지오 표시 텍스트 (아래 줄). 쌓였다가 commitDelay 후 메인에 반영.")]
    [SerializeField] private Text pendingText;

    [Header("크기 (인스펙터에서 수정하면 아이콘/텍스트 크기 적용)")]
    [SerializeField] [Min(16f)] private float iconSize = 36f;
    [SerializeField] [Min(12)] private int fontSize = 24;
    [SerializeField] [Min(10)] private int pendingFontSize = 18;

    [Header("표시")]
    [SerializeField] private string prefix = "";
    [SerializeField] private int currentGeo;

    [Header("대기 반영")]
    [Tooltip("지오 획득 후 이 시간(초) 동안 추가 획득이 없으면 대기량을 메인 지오에 반영.")]
    [SerializeField] [Min(0.1f)] private float commitDelay = 1.5f;
    [Tooltip("대기 텍스트 접두사 (예: +50)")]
    [SerializeField] private string pendingPrefix = "+";

    [Header("디버그 (플레이 중에만 동작)")]
    [Tooltip("체크 시 F3/F4로 지오 획득·차감 테스트. 빌드에서는 끄는 걸 권장.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("디버그로 한 번에 증감할 지오 양")]
    [SerializeField] private int debugStep = 10;

    private CanvasGroup _canvasGroup;
    private int _pendingGeo;
    private float _pendingTimer;

    private void Awake()
    {
        if (geoText == null)
            geoText = GetComponentInChildren<Text>();
        if (pendingText == null && transform.childCount > 0)
        {
            var t = transform.Find("PendingText");
            if (t != null) pendingText = t.GetComponent<Text>();
        }
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        ApplySizes();
        RefreshPendingText();
        RefreshVisibility();
    }

    private void Update()
    {
        if (enableDebugKeys)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f3Key.wasPressedThisFrame)
                    AddGeo(debugStep);
                if (keyboard.f4Key.wasPressedThisFrame)
                    SetGeo(currentGeo - debugStep);
            }
        }

        if (_pendingGeo <= 0) return;
        _pendingTimer -= Time.deltaTime;
        if (_pendingTimer <= 0f)
            CommitPending();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplySizes();
    }
#endif

    /// <summary> iconSize, fontSize, 행 높이 등을 자식 UI에 적용. 에디터에서 크기 수정 시 호출됨. </summary>
    public void ApplySizes()
    {
        if (geoImage != null)
        {
            var rect = geoImage.rectTransform;
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            var le = geoImage.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth = iconSize;
                le.preferredHeight = iconSize;
            }
        }
        if (geoText != null)
            geoText.fontSize = fontSize;
        if (pendingText != null)
            pendingText.fontSize = pendingFontSize;
    }

    /// <summary> 지오를 대기 풀에 추가. commitDelay 후 메인 지오에 반영됨. </summary>
    public void AddGeo(int amount)
    {
        if (amount <= 0) return;
        _pendingGeo += amount;
        _pendingTimer = commitDelay;
        RefreshPendingText();
        RefreshVisibility();
    }

    /// <summary> 현재 지오를 지정값으로 설정 (세이브 로드 등). 대기량은 유지. </summary>
    public void SetGeo(int amount)
    {
        currentGeo = Mathf.Max(0, amount);
        if (geoText != null)
            geoText.text = prefix + currentGeo.ToString();
        RefreshVisibility();
    }

    private void CommitPending()
    {
        if (_pendingGeo <= 0) return;
        currentGeo += _pendingGeo;
        _pendingGeo = 0;
        if (geoText != null)
            geoText.text = prefix + currentGeo.ToString();
        RefreshPendingText();
        RefreshVisibility();
    }

    private void RefreshPendingText()
    {
        if (pendingText == null) return;
        if (_pendingGeo <= 0)
            pendingText.text = "";
        else
            pendingText.text = pendingPrefix + _pendingGeo.ToString();
    }

    /// <summary> 지오·대기량 둘 다 0이면 숨김, 하나라도 있으면 표시. </summary>
    private void RefreshVisibility()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) return;
        bool show = currentGeo > 0 || _pendingGeo > 0;
        _canvasGroup.alpha = show ? 1f : 0f;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.interactable = show;
    }

    public int GetGeo() => currentGeo;
    public int GetPendingGeo() => _pendingGeo;
}
