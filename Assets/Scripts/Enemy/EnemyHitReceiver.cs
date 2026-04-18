using UnityEngine;

// 몬스터 피격을 감지하고 데미지를 처리합니다. (SRP)
// Enemy GameObject의 자식 "Hurtbox" 오브젝트에 부착합니다.
// 레이어는 EnemyHurtbox로 설정해야 합니다.
public class EnemyHitReceiver : HitReceiverBase
{
    [SerializeField] private LayerMask playerAttackMask;

    private EnemyStats _enemyStats;
    private SpineAnimationDriver _animationDriver;
    private EnemyHitState _hitState;

    protected override void Awake()
    {
        base.Awake();
        _enemyStats      = GetComponentInParent<EnemyStats>();
        _animationDriver = GetComponentInParent<SpineAnimationDriver>();
        _hitState        = GetComponentInParent<EnemyHitState>();
    }

    // 투사체 등 외부에서 직접 데미지를 줄 때 호출합니다.
    // sourcePosition : 공격 발생 위치 (넉백 방향 계산용, 생략 시 정면 넉백)
    public void ReceiveHit(float damage, Vector2 sourcePosition = default)
    {
        if (IsInvincible) return;
        _enemyStats?.TakeDamage(damage);
        _animationDriver?.PlayHit();

        if (_enemyStats == null || !_enemyStats.IsDead)
            _hitState?.Enter(sourcePosition);

        StartInvincibility();

        // 원거리/스킬 공격 적중 — 히트렉·카메라 셰이크 이벤트 발행 (소울 충전 제외)
        EventBus.Publish(new EnemyHitByPlayerEvent(isMelee: false));
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;

        // PlayerAttackHitbox 레이어만 처리 — 플레이어 몸 접촉은 무시합니다.
        if ((playerAttackMask.value & (1 << other.gameObject.layer)) == 0) return;

        var playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) return;

        _enemyStats?.TakeDamage(playerStats.TotalAttackPower);
        Debug.Log($"[EnemyHitReceiver] {transform.parent?.name} HP: {_enemyStats?.CurrentHealth} / {_enemyStats?.MaxHealth}");
        _animationDriver?.PlayHit();

        // 사망 시 히트 스테이트 적용 안 함 — 사망 처리에 맡깁니다.
        if (_enemyStats == null || !_enemyStats.IsDead)
            _hitState?.Enter(other.transform.position);

        StartInvincibility();

        // 근접 공격 적중 — 소울 충전 이벤트 발행
        EventBus.Publish(new EnemyHitByPlayerEvent(isMelee: true));
    }
}
