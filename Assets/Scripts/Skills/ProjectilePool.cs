using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

// prefab 키 기반 로컬 투사체 풀입니다. 플레이어 GameObject에 부착합니다.
// 씬 언로드 시 OnDisable에서 풀을 정리해 stale 참조를 방지합니다.
public class ProjectilePool : MonoBehaviour
{
    [SerializeField, Min(1)] private int defaultCapacity = 5;
    [SerializeField, Min(1)] private int maxSize = 20;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

    private void OnDisable()
    {
        foreach (var pool in _pools.Values)
            pool.Clear();
        _pools.Clear();
    }

    // 풀에서 투사체를 꺼내 position/rotation을 설정한 뒤 반환합니다.
    public GameObject Get(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        var pool = GetOrCreatePool(prefab);
        GameObject go = pool.Get();
        go.transform.SetPositionAndRotation(position, rotation);
        return go;
    }

    // 투사체를 비활성화하고 풀에 반납합니다.
    public void Release(GameObject prefab, GameObject instance)
    {
        if (instance == null) return;
        if (!_pools.TryGetValue(prefab, out var pool)) return;
        pool.Release(instance);
    }

    private ObjectPool<GameObject> GetOrCreatePool(GameObject prefab)
    {
        if (_pools.TryGetValue(prefab, out var existing))
            return existing;

        var pool = new ObjectPool<GameObject>(
            createFunc:    () => Instantiate(prefab),
            actionOnGet:   go => go.SetActive(true),
            actionOnRelease: go =>
            {
                if (go != null) go.SetActive(false);
            },
            actionOnDestroy: go =>
            {
                if (go != null) Destroy(go);
            },
            collectionCheck: false,
            defaultCapacity: defaultCapacity,
            maxSize: maxSize
        );

        _pools[prefab] = pool;
        return pool;
    }
}
