using UnityEngine;

// 저장/불러오기를 총괄하는 DDOL 싱글톤입니다.
// Repository 패턴으로 저장 방식을 교체 가능합니다. (DIP)
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // 슬롯 0 = AUTO(자동저장), 슬롯 1~3 = 수동저장
    public const int AutoSlot        = 0;
    public const int ManualSlotCount = 3;
    public const int TotalSlots      = 4;

    private IDataRepository _repository;
    private SaveData        _pendingRestore;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _repository = new JsonRepository();
    }

    private void OnEnable()  => EventBus.Subscribe<StageLoadedEvent>(OnStageLoaded);
    private void OnDisable() => EventBus.Unsubscribe<StageLoadedEvent>(OnStageLoaded);

    // StageLoadedEvent 이후 복원 → HUD 바인더가 이미 구독된 상태이므로 이벤트로 UI 자동 갱신됩니다.
    private void OnStageLoaded(StageLoadedEvent _)
    {
        if (_pendingRestore == null) return;
        ApplyRestore(_pendingRestore);
        _pendingRestore = null;
    }

    private void ApplyRestore(SaveData data)
    {
        var player = GameManager.Instance?.Player as CharacterStats;
        if (player != null)
        {
            player.SetHealth(data.currentHealth);
            (player as PlayerStats)?.SetSoul(data.currentSoul);
        }

        var geoUI = FindAnyObjectByType<GeoUI>();
        geoUI?.SetGeo(data.geoAmount);

        GameManager.Instance?.SetPlaytime(data.playtime);
    }

    // ── 저장 ─────────────────────────────────────────────────────────────────

    /// <summary> 현재 씬 기준으로 슬롯에 저장합니다. </summary>
    public void Save(int slot)
    {
        var data = BuildSaveData(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        if (data == null) return;
        _repository.Save(data, slot);
        Debug.Log($"[SaveManager] Slot {slot} 저장 완료 ({data.savedAt})");
    }

    /// <summary> 다음 씬 이름으로 AUTO 슬롯(0)에 자동 저장합니다. </summary>
    public void AutoSave(string nextSceneName)
    {
        var data = BuildSaveData(nextSceneName);
        if (data == null) return;
        _repository.Save(data, AutoSlot);
        Debug.Log($"[SaveManager] AutoSave → {nextSceneName}");
    }

    private SaveData BuildSaveData(string sceneName)
    {
        var gm = GameManager.Instance;
        if (gm == null) return null;

        var player      = gm.Player as CharacterStats;
        var playerStats = player as PlayerStats;
        var geoUI       = FindAnyObjectByType<GeoUI>();

        return new SaveData
        {
            sceneName     = sceneName,
            currentHealth = player?.CurrentHealth ?? 0f,
            maxHealth     = player?.MaxHealth     ?? 0f,
            currentSoul   = playerStats?.CurrentSoul ?? 0,
            geoAmount     = geoUI?.GetGeo() ?? 0,
            savedAt       = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            playtime      = gm.Playtime,
        };
    }

    // ── 불러오기 ──────────────────────────────────────────────────────────────

    /// <summary> 슬롯에서 불러옵니다. 씬 로드 후 StageLoadedEvent에서 상태를 복원합니다. </summary>
    public bool LoadGame(int slot)
    {
        var data = _repository.Load(slot);
        if (data == null)
        {
            Debug.LogWarning($"[SaveManager] Slot {slot} 저장 데이터 없음");
            return false;
        }
        _pendingRestore = data;
        GameManager.Instance?.LoadScene(data.sceneName);
        return true;
    }

    // ── 슬롯 정보 ─────────────────────────────────────────────────────────────

    /// <summary> Title 슬롯 버튼 미리보기용 정보를 반환합니다. </summary>
    public SlotInfo GetSlotInfo(int slot)
    {
        if (!_repository.Exists(slot)) return new SlotInfo { isEmpty = true };
        var data = _repository.Load(slot);
        return new SlotInfo
        {
            isEmpty   = false,
            sceneName = data.sceneName,
            savedAt   = data.savedAt,
            playtime  = data.playtime,
        };
    }

    public void DeleteSlot(int slot)
    {
        _repository.Delete(slot);
        Debug.Log($"[SaveManager] Slot {slot} 삭제");
    }
}
