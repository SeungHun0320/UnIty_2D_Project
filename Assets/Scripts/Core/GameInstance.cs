using UnityEngine;

// 게임 전체의 단일 DDOL 진입점. 모든 매니저를 자식으로 보유하여 씬 전환 후에도 유지합니다.
[DefaultExecutionOrder(-200)]
public class GameInstance : MonoBehaviour
{
    public static GameInstance Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
