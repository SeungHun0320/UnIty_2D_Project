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

    [Header("Death Knockback")]
    [SerializeField, Min(0f)] private float deathKnockbackX = 4f;
    [SerializeField, Min(0f)] private float deathKnockbackY = 6f;

    private EnemyStats        _stats;
    private HitScaleEffect    _scaleEffect;
    private EnemyHitReceiver  _hitReceiver;
    private ItemDropper       _itemDropper;

    private void Awake()
    {
        _stats       = GetComponent<EnemyStats>();
        _scaleEffect = GetComponent<HitScaleEffect>();
        _hitReceiver = GetComponentInChildren<EnemyHitReceiver>();
        _itemDropper = GetComponent<ItemDropper>();

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

        // 사망 넉백 — 마지막 피격 방향으로 튕겨납니다.
        if (rb != null)
        {
            float dirX = 0f;
            if (_hitReceiver != null)
            {
                float dx = transform.position.x - _hitReceiver.LastHitSourcePos.x;
                dirX = Mathf.Approximately(dx, 0f) ? 1f : Mathf.Sign(dx);
            }
            rb.bodyType       = RigidbodyType2D.Dynamic;
            rb.linearVelocity = new Vector2(dirX * deathKnockbackX, deathKnockbackY);
        }

        // 사망 애니메이션
        animationDriver?.PlayDead();

        // 피격의 절반 강도로 squash & stretch 재생
        _scaleEffect?.Play(destroyDelay * 0.5f, 0.5f);

        // 사망 애니메이션 종료 직전 드롭 + 제거
        StartCoroutine(DropThenDestroy());
    }

    private System.Collections.IEnumerator DropThenDestroy()
    {
        yield return new WaitForSeconds(destroyDelay - 0.05f);
        _itemDropper?.TriggerDrop(transform.position);
        yield return new WaitForSeconds(0.05f);
        Destroy(gameObject);
    }
}
