using UnityEngine;

// SetupMaskAnimator가 읽어서 MaskHit.anim 스케일 커브를 생성하는 설정 SO입니다.
// Assets/Animations/UI/Mask/MaskAnimatorConfig.asset 에 저장합니다.
[CreateAssetMenu(menuName = "UI/MaskAnimatorConfig")]
public class MaskAnimatorConfig : ScriptableObject
{
    [Header("Hit 프레임별 스케일")]
    [Tooltip("Hit 스프라이트 프레임 수와 일치시킬 것. 비어 있으면 스케일 커브 미적용.")]
    public float[] hitFrameScales;
}
