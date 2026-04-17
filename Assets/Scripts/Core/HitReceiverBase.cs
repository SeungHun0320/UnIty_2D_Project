using UnityEngine;

// PlayerHitReceiver / EnemyHitReceiver 공통 무적 타이머 로직을 담는 베이스 클래스입니다.
// 무적 시간 감산, IsInvincible 프로퍼티, OnTriggerStay2D → OnTriggerEnter2D 위임을 제공합니다.
[RequireComponent(typeof(Collider2D))]
public abstract class HitReceiverBase : MonoBehaviour
{
    [Header("Invincibility")]
    [SerializeField] protected float invincibilityDuration = 0.5f;

    private float _invincibilityTimer;

    public bool IsInvincible => _invincibilityTimer > 0f;

    protected virtual void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    protected virtual void Update()
    {
        if (_invincibilityTimer > 0f)
            _invincibilityTimer -= Time.deltaTime;
    }

    // 히트박스가 활성화될 때 이미 겹쳐있는 경우도 처리합니다.
    private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

    protected abstract void OnTriggerEnter2D(Collider2D other);

    // 서브클래스가 피격 처리 후 무적 타이머를 시작할 때 호출합니다.
    protected void StartInvincibility() => _invincibilityTimer = invincibilityDuration;
}
