using System.Collections;
using UnityEngine;

// 게임 전체의 단일 DDOL 진입점 — Facade 패턴.
// 외부 코드는 반드시 GameInstance.Instance.XXX() 로만 매니저 기능에 접근합니다.
// RespawnPlayer·OnStageClear처럼 여러 컴포넌트가 협력하는 조작은 이 클래스에서 직접 조율합니다.
[DefaultExecutionOrder(-200)]
public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }

    private GameManager            _game;
    private UIManager              _ui;
    private SaveManager            _save;
    private HitstopManager         _hitstop;
    private PlayerSpawner          _spawner;
    private SceneTransitionManager _scene;

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
        _spawner = GetComponentInChildren<PlayerSpawner>(true);
        _scene   = GetComponentInChildren<SceneTransitionManager>(true);

        if (_game    == null) Debug.LogError("[GameInstance] GameManager를 찾지 못했습니다.");
        if (_ui      == null) Debug.LogError("[GameInstance] UIManager를 찾지 못했습니다.");
        if (_save    == null) Debug.LogError("[GameInstance] SaveManager를 찾지 못했습니다.");
        if (_hitstop == null) Debug.LogError("[GameInstance] HitstopManager를 찾지 못했습니다.");
        if (_spawner == null) Debug.LogError("[GameInstance] PlayerSpawner를 찾지 못했습니다.");
        if (_scene   == null) Debug.LogError("[GameInstance] SceneTransitionManager를 찾지 못했습니다.");

        // SceneTransitionManager에 의존 컴포넌트를 주입합니다.
        _scene?.Init(_game, _spawner, _save);
    }

    // ── GameManager 위임 ─────────────────────────────────────────────────────

    public GameState       CurrentGameState => _game != null ? _game.CurrentGameState : GameState.Initializing;
    public ICharacterStats Player           => _spawner?.Player;
    public float           Playtime         => _game?.Playtime ?? 0f;

    public void SetGameState(GameState s) => _game?.SetGameState(s);
    public void SetPlaytime(float t)      => _game?.SetPlaytime(t);
    public void OnPlayerDead()            => _game?.OnPlayerDead();
    public void Pause()                   => _game?.Pause();
    public void Resume()                  => _game?.Resume();
    public void TogglePause()             => _game?.TogglePause();
    public void QuitGame()                => _game?.QuitGame();

    // ── SceneTransitionManager 위임 ──────────────────────────────────────────

    public void RegisterStage(StageContext ctx) => _scene?.RegisterStage(ctx);
    public void LoadScene(string sceneName)     => _scene?.LoadScene(sceneName);
    public void LoadNextStage()                 => _scene?.LoadNextStage();
    public void LoadPreviousStage()             => _scene?.LoadPreviousStage();

    public void GoToTitle()
    {
        _ui?.HideAllPanels();
        _scene?.GoToTitle();
    }

    public void StartNewGame()
    {
        _game?.SetPlaytime(0f);
        _scene?.LoadScene("Stage1");
    }

    // ── 복합 조작 (여러 컴포넌트 협력) ──────────────────────────────────────

    public void RespawnPlayer()
    {
        var startPoint = _scene?.StartPoint;
        if (_spawner == null || startPoint == null)
        {
            Debug.LogWarning("[GameInstance] RespawnPlayer: PlayerSpawner 또는 startPoint가 없습니다.");
            return;
        }
        (_spawner.Player as CharacterStats)?.ResetHealthToMax();
        _spawner.Teleport(startPoint.position);
        _game?.SetGameState(GameState.Playing);
        EventBus.Publish(new StageRestartEvent());
        EventBus.Publish(new PlayerRespawnEvent(startPoint.position));
    }

    public void OnStageClear()
    {
        if (_game?.CurrentGameState == GameState.StageClear) return;
        _game?.SetGameState(GameState.StageClear);
        Debug.Log("[GameInstance] Stage Clear!");

        if (!string.IsNullOrEmpty(_scene?.NextSceneName))
            StartCoroutine(AutoLoadNextStage());
        else
        {
            Time.timeScale = 0f;
            EventBus.Publish(new StageClearEvent());
        }
    }

    private IEnumerator AutoLoadNextStage()
    {
        yield return new WaitForSeconds(1f);
        LoadNextStage();
    }

    // ── UIManager 위임 ───────────────────────────────────────────────────────

    public void ShowPanel<T>() where T : BasePanel => _ui?.Show<T>();
    public void HidePanel<T>() where T : BasePanel => _ui?.Hide<T>();
    public T    GetPanel<T>()  where T : BasePanel => _ui != null ? _ui.Get<T>() : null;
    public void HideAllPanels()                    => _ui?.HideAllPanels();

    // ── SaveManager 위임 ─────────────────────────────────────────────────────

    public void     Save(int slot)                        => _save?.Save(slot);
    public bool     LoadGame(int slot)                    => _save != null && _save.LoadGame(slot);
    public SlotInfo GetSlotInfo(int slot)                 => _save?.GetSlotInfo(slot) ?? new SlotInfo { isEmpty = true };
    public void     DeleteSlot(int slot)                  => _save?.DeleteSlot(slot);
    public void     RegisterGeoWallet(GeoWallet wallet)   => _save?.RegisterGeoWallet(wallet);
    public void     UnregisterGeoWallet(GeoWallet wallet) => _save?.UnregisterGeoWallet(wallet);

    // ── HitstopManager 위임 ──────────────────────────────────────────────────

    public void TriggerHitstop(float duration) => _hitstop?.TriggerHitstop(duration);
}
