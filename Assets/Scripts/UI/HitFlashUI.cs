using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 피격 시 한 번만 재생되는 Hit 플래시/연출 UI.
/// Mask와 별도로 두고, 고정 슬롯 크기 + Preserve Aspect로 프레임 크기가 제각각이어도 맞춤.
/// </summary>
public class HitFlashUI : MonoBehaviour
{
    [Header("Hit 스프라이트")]
    [Tooltip("Hit 연출 프레임들 (순서대로 한 번 재생). 크기가 달라도 고정 슬롯 안에서 맞춤.")]
    [SerializeField] private Sprite[] hitSprites;
    [Tooltip("한 프레임당 표시 시간(초).")]
    [SerializeField] private float frameDuration = 0.06f;

    [Header("슬롯 크기 (프레임 크기 제각각일 때 고정 권장)")]
    [Tooltip("표시 영역 가로(px).")]
    [SerializeField] private float slotWidth = 80f;
    [Tooltip("표시 영역 세로(px).")]
    [SerializeField] private float slotHeight = 80f;
    [Tooltip("마스크 슬롯 대비 위치 보정(px). 살짝 어긋나면 X,Y로 조정.")]
    [SerializeField] private Vector2 positionOffset = Vector2.zero;
    [Tooltip("스프라이트 비율 유지")]
    [SerializeField] private bool preserveAspect = true;

    [Header("참조 (비어 있으면 자동 생성)")]
    [Tooltip("Hit 이미지를 그릴 Image. 없으면 자식에서 찾거나 Icon 자동 생성.")]
    [SerializeField] private Image hitImage;

    private RectTransform _slotRect;
    private bool _isPlaying;

    private void Awake()
    {
        EnsureReferences();
        if (hitImage != null)
        {
            hitImage.enabled = false;
            hitImage.preserveAspect = preserveAspect;
        }
        ApplySlotSize();
    }

    private void OnValidate()
    {
        slotWidth = Mathf.Max(1f, slotWidth);
        slotHeight = Mathf.Max(1f, slotHeight);
        frameDuration = Mathf.Max(0.001f, frameDuration);
    }

    private void EnsureReferences()
    {
        if (hitImage != null) return;

        Transform icon = transform.Find("Icon");
        if (icon != null)
        {
            hitImage = icon.GetComponent<Image>();
            if (hitImage != null) return;
        }

        var img = GetComponent<Image>();
        if (img != null)
        {
            hitImage = img;
            return;
        }

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(transform, false);
        var rt = iconGo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        hitImage = iconGo.AddComponent<Image>();
        hitImage.preserveAspect = preserveAspect;
        hitImage.color = Color.white;
    }

    private void ApplySlotSize()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;
        _slotRect = rt;
        if (rt.anchorMin != rt.anchorMax)
            return;
        rt.sizeDelta = new Vector2(slotWidth, slotHeight);
    }

    /// <summary> Hit 연출 한 번 재생. slotIndex = 방금 빈칸이 된 마스크 슬롯(그 위치에 연출). </summary>
    public void PlayHit(int slotIndex = 0)
    {
        if (_isPlaying || hitSprites == null || hitSprites.Length == 0)
            return;

        PositionToSlot(slotIndex);
        transform.SetAsLastSibling();

        StopAllCoroutines();
        StartCoroutine(PlayHitCoroutine());
    }

    private void PositionToSlot(int slotIndex)
    {
        Transform parent = transform.parent;
        if (parent == null) return;
        if (slotIndex < 0 || slotIndex >= parent.childCount) return;

        Transform slot = parent.GetChild(slotIndex);
        if (slot == transform) return;

        var slotRect = slot as RectTransform;
        var myRect = GetComponent<RectTransform>();
        if (slotRect == null || myRect == null) return;

        myRect.anchorMin = slotRect.anchorMin;
        myRect.anchorMax = slotRect.anchorMax;
        myRect.pivot = slotRect.pivot;
        myRect.anchoredPosition = slotRect.anchoredPosition + positionOffset;
        // 인스펙터에서 slotWidth/slotHeight 키우면 그 크기로 적용 (0 이하면 슬롯 크기 그대로)
        if (slotWidth > 0f && slotHeight > 0f)
            myRect.sizeDelta = new Vector2(slotWidth, slotHeight);
        else
            myRect.sizeDelta = slotRect.sizeDelta;
    }

    private IEnumerator PlayHitCoroutine()
    {
        _isPlaying = true;
        if (hitImage != null)
        {
            hitImage.enabled = true;
            hitImage.sprite = hitSprites[0];
        }

        for (int i = 0; i < hitSprites.Length; i++)
        {
            if (hitImage != null && hitSprites[i] != null)
                hitImage.sprite = hitSprites[i];
            yield return new WaitForSeconds(frameDuration);
        }

        if (hitImage != null)
            hitImage.enabled = false;
        _isPlaying = false;
    }

    /// <summary> 재생 중이면 true. </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary> 슬롯 크기 설정 (런타임). </summary>
    public void SetSlotSize(float width, float height)
    {
        slotWidth = Mathf.Max(1f, width);
        slotHeight = Mathf.Max(1f, height);
        ApplySlotSize();
    }
}
