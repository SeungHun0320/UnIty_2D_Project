using UnityEngine;

// 플레이어 프리팹 생성·배치·DDOL 수명 관리를 담당합니다. (SRP)
// SceneTransitionManager.OnSceneLoaded에서 EnsurePlayer → PositionPlayer 순으로 호출됩니다.
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Bootstrap → 게임 씬 최초 진입 시 동적으로 생성할 Player 프리팹입니다.")]
    [SerializeField] private GameObject playerPrefab;

    private ICharacterStats _playerStats;
    private Transform       _playerTransform;
    private PlayerMover     _playerMover;

    public ICharacterStats Player => _playerStats;

    // 플레이어가 없을 때만 생성합니다. 씬 전환 간 1회만 인스턴스화됩니다.
    public void EnsurePlayer()
    {
        // Unity Object는 파괴 후 null 비교가 올바르게 동작합니다.
        if (_playerTransform != null) return;

        _playerStats = null;
        _playerMover = null;

        if (playerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawner] playerPrefab이 설정되지 않았습니다.");
            return;
        }

        var go = Instantiate(playerPrefab);
        DontDestroyOnLoad(go);

        var ps = go.GetComponentInChildren<PlayerStats>(true);
        if (ps != null)
        {
            _playerStats     = ps;
            _playerTransform = go.transform;
            _playerMover     = go.GetComponentInChildren<PlayerMover>(true);
        }
        else
        {
            Debug.LogWarning("[PlayerSpawner] Player 프리팹에서 PlayerStats를 찾지 못했습니다.");
        }
    }

    // 진입 방향에 따라 start/goalPoint로 이동합니다.
    public void PositionPlayer(StageContext ctx, bool enterFromGoal)
    {
        if (_playerTransform == null) return;
        if (ctx == null)
        {
            Debug.LogWarning("[PlayerSpawner] StageContext가 없어 스폰 위치를 설정할 수 없습니다.");
            return;
        }

        Transform spawnPoint = enterFromGoal ? ctx.goalPoint : ctx.startPoint;
        if (spawnPoint == null)
        {
            Debug.LogWarning("[PlayerSpawner] 스폰 포인트가 StageContext에 설정되지 않았습니다.");
            return;
        }
        Teleport(spawnPoint.position);
    }

    // 직접 위치로 이동합니다. RespawnPlayer 등에서 호출합니다.
    public void Teleport(Vector3 position)
    {
        if (_playerMover != null)
            _playerMover.Teleport(position);
        else if (_playerTransform != null)
            _playerTransform.position = position;
    }
}
