using UnityEngine;

// 공격 판정 콜라이더를 제어하는 인터페이스입니다. (DIP)
// PlayerAttackHitbox / EnemyAttackHitbox가 구현하며,
// PlayerStateMachine / RustBlackboard는 이 인터페이스에만 의존합니다.
public interface IAttackHitbox
{
    void Activate();
    void Deactivate();
    void SetSize(Vector2 size, Vector2 offset);
}
