using UnityEngine;

// 아이템 픽업 공통 기반 클래스.
// 트리거 감지 → IPickupEffect(있으면 완료 대기, 없으면 즉시) → ApplyEffect() → Destroy 순으로 동작합니다.
[RequireComponent(typeof(Collider2D))]
public abstract class ItemPickupBase : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool _collected;
    private IPickupEffect _effect;
    private Collider2D _collider;

    protected virtual void Awake()
    {
        _effect = GetComponent<IPickupEffect>();
        _collider = GetComponent<Collider2D>();

        if (_collider != null)
            _collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected) return;
        if (!other.CompareTag(playerTag)) return;

        _collected = true;
        StartCollect();
    }

    private void StartCollect()
    {
        // 중복 트리거 방지를 위해 콜라이더 즉시 비활성화
        if (_collider != null) _collider.enabled = false;

        if (_effect != null)
        {
            _effect.Play(transform.position, () =>
            {
                ApplyEffect();
                Destroy(gameObject);
            });
        }
        else
        {
            ApplyEffect();
            Destroy(gameObject);
        }
    }

    // 실제 게임 효과(돈 지급, 회복 등). 수집 완료 후 1회 호출됩니다.
    protected abstract void ApplyEffect();
}
