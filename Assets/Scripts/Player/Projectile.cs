using UnityEngine;

// 원거리 스킬에서 발사되는 투사체입니다.
// 지정 방향으로 이동하며 EnemyHurtbox에 닿으면 데미지를 줍니다.
// speedCurve로 발사 후 프레임별 속도를 인스펙터에서 조절할 수 있습니다.
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

    private float   _damage;
    private Vector2 _direction;
    private float   _elapsed;

    public void Init(Vector2 direction, float damage)
    {
        _direction = direction.normalized;
        _damage    = damage;
        _elapsed   = 0f;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float currentSpeed = speedCurve.Evaluate(_elapsed);
        transform.Translate(_direction * currentSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHitReceiver enemy = other.GetComponent<EnemyHitReceiver>();
        if (enemy == null) return;

        enemy.ReceiveHit(_damage);
        Destroy(gameObject);
    }
}
