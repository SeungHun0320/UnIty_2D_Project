using System.Collections;
using UnityEngine;

// BackTrigger / GoalTrigger 공통 로직을 담는 추상 기반 클래스입니다.
// 씬 로드 쿨다운, 태그 확인, EventBus 구독을 처리하며 실제 동작은 OnTriggered()에 위임합니다.
[RequireComponent(typeof(Collider2D))]
public abstract class StageTriggerBase : MonoBehaviour
{
    [SerializeField] protected string playerTag = "Player";
    [SerializeField, Min(0f)] protected float loadCooldown = 0.5f;

    private bool _ready = true;

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

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
        if (!_ready || !other.CompareTag(playerTag)) return;
        OnTriggered();
    }

    protected abstract void OnTriggered();
}
