using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 할로우 나이트 스타일 HUD 통합 관리.
/// - 마스크(체력), 소울, 지오 참조
/// - 필요 시 페이드 인/아웃 (전투 중이 아닐 때 HUD 숨김 등)
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private MaskUI maskUI;
    [SerializeField] private HitFlashUI hitFlashUI;
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

    public void SetHealth(int current, int max, int previousHealth = -1)
    {
        if (maskUI == null) return;
        maskUI.SetMaxHealth(max);
        maskUI.SetHealth(current, previousHealth);
        // Hit 애니메이션은 MaskUI 내부에서 해당 슬롯 Animator가 직접 처리합니다.
    }

    public void SetSoul(int current, int max = -1)
    {
        if (soulUI == null) return;
        if (max > 0) soulUI.SetMaxSoul(max);
        soulUI.SetSoul(current);
    }

    public void SetGeo(int amount)
    {
        if (geoUI == null) return;
        geoUI.SetGeo(amount);
    }

    /// <summary> 지오 획득. 대기 텍스트에 쌓였다가 일정 시간 후 메인 지오에 반영됨. </summary>
    public void AddGeo(int amount)
    {
        if (geoUI == null) return;
        geoUI.AddGeo(amount);
    }

    public void Show() => _wantsVisible = true;
    public void Hide() => _wantsVisible = false;

    /// <summary> 외부에서 Hit 연출 직접 요청 시 MaskUI를 통해 처리합니다. </summary>
    public void PlayHitFlash(int slotIndex = 0)
    {
        maskUI?.SetHealth(maskUI.GetCurrentHealth(), slotIndex + 1);
    }
}
