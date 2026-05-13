using System;
using System.Collections.Generic;
using UnityEngine;

// UI 패널 등록/조회 및 게임 이벤트-패널 연결을 담당하는 싱글톤입니다.
// DDOL로 동작하며 canvasRoot(Canvas 루트 오브젝트)도 함께 유지합니다.
public class UIManager : MonoBehaviour
{
    [Header("Persistent Canvas")]
    [Tooltip("씬 전환 시 유지할 Canvas 루트 오브젝트를 연결합니다.")]
    [SerializeField] private GameObject canvasRoot;
    [Tooltip("씬 전환 시 유지할 EventSystem 오브젝트를 연결합니다.")]
    [SerializeField] private GameObject eventSystemRoot;

    [Header("Game HUD")]
    [Tooltip("게임 플레이 중에만 표시할 HUD 루트 오브젝트 (체력/소울/Geo 등)")]
    [SerializeField] private GameObject gameHUDRoot;

    [Header("Mobile UI")]
    [Tooltip("씬 전환 시 숨길 모바일 전용 버튼 UI 루트입니다.")]
    [SerializeField] private GameObject mobileUIRoot;

    [Header("Panels")]
    [SerializeField] private DeathPanel      deathPanel;
    [SerializeField] private StageClearPanel stageClearPanel;
    [SerializeField] private PausePanel      pausePanel;
    [SerializeField] private SavePanel       savePanel;

    private readonly Dictionary<Type, BasePanel> _panels = new();

    private void Awake()
    {
        if (canvasRoot != null)      DontDestroyOnLoad(canvasRoot);
        else                         Debug.LogWarning("[UIManager] canvasRoot가 연결되지 않았습니다.");
        if (eventSystemRoot != null) DontDestroyOnLoad(eventSystemRoot);
        else                         Debug.LogWarning("[UIManager] eventSystemRoot가 연결되지 않았습니다.");

        // mobileUIRoot가 미연결이면 canvasRoot 하위에서 탐색합니다.
        if (mobileUIRoot == null && canvasRoot != null)
        {
            var t = canvasRoot.transform.Find("MobileUI");
            if (t != null) mobileUIRoot = t.gameObject;
        }

        if (deathPanel != null)      Register(deathPanel);
        if (stageClearPanel != null) Register(stageClearPanel);
        if (pausePanel != null)      Register(pausePanel);
        if (savePanel != null)       Register(savePanel);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<StageClearEvent>(OnStageClear);

        EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
        EventBus.Subscribe<GameResumedEvent>(OnGameResumed);
        EventBus.Subscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<StageClearEvent>(OnStageClear);

        EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
        EventBus.Unsubscribe<GameResumedEvent>(OnGameResumed);
        EventBus.Unsubscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnPlayerDead(PlayerDeadEvent _)       => Show<DeathPanel>();
    private void OnPlayerRespawn(PlayerRespawnEvent _) => Hide<DeathPanel>();
    private void OnStageClear(StageClearEvent _)       => Show<StageClearPanel>();

    private void OnGamePaused(GamePausedEvent _)   => Show<PausePanel>();
    private void OnGameResumed(GameResumedEvent _)     => Hide<PausePanel>();

    // 씬 전환 완료 시 모든 패널을 닫고 게임 HUD를 표시합니다.
    private void OnStageLoaded(StageLoadedEvent _)
    {
        Hide<DeathPanel>();
        Hide<StageClearPanel>();
        Hide<PausePanel>();
        Hide<SavePanel>();
        if (gameHUDRoot != null) gameHUDRoot.SetActive(true);
    }

    public void Register(BasePanel panel)
    {
        _panels[panel.GetType()] = panel;
    }

    public void Show<T>() where T : BasePanel
    {
        if (_panels.TryGetValue(typeof(T), out var panel))
            panel.Show();
        else
            Debug.LogWarning($"[UIManager] {typeof(T).Name} 패널이 등록되지 않았습니다.");
    }

    public void Hide<T>() where T : BasePanel
    {
        if (_panels.TryGetValue(typeof(T), out var panel))
            panel.Hide();
        else
            Debug.LogWarning($"[UIManager] {typeof(T).Name} 패널이 등록되지 않았습니다.");
    }

    public T Get<T>() where T : BasePanel
    {
        _panels.TryGetValue(typeof(T), out var panel);
        return panel as T;
    }

    // 씬 전환 전 모든 패널과 게임 HUD, 모바일 UI를 숨깁니다. (GoToTitle 등에서 호출)
    public void HideAllPanels()
    {
        foreach (var panel in _panels.Values)
            panel?.Hide();
        if (gameHUDRoot  != null) gameHUDRoot.SetActive(false);
        if (mobileUIRoot != null) mobileUIRoot.SetActive(false);
    }
}
