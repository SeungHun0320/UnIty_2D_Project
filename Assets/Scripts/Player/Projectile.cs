using UnityEngine;

// 원거리 스킬에서 발사되는 투사체입니다.
// 지정 방향으로 이동하며 EnemyHurtbox에 닿으면 데미지를 줍니다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed        = 10f;
    [SerializeField] private float lifeTime     = 3f;

    private float   _damage;
    private Vector2 _direction;

    public void Init(Vector2 direction, float damage)
    {
        _direction = direction.normalized;
        _damage    = damage;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Translate(_direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyHitReceiver enemy = other.GetComponent<EnemyHitReceiver>();
        if (enemy == null) return;

        enemy.ReceiveHit(_damage);
        Destroy(gameObject);
    }
}
