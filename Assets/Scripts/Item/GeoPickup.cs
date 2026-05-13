using UnityEngine;

// 코인(지오) 픽업 아이템. 플레이어가 접촉하면 GeoCollectedEvent를 발행해 GeoWallet에 지오를 지급합니다.
public class GeoPickup : ItemPickupBase
{
    [SerializeField] private GeoItemDataSO data;

    private int _amount;

    protected override void Awake()
    {
        base.Awake();

        // 스폰 시 수량 확정 (이후 SetAmount로 덮어쓸 수 있음)
        if (data != null)
            _amount = data.RollAmount();
    }

    // ItemDropper에서 직접 수량을 지정할 때 호출합니다.
    public void SetAmount(int amount) => _amount = amount;

    protected override void ApplyEffect()
    {
        EventBus.Publish(new GeoCollectedEvent(_amount));
    }
}
