using System;
using UnityEngine;

// 플레이어 지오(골드) 소지량을 관리하는 Model입니다.
// GeoCollectedEvent(EventBus) → AddGeo(), SaveManager → SetGeo/GetGeo(), GeoRewardBinder → 이벤트 구독 흐름으로 사용합니다.
public class GeoWallet : MonoBehaviour
{
    public event Action<int> OnGeoAdded;  // 픽업으로 증가한 delta 값
    public event Action<int> OnGeoSet;    // 세이브 로드 등 전체 재설정 후 total 값

    private int _currentGeo;

    public int CurrentGeo => _currentGeo;

    private void OnEnable()
    {
        EventBus.Subscribe<GeoCollectedEvent>(OnGeoCollected);
        GameInstance.Instance?.RegisterGeoWallet(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GeoCollectedEvent>(OnGeoCollected);
        GameInstance.Instance?.UnregisterGeoWallet(this);
    }

    private void OnGeoCollected(GeoCollectedEvent e) => AddGeo(e.Amount);

    /// <summary> 지오를 획득합니다. GeoUI에서 대기 텍스트 애니메이션이 재생됩니다. </summary>
    public void AddGeo(int amount)
    {
        if (amount <= 0) return;
        _currentGeo += amount;
        OnGeoAdded?.Invoke(amount);
    }

    /// <summary> 지오를 직접 설정합니다. 세이브 로드 시 호출됩니다. </summary>
    public void SetGeo(int amount)
    {
        _currentGeo = Mathf.Max(0, amount);
        OnGeoSet?.Invoke(_currentGeo);
    }

    public int GetGeo() => _currentGeo;
}
