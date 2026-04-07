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

    private Rigidbody2D _rb;
    private Collider2D _col;

    private Vector2 _velocity;
    private bool _isGrounded;
    private bool _wasGrounded = true;
    private float _lastGroundedTime;

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
        // 수평 속도
        _velocity.x = Mathf.Abs(moveInput.x) > movingThreshold ? moveInput.x * moveSpeed : 0f;

        // 수직: 공중에서만 중력 적용
        if (!_isGrounded)
            _velocity.y = Mathf.Max(_velocity.y - gravityStrength * Time.fixedDeltaTime, -maxFallSpeed);

        // 충돌 해결 후 이동
        Vector2 delta = ResolveCollisions(_velocity * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + delta);

        // 이동 후 접지 판정
        bool grounded = CheckGrounded();

        bool justLanded = !_wasGrounded && grounded;
        if (justLanded)
            OnLanded?.Invoke();

        // 지면에 닿았을 때 낙하 속도 초기화
        if (grounded)
            _velocity.y = Mathf.Min(_velocity.y, 0f);

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
            RaycastHit2D hit = Physics2D.BoxCast(b.center, vSize, 0f, dir, dist, groundLayers);
            if (hit.collider != null && hit.collider != _col)
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
            RaycastHit2D hit = Physics2D.BoxCast(b.center, hSize, 0f, dir, dist, groundLayers);
            if (hit.collider != null && hit.collider != _col)
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
        RaycastHit2D hit = Physics2D.BoxCast(b.center, size, 0f, Vector2.down, skinWidth * 3f, groundLayers);
        return hit.collider != null && hit.collider != _col;
    }

    public void Jump()
    {
        _velocity.y = jumpForce;
        _isGrounded = false;
        _wasGrounded = false;
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
