using UnityEngine;

// 플레이어 공격 판정 전용 트리거 콜라이더를 제어합니다. (SRP)
// Player GameObject의 자식 "AttackHitbox" 오브젝트에 부착합니다.
// 레이어는 PlayerAttackHitbox로 설정해야 합니다.
[RequireComponent(typeof(Collider2D))]
public class PlayerAttackHitbox : MonoBehaviour, IAttackHitbox
{
    private Collider2D _col;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        Deactivate();
    }

    // 공격 시작 시 PlayerStateMachine/AttackingState에서 호출합니다.
    public void Activate() => _col.enabled = true;

    // 공격 종료 시 PlayerStateMachine/AttackingState에서 호출합니다.
    public void Deactivate() => _col.enabled = false;

    // 공격 종류에 따라 히트박스 크기/위치를 조정할 때 사용합니다.
    public void SetSize(Vector2 size, Vector2 offset)
    {
        if (_col is BoxCollider2D box)
        {
            box.size = size;
            box.offset = offset;
        }
        else if (_col is CapsuleCollider2D capsule)
        {
            capsule.size = size;
            capsule.offset = offset;
        }
    }
}
