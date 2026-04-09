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

    [Header("AI Settings")]
    [SerializeField] private EnemyAISO aiSettings;

    // SO에서 값을 읽는 프로퍼티들 — 노드는 기존과 동일하게 Blackboard.xxx로 접근합니다.
    public float sightRange         => aiSettings != null ? aiSettings.sightRange         : 8f;
    public float chaseRange         => aiSettings != null ? aiSettings.chaseRange         : 10f;
    public float attackRange        => aiSettings != null ? aiSettings.attackRange        : 1.5f;
    public float moveSpeed          => aiSettings != null ? aiSettings.moveSpeed          : 2f;
    public float attackDuration     => aiSettings != null ? aiSettings.attackDuration     : 0.5f;
    public float attackCooldown     => aiSettings != null ? aiSettings.attackCooldown     : 1.0f;
    public LayerMask groundLayers   => aiSettings != null ? aiSettings.groundLayers       : default;
    public float ledgeCheckDistance => aiSettings != null ? aiSettings.ledgeCheckDistance : 0.5f;
    public float wallCheckDistance  => aiSettings != null ? aiSettings.wallCheckDistance  : 0.3f;

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
