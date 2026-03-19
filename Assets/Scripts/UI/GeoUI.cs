using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 할로우 나이트 스타일 지오(골드) UI. 왼쪽에 지오 아이콘, 오른쪽에 숫자 텍스트.
/// 인스펙터에서 아이콘/폰트 크기 조절 시 레이아웃에 자동 반영됨.
/// </summary>
public class GeoUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("지오 아이콘 이미지 (판넬 왼쪽). 인스펙터에서 스프라이트 할당.")]
    [SerializeField] private Image geoImage;
    [Tooltip("지오 수량 텍스트 (판넬 오른쪽).")]
    [SerializeField] private Text geoText;

    [Header("크기 (인스펙터에서 수정하면 아이콘/텍스트 크기 적용)")]
    [SerializeField] [Min(16f)] private float iconSize = 36f;
    [SerializeField] [Min(12)] private int fontSize = 24;

    [Header("표시")]
    [SerializeField] private string prefix = "";
    [SerializeField] private int currentGeo;

    [Header("디버그 (플레이 중에만 동작)")]
    [Tooltip("체크 시 F3/F4로 지오 획득·차감 테스트. 빌드에서는 끄는 걸 권장.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("디버그로 한 번에 증감할 지오 양")]
    [SerializeField] private int debugStep = 10;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (geoText == null)
            geoText = GetComponentInChildren<Text>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        ApplySizes();
        RefreshVisibility();
    }

    private void Update()
    {
        if (!enableDebugKeys) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f3Key.wasPressedThisFrame)
        {
            SetGeo(currentGeo + debugStep);
#if UNITY_EDITOR
            Debug.Log($"[GeoUI] 지오 획득: {currentGeo}");
#endif
        }
        if (keyboard.f4Key.wasPressedThisFrame)
        {
            SetGeo(currentGeo - debugStep);
#if UNITY_EDITOR
            Debug.Log($"[GeoUI] 지오 차감: {currentGeo}");
#endif
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (geoImage != null || geoText != null)
            ApplySizes();
    }
#endif

    /// <summary> iconSize, fontSize 값을 자식 UI에 적용. 에디터에서 크기 수정 시 호출됨. </summary>
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
    }

    public void SetGeo(int amount)
    {
        currentGeo = Mathf.Max(0, amount);
        if (geoText != null)
            geoText.text = prefix + currentGeo.ToString();
        RefreshVisibility();
    }

    /// <summary> 지오 0이면 보이지 않게만 하고, 1 이상이면 표시. (오브젝트는 항상 활성이라 F3/SetGeo 동작함) </summary>
    private void RefreshVisibility()
    {
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) return;
        bool show = currentGeo > 0;
        _canvasGroup.alpha = show ? 1f : 0f;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.interactable = show;
    }

    public int GetGeo() => currentGeo;
}
