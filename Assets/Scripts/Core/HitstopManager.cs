using System.Collections;
using UnityEngine;

// 타격감 향상을 위한 히트렉(Hitstop) 관리 싱글톤
// 공격 명중 순간 Time.timeScale을 잠깐 멈춰 임팩트를 연출합니다.
public class HitstopManager : MonoBehaviour
{
    public static HitstopManager Instance { get; private set; }

    [Header("Hitstop Durations")]
    [SerializeField] private float playerHitDuration = 0.08f; // 플레이어 피격 히트렉 시간(초)
    [SerializeField] private float enemyHitDuration  = 0.05f; // 적 피격 히트렉 시간(초)

    [Header("Hitstop Strength")]
    [SerializeField] [Range(0f, 0.3f)] private float hitstopTimeScale = 0f; // 0 = 완전 정지

    private Coroutine _hitstopCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Subscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Unsubscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    }

    private void OnPlayerHit(PlayerHitByEnemyEvent _) => TriggerHitstop(playerHitDuration);
    private void OnEnemyHit(EnemyHitByPlayerEvent _)  => TriggerHitstop(enemyHitDuration);

    // 외부에서 직접 히트렉을 발동시킬 수 있습니다.
    public void TriggerHitstop(float duration)
    {
        if (_hitstopCoroutine != null)
            StopCoroutine(_hitstopCoroutine);
        _hitstopCoroutine = StartCoroutine(HitstopCoroutine(duration));
    }

    private IEnumerator HitstopCoroutine(float duration)
    {
        Time.timeScale = hitstopTimeScale;
        yield return new WaitForSecondsRealtime(duration);
        // 일시정지·StageClear 등으로 timeScale을 0으로 바꿔야 하는 상태라면 복원하지 않습니다.
        if (GameManager.Instance?.CurrentGameState == GameState.Playing)
            Time.timeScale = 1f;
        _hitstopCoroutine = null;
    }
}
