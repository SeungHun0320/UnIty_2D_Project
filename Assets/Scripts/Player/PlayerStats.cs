using UnityEngine;

// 플레이어 전용 스탯 컴포넌트입니다. (SRP: 스탯 관리만 담당)
// 공통 스탯(CharacterStats)을 상속받아 플레이어만의 확장 포인트를 제공합니다.
// 디버그 키 입력은 PlayerDebugController가 담당합니다.
public class PlayerStats : CharacterStats
{
    [Header("Player Only")]
    [Tooltip("플레이어 전용 추가 공격력(버프 등)에 사용합니다.")]
    [SerializeField] private float bonusAttackPower = 0f;

    // 총 공격력을 계산할 때 사용할 수 있는 프로퍼티입니다.
    public float TotalAttackPower => AttackPower + bonusAttackPower;
}
