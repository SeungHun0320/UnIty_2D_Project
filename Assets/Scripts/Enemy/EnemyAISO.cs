using UnityEngine;

// 몬스터 AI 설정을 담는 ScriptableObject입니다.
// 같은 타입의 몬스터 여러 마리가 하나의 SO를 공유하며,
// 변종(Elite, Boss 등)은 별도 SO 에셋으로 만들어 교체합니다.
[CreateAssetMenu(fileName = "EnemyAISO", menuName = "Game/Enemy AI Settings")]
public class EnemyAISO : ScriptableObject
{
    [Header("Combat")]
    public float sightRange       = 8f;
    public float chaseRange       = 10f;
    [Tooltip("이 Y 거리 초과 시 시야에서 놓치고 추적을 포기합니다.")]
    public float maxYDistance     = 4f;
    public float attackRange      = 1.5f;
    [Tooltip("이 거리 이하로 접근하면 이동을 멈추고 공격 대기합니다. attackRange 이상으로 설정하세요.")]
    public float stopChaseRange   = 2f;
    public float moveSpeed        = 2f;
    public float attackDuration   = 0.5f;
    public float attackCooldown   = 1.0f;

    [Header("Ground / Wall Detection")]
    public LayerMask groundLayers;
    public float ledgeCheckDistance = 0.5f;
    public float wallCheckDistance  = 0.3f;
}
