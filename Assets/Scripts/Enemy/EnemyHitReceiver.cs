using UnityEngine;

// 몬스터 피격을 감지하고 데미지를 처리합니다. (SRP)
// Enemy GameObject의 자식 "Hurtbox" 오브젝트에 부착합니다.
// 레이어는 EnemyHurtbox로 설정해야 합니다.
[RequireComponent(typeof(Collider2D))]
public class EnemyHitReceiver : MonoBehaviour
{
    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 0.2f;

    private EnemyStats _enemyStats;
    private SpineAnimationDriver _animationDriver;
    private float _invincibilityTimer;

    public bool IsInvincible => _invincibilityTimer > 0f;

    private void Awake()
    {
        _enemyStats = GetComponentInParent<EnemyStats>();
        _animationDriver = GetComponentInParent<SpineAnimationDriver>();
        _playerAttackLayer = LayerMask.NameToLayer("PlayerAttackHitbox");

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    private int _playerAttackLayer;

    // 투사체 등 외부에서 직접 데미지를 줄 때 호출합니다.
    public void ReceiveHit(float damage)
    {
        if (IsInvincible) return;
        _enemyStats?.TakeDamage(damage);
        _animationDriver?.PlayHit();
        _invincibilityTimer = invincibilityDuration;
    }

    // 히트박스 활성화 시점에 이미 겹쳐있는 경우도 처리합니다.
    private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;

        // PlayerAttackHitbox 레이어만 처리 — 플레이어 몸 접촉은 무시합니다.
        if (other.gameObject.layer != _playerAttackLayer) return;

        var playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) return;

        _enemyStats?.TakeDamage(playerStats.TotalAttackPower);
        Debug.Log($"[EnemyHitReceiver] {transform.parent?.name} HP: {_enemyStats?.CurrentHealth} / {_enemyStats?.MaxHealth}");
        _animationDriver?.PlayHit();
        _invincibilityTimer = invincibilityDuration;
    }
}
