using UnityEngine;

// 코인(지오) 픽업 아이템. 플레이어가 접촉하면 GeoUI에 지오를 지급합니다.
public class GeoPickup : ItemPickupBase
{
    [SerializeField] private GeoItemDataSO data;

    private int _amount;
    private GeoUI _geoUI;

    protected override void Awake()
    {
        base.Awake();

        _geoUI = FindAnyObjectByType<GeoUI>();
        if (_geoUI == null)
            Debug.LogWarning("[GeoPickup] GeoUI를 찾을 수 없습니다.");

        // 스폰 시 수량 확정 (이후 SetAmount로 덮어쓸 수 있음)
        if (data != null)
            _amount = data.RollAmount();
    }

    // ItemDropper에서 직접 수량을 지정할 때 호출합니다.
    public void SetAmount(int amount) => _amount = amount;

    protected override void ApplyEffect()
    {
        if (_geoUI == null) return;
        _geoUI.AddGeo(_amount);
    }
}
