using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 씬 전환과 StageContext 관리를 담당합니다. (SRP)
// GameInstance.Awake()에서 Init()으로 의존 컴포넌트를 주입받습니다.
[DefaultExecutionOrder(-100)]
public class SceneTransitionManager : MonoBehaviour
{
    private static readonly HashSet<string> _nonGameScenes = new() { "Bootstrap", "Title" };

    private GameManager   _game;
    private PlayerSpawner _spawner;
    private SaveManager   _save;

    private StageContext _stageContext;
    private bool         _isLoadingScene;
    private bool         _enterFromGoal;

    public string    NextSceneName     => _stageContext?.nextSceneName;
    public string    PreviousSceneName => _stageContext?.previousSceneName;
    public Transform StartPoint        => _stageContext?.startPoint;

    // GameInstance.Awake()에서 호출됩니다.
    public void Init(GameManager game, PlayerSpawner spawner, SaveManager save)
    {
        _game    = game;
        _spawner = spawner;
        _save    = save;
    }

    private void Awake()     => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isLoadingScene = false;
        if (_nonGameScenes.Contains(scene.name)) return;

        _spawner?.EnsurePlayer();

        if (_stageContext == null)
            _stageContext = FindAnyObjectByType<StageContext>();

        _spawner?.PositionPlayer(_stageContext, _enterFromGoal);

        _enterFromGoal = false;
        Time.timeScale = 1f;
        _game?.SetGameState(GameState.Playing);
        EventBus.Publish(new StageLoadedEvent(_stageContext));
    }

    // ── 스테이지 등록 ─────────────────────────────────────────────────────────

    // StageContext.Awake()에서 GameInstance를 통해 호출됩니다.
    public void RegisterStage(StageContext ctx)
    {
        if (ctx != null) _stageContext = ctx;
    }

    // ── 씬 전환 ──────────────────────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        if (_isLoadingScene) return;
        if (string.IsNullOrEmpty(sceneName)) { Debug.LogWarning("[SceneTransitionManager] 씬 이름이 비어있습니다."); return; }
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    public void LoadNextStage()
    {
        if (_isLoadingScene) return;
        if (string.IsNullOrEmpty(_stageContext?.nextSceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] 다음 씬이 설정되지 않았습니다.");
            return;
        }
        _save?.AutoSave(_stageContext.nextSceneName);
        _enterFromGoal  = false;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.nextSceneName));
    }

    public void LoadPreviousStage()
    {
        if (_isLoadingScene) return;
        if (string.IsNullOrEmpty(_stageContext?.previousSceneName))
        {
            Debug.LogWarning("[SceneTransitionManager] 이전 씬이 설정되지 않았습니다.");
            return;
        }
        _save?.AutoSave(_stageContext.previousSceneName);
        _enterFromGoal  = true;
        _isLoadingScene = true;
        StartCoroutine(LoadSceneCoroutine(_stageContext.previousSceneName));
    }

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
}
