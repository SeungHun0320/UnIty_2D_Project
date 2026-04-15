using System;
using UnityEngine;

/// <summary>
/// 게임 전체 생명주기를 관리하는 싱글톤입니다. (DIP 적용)
/// - 게임 상태 (Initializing, Playing, Paused, GameOver)
/// - Player/Enemy 참조는 ICharacterStats 인터페이스로 접근
/// - 게임 차원 이벤트 발생 (OnGameStateChanged + EventBus)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // DIP: 구체 클래스(PlayerStats/EnemyStats) 대신 인터페이스로 참조
    [SerializeField] private PlayerStats playerStatsComponent;
    [SerializeField] private EnemyStats enemyStatsComponent;

    [Header("Stage")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform playerTransform;

    private ICharacterStats _playerStats;
    private ICharacterStats _enemyStats;

    private GameState _currentGameState = GameState.Initializing;

    public GameState CurrentGameState => _currentGameState;

    // 인터페이스를 통해 외부에 노출
    public ICharacterStats Player => _playerStats;
    public ICharacterStats Enemy => _enemyStats;

    // 게임 상태 변화 이벤트 (직접 구독 + EventBus 둘 다 지원)
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        if (playerStatsComponent == null)
            playerStatsComponent = FindObjectOfType<PlayerStats>();

        if (enemyStatsComponent == null)
            enemyStatsComponent = FindObjectOfType<EnemyStats>();

        _playerStats = playerStatsComponent;
        _enemyStats = enemyStatsComponent;

        if (playerTransform == null && playerStatsComponent != null)
            playerTransform = playerStatsComponent.transform;

        SetGameState(GameState.Playing);
    }

    private void OnEnable()  => EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDeadEvent);
    private void OnDisable() => EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDeadEvent);

    private void OnDestroy()
    {
        // DontDestroyOnLoad 오브젝트가 씬 종료 시 파괴될 때 정리합니다.
        EventBus.Clear();
    }

    /// <summary> 게임 상태를 변경하고 이벤트를 발생시킵니다. </summary>
    public void SetGameState(GameState newState)
    {
        if (_currentGameState == newState)
            return;

        _currentGameState = newState;
        OnGameStateChanged?.Invoke(_currentGameState);
        EventBus.Publish(new GameStateChangedEvent(_currentGameState));

        Debug.Log($"[GameManager] Game State Changed: {_currentGameState}");
    }

    /// <summary> PlayerDeadEvent 구독 핸들러 — 리스폰 처리. </summary>
    private void OnPlayerDeadEvent(PlayerDeadEvent _)
    {
        // 사망 즉시 리스폰하지 않습니다. E키 입력 후 SpineInputController가 RespawnPlayer()를 호출합니다.
    }

    /// <summary> 플레이어를 StartPoint로 이동시키고 체력을 회복합니다. </summary>
    public void RespawnPlayer()
    {
        if (playerTransform == null || startPoint == null)
        {
            Debug.LogWarning("[GameManager] RespawnPlayer: playerTransform 또는 startPoint가 없습니다.");
            return;
        }

        playerStatsComponent?.ResetHealthToMax();
        playerTransform.position = startPoint.position;
        SetGameState(GameState.Playing);
        EventBus.Publish(new PlayerRespawnEvent(startPoint.position));
        Debug.Log("[GameManager] Player respawned at StartPoint.");
    }

    /// <summary> 스테이지를 재시작합니다. 플레이어 리셋 + 적 재생성. </summary>
    public void RestartStage()
    {
        if (_currentGameState != GameState.StageClear) return;
        RespawnPlayer();
        EventBus.Publish(new StageRestartEvent());
        Debug.Log("[GameManager] Stage Restarted.");
    }

    /// <summary> 스테이지 클리어 처리. </summary>
    public void OnStageClear()
    {
        if (_currentGameState == GameState.StageClear) return;
        SetGameState(GameState.StageClear);
        EventBus.Publish(new StageClearEvent());
        Debug.Log("[GameManager] Stage Clear!");
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
    }

    /// <summary> 게임 재개. </summary>
    public void Resume()
    {
        if (_currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
            Time.timeScale = 1f;
        }
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
