using UnityEngine;

// 플레이어 소울 데이터를 SoulUI에 연동하는 ViewModel입니다. (PlayerStatsBinder 상속)
// PlayerStats.OnSoulChanged를 구독해 검증 후 SoulUI.SetSoul/SetMaxSoul을 호출합니다.
public class PlayerSoulBinder : PlayerStatsBinder
{
    [Header("View")]
    [SerializeField] private SoulUI soulUI;

    private int _lastSoul    = -1;
    private int _lastMaxSoul = -1;

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
        // 씬 재로드 시 캐시를 리셋해 강제 동기화합니다.
        _lastSoul = _lastMaxSoul = -1;
        if (playerStats != null)
            HandleSoulChanged(playerStats.CurrentSoul, playerStats.MaxSoul);
    }

    // 소울이 변경될 때마다 검증 후 SoulUI를 갱신합니다.
    private void HandleSoulChanged(int currentSoul, int maxSoul)
    {
        if (soulUI == null) return;

        int validMax  = Mathf.Max(1, maxSoul);
        int validSoul = Mathf.Clamp(currentSoul, 0, validMax);

        if (validMax != _lastMaxSoul)
        {
            soulUI.SetMaxSoul(validMax);
            _lastMaxSoul = validMax;
        }

        if (validSoul != _lastSoul)
        {
            soulUI.SetSoul(validSoul);
            _lastSoul = validSoul;
        }
    }
}
