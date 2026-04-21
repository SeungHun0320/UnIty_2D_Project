using UnityEngine;
using UnityEngine.EventSystems;

// 씬마다 EventSystem이 필요하지만 Bootstrap의 DDOL EventSystem과 중복을 방지합니다.
// Title 씬 EventSystem에 부착하면 이미 하나가 있을 때 자신을 제거합니다.
public class SingletonEventSystem : MonoBehaviour
{
    private void Awake()
    {
        if (FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length > 1)
            Destroy(gameObject);
    }
}
