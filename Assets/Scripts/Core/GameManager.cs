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

        SetGameState(GameState.Playing);
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
    GameOver
}
