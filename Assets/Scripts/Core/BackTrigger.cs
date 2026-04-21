using System.Collections;
using UnityEngine;

// 씬 왼쪽 끝(시작 지점 부근)에 배치합니다. 플레이어가 진입하면 이전 씬으로 이동합니다.
[RequireComponent(typeof(Collider2D))]
public class BackTrigger : MonoBehaviour
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
        GameManager.Instance?.LoadPreviousStage();
    }
}
