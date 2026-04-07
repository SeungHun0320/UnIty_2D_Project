// 플레이어 애니메이션 제어를 추상화하는 인터페이스입니다. (DIP)
// SpineInputController / PlayerStateMachine이 구체 구현이 아닌 이 인터페이스에 의존합니다.
public interface IAnimationDriver
{
    void PlayIdle(bool forceRestart = false);
    void SetMoving(bool moving);
    void PlayJump();
    void PlayAttack();
    void NotifyVelocityY(float vy);
    void NotifyLanded();
}
