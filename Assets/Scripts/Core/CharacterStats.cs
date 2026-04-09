using System;
using UnityEngine;

// 캐릭터(플레이어/몬스터) 공통 스탯 인터페이스입니다.
// IDamageable + IHealable을 조합해 공격력까지 포함합니다. (ISP)
public interface ICharacterStats : IDamageable, IHealable
{
    float AttackPower { get; }
}

// 캐릭터(플레이어/몬스터) 공통 스탯 베이스 클래스입니다.
// 체력/공격력 관리와 데미지/회복 로직만 책임집니다.
public abstract class CharacterStats : MonoBehaviour, ICharacterStats, IDamageable, IHealable
{
    [Header("Base Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float attackPower = 10f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackPower => attackPower;

    public bool IsDead => currentHealth <= 0f;

    // 뷰모델/기타 시스템에서 구독할 수 있는 체력 변경 이벤트입니다.
    public event Action<float, float> OnHealthChanged;

    // 사망 시 발생하는 이벤트입니다. EnemyDeathHandler 등 외부 컴포넌트에서 구독합니다.
    public event Action OnDeadEvent;

    // 초기 체력을 최대 체력 범위 안으로 보정합니다.
    protected virtual void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        RaiseHealthChanged();
    }

    // 데미지를 적용하는 기본 로직입니다.
    public virtual void TakeDamage(float damage)
    {
        if (damage <= 0f || IsDead) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        RaiseHealthChanged();
        if (IsDead) OnDead();
    }

    // 사망 시 서브클래스에서 처리를 정의합니다. (OCP)
    protected virtual void OnDead() { OnDeadEvent?.Invoke(); }

    // 회복 로직의 기본 구현입니다.
    public virtual void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        RaiseHealthChanged();
    }

    // 완전 회복용 헬퍼입니다.
    public virtual void ResetHealthToMax()
    {
        currentHealth = maxHealth;
        RaiseHealthChanged();
    }

    // 최대 체력을 증감시키기 위한 메서드입니다(디버그/레벨업 등에 사용할 수 있습니다).
    public virtual void AddMaxHealth(float amount)
    {
        if (Mathf.Approximately(amount, 0f)) return;

        maxHealth = Mathf.Max(1f, maxHealth + amount);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        RaiseHealthChanged();
    }

    // 체력 변경 시 이벤트를 발생시키는 헬퍼입니다.
    // 기존 OnHealthChanged(직접 구독)와 EventBus 둘 다 발행합니다.
    protected void RaiseHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        EventBus.Publish(new HealthChangedEvent(currentHealth, maxHealth, this is PlayerStats));
    }
}

