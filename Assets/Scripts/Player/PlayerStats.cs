using System;
using UnityEngine;

// 플레이어 전용 스탯 컴포넌트입니다. (SRP: 스탯 관리만 담당)
// 공통 스탯(CharacterStats)을 상속받아 Soul 등 플레이어 전용 확장을 제공합니다.
public class PlayerStats : CharacterStats
{
    [Header("Player Only")]
    [Tooltip("플레이어 전용 추가 공격력(버프 등)에 사용합니다.")]
    [SerializeField] private float bonusAttackPower = 0f;

    [Header("Soul")]
    [SerializeField, Min(1)] private int maxSoul = 99;
    [SerializeField, Min(0)] private int currentSoul = 0;
    [Tooltip("적 히트 1회당 충전되는 소울 량입니다.")]
    [SerializeField, Min(0)] private int soulPerHit = 5;

    // 총 공격력을 계산할 때 사용할 수 있는 프로퍼티입니다.
    public float TotalAttackPower => AttackPower + bonusAttackPower;

    public int CurrentSoul => currentSoul;
    public int MaxSoul     => maxSoul;

    // PlayerSoulBinder(ViewModel)가 구독합니다.
    public event Action<int, int> OnSoulChanged;

    protected override void Awake()
    {
        base.Awake();
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
        RaiseSoulChanged();
    }

    private void OnEnable()  => EventBus.Subscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    private void OnDisable() => EventBus.Unsubscribe<EnemyHitByPlayerEvent>(OnEnemyHit);

    // 적 피격 이벤트 수신 시 soulPerHit만큼 소울을 충전합니다.
    private void OnEnemyHit(EnemyHitByPlayerEvent _) => AddSoul(soulPerHit);

    // 소울을 소모합니다. 잔량이 부족하면 false를 반환합니다.
    public bool UseSoul(int amount)
    {
        if (amount <= 0) return true;
        if (currentSoul < amount) return false;

        currentSoul -= amount;
        RaiseSoulChanged();
        return true;
    }

    // 소울을 충전합니다. (적 처치, 힐 스킬 등에서 호출)
    public void AddSoul(int amount)
    {
        if (amount <= 0) return;
        currentSoul = Mathf.Clamp(currentSoul + amount, 0, maxSoul);
        RaiseSoulChanged();
    }

    // 사망 시 GameManager에 알려 게임 오버를 처리합니다.
    protected override void OnDead() => GameManager.Instance?.OnPlayerDead();

    private void RaiseSoulChanged()
    {
        OnSoulChanged?.Invoke(currentSoul, maxSoul);
    }
}
