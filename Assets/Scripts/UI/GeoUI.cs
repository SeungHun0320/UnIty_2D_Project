using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 할로우나이트 스타일 지오(골드) UI — 렌더링 전담 View입니다.
// 데이터 갱신은 GeoRewardBinder(ViewModel)가 담당합니다.
public class GeoUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("지오 아이콘 이미지 (판넬 왼쪽).")]
    [SerializeField] private Image geoImage;
    [Tooltip("지오 수량 텍스트 (판넬 오른쪽).")]
    [SerializeField] private Text geoText;
    [Tooltip("대기 중인 지오 표시 텍스트 (아래 줄).")]
    [SerializeField] private Text pendingText;

    [Header("크기")]
    [SerializeField] [Min(16f)] private float iconSize = 36f;
    [SerializeField] [Min(12)] private int fontSize = 24;
    [SerializeField] [Min(10)] private int pendingFontSize = 18;

    [Header("표시")]
    [SerializeField] private string prefix = "";
    [SerializeField] private string pendingPrefix = "+";

    private CanvasGroup _canvasGroup;
    private bool _sizesQueued;

    private void Awake()
    {
        if (geoText == null)
            geoText = GetComponentInChildren<Text>();
        if (pendingText == null)
        {
            var t = transform.Find("PendingText");
            if (t != null) pendingText = t.GetComponent<Text>();
        }
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void Start() => QueueApplySizes();

#if UNITY_EDITOR
    private void OnValidate() => QueueApplySizes();
#endif

    // GeoRewardBinder에서 호출 — 현재+대기 Geo를 동시에 갱신합니다.
    public void SyncDisplay(int currentGeo, int pendingGeo)
    {
        if (geoText != null)
            geoText.text = prefix + currentGeo;
        if (pendingText != null)
            pendingText.text = pendingGeo > 0 ? pendingPrefix + pendingGeo : "";
    }

    // GeoRewardBinder에서 호출 — 대기 Geo 텍스트만 갱신합니다.
    public void UpdatePendingGeo(int pending)
    {
        if (pendingText != null)
            pendingText.text = pending > 0 ? pendingPrefix + pending : "";
    }

    // GeoRewardBinder에서 호출 — 표시 여부를 결정합니다.
    public void SetVisible(bool show)
    {
        if (_canvasGroup == null) return;
        _canvasGroup.alpha          = show ? 1f : 0f;
        _canvasGroup.blocksRaycasts = show;
        _canvasGroup.interactable   = show;
    }

    // iconSize, fontSize 등을 자식 UI에 적용합니다.
    public void ApplySizes()
    {
        if (geoImage != null)
        {
            var rect = geoImage.rectTransform;
            rect.sizeDelta = new Vector2(iconSize, iconSize);
            var le = geoImage.GetComponent<LayoutElement>();
            if (le != null)
            {
                le.preferredWidth  = iconSize;
                le.preferredHeight = iconSize;
            }
        }
        if (geoText != null)     geoText.fontSize     = fontSize;
        if (pendingText != null)  pendingText.fontSize  = pendingFontSize;
    }

    private void QueueApplySizes()
    {
        if (_sizesQueued) return;
        if (!gameObject.activeInHierarchy) { ApplySizes(); return; }
        _sizesQueued = true;
        StartCoroutine(ApplySizesNextFrame());
    }

    private IEnumerator ApplySizesNextFrame()
    {
        yield return null;
        _sizesQueued = false;
        ApplySizes();
    }


}
