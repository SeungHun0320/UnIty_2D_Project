using UnityEngine;

// 씬마다 하나씩 배치합니다. Awake에서 GameManager에 등록해 씬별 참조를 제공합니다.
public class StageContext : MonoBehaviour
{
    [Header("스폰 포인트")]
    [Tooltip("정방향(다음 씬→이 씬) 진입 시 플레이어 스폰 위치")]
    public Transform startPoint;
    [Tooltip("역방향(이전 씬→이 씬) 진입 시 플레이어 스폰 위치 — GoalTrigger 근처에 배치")]
    public Transform goalPoint;

    [Header("씬 연결")]
    [Tooltip("다음 스테이지 씬 이름. 비어있으면 마지막 스테이지로 간주합니다.")]
    public string nextSceneName;
    [Tooltip("이전 스테이지 씬 이름. 비어있으면 첫 스테이지로 간주합니다.")]
    public string previousSceneName;

    private void Awake()
    {
        if (GameInstance.Instance == null)
        {
            Debug.LogWarning("[StageContext] GameInstance가 없습니다.");
            return;
        }
        GameInstance.Instance.RegisterStage(this);
    }
}
