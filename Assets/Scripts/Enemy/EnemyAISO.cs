using UnityEngine;

// 몬스터 AI 설정을 담는 ScriptableObject입니다.
// 같은 타입의 몬스터 여러 마리가 하나의 SO를 공유하며,
// 변종(Elite, Boss 등)은 별도 SO 에셋으로 만들어 교체합니다.
[CreateAssetMenu(fileName = "EnemyAISO", menuName = "Game/Enemy AI Settings")]
public class EnemyAISO : ScriptableObject
{
    [Header("Combat")]
    public float sightRange      = 8f;
    public float chaseRange      = 10f;
    public float attackRange     = 1.5f;
    public float moveSpeed       = 2f;
    public float attackDuration  = 0.5f;

    [Header("Ground / Wall Detection")]
    public LayerMask groundLayers;
    public float ledgeCheckDistance = 0.5f;
    public float wallCheckDistance  = 0.3f;
}
