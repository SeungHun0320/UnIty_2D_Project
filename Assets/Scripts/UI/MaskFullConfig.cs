using UnityEngine;

// MaskSlot의 Full 애니메이션 설정 SO.
// 스프라이트 배열, 프레임 속도, 프레임별 회전 보정값을 보관합니다.
[CreateAssetMenu(menuName = "UI/Mask/FullConfig")]
public class MaskFullConfig : ScriptableObject
{
    [Header("스프라이트")]
    public Sprite[] sprites;

    [Header("타이밍")]
    [Tooltip("초당 프레임 수 (8 = 0.125s/frame)")]
    public float frameRate = 8f;

    [Header("프레임별 회전 보정 (스프라이트 배열과 동일한 길이로 맞출 것)")]
    [Tooltip("아틀라스에 회전된 채로 저장된 스프라이트를 보정합니다. 보정 불필요한 프레임은 (0,0,0).")]
    public Vector3[] rotations;

    [Header("주기적 재생")]
    [Tooltip("한 사이클 재생 후 다음 재생까지 대기 시간(초). 0이면 즉시 반복.")]
    public float intervalSeconds = 2f;
}
