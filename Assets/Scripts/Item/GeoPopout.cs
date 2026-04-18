using System.Collections;
using UnityEngine;

// 스폰 즉시 랜덤 각도로 튀어나오고, 바닥/벽 Raycast로 충돌 처리 후 정착합니다.
[RequireComponent(typeof(Collider2D))]
public class GeoPopout : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 5f;
    [SerializeField] private float gravity = 18f;
    [SerializeField] private float bounceDamping = 0.45f;
    [SerializeField] private int maxBounces = 2;
    [SerializeField] private LayerMask groundLayer;

    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];

    private Collider2D _collider;
    private SpriteRenderer _sr;
    private Vector2 _velocity;
    private int _bounceCount;

    private void Start()
    {
        _collider = GetComponent<Collider2D>();
        _sr       = GetComponent<SpriteRenderer>();
        if (_collider != null) _collider.enabled = false;

        float angle = Random.Range(30f, 150f) * Mathf.Deg2Rad;
        float speed = Random.Range(launchSpeed * 0.7f, launchSpeed);
        _velocity   = new Vector2(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed);

        StartCoroutine(PopoutRoutine());
    }

    private IEnumerator PopoutRoutine()
    {
        while (true)
        {
            float dt = Time.deltaTime;
            _velocity.y -= gravity * dt;

            // 바닥 감지 (하강 중에만)
            if (_velocity.y < 0f)
            {
                float checkDist  = Mathf.Abs(_velocity.y * dt) + 0.05f;
                RaycastHit2D ground = NonTriggerRaycast(BottomPoint(), Vector2.down, checkDist);

                if (ground.collider != null)
                {
                    float bottomOffset = transform.position.y - BottomPoint().y;
                    transform.position = new Vector3(transform.position.x, ground.point.y + bottomOffset, transform.position.z);

                    _bounceCount++;
                    bool tooWeak = Mathf.Abs(_velocity.y) * bounceDamping < 0.8f;
                    if (_bounceCount >= maxBounces || tooWeak)
                    {
                        Settle();
                        yield break;
                    }

                    _velocity.y = -_velocity.y * bounceDamping;
                    _velocity.x *= 0.6f;
                }
            }

            // 벽 감지 (수평 이동 방향)
            if (_velocity.x != 0f)
            {
                Vector2 dir      = _velocity.x > 0f ? Vector2.right : Vector2.left;
                float checkDist  = Mathf.Abs(_velocity.x * dt) + 0.05f;
                RaycastHit2D wall = NonTriggerRaycast(transform.position, dir, checkDist);
                if (wall.collider != null)
                    _velocity.x = -_velocity.x * bounceDamping;
            }

            transform.position += (Vector3)(_velocity * dt);
            yield return null;
        }
    }

    // Trigger 제외 Raycast — 정착 동전 Collider 오인 방지
    private RaycastHit2D NonTriggerRaycast(Vector2 origin, Vector2 dir, float dist)
    {
        int count = Physics2D.RaycastNonAlloc(origin, dir, _hitBuffer, dist, groundLayer);
        for (int i = 0; i < count; i++)
            if (!_hitBuffer[i].collider.isTrigger)
                return _hitBuffer[i];
        return default;
    }

    private Vector2 BottomPoint() =>
        _sr != null ? new Vector2(transform.position.x, _sr.bounds.min.y) : (Vector2)transform.position;

    private void Settle()
    {
        _velocity = Vector2.zero;
        if (_collider != null) _collider.enabled = true;
    }
}
