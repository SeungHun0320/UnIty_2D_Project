using UnityEngine;

/// <summary>
/// HUD 컨테이너. MaskUI·SoulUI·GeoUI를 보유하며 전투 여부에 따라 페이드를 처리합니다.
/// 데이터 갱신은 각 Binder(PlayerHealthMaskBinder, PlayerSoulBinder, GeoUI)가 담당합니다.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private MaskUI maskUI;
    [SerializeField] private SoulUI soulUI;
    [SerializeField] private GeoUI geoUI;

    [Header("캔버스/그룹 (페이드용)")]
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private bool fadeWhenFullHealth = false;
    [SerializeField] private float fadeSpeed = 2f;

    private bool _wantsVisible = true;

    private void Awake()
    {
        if (hudCanvasGroup == null)
            hudCanvasGroup = GetComponent<CanvasGroup>();
        if (hudCanvasGroup == null)
            hudCanvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (!fadeWhenFullHealth) return;
        bool full = maskUI != null && maskUI.GetCurrentHealth() >= maskUI.GetMaxHealth();
        _wantsVisible = !full;
        float target = _wantsVisible ? 1f : 0.3f;
        if (hudCanvasGroup != null)
            hudCanvasGroup.alpha = Mathf.MoveTowards(hudCanvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
    }

    public void Show() => _wantsVisible = true;
    public void Hide() => _wantsVisible = false;
}
