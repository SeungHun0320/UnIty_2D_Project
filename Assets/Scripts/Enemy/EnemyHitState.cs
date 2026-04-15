using UnityEngine;

// 몬스터 피격 상태를 관리합니다. (SRP)
// 넉백 바운스를 처리하며, BT가 IsActive를 읽어 AI를 차단합니다.
// 스케일 사인파는 HitScaleEffect에 위임합니다.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyHitState : MonoBehaviour
{
    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackForceX = 3f;
    [SerializeField, Min(0f)] private float knockbackForceY = 5f;
    [SerializeField, Min(0f)] private float hitStunDuration = 0.4f;

    private Rigidbody2D _rb;
    private HitScaleEffect _scaleEffect;
    private float _stunTimer;

    // BT 조건 노드(IsEnemyInHitStateNode)가 읽습니다.
    public bool IsActive => _stunTimer > 0f;

    private void Awake()
    {
        _rb          = GetComponent<Rigidbody2D>();
        _scaleEffect = GetComponent<HitScaleEffect>();
    }

    private void Update()
    {
        if (_stunTimer > 0f)
            _stunTimer -= Time.deltaTime;
    }

    // EnemyHitReceiver가 피격 시 호출합니다.
    // hitSourcePos : 공격 발생 위치 (넉백 방향 계산에 사용)
    public void Enter(Vector2 hitSourcePos)
    {
        float dirX = transform.position.x - hitSourcePos.x;
        dirX = Mathf.Approximately(dirX, 0f) ? 1f : Mathf.Sign(dirX);

        _rb.linearVelocity = new Vector2(dirX * knockbackForceX, knockbackForceY);
        _stunTimer = hitStunDuration;

        _scaleEffect?.Play(hitStunDuration);
    }
}
