using System.Collections;
using UnityEngine;

// 타격감 향상을 위한 히트렉(Hitstop) 관리 싱글톤
// 공격 명중 순간 Time.timeScale을 잠깐 멈춰 임팩트를 연출합니다.
public class HitstopManager : MonoBehaviour
{
    [Header("Hitstop Durations")]
    [SerializeField] private float playerHitDuration = 0.08f; // 플레이어 피격 히트렉 시간(초)
    [SerializeField] private float enemyHitDuration  = 0.05f; // 적 피격 히트렉 시간(초)

    [Header("Hitstop Strength")]
    [SerializeField] [Range(0f, 0.3f)] private float hitstopTimeScale = 0f; // 0 = 완전 정지

    [Header("Death Slowmo")]
    [SerializeField] [Range(0f, 1f)] private float deathTimeScale = 0.3f; // 사망 시 슬로우모션 배율

    private Coroutine _hitstopCoroutine;

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Subscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
        EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Subscribe<PlayerRespawnEvent>(OnPlayerRespawn);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Unsubscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
        EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
        EventBus.Unsubscribe<PlayerRespawnEvent>(OnPlayerRespawn);
    }

    private void OnPlayerHit(PlayerHitByEnemyEvent _) => TriggerHitstop(playerHitDuration);
    private void OnEnemyHit(EnemyHitByPlayerEvent _)  => TriggerHitstop(enemyHitDuration);

    // 사망 시 진행 중인 히트렉을 취소하고 슬로우모션으로 전환합니다.
    private void OnPlayerDead(PlayerDeadEvent _)
    {
        if (_hitstopCoroutine != null)
        {
            StopCoroutine(_hitstopCoroutine);
            _hitstopCoroutine = null;
        }
        Time.timeScale = deathTimeScale;
    }

    // 리스폰 시 히트렉·슬로우모션을 모두 취소하고 timeScale을 복원합니다.
    private void OnPlayerRespawn(PlayerRespawnEvent _)
    {
        if (_hitstopCoroutine != null)
        {
            StopCoroutine(_hitstopCoroutine);
            _hitstopCoroutine = null;
        }
        Time.timeScale = 1f;
    }

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
        // Paused·StageClear·GameOver는 각자 timeScale을 관리하므로 복원하지 않습니다.
        var state = GameInstance.Instance?.CurrentGameState ?? GameState.Playing;
        if (state == GameState.Playing)
            Time.timeScale = 1f;
        _hitstopCoroutine = null;
    }
}
