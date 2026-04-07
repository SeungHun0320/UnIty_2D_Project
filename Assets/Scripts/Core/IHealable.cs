// 회복 가능한 객체의 인터페이스입니다. (ISP)
public interface IHealable
{
    void Heal(float amount);
    void ResetHealthToMax();
}
