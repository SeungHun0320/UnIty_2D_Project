using UnityEngine;

// 적 스폰 위치와 프리팹을 지정하는 마커 컴포넌트입니다.
// EnemySpawner의 자식 오브젝트에 부착하고 씬에서 원하는 위치에 배치합니다.
public class EnemySpawnPoint : MonoBehaviour
{
    [Tooltip("이 위치에서 스폰할 적 프리팹입니다.")]
    public GameObject prefab;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (prefab == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, prefab.name);
    }
#endif
}
