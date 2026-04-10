using UnityEngine;

// 스킬 실행에 필요한 참조를 묶은 컨텍스트 클래스입니다.
// PlayerStateMachine이 생성하여 SkillData.Execute()에 넘겨줍니다.
public class SkillContext
{
    public PlayerMover      Mover;
    public IAttackHitbox    Hitbox;
    public IAnimationDriver Anim;
    public PlayerStats      Stats;
    public Transform        Origin;  // 투사체 스폰 위치 및 방향 참조 (localScale.x 부호로 방향 판별)
}
