using UnityEngine;
using UnityEngine.SceneManagement;

// Bootstrap 씬에서 가장 먼저 실행되어 Title 씬을 로드합니다.
// Bootstrap 씬에는 GameManager, UIManager, SaveManager 등 DDOL 오브젝트가 위치합니다.
[DefaultExecutionOrder(-99)]
public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private string firstSceneName = "Title";

    private void Start()
    {
        if (string.IsNullOrEmpty(firstSceneName))
        {
            Debug.LogWarning("[BootstrapLoader] firstSceneName이 설정되지 않았습니다.");
            return;
        }
        SceneManager.LoadScene(firstSceneName);
    }
}
