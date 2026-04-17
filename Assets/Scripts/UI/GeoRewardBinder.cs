using UnityEngine;

// 적 처치 시 GeoUI에 보상을 전달하는 ViewModel입니다. (MVVM Binder)
// EnemyDeadEvent를 구독해 EnemyStats.geoReward를 읽고 GeoUI.AddGeo()를 호출합니다.
public class GeoRewardBinder : MonoBehaviour
{
    [SerializeField] private GeoUI geoUI;

    private void Awake()
    {
        if (geoUI == null)
            geoUI = FindAnyObjectByType<GeoUI>();
    }

    private void OnEnable()  => EventBus.Subscribe<EnemyDeadEvent>(OnEnemyDead);
    private void OnDisable() => EventBus.Unsubscribe<EnemyDeadEvent>(OnEnemyDead);

    private void OnEnemyDead(EnemyDeadEvent evt)
    {
        if (geoUI == null || evt.Enemy == null) return;
        var stats = evt.Enemy.GetComponent<EnemyStats>();
        if (stats == null || stats.geoRewardRange.y <= 0) return;
        geoUI.AddGeo(stats.RollGeoReward());
    }
}
