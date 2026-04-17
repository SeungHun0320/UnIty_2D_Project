using UnityEngine;

// PlayerStats ↔ UI를 연결하는 ViewModel 베이스 클래스입니다. (Template Method)
// PlayerHealthMaskBinder / PlayerSoulBinder 등 모든 플레이어 UI 바인더가 이를 상속합니다.
// 공통 lifecycle(Subscribe/Unsubscribe/초기동기화)과 PlayerStats 자동 탐색을 제공합니다.
public abstract class PlayerStatsBinder : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected PlayerStats playerStats;

    protected virtual void Awake()  => EnsureStats();

    private void OnEnable()
    {
        EnsureStats();
        Subscribe();
        SyncInitial();
    }

    private void OnDisable() => Unsubscribe();

    // PlayerStats 참조가 없으면 씬에서 자동으로 탐색합니다.
    protected void EnsureStats()
    {
        if (playerStats == null)
            playerStats = FindAnyObjectByType<PlayerStats>();
    }

    // Model 이벤트에 구독합니다. OnEnable 시 호출됩니다.
    protected abstract void Subscribe();

    // Model 이벤트 구독을 해제합니다. OnDisable 시 호출됩니다.
    protected abstract void Unsubscribe();

    // OnEnable 시 현재 Model 상태를 View에 1회 동기화합니다.
    protected abstract void SyncInitial();
}
