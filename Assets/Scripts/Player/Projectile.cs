using UnityEngine;

// 원거리 스킬에서 발사되는 투사체입니다.
// 지정 방향으로 이동하며 EnemyHurtbox 레이어에 닿으면 데미지를 줍니다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 3f;

    [Tooltip("시간(초)에 따른 속도 배율 커브. X축=경과시간, Y축=속도(유닛/초)")]
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(
        new Keyframe(0f,    0f),
        new Keyframe(0.08f, 0f),
        new Keyframe(0.25f, 10f),
        new Keyframe(0.4f,  15f)
    );

    private Rigidbody2D _rb;
    private float       _damage;
    private Vector2     _direction;
    private float       _elapsed;
    private bool        _initialized;
    private int         _enemyHurtboxLayer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _enemyHurtboxLayer = LayerMask.NameToLayer("EnemyHurtbox");
    }

    // Init() 호출 전까지 이동하지 않도록 _initialized 플래그로 보호합니다.
    public void Init(Vector2 direction, float damage)
    {
        _direction   = direction.normalized;
        _damage      = damage;
        _elapsed     = 0f;
        _initialized = true;
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        if (!_initialized) return;

        _elapsed += Time.fixedDeltaTime;
        float currentSpeed = speedCurve.Evaluate(_elapsed);
        // Kinematic RB2D는 MovePosition으로 이동해야 물리 충돌이 올바르게 처리됩니다.
        _rb.MovePosition(_rb.position + _direction * currentSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // EnemyHurtbox 레이어만 처리합니다.
        if (other.gameObject.layer != _enemyHurtboxLayer) return;

        EnemyHitReceiver enemy = other.GetComponent<EnemyHitReceiver>();
        if (enemy == null) return;

        enemy.ReceiveHit(_damage);
        Destroy(gameObject);
    }
}
