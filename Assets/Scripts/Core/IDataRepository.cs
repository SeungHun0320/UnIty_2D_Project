// 저장 방식을 교체할 수 있도록 추상화한 인터페이스입니다. (DIP)
// 현재 구현체: JsonRepository / 향후: CloudRepository 등으로 교체 가능합니다.
public interface IDataRepository
{
    void     Save(SaveData data, int slot);
    SaveData Load(int slot);
    void     Delete(int slot);
    bool     Exists(int slot);
}
