using UnityEngine;
using UnityEngine.InputSystem;

// 게임 시스템 입력(일시정지·리스폰)을 담당합니다. (SRP)
// 플레이어 움직임/스킬 입력은 SpineInputController가 처리합니다.
public class GameInputHandler : MonoBehaviour
{
    private InputAction _pauseAction;
    private InputAction _respawnAction;

    private bool _waitingForRespawn;

    private void Awake()
    {
        _pauseAction = new InputAction(name: "Pause", type: InputActionType.Button);
        _pauseAction.AddBinding("<Keyboard>/escape");
        _pauseAction.AddBinding("<Gamepad>/start");

        _respawnAction = new InputAction(name: "Respawn", type: InputActionType.Button);
        _respawnAction.AddBinding("<Keyboard>/e");
        _respawnAction.AddBinding("<Gamepad>/buttonEast");
    }

    private void OnEnable()
    {
        _pauseAction.performed += OnPausePerformed;
        _pauseAction.Enable();

        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Subscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnDisable()
    {
        _pauseAction.performed -= OnPausePerformed;
        _pauseAction.Disable();

        _respawnAction.performed -= OnRespawnPerformed;
        _respawnAction.Disable();

        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
        EventBus.Unsubscribe<StageLoadedEvent>(OnStageLoaded);
    }

    private void OnDestroy()
    {
        _pauseAction.Dispose();
        _respawnAction.Dispose();
    }

    private void OnPausePerformed(InputAction.CallbackContext _)
        => GameInstance.Instance?.TogglePause();

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
        GameInstance.Instance?.RespawnPlayer();
    }

    private void OnPlayerRespawn(PlayerRespawnEvent _)
    {
        _waitingForRespawn = false;
        _respawnAction.performed -= OnRespawnPerformed;
        _respawnAction.Disable();
    }

    // 씬 전환 시 이전 씬의 대기 상태를 초기화합니다.
    private void OnStageLoaded(StageLoadedEvent _)
    {
        if (_waitingForRespawn)
        {
            _waitingForRespawn = false;
            _respawnAction.performed -= OnRespawnPerformed;
            _respawnAction.Disable();
        }
    }
}
