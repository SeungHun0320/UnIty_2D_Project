using UnityEngine;

// 플레이어 피격을 감지하고 데미지 + 넉백을 처리합니다. (SRP)
// Player GameObject의 자식 "Hurtbox" 오브젝트에 부착합니다.
// 해당 자식 오브젝트는 isTrigger = true인 Collider2D와 PlayerHurtbox 레이어를 가져야 합니다.
public class PlayerHitReceiver : HitReceiverBase
{
    [Header("Knockback")]
    [SerializeField] private float knockbackSpeedX = 6f;
    [SerializeField] private float knockbackSpeedY = 4f;

    private PlayerStats _playerStats;
    private PlayerMover _playerMover;
    private PlayerStateMachine _playerStateMachine;
    private HitScaleEffect _scaleEffect;

    protected override void Awake()
    {
        base.Awake();
        // 부모 오브젝트에서 컴포넌트를 가져옵니다.
        _playerStats        = GetComponentInParent<PlayerStats>();
        _playerMover        = GetComponentInParent<PlayerMover>();
        _playerStateMachine = GetComponentInParent<PlayerStateMachine>();
        _scaleEffect        = GetComponentInParent<HitScaleEffect>();
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;

        var enemyStats = other.GetComponentInParent<EnemyStats>();
        if (enemyStats == null || enemyStats.IsDead) return;

        // 넉백 방향 계산 (이벤트에도 사용)
        float dirX = Mathf.Sign(transform.position.x - other.transform.position.x);

        // 히트렉·카메라 셰이크를 먼저 발동 — 이후 데미지/Flash가 hitstop 안에서 시작되도록
        EventBus.Publish(new PlayerHitByEnemyEvent(new Vector2(dirX, 0f)));

        _playerStats?.TakeDamage(enemyStats.AttackPower);

        // 히트 상태 전환 → 애니메이션 재생
        _playerStateMachine?.OnHit();

        // 넉백 적용
        _playerMover?.ApplyKnockback(new Vector2(dirX * knockbackSpeedX, knockbackSpeedY));

        // 스케일 사인파 — invincibilityDuration 기준으로 재생합니다.
        _scaleEffect?.Play(invincibilityDuration * 0.4f);

        StartInvincibility();
    }
}
