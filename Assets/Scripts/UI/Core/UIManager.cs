using System;
using System.Collections.Generic;
using UnityEngine;

// UI 패널 등록/조회 및 게임 이벤트-패널 연결을 담당하는 싱글톤입니다.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private DeathPanel deathPanel;
    [SerializeField] private StageClearPanel stageClearPanel;

    private readonly Dictionary<Type, BasePanel> _panels = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (deathPanel != null)      Register(deathPanel);
        if (stageClearPanel != null) Register(stageClearPanel);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<StageClearEvent>(OnStageClear);
        EventBus.Subscribe<StageRestartEvent>(OnStageRestart);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<StageClearEvent>(OnStageClear);
        EventBus.Unsubscribe<StageRestartEvent>(OnStageRestart);
    }

    private void OnPlayerDead(PlayerDeadEvent _)       => Show<DeathPanel>();
    private void OnPlayerRespawn(PlayerRespawnEvent _) => Hide<DeathPanel>();
    private void OnStageClear(StageClearEvent _)       => Show<StageClearPanel>();
    // 스테이지 재시작 시 스테이지 클리어 패널을 숨깁니다.
    private void OnStageRestart(StageRestartEvent _)   => Hide<StageClearPanel>();

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
}
