using UnityEngine;

// 플레이어 소울 데이터를 SoulUI에 연동하는 ViewModel입니다. (PlayerStatsBinder 상속)
// PlayerStats.OnSoulChanged를 구독해 SoulUI.SetSoul/SetMaxSoul을 호출합니다.
public class PlayerSoulBinder : PlayerStatsBinder
{
    [Header("View")]
    [SerializeField] private SoulUI soulUI;

    protected override void Awake()
    {
        base.Awake();
        if (soulUI == null)
            soulUI = FindAnyObjectByType<SoulUI>();
    }

    protected override void Subscribe()
    {
        if (playerStats != null)
            playerStats.OnSoulChanged += HandleSoulChanged;
    }

    protected override void Unsubscribe()
    {
        if (playerStats != null)
            playerStats.OnSoulChanged -= HandleSoulChanged;
    }

    protected override void SyncInitial()
    {
        if (playerStats != null)
            HandleSoulChanged(playerStats.CurrentSoul, playerStats.MaxSoul);
    }

    // 소울이 변경될 때마다 SoulUI를 갱신합니다.
    private void HandleSoulChanged(int currentSoul, int maxSoul)
    {
        if (soulUI == null) return;
        soulUI.SetMaxSoul(maxSoul);
        soulUI.SetSoul(currentSoul);
    }
}
