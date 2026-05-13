using UnityEngine;

// 게임 상태·플레이타임만 담당합니다. (SRP)
// 씬 전환 → SceneTransitionManager, 플레이어 생성·배치 → PlayerSpawner로 분리됐습니다.
[DefaultExecutionOrder(-101)]
public class GameManager : MonoBehaviour
{
    private GameState _currentGameState = GameState.Initializing;
    private float     _playtime;

    public GameState CurrentGameState => _currentGameState;
    public float     Playtime         => _playtime;

    public event System.Action<GameState> OnGameStateChanged;

    private void Update()
    {
        // Pause·StageClear 중에는 누적하지 않습니다. (unscaledDeltaTime으로 timeScale 무관)
        if (_currentGameState == GameState.Playing)
            _playtime += Time.unscaledDeltaTime;
    }

    public void SetPlaytime(float t) => _playtime = Mathf.Max(0f, t);

    public void SetGameState(GameState newState)
    {
        if (_currentGameState == newState) return;
        _currentGameState = newState;
        OnGameStateChanged?.Invoke(_currentGameState);
        EventBus.Publish(new GameStateChangedEvent(_currentGameState));
        Debug.Log($"[GameManager] GameState → {_currentGameState}");
    }

    public void OnPlayerDead()
    {
        SetGameState(GameState.GameOver);
        EventBus.Publish(new PlayerDeadEvent());
    }

    public void Pause()
    {
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
        EventBus.Publish(new GamePausedEvent());
    }

    public void Resume()
    {
        if (_currentGameState != GameState.Paused) return;
        SetGameState(GameState.Playing);
        Time.timeScale = 1f;
        EventBus.Publish(new GameResumedEvent());
    }

    public void TogglePause()
    {
        if (_currentGameState == GameState.Paused)       Resume();
        else if (_currentGameState == GameState.Playing) Pause();
    }

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
