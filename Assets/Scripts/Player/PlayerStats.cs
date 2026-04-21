using System;
using UnityEngine;

// 플레이어 전용 스탯 컴포넌트입니다. (SRP: 스탯 관리만 담당)
// 씬 전환 시 데이터 유지를 위해 DDOL 싱글톤으로 동작합니다.
public class PlayerStats : CharacterStats
{
    // 씬 전환 후 중복 인스턴스를 제거하기 위한 정적 참조입니다.
    private static PlayerStats _instance;
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
        // 이미 DDOL 인스턴스가 있으면 중복 제거합니다. (씬에 Player 오브젝트가 있을 경우 대비)
        if (_instance != null && _instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        base.Awake();
        currentSoul = Mathf.Clamp(currentSoul, 0, maxSoul);
        RaiseSoulChanged();
    }

    private void OnEnable()  => EventBus.Subscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    private void OnDisable() => EventBus.Unsubscribe<EnemyHitByPlayerEvent>(OnEnemyHit);

    // 근접 공격 적중 시에만 소울을 충전합니다. (투사체 적중 제외)
    private void OnEnemyHit(EnemyHitByPlayerEvent evt) { if (evt.IsMelee) AddSoul(soulPerHit); }

    // 세이브 복원 등 직접 소울을 지정할 때 사용합니다.
    public void SetSoul(int amount)
    {
        currentSoul = Mathf.Clamp(amount, 0, maxSoul);
        RaiseSoulChanged();
    }

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
