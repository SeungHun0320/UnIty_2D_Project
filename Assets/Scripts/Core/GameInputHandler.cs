using UnityEngine;
using UnityEngine.InputSystem;

// 게임 시스템 입력(일시정지·리스폰·재시작)을 담당합니다. (SRP)
// 플레이어 움직임/스킬 입력은 SpineInputController가 처리합니다.
public class GameInputHandler : MonoBehaviour
{
    private InputAction _pauseAction;
    private InputAction _respawnAction;
    private InputAction _restartAction;

    private bool _waitingForRespawn;
    private bool _waitingForRestart;

    private void Awake()
    {
        _pauseAction = new InputAction(name: "Pause", type: InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Gamepad>/start");

        _respawnAction = new InputAction(name: "Respawn", type: InputActionType.Button);
        _respawnAction.AddBinding("<Keyboard>/e");
        _respawnAction.AddBinding("<Gamepad>/buttonEast");

        _restartAction = new InputAction(name: "Restart", type: InputActionType.Button);
        _restartAction.AddBinding("<Keyboard>/r");
        _restartAction.AddBinding("<Gamepad>/start");
    }

    private void OnEnable()
    {
        _pauseAction.performed += OnPausePerformed;
        _pauseAction.Enable();

        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<StageClearEvent>(OnStageClear);
        EventBus.Subscribe<StageRestartEvent>(OnStageRestart);
        EventBus.Subscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnDisable()
    {
        _pauseAction.performed -= OnPausePerformed;
        _pauseAction.Disable();

        _respawnAction.performed -= OnRespawnPerformed;
        _respawnAction.Disable();

        _restartAction.performed -= OnRestartPerformed;
        _restartAction.Disable();

        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<StageClearEvent>(OnStageClear);
        EventBus.Unsubscribe<StageRestartEvent>(OnStageRestart);
        EventBus.Unsubscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
        => GameManager.Instance?.TogglePause();

    // 사망 시 리스폰 대기 모드로 전환합니다.
    private void OnPlayerDead(PlayerDeadEvent _)
    {
        if (_waitingForRespawn) return;
        _waitingForRespawn = true;
        _respawnAction.performed += OnRespawnPerformed;
        _respawnAction.Enable();
    }

    private void OnRespawnPerformed(InputAction.CallbackContext _)
    {
        if (!_waitingForRespawn) return;
        GameManager.Instance?.RespawnPlayer();
    }

    private void OnPlayerRespawn(PlayerRespawnEvent _)
    {
        _waitingForRespawn = false;
        _respawnAction.performed -= OnRespawnPerformed;
        _respawnAction.Disable();
    }

    // 스테이지 클리어 시 재시작 대기 모드로 전환합니다.
    private void OnStageClear(StageClearEvent _)
    {
        if (_waitingForRestart) return;
        _waitingForRestart = true;
        _restartAction.performed += OnRestartPerformed;
        _restartAction.Enable();
    }

    private void OnRestartPerformed(InputAction.CallbackContext _)
    {
        if (!_waitingForRestart) return;
        _waitingForRestart = false;
        _restartAction.performed -= OnRestartPerformed;
        _restartAction.Disable();
        GameManager.Instance?.RestartStage();
    }

    private void OnStageRestart(StageRestartEvent _)
    {
        // UI 버튼으로 재시작 시에도 액션 구독/활성화를 정리합니다.
        _waitingForRestart = false;
        _restartAction.performed -= OnRestartPerformed;
        _restartAction.Disable();
    }

    // 씬 전환 시 이전 씬의 대기 상태를 초기화합니다.
    private void OnStageLoaded(StageLoadedEvent _)
    {
        if (_waitingForRestart)
        {
            _waitingForRestart = false;
            _restartAction.performed -= OnRestartPerformed;
            _restartAction.Disable();
        }
        if (_waitingForRespawn)
        {
            _waitingForRespawn = false;
            _respawnAction.performed -= OnRespawnPerformed;
            _respawnAction.Disable();
        }
    }
}
