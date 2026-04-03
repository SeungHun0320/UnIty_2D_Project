using System;
using UnityEngine;

/// <summary>
/// 게임 전체 생명주기를 관리하는 싱글톤입니다.
/// - 게임 상태 (Initializing, Playing, Paused, GameOver)
/// - Player/Monster 참조
/// - 게임 차원 이벤트 발생
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private EnemyStats enemyStats;

    private GameState _currentGameState = GameState.Initializing;

    public GameState CurrentGameState => _currentGameState;
    public PlayerStats Player => playerStats;
    public EnemyStats Enemy => enemyStats;

    // 게임 상태 변화 이벤트
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        // 싱글톤 패턴 구현
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
        // 자동 참조 찾기 (에디터에서 설정하지 않은 경우)
        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        if (enemyStats == null)
            enemyStats = FindObjectOfType<EnemyStats>();

        // 게임 상태를 Playing으로 변경
        SetGameState(GameState.Playing);
    }

    /// <summary> 게임 상태를 변경하고 이벤트를 발생시킵니다. </summary>
    public void SetGameState(GameState newState)
    {
        if (_currentGameState == newState)
            return;

        _currentGameState = newState;
        OnGameStateChanged?.Invoke(_currentGameState);

        Debug.Log($"[GameManager] Game State Changed: {_currentGameState}");
    }

    /// <summary> 플레이어 사망 처리. </summary>
    public void OnPlayerDead()
    {
        SetGameState(GameState.GameOver);
        Debug.Log("[GameManager] Player is dead. Game Over!");
    }

    /// <summary> 적 사망 처리. </summary>
    public void OnEnemyDead()
    {
        Debug.Log("[GameManager] Enemy is dead!");
        // 추후 레벨 클리어 로직 추가 가능
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
