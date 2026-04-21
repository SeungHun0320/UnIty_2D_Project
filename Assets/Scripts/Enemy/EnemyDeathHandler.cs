using UnityEngine;

// 몬스터 사망 시퀀스를 담당합니다. (SRP)
// EnemyStats.OnDeadEvent를 구독해 AI 중단 → 사망 넉백 → 사망 애니메이션 → 오브젝트 제거를 처리합니다.
[RequireComponent(typeof(EnemyStats))]
public class EnemyDeathHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RustBehaviourTree    behaviourTree;
    [SerializeField] private EnemyAnimationDriver animationDriver;

    [Header("Settings")]
    [SerializeField, Min(0f)] private float destroyDelay = 2f;

    private EnemyStats       _stats;
    private EnemyHitState    _hitState;
    private EnemyHitReceiver _hitReceiver;
    private ItemDropper      _itemDropper;

    private void Awake()
    {
        _stats       = GetComponent<EnemyStats>();
        _hitState    = GetComponent<EnemyHitState>();
        _hitReceiver = GetComponentInChildren<EnemyHitReceiver>();
        _itemDropper = GetComponent<ItemDropper>();

        if (behaviourTree == null)
            behaviourTree = GetComponent<RustBehaviourTree>();
        if (animationDriver == null)
            animationDriver = GetComponent<EnemyAnimationDriver>();
    }

    private void OnEnable()  { if (_stats != null) _stats.OnDeadEvent += HandleDeath; }
    private void OnDisable() { if (_stats != null) _stats.OnDeadEvent -= HandleDeath; }

    private void HandleDeath()
    {
        // AI 중단
        if (behaviourTree != null)
            behaviourTree.enabled = false;

        // 사망 넉백 — EnemyHitState 재사용 (방향 계산 · 속도 적용 · 스케일 이펙트 일괄 처리)
        if (_hitReceiver != null)
            _hitState?.Enter(_hitReceiver.LastHitSourcePos);

        // 사망 애니메이션
        animationDriver?.PlayDead();

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
