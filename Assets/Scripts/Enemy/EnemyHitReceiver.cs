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

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsInvincible) return;

        var playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null) return;

        _enemyStats?.TakeDamage(playerStats.TotalAttackPower);
        _animationDriver?.PlayHit();
        _invincibilityTimer = invincibilityDuration;
    }
}
