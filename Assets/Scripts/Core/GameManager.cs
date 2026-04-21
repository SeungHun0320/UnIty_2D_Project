using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager는 모든 스크립트보다 먼저 Awake/Start가 실행되어야 합니다.
// -100으로 설정해 플레이어/적 컴포넌트보다 우선 초기화됩니다.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // DIP: 구체 클래스 대신 인터페이스로 참조 (씬 로드 후 OnSceneLoaded에서 설정)
    private ICharacterStats _playerStats;
    private ICharacterStats _enemyStats;
    private StageContext    _stageContext;
    private Transform       _playerTransform;

    // 씬 전환 방향 플래그 — true면 goalPoint에서 스폰합니다.
    private bool _enterFromGoal;
    private bool _isLoadingScene;

    private GameState _currentGameState = GameState.Initializing;

    public GameState CurrentGameState => _currentGameState;

    public ICharacterStats Player => _playerStats;
    public ICharacterStats Enemy  => _enemyStats;

    public event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 씬이 완전히 로드된 뒤(모든 Awake 완료 후) 호출됩니다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;

        // Bootstrap 씬 자체가 로드될 때는 게임 초기화를 수행하지 않습니다.
        if (scene.name == "Bootstrap") return;

        // 플레이어 참조 초기화 (Bootstrap에서 DDOL로 넘어오므로 최초 1회만 실행)
        if (_playerStats == null)
        {
            var ps = FindAnyObjectByType<PlayerStats>();
            if (ps != null)
            {
                _playerStats     = ps;
                _playerTransform = ps.transform.root;
            }
            else
            {
                Debug.LogWarning("[GameManager] PlayerStats를 씬에서 찾지 못했습니다.");
            }
        }

        if (_stageContext == null) return;

        // 진입 방향에 따라 스폰 위치를 결정합니다.
        Transform spawnPoint = _enterFromGoal ? _stageContext.goalPoint : _stageContext.startPoint;

        if (spawnPoint == null)
        {
            Debug.LogWarning("[GameManager] 스폰 포인트가 StageContext에 설정되지 않았습니다.");
            return;
        }

        if (_playerTransform == null) return;
        _playerTransform.position = spawnPoint.position;

        _enterFromGoal = false;
        Time.timeScale = 1f;
        SetGameState(GameState.Playing);
        EventBus.Publish(new StageLoadedEvent(_stageContext));
    }

    private void OnEnable()  => EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDeadEvent);
    private void OnDisable() => EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDeadEvent);

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus.Clear();
        }
    }

    // ── 스테이지 등록 ────────────────────────────────────────────────────────

    /// <summary> StageContext.Awake()에서 호출됩니다. 씬 참조를 갱신합니다. </summary>
    public void RegisterStage(StageContext ctx)
    {
        if (ctx == null) return;
        _stageContext = ctx;
    }

    // ── 씬 전환 ──────────────────────────────────────────────────────────────

    /// <summary> 다음 스테이지로 이동합니다. nextSceneName이 비어있으면 무시합니다. </summary>
    public void LoadNextStage()
    {
        if (_isLoadingScene) return;
        if (_stageContext == null || string.IsNullOrEmpty(_stageContext.nextSceneName))
        {
            Debug.LogWarning("[GameManager] 다음 씬이 설정되지 않았습니다.");
            return;
        }
        _enterFromGoal  = false;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.nextSceneName));
    }

    /// <summary> 이전 스테이지로 이동합니다. previousSceneName이 비어있으면 무시합니다. </summary>
    public void LoadPreviousStage()
    {
        if (_isLoadingScene) return;
        if (_stageContext == null || string.IsNullOrEmpty(_stageContext.previousSceneName))
        {
            Debug.LogWarning("[GameManager] 이전 씬이 설정되지 않았습니다.");
            return;
        }
        _enterFromGoal  = true;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.previousSceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName);
    }

    // ── 게임 상태 ─────────────────────────────────────────────────────────────

    /// <summary> 게임 상태를 변경하고 이벤트를 발생시킵니다. </summary>
    public void SetGameState(GameState newState)
    {
        if (_currentGameState == newState) return;
        _currentGameState = newState;
        OnGameStateChanged?.Invoke(_currentGameState);
        EventBus.Publish(new GameStateChangedEvent(_currentGameState));
        Debug.Log($"[GameManager] Game State Changed: {_currentGameState}");
    }

    private void OnPlayerDeadEvent(PlayerDeadEvent _) { }

    /// <summary> 플레이어를 startPoint로 이동시키고 체력을 회복합니다. </summary>
    public void RespawnPlayer()
    {
        if (_playerTransform == null || _stageContext?.startPoint == null)
        {
            Debug.LogWarning("[GameManager] RespawnPlayer: playerTransform 또는 startPoint가 없습니다.");
            return;
        }

        (_playerStats as CharacterStats)?.ResetHealthToMax();
        _playerTransform.position = _stageContext.startPoint.position;
        SetGameState(GameState.Playing);
        EventBus.Publish(new PlayerRespawnEvent(_stageContext.startPoint.position));
        Debug.Log("[GameManager] Player respawned at StartPoint.");
    }

    /// <summary> 스테이지를 재시작합니다. 플레이어 리셋 + 적 재생성. </summary>
    public void RestartStage()
    {
        if (_currentGameState != GameState.StageClear
         && _currentGameState != GameState.Paused) return;

        Time.timeScale = 1f;
        RespawnPlayer();
        EventBus.Publish(new StageRestartEvent());
        Debug.Log("[GameManager] Stage Restarted.");
    }

    /// <summary> 스테이지 클리어 처리. 다음 씬이 있으면 즉시 전환, 없으면 StageClear 패널을 표시합니다. </summary>
    public void OnStageClear()
    {
        if (_currentGameState == GameState.StageClear) return;
        SetGameState(GameState.StageClear);

        bool hasNextScene = _stageContext != null && !string.IsNullOrEmpty(_stageContext.nextSceneName);

        if (hasNextScene)
        {
            // 중간 스테이지: 패널 없이 바로 다음 씬으로 전환
            StartCoroutine(AutoLoadNextStage());
        }
        else
        {
            // 마지막 스테이지: StageClear 패널 표시
            Time.timeScale = 0f;
            EventBus.Publish(new StageClearEvent());
        }

        Debug.Log("[GameManager] Stage Clear!");
    }

    // 1초 대기 후 다음 씬으로 자동 전환합니다.
    private IEnumerator AutoLoadNextStage()
    {
        yield return new WaitForSeconds(1f);
        LoadNextStage();
    }

    /// <summary> 플레이어 사망 처리. </summary>
    public void OnPlayerDead()
    {
        SetGameState(GameState.GameOver);
        EventBus.Publish(new PlayerDeadEvent());
        Debug.Log("[GameManager] Player is dead. Game Over!");
    }

    /// <summary> 적 사망 처리. </summary>
    public void OnEnemyDead(GameObject enemy = null)
    {
        EventBus.Publish(new EnemyDeadEvent(enemy));
        Debug.Log("[GameManager] Enemy is dead!");
    }

    /// <summary> 게임 일시정지. </summary>
    public void Pause()
    {
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
        EventBus.Publish(new GamePausedEvent());
    }

    /// <summary> 게임 재개. </summary>
    public void Resume()
    {
        if (_currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            Time.timeScale = 1f;
            EventBus.Publish(new GameResumedEvent());
        }
    }

    /// <summary> 현재 상태에 따라 일시정지/재개를 토글합니다. </summary>
    public void TogglePause()
    {
        if (_currentGameState == GameState.Paused)
            Resume();
        else if (_currentGameState == GameState.Playing)
            Pause();
    }

    /// <summary> 게임을 종료합니다. 에디터에서는 플레이 모드를 종료합니다. </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

public enum GameState
{
    Initializing,
    Playing,
    Paused,
    GameOver,
    StageClear
}
