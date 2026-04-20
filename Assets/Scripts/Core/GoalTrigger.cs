using UnityEngine;

// Goal 트리거에 플레이어가 진입하면 GameManager에 스테이지 클리어를 알립니다.
[RequireComponent(typeof(Collider2D))]
public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        GameManager.Instance?.OnStageClear();
    }
}
