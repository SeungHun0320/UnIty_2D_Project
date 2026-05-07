using System.Collections;
using UnityEngine;

// Goal 트리거에 플레이어가 진입하면 GameManager에 스테이지 클리어를 알립니다.
[RequireComponent(typeof(Collider2D))]
public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField, Min(0f)] private float loadCooldown = 0.5f;

    private bool _ready = true;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnEnable()  => EventBus.Subscribe<StageLoadedEvent>(OnStageLoaded);
    private void OnDisable() => EventBus.Unsubscribe<StageLoadedEvent>(OnStageLoaded);

    // 씬 로드 직후 스폰 위치가 트리거 위에 겹칠 경우 즉시 발동을 막습니다.
    private void OnStageLoaded(StageLoadedEvent _) => StartCoroutine(CooldownRoutine());

    private IEnumerator CooldownRoutine()
    {
        _ready = false;
        yield return new WaitForSeconds(loadCooldown);
        _ready = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_ready) return;
        if (!other.CompareTag(playerTag)) return;
        GameInstance.Instance?.OnStageClear();
    }
}
