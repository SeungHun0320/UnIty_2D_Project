using System.Collections.Generic;
using UnityEngine;

// 자식 오브젝트의 EnemySpawnPoint를 수집해 적을 생성/제거하는 스포너입니다. (SRP)
// StageRestartEvent 수신 시 전체 재스폰합니다.
public class EnemySpawner : MonoBehaviour
{
    private EnemySpawnPoint[]      _spawnPoints;
    private readonly List<GameObject> _spawnedEnemies = new();

    private void Awake()
    {
        _spawnPoints = GetComponentsInChildren<EnemySpawnPoint>();
    }

    private void Start() => SpawnAll();

    private void OnEnable()  => EventBus.Subscribe<StageRestartEvent>(OnStageRestart);
    private void OnDisable() => EventBus.Unsubscribe<StageRestartEvent>(OnStageRestart);

    private void OnStageRestart(StageRestartEvent _)
    {
        DespawnAll();
        SpawnAll();
    }

    private void SpawnAll()
    {
        foreach (var point in _spawnPoints)
        {
            if (point == null || point.prefab == null) continue;
            var go = Instantiate(point.prefab, point.transform.position, point.transform.rotation);
            _spawnedEnemies.Add(go);
        }
    }

    private void DespawnAll()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        _spawnedEnemies.Clear();
    }
}
