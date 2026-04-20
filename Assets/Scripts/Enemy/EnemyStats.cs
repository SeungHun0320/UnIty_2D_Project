using UnityEngine;

// 적(몬스터) 전용 스탯 컴포넌트입니다.
// 기본 스탯(CharacterStats)에 더해 드랍 경험치 등 몬스터 전용 데이터를 담습니다.
public class EnemyStats : CharacterStats
{
    [Header("Enemy Only")]
    [Tooltip("플레이어가 처치 시 얻는 경험치 범위입니다. (최소, 최대) 랜덤 지급됩니다.")]
    public Vector2Int rewardExpRange = new Vector2Int(0, 0);
    // 지오 보상은 ItemDropper 컴포넌트의 dropTable로 관리합니다.

    protected override void OnDead()
    {
        base.OnDead();
        // 피격 감지만 비활성화 — 메인 물리 콜라이더는 유지해 지형 충돌이 동작합니다.
        GetComponentInChildren<EnemyHitReceiver>()?.gameObject.SetActive(false);
        GetComponentInChildren<EnemyAttackHitbox>()?.Deactivate();
        // EnemyDeadEvent는 GameManager.OnEnemyDead() 내부에서 발행합니다. 중복 발행 방지.
        GameManager.Instance?.OnEnemyDead(gameObject);
    }
}

