using UnityEngine;

// 몬스터 피격을 감지하고 데미지를 처리합니다. (SRP)
// Enemy GameObject의 자식 "Hurtbox" 오브젝트에 부착합니다.
// 레이어는 EnemyHurtbox로 설정해야 합니다.
public class EnemyHitReceiver : HitReceiverBase
{
    [SerializeField] private LayerMask playerAttackMask;

    private EnemyStats          _enemyStats;
    private SpineAnimationDriver _animationDriver;
    private EnemyHitState       _hitState;

    // 마지막 피격 위치 — EnemyDeathHandler가 사망 시 넉백 방향 계산에 사용합니다.
    public Vector2 LastHitSourcePos { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        _enemyStats      = GetComponentInParent<EnemyStats>();
        _animationDriver = GetComponentInParent<SpineAnimationDriver>();
        _hitState        = GetComponentInParent<EnemyHitState>();
    }

    // 투사체 등 외부에서 직접 데미지를 줄 때 호출합니다.
    public void ReceiveHit(float damage, Vector2 sourcePosition = default)
    {
        if (IsInvincible) return;
        ApplyHit(damage, sourcePosition, isMelee: false);
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;
        if ((playerAttackMask.value & (1 << other.gameObject.layer)) == 0) return;

        var playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) return;

        ApplyHit(playerStats.TotalAttackPower, other.transform.position, isMelee: true);
    }

    private void ApplyHit(float damage, Vector2 sourcePosition, bool isMelee)
    {
        LastHitSourcePos = sourcePosition;
        _enemyStats?.TakeDamage(damage);
        Debug.Log($"[EnemyHitReceiver] {transform.parent?.name} HP: {_enemyStats?.CurrentHealth} / {_enemyStats?.MaxHealth}");
        _animationDriver?.PlayHit();

        if (_enemyStats == null || !_enemyStats.IsDead)
            _hitState?.Enter(sourcePosition);

        StartInvincibility();
        EventBus.Publish(new EnemyHitByPlayerEvent(isMelee));
    }
}
