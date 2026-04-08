using UnityEngine;

// 플레이어 피격을 감지하고 데미지 + 넉백을 처리합니다. (SRP)
// Player GameObject의 자식 "Hurtbox" 오브젝트에 부착합니다.
// 해당 자식 오브젝트는 isTrigger = true인 Collider2D와 PlayerHurtbox 레이어를 가져야 합니다.
[RequireComponent(typeof(Collider2D))]
public class PlayerHitReceiver : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField] private float knockbackSpeedX = 6f;
    [SerializeField] private float knockbackSpeedY = 4f;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.8f;

    private PlayerStats _playerStats;
    private PlayerMover _playerMover;
    private PlayerStateMachine _playerStateMachine;
    private float _invincibilityTimer;

    public bool IsInvincible => _invincibilityTimer > 0f;

    private void Awake()
    {
        // 부모 오브젝트에서 컴포넌트를 가져옵니다.
        _playerStats = GetComponentInParent<PlayerStats>();
        _playerMover = GetComponentInParent<PlayerMover>();
        _playerStateMachine = GetComponentInParent<PlayerStateMachine>();

        // Collider2D가 Trigger인지 보정합니다.
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    // 무적시간이 끝난 뒤 몬스터에 계속 닿아있어도 재발동되도록 Stay도 처리합니다.
    private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;

        var enemyStats = other.GetComponentInParent<EnemyStats>();
        if (enemyStats == null) return;

        // 데미지 적용
        _playerStats?.TakeDamage(enemyStats.AttackPower);

        // 히트 상태 전환 → 애니메이션 재생
        _playerStateMachine?.OnHit();

        // 넉백 방향: 적 → 플레이어 (수평) + 위쪽
        float dirX = Mathf.Sign(transform.position.x - other.transform.position.x);
        _playerMover?.ApplyKnockback(new Vector2(dirX * knockbackSpeedX, knockbackSpeedY));

        _invincibilityTimer = invincibilityDuration;
    }
}
