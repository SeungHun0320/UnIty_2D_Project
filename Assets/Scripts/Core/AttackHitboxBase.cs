using UnityEngine;

// PlayerAttackHitbox / EnemyAttackHitbox 공통 로직을 담는 추상 베이스 클래스입니다.
[RequireComponent(typeof(Collider2D))]
public abstract class AttackHitboxBase : MonoBehaviour, IAttackHitbox
{
    protected Collider2D _col;

    protected virtual void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        Deactivate();
    }

    public void Activate()   => _col.enabled = true;
    public void Deactivate() => _col.enabled = false;

    public void SetSize(Vector2 size, Vector2 offset)
    {
        if (_col is BoxCollider2D box)
        {
            box.size   = size;
            box.offset = offset;
        }
        else if (_col is CapsuleCollider2D capsule)
        {
            capsule.size   = size;
            capsule.offset = offset;
        }
    }
}
