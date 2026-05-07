using UnityEngine;

// 게임 전체의 단일 DDOL 진입점 — Facade 패턴.
// 외부 코드는 반드시 GameInstance.Instance.XXX() 로만 매니저 기능에 접근합니다.
[DefaultExecutionOrder(-200)]
public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }

    private GameManager    _game;
    private UIManager      _ui;
    private SaveManager    _save;
    private HitstopManager _hitstop;

    // SaveManager 상수를 Facade로 재노출합니다.
    public const int AutoSlot        = 0;
    public const int ManualSlotCount = 3;
    public const int TotalSlots      = 4;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _game    = GetComponentInChildren<GameManager>(true);
        _ui      = GetComponentInChildren<UIManager>(true);
        _save    = GetComponentInChildren<SaveManager>(true);
        _hitstop = GetComponentInChildren<HitstopManager>(true);

        if (_game    == null) Debug.LogError("[GameInstance] GameManager를 찾지 못했습니다.");
        if (_ui      == null) Debug.LogError("[GameInstance] UIManager를 찾지 못했습니다.");
        if (_save    == null) Debug.LogError("[GameInstance] SaveManager를 찾지 못했습니다.");
        if (_hitstop == null) Debug.LogError("[GameInstance] HitstopManager를 찾지 못했습니다.");
    }

    // ── GameManager 위임 ─────────────────────────────────────────────────────

    public GameState       CurrentGameState => _game != null ? _game.CurrentGameState : GameState.Initializing;
    public ICharacterStats Player           => _game?.Player;
    public float           Playtime         => _game?.Playtime ?? 0f;

    public void RegisterStage(StageContext ctx)  => _game?.RegisterStage(ctx);
    public void LoadScene(string sceneName)      => _game?.LoadScene(sceneName);
    public void LoadNextStage()                  => _game?.LoadNextStage();
    public void LoadPreviousStage()              => _game?.LoadPreviousStage();
    public void GoToTitle()                      => _game?.GoToTitle();
    public void StartNewGame()                   => _game?.StartNewGame();
    public void SetPlaytime(float t)             => _game?.SetPlaytime(t);
    public void SetGameState(GameState s)        => _game?.SetGameState(s);
    public void RespawnPlayer()                  => _game?.RespawnPlayer();
    public void OnStageClear()                   => _game?.OnStageClear();
    public void OnPlayerDead()                   => _game?.OnPlayerDead();
    public void OnEnemyDead(GameObject e = null) => _game?.OnEnemyDead(e);
    public void Pause()                          => _game?.Pause();
    public void Resume()                         => _game?.Resume();
    public void TogglePause()                    => _game?.TogglePause();
    public void QuitGame()                       => _game?.QuitGame();

    // ── UIManager 위임 ───────────────────────────────────────────────────────

    public void ShowPanel<T>() where T : BasePanel => _ui?.Show<T>();
    public void HidePanel<T>() where T : BasePanel => _ui?.Hide<T>();
    public T    GetPanel<T>()  where T : BasePanel => _ui != null ? _ui.Get<T>() : null;
    public void HideAllPanels()                    => _ui?.HideAllPanels();

    // ── SaveManager 위임 ─────────────────────────────────────────────────────

    public void     Save(int slot)         => _save?.Save(slot);
    public void     AutoSave(string scene) => _save?.AutoSave(scene);
    public bool     LoadGame(int slot)     => _save != null && _save.LoadGame(slot);
    public SlotInfo GetSlotInfo(int slot)  => _save?.GetSlotInfo(slot) ?? new SlotInfo { isEmpty = true };
    public void     DeleteSlot(int slot)   => _save?.DeleteSlot(slot);

    // ── HitstopManager 위임 ──────────────────────────────────────────────────

    public void TriggerHitstop(float duration) => _hitstop?.TriggerHitstop(duration);
}
