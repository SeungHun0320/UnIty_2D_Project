using UnityEngine;

// 러스트 몬스터 AI가 공유하는 상태/설정을 담는 블랙보드입니다.
// 비헤이비어 트리 노드들은 이 데이터를 읽고 판단합니다.

public class RustBlackboard : MonoBehaviour
{
    [Header("References")]
    public Transform selfTransform;
    public Transform playerTransform;
    public SpineAnimationDriver animationDriver;
    public Rigidbody2D rb;
    public Collider2D col;
    [SerializeField] private EnemyAttackHitbox attackHitboxComponent;
    public IAttackHitbox AttackHitbox => attackHitboxComponent;

    [Header("Combat Settings")]
    public float sightRange = 8f;
    public float chaseRange = 10f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;

    [Header("Attack Timing")]
    [Tooltip("한 번의 공격 애니메이션이 유지되는 시간(초)입니다.")]
    public float attackDuration = 0.5f;

    [Header("Ground / Wall Detection")]
    public LayerMask groundLayers;
    [Tooltip("발 앞쪽 낙하 감지 Raycast 거리")]
    public float ledgeCheckDistance = 0.5f;
    [Tooltip("앞쪽 벽 감지 Raycast 거리")]
    public float wallCheckDistance = 0.3f;

    [Header("State")]
    public float currentHealth = 10f;
    public float maxHealth = 10f;

    private void Awake()
    {
        if (selfTransform == null)
            selfTransform = transform;

        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (col == null)
            col = GetComponent<Collider2D>();
    }

    public bool IsDead => currentHealth <= 0f;

    public float DistanceToPlayer
    {
        get
        {
            if (playerTransform == null || selfTransform == null)
                return float.PositiveInfinity;

            return Vector2.Distance(selfTransform.position, playerTransform.position);
        }
    }

    // 테스트 체크리스트:
    // - 플레이어 참조가 없을 때 DistanceToPlayer가 무한대로 처리되는지 확인
    // - selfTransform/animationDriver 자동 할당이 제대로 되는지 확인
}
