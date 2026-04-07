// 데미지를 받을 수 있는 객체의 인터페이스입니다. (ISP)
public interface IDamageable
{
    float MaxHealth { get; }
    float CurrentHealth { get; }
    bool IsDead { get; }
    void TakeDamage(float damage);
}
