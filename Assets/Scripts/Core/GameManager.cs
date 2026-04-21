using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// GameManager는 모든 스크립트보다 먼저 Awake/Start가 실행되어야 합니다.
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [Tooltip("Bootstrap → 게임 씬 최초 진입 시 동적으로 생성할 Player 프리팹입니다.")]
    [SerializeField] private GameObject playerPrefab;

    // Bootstrap·Title은 게임 씬이 아니므로 OnSceneLoaded에서 건너뜁니다.
    private static readonly HashSet<string> _nonGameScenes = new() { "Bootstrap", "Title" };

    private ICharacterStats _playerStats;
    private StageContext    _stageContext;
    private Transform       _playerTransform;
    private PlayerMover     _playerMover;

    private bool  _enterFromGoal;
    private bool  _isLoadingScene;
    private float _playtime;

    private GameState _currentGameState = GameState.Initializing;

    public GameState CurrentGameState => _currentGameState;
    public ICharacterStats Player    => _playerStats;
    public float Playtime            => _playtime;

    public event System.Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        // 실제 플레이 중일 때만 누적합니다. (Pause·StageClear 제외)
        if (_currentGameState == GameState.Playing)
            _playtime += Time.unscaledDeltaTime;
    }

    // 씬이 완전히 로드된 뒤(모든 Awake 완료 후) 호출됩니다.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;

        // Bootstrap·Title은 게임 초기화를 수행하지 않습니다.
        if (_nonGameScenes.Contains(scene.name)) return;

        // _playerTransform(Unity Object)이 null이면 파괴됐거나 미생성 → 재생성합니다.
        // _playerStats는 interface 타입이라 파괴 후에도 null이 아닐 수 있으므로 Transform으로 판단합니다.
        if (_playerTransform == null)
        {
            _playerStats = null;
            _playerMover = null;
            if (playerPrefab != null)
            {
                var go = Instantiate(playerPrefab);
                var ps = go.GetComponentInChildren<PlayerStats>(true);
                if (ps != null)
                {
                    _playerStats     = ps;
                    _playerTransform = go.transform;
                    _playerMover     = go.GetComponentInChildren<PlayerMover>(true);
                }
                else Debug.LogWarning("[GameManager] Player 프리팹에서 PlayerStats를 찾지 못했습니다.");
            }
            else Debug.LogWarning("[GameManager] playerPrefab이 설정되지 않았습니다.");
        }

        if (_stageContext == null)
        {
            Debug.LogWarning("[GameManager] StageContext가 등록되지 않았습니다. 스폰 위치를 설정할 수 없습니다.");
            return;
        }

        // 진입 방향에 따라 스폰 위치를 결정합니다.
        Transform spawnPoint = _enterFromGoal ? _stageContext.goalPoint : _stageContext.startPoint;
        if (spawnPoint == null)
        {
            Debug.LogWarning("[GameManager] 스폰 포인트가 StageContext에 설정되지 않았습니다.");
            return;
        }

        if (_playerTransform == null) return;

        // Kinematic Rigidbody2D는 rb.position도 함께 설정해야 위치가 올바르게 반영됩니다.
        if (_playerMover != null)
            _playerMover.Teleport(spawnPoint.position);
        else
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

    /// <summary> StageContext.Awake()에서 호출됩니다. </summary>
    public void RegisterStage(StageContext ctx)
    {
        if (ctx == null) return;
        _stageContext = ctx;
    }

    // ── 씬 전환 ──────────────────────────────────────────────────────────────

    /// <summary> 지정한 씬을 로드합니다. SaveManager.LoadGame() 등에서 호출합니다. </summary>
    public void LoadScene(string sceneName)
    {
        if (_isLoadingScene) return;
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("[GameManager] 씬 이름이 비어있습니다."); return; }
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary> 다음 스테이지로 이동합니다. 이동 전 AutoSave를 수행합니다. </summary>
    public void LoadNextStage()
    {
        if (_isLoadingScene) return;
        if (_stageContext == null || string.IsNullOrEmpty(_stageContext.nextSceneName))
        {
            Debug.LogWarning("[GameManager] 다음 씬이 설정되지 않았습니다.");
            return;
        }
        SaveManager.Instance?.AutoSave(_stageContext.nextSceneName);
        _enterFromGoal  = false;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.nextSceneName));
    }

    /// <summary> 이전 스테이지로 이동합니다. 이동 전 AutoSave를 수행합니다. </summary>
    public void LoadPreviousStage()
    {
        if (_isLoadingScene) return;
        if (_stageContext == null || string.IsNullOrEmpty(_stageContext.previousSceneName))
        {
            Debug.LogWarning("[GameManager] 이전 씬이 설정되지 않았습니다.");
            return;
        }
        SaveManager.Instance?.AutoSave(_stageContext.previousSceneName);
        _enterFromGoal  = true;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.previousSceneName));
    }

    /// <summary> Title 씬으로 돌아갑니다. </summary>
    public void GoToTitle()
    {
        if (_isLoadingScene) return;
        Time.timeScale  = 1f;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine("Title"));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        yield return SceneManager.LoadSceneAsync(sceneName);
    }

    // ── 게임 시작 / 불러오기 ─────────────────────────────────────────────────

    /// <summary> 새 게임을 시작합니다. 플레이타임을 초기화하고 Stage1을 로드합니다. </summary>
    public void StartNewGame()
    {
        _playtime = 0f;
        LoadScene("Stage1");
    }

    /// <summary> 플레이타임을 직접 설정합니다. (불러오기 시 복원) </summary>
    public void SetPlaytime(float t) => _playtime = Mathf.Max(0f, t);

    // ── 게임 상태 ─────────────────────────────────────────────────────────────

    public void SetGameState(GameState newState)
    {
        if (_currentGameState == newState) return;
        _currentGameState = newState;
        OnGameStateChanged?.Invoke(_currentGameState);
        EventBus.Publish(new GameStateChangedEvent(_currentGameState));
        Debug.Log($"[GameManager] GameState → {_currentGameState}");
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
        if (_playerMover != null)
            _playerMover.Teleport(_stageContext.startPoint.position);
        else
            _playerTransform.position = _stageContext.startPoint.position;
        SetGameState(GameState.Playing);
        EventBus.Publish(new PlayerRespawnEvent(_stageContext.startPoint.position));
    }

    /// <summary> 스테이지를 재시작합니다. (마지막 스테이지 클리어 패널에서 사용) </summary>
    public void RestartStage()
    {
        if (_currentGameState != GameState.StageClear
         && _currentGameState != GameState.Paused) return;
        Time.timeScale = 1f;
        RespawnPlayer();
        EventBus.Publish(new StageRestartEvent());
    }

    /// <summary> 스테이지 클리어 처리. 다음 씬이 있으면 즉시 전환, 없으면 StageClear 패널 표시. </summary>
    public void OnStageClear()
    {
        if (_currentGameState == GameState.StageClear) return;
        SetGameState(GameState.StageClear);

        bool hasNextScene = _stageContext != null && !string.IsNullOrEmpty(_stageContext.nextSceneName);
        if (hasNextScene)
        {
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
    }

    /// <summary> 적 사망 처리. </summary>
    public void OnEnemyDead(GameObject enemy = null) =>
        EventBus.Publish(new EnemyDeadEvent(enemy));

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
        if (_currentGameState != GameState.Paused) return;
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        EventBus.Publish(new GameResumedEvent());
    }

    /// <summary> 현재 상태에 따라 일시정지/재개를 토글합니다. </summary>
    public void TogglePause()
    {
        if (_currentGameState == GameState.Paused)  Resume();
        else if (_currentGameState == GameState.Playing) Pause();
    }

    /// <summary> 게임을 종료합니다. </summary>
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
