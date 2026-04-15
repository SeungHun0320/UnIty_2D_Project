// 플레이어 애니메이션 제어를 추상화하는 인터페이스입니다. (DIP)
// SpineInputController / PlayerStateMachine이 구체 구현이 아닌 이 인터페이스에 의존합니다.
public interface IAnimationDriver
{
    void PlayIdle(bool forceRestart = false);
    void SetMoving(bool moving);
    void PlayJump();
    void PlayAttack();
    void PlayHit();
    void PlayDead();
    void NotifyLanded();

    // 애니메이션 키를 직접 지정해 재생합니다. 완료 시 OnActionComplete가 발행됩니다.
    void PlayAnimation(string animationKey);

    // 공격/스킬 애니메이션 완료 시 발행됩니다.
    event System.Action OnActionComplete;

    // 현재 트랙 0의 TimeScale을 반환합니다. ActivateHitbox 타이밍 보정에 사용합니다.
    float CurrentTrackTimeScale { get; }
}
