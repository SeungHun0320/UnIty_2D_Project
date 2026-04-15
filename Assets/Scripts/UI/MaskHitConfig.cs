using UnityEngine;

// MaskSlot의 Hit 애니메이션 설정 SO.
// 피격 스프라이트 배열, 프레임 속도, 프레임별 스케일을 보관합니다.
[CreateAssetMenu(menuName = "UI/Mask/HitConfig")]
public class MaskHitConfig : ScriptableObject
{
    [Header("스프라이트")]
    public Sprite[] sprites;

    [Header("타이밍")]
    [Tooltip("초당 프레임 수")]
    public float frameRate = 11f;

    [Header("프레임별 스케일 (스프라이트 배열과 동일한 길이로 맞출 것)")]
    [Tooltip("피격 임팩트 스케일 펀치. 비어 있으면 스케일 고정 1.")]
    public float[] scalePerFrame;
}
