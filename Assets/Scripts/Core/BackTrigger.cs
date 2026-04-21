using UnityEngine;

// 씬 왼쪽 끝(시작 지점 부근)에 배치합니다. 플레이어가 진입하면 이전 씬으로 이동합니다.
[RequireComponent(typeof(Collider2D))]
public class BackTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        GameManager.Instance?.LoadPreviousStage();
    }
}
