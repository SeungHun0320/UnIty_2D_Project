using UnityEngine;

// 러스트 몬스터 AI가 공유하는 상태/설정을 담는 블랙보드입니다.
// 비헤이비어 트리 노드들은 이 데이터를 읽고 판단합니다.

public class RustBlackboard : MonoBehaviour
{
    [Header("References")]
    public Transform selfTransform;
    public Transform playerTransform;
    public EnemyStats enemyStats;
    public EnemyAnimationDriver animationDriver;
    public Rigidbody2D rb;
    public Collider2D col;
    [SerializeField] private EnemyAttackHitbox attackHitboxComponent;
    [SerializeField] private EnemyHitState hitStateComponent;
    public IAttackHitbox AttackHitbox => attackHitboxComponent;
    public bool IsInHitState => hitStateComponent != null && hitStateComponent.IsActive;

    [Header("AI Settings")]
    [SerializeField] private EnemyAISO aiSettings;

    // SO에서 값을 읽는 프로퍼티들 — 노드는 기존과 동일하게 Blackboard.xxx로 접근합니다.
    public float sightRange         => aiSettings != null ? aiSettings.sightRange         : 8f;
    public float chaseRange         => aiSettings != null ? aiSettings.chaseRange         : 10f;
    public float maxYDistance       => aiSettings != null ? aiSettings.maxYDistance       : 4f;
    public float attackRange        => aiSettings != null ? aiSettings.attackRange        : 1.5f;
    public float stopChaseRange     => aiSettings != null ? aiSettings.stopChaseRange     : 2f;
    public float moveSpeed          => aiSettings != null ? aiSettings.moveSpeed          : 2f;
    public float attackDuration     => aiSettings != null ? aiSettings.attackDuration     : 0.5f;
    public float attackCooldown     => aiSettings != null ? aiSettings.attackCooldown     : 1.0f;
    public LayerMask groundLayers   => aiSettings != null ? aiSettings.groundLayers       : default;
    public float ledgeCheckDistance => aiSettings != null ? aiSettings.ledgeCheckDistance : 0.5f;
    public float wallCheckDistance  => aiSettings != null ? aiSettings.wallCheckDistance  : 0.3f;

    private void Awake()
    {
        if (selfTransform == null)
            selfTransform = transform;

        if (enemyStats == null)
            enemyStats = GetComponent<EnemyStats>();

        if (animationDriver == null)
            animationDriver = GetComponent<EnemyAnimationDriver>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (col == null)
            col = GetComponent<Collider2D>();

        if (hitStateComponent == null)
            hitStateComponent = GetComponent<EnemyHitState>();
    }

    private void Start()
    {
        // Start에서 탐색 — PlayerSpawner가 sceneLoaded(Awake~Start 사이)에서 플레이어를 생성하므로 Start 시점에는 반드시 존재합니다.
        if (playerTransform == null)
        {
            var player = FindAnyObjectByType<PlayerStats>();
            if (player != null) playerTransform = player.transform;
        }
    }

    public bool IsDead => enemyStats != null && enemyStats.IsDead;

    // 수평 거리 — 이동/범위 판단에 사용합니다.
    public float DistanceToPlayer
    {
        get
        {
            if (playerTransform == null || selfTransform == null) return float.PositiveInfinity;
            return Mathf.Abs(playerTransform.position.x - selfTransform.position.x);
        }
    }

    // 수직 거리 — 층 판단(시야 차단)에 사용합니다.
    public float VerticalDistanceToPlayer
    {
        get
        {
            if (playerTransform == null || selfTransform == null) return float.PositiveInfinity;
            return Mathf.Abs(playerTransform.position.y - selfTransform.position.y);
        }
    }

    // 테스트 체크리스트:
    // - 플레이어 참조가 없을 때 DistanceToPlayer가 무한대로 처리되는지 확인
    // - selfTransform/animationDriver 자동 할당이 제대로 되는지 확인
}
