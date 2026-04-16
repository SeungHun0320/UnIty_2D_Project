using UnityEngine;

// 몬스터 사망 시퀀스를 담당합니다. (SRP)
// EnemyStats.OnDeadEvent를 구독해 AI 중단 → 물리 정지 → 사망 애니메이션 → 오브젝트 제거를 처리합니다.
[RequireComponent(typeof(EnemyStats))]
public class EnemyDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RustBehaviourTree behaviourTree;
    [SerializeField] private EnemyAnimationDriver animationDriver;
    [SerializeField] private Rigidbody2D rb;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float destroyDelay = 2f;

    private EnemyStats _stats;

    private void Awake()
    {
        _stats = GetComponent<EnemyStats>();

        if (behaviourTree == null)
            behaviourTree = GetComponent<RustBehaviourTree>();
        if (animationDriver == null)
            animationDriver = GetComponent<EnemyAnimationDriver>();
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()  { if (_stats != null) _stats.OnDeadEvent += HandleDeath; }
    private void OnDisable() { if (_stats != null) _stats.OnDeadEvent -= HandleDeath; }

    private void HandleDeath()
    {
        // AI 중단
        if (behaviourTree != null)
            behaviourTree.enabled = false;

        // 물리 정지
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 사망 애니메이션
        animationDriver?.PlayDead();

        // 사망 애니메이션 완료 후 제거
        Destroy(gameObject, destroyDelay);
    }
}
