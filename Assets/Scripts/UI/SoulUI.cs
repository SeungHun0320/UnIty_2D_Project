using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// 할로우 나이트 스타일 소울(기력) UI.
/// - vesselImage: 비어 있는 용기/프레임 (동그라미 + 흰 뿔 장식 등). 항상 전체 표시.
/// - fillImage: 꽉 찬 소울 이미지 (하얀 소울이 가득 찬 동그라미).
///   Image Type = Filled 로 두고, fillAmount 만큼만 보이게 해서 "마스크"처럼 사용.
///   세로 채움 시 동그라미 안에 하얀 소울이 아래에서 위로 차오르는 느낌.
/// </summary>
public class SoulUI : MonoBehaviour
{
    [Header("용기 & 채움 이미지")]
    [Tooltip("비어 있는 용기/프레임 (동그라미 테두리 + 장식). 여기에 스프라이트 넣으면 vesselImage에 적용됨.")]
    [SerializeField] private Sprite vesselSprite;
    [Tooltip("용기 이미지가 붙어 있는 UI Image (자식 VesselImage). 스프라이트는 위 vesselSprite 또는 여기서 직접 넣기.")]
    [SerializeField] private Image vesselImage;
    [Tooltip("꽉 찬 소울 스프라이트. 여기에 넣으면 fillImage에 적용됨.")]
    [SerializeField] private Sprite fillSprite;
    [Tooltip("채움 이미지가 붙어 있는 UI Image (자식 FillImage). Filled로 fillAmount만큼만 보임.")]
    [SerializeField] private Image fillImage;
    [Tooltip("채움 방향. Vertical Bottom = 동그라미 안에 세로로 아래→위 채움.")]
    [SerializeField] private FillDirection fillDirection = FillDirection.VerticalBottom;
    [Tooltip("fillImage에 스프라이트가 없을 때 쓸 기본 이미지. None이면 자동 할당 안 함.")]
    [SerializeField] private DefaultFillSprite defaultFillSprite = DefaultFillSprite.WaveFill;

    [Header("값")]
    [SerializeField] private int maxSoul = 99;
    [SerializeField] private int currentSoul = 0;

    [Header("디버그 (플레이 중에만 동작)")]
    [Tooltip("체크 시 F1/F2로 소울 증감 테스트. 빌드에서는 끄는 걸 권장.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("디버그로 한 번에 증감할 양")]
    [SerializeField] private int debugStep = 10;

    public enum FillDirection
    {
        VerticalBottom,  // 아래에서 위로 (소울 차오름)
        VerticalTop,
        HorizontalLeft,
        HorizontalRight
    }

    public enum DefaultFillSprite
    {
        None,       // 사용 안 함 (스프라이트 없으면 안 보일 수 있음)
        White,      // 흰색 단색
        WaveFill    // 물결 모양 상단 채움
    }

    private void OnValidate()
    {
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
        ApplySpritesToImages();
    }

    private void Awake()
    {
        ApplySpritesToImages();
        EnsureFillSprite();
        ApplyFillSettings();
    }

    private void Update()
    {
        if (!enableDebugKeys) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            SetSoul(currentSoul - debugStep);
#if UNITY_EDITOR
            Debug.Log($"[SoulUI] 소울 감소: {currentSoul}/{maxSoul}");
#endif
        }
        if (keyboard.f2Key.wasPressedThisFrame)
        {
            SetSoul(currentSoul + debugStep);
#if UNITY_EDITOR
            Debug.Log($"[SoulUI] 소울 증가: {currentSoul}/{maxSoul}");
#endif
        }
    }

    /// <summary> SoulUI 인스펙터에 넣은 vesselSprite, fillSprite를 각 Image에 적용. </summary>
    private void ApplySpritesToImages()
    {
        if (vesselImage != null && vesselSprite != null)
            vesselImage.sprite = vesselSprite;
        if (fillImage != null && fillSprite != null)
            fillImage.sprite = fillSprite;
    }

    private void EnsureFillSprite()
    {
        if (fillImage == null || defaultFillSprite == DefaultFillSprite.None) return;
        if (fillImage.sprite != null) return;

        fillImage.sprite = defaultFillSprite == DefaultFillSprite.WaveFill
            ? UIDefaultSprites.WaveFill
            : UIDefaultSprites.White;
        fillImage.color = new Color(1f, 1f, 1f, 0.95f);
    }

    public void SetMaxSoul(int max)
    {
        maxSoul = Mathf.Max(1, max);
        Refresh();
    }

    public void SetSoul(int current)
    {
        currentSoul = Mathf.Clamp(current, 0, maxSoul);
        Refresh();
    }

    public int GetSoul() => currentSoul;
    public int GetMaxSoul() => maxSoul;

    private void ApplyFillSettings()
    {
        if (fillImage == null) return;

        fillImage.type = Image.Type.Filled;

        switch (fillDirection)
        {
            case FillDirection.VerticalBottom:
                fillImage.fillMethod = Image.FillMethod.Vertical;
                fillImage.fillOrigin = (int)Image.OriginVertical.Bottom;
                break;
            case FillDirection.VerticalTop:
                fillImage.fillMethod = Image.FillMethod.Vertical;
                fillImage.fillOrigin = (int)Image.OriginVertical.Top;
                break;
            case FillDirection.HorizontalLeft:
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
                break;
            case FillDirection.HorizontalRight:
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = (int)Image.OriginHorizontal.Right;
                break;
        }
    }

    private void Refresh()
    {
        if (fillImage == null) return;

        ApplyFillSettings();
        fillImage.fillAmount = (float)currentSoul / Mathf.Max(1, maxSoul);
    }
}
