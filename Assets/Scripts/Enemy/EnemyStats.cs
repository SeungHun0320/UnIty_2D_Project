using UnityEngine;

// 적(몬스터) 전용 스탯 컴포넌트입니다.
// 기본 스탯(CharacterStats)에 더해 드랍 경험치 등 몬스터 전용 데이터를 담습니다.
public class EnemyStats : CharacterStats
{
    [Header("Enemy Only")]
    [Tooltip("플레이어가 처치 시 얻는 경험치 양입니다.")]
    public int rewardExp = 0;
}

