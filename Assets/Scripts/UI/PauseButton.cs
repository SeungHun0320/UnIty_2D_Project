using UnityEngine;
using UnityEngine.UI;

// 모바일 전용 일시정지 버튼. Inspector 연결 없이 GameInstance Facade를 직접 호출합니다.
[RequireComponent(typeof(Button))]
public class PauseButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => GameInstance.Instance?.TogglePause());
    }
}
