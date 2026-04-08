using UnityEngine;

// 플레이어 물리 이동을 전담하는 Kinematic Character Controller입니다. (SRP)
// Dynamic Rigidbody2D 대신 수동 속도/중력/충돌 처리로 정밀한 플랫포머 조작감을 구현합니다.
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMover : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Range(0f, 1f)] private float movingThreshold = 0.05f;

    [Header("Jump")]
    [SerializeField, Min(0f)] private float jumpForce = 7f;
    [SerializeField, Min(0f)] private float gravityStrength = 30f;
    [SerializeField, Min(0f)] private float maxFallSpeed = 20f;
    [SerializeField, Min(0f)] private float coyoteTime = 0.08f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField, Min(0.001f)] private float skinWidth = 0.02f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackDuration = 0.25f;

    private Rigidbody2D _rb;
    private Collider2D _col;

    private Vector2 _velocity;
    private bool _isGrounded;
    private bool _wasGrounded = true;
    private float _lastGroundedTime;
    private bool _isJumping;

    private float _knockbackTimer;

    public bool IsGrounded => _isGrounded;
    public bool CanJump => _isGrounded || (Time.time - _lastGroundedTime) <= coyoteTime;
    public float MovingThreshold => movingThreshold;

    // 착지 이벤트: SpineInputController가 구독하여 애니메이션 드라이버에 전달합니다.
    public event System.Action OnLanded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        _col = GetComponent<Collider2D>();
    }

    public void FixedTick(Vector2 moveInput)
    {
        // 넉백 중에는 moveInput 무시
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.fixedDeltaTime;
        }
        else
        {
            // 수평 속도
            _velocity.x = Mathf.Abs(moveInput.x) > movingThreshold ? moveInput.x * moveSpeed : 0f;
        }

        // 수직: 공중에서만 중력 적용 (넉백 중에도 중력/점프는 정상 작동)
        if (!_isGrounded)
            _velocity.y = Mathf.Max(_velocity.y - gravityStrength * Time.fixedDeltaTime, -maxFallSpeed);

        // 충돌 해결 후 이동
        Vector2 delta = ResolveCollisions(_velocity * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + delta);

        // 이동 후 접지 판정
        // _isJumping 중에는 지면 오감지 방지 (점프 직후 아직 지면에 닿아있는 프레임 무시)
        bool rawGrounded = CheckGrounded();
        if (_isJumping)
        {
            if (rawGrounded) rawGrounded = false;  // 아직 지면 근처 → 오감지 억제
            else _isJumping = false;               // 실제로 지면을 벗어남 → 플래그 해제
        }
        bool grounded = rawGrounded;

        bool justLanded = !_wasGrounded && grounded;
        if (justLanded) OnLanded?.Invoke();

        // 낙하 속도 초기화 (상승 중에는 건드리지 않음)
        if (grounded)
            _velocity.y = 0f;

        _isGrounded = grounded;
        _wasGrounded = grounded;
        if (grounded)
            _lastGroundedTime = Time.time;
    }

    // 이동 delta에 대해 수직/수평 충돌을 감지하고 안전한 이동량을 반환합니다.
    private Vector2 ResolveCollisions(Vector2 delta)
    {
        Bounds b = _col.bounds;
        // 수직용: 좌우를 약간 줄여 모서리 걸림 방지
        Vector2 vSize = new Vector2(b.size.x - skinWidth * 2f, b.size.y - skinWidth * 2f);
        // 수평용: 바닥/천장 모서리에 걸리지 않도록 높이를 충분히 줄임
        Vector2 hSize = new Vector2(b.size.x - skinWidth * 2f, b.size.y - skinWidth * 8f);

        // 수직 충돌 처리
        if (delta.y != 0f)
        {
            Vector2 dir = delta.y > 0f ? Vector2.up : Vector2.down;
            float dist = Mathf.Abs(delta.y) + skinWidth;
            RaycastHit2D hit = BoxCastSolid(b.center, vSize, dir, dist);
            if (hit.collider != null)
            {
                float allowed = Mathf.Max(hit.distance - skinWidth, 0f);
                delta.y = delta.y > 0f ? allowed : -allowed;
                _velocity.y = 0f;
            }
        }

        // 수평 충돌 처리 (hSize로 바닥/천장 모서리 무시)
        if (delta.x != 0f)
        {
            Vector2 dir = delta.x > 0f ? Vector2.right : Vector2.left;
            float dist = Mathf.Abs(delta.x) + skinWidth;
            RaycastHit2D hit = BoxCastSolid(b.center, hSize, dir, dist);
            if (hit.collider != null)
            {
                float allowed = Mathf.Max(hit.distance - skinWidth, 0f);
                delta.x = delta.x > 0f ? allowed : -allowed;
                _velocity.x = 0f;
            }
        }

        return delta;
    }

    // skinWidth * 3 범위 내에 지면이 있으면 접지 판정합니다.
    private bool CheckGrounded()
    {
        Bounds b = _col.bounds;
        Vector2 size = new Vector2(b.size.x * 0.9f - skinWidth * 2f, b.size.y - skinWidth * 2f);
        RaycastHit2D hit = BoxCastSolid(b.center, size, Vector2.down, skinWidth * 3f);
        return hit.collider != null;
    }

    // 트리거를 제외하고 이동 방향과 반대 노말을 가진 첫 번째 솔리드 충돌체를 반환합니다.
    // BoxCast는 가장 가까운 하나만 반환하므로, 트리거가 더 가까울 경우 그 뒤의 솔리드를 놓칩니다.
    // BoxCastNonAlloc으로 전체 히트 목록을 순회해 이 문제를 해결합니다.
    private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[8];
    private RaycastHit2D BoxCastSolid(Vector2 origin, Vector2 size, Vector2 direction, float distance)
    {
        int count = Physics2D.BoxCastNonAlloc(origin, size, 0f, direction, _hitBuffer, distance, groundLayers);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D h = _hitBuffer[i];
            if (h.collider == _col || h.collider.isTrigger) continue;
            if (Vector2.Dot(h.normal, direction) < 0f) return h;
        }
        return default;
    }

    // 피격 시 외부에서 호출합니다. 넉백 방향은 (적 → 플레이어) 방향으로 계산해서 넘겨주세요.
    public void ApplyKnockback(Vector2 knockbackVelocity)
    {
        _velocity = knockbackVelocity;
        _knockbackTimer = knockbackDuration;
        _isGrounded = false;
    }

    public void Jump()
    {
        _velocity.y = jumpForce;
        _isGrounded = false;
        _wasGrounded = false;
        _isJumping = true;
    }

    public void FlipByInput(float inputX)
    {
        if (Mathf.Abs(inputX) <= 0.001f) return;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(inputX);
        transform.localScale = scale;
    }

    public float GetVelocityY() => _velocity.y;

    // 외부에서 즉시 접지 여부를 확인할 때 사용합니다 (점프 입력 이벤트에서 호출).
    public bool CheckGroundedImmediate() => CheckGrounded();
}
