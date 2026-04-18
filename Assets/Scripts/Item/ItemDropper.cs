using System.Collections.Generic;
using UnityEngine;

// 아이템 드랍 테이블 컴포넌트. 적 오브젝트에 붙여서 사용합니다.
// EnemyDeadEvent를 구독해 자신이 죽었을 때만 드랍을 처리합니다.
public class ItemDropper : MonoBehaviour
{
    [System.Serializable]
    public struct DropEntry
    {
        [Tooltip("드랍할 픽업 프리팹 (ItemPickupBase 상속)")]
        public ItemPickupBase prefab;

        [Tooltip("드랍 확률 (0 = 0%, 1 = 100%)")]
        [Range(0f, 1f)] public float chance;

        [Tooltip("한 번에 드랍할 수량 범위")]
        public Vector2Int countRange;
    }

    [SerializeField] private List<DropEntry> dropTable;

    private void OnEnable()  => EventBus.Subscribe<EnemyDeadEvent>(OnEnemyDead);
    private void OnDisable() => EventBus.Unsubscribe<EnemyDeadEvent>(OnEnemyDead);

    private void OnEnemyDead(EnemyDeadEvent evt)
    {
        // 자신의 사망 이벤트만 처리
        if (evt.Enemy != gameObject) return;
        Drop(transform.position);
    }

    private void Drop(Vector3 position)
    {
        foreach (var entry in dropTable)
        {
            if (entry.prefab == null) continue;
            if (Random.value > entry.chance) continue;

            int count = Random.Range(entry.countRange.x, Mathf.Max(entry.countRange.x, entry.countRange.y) + 1);
            for (int i = 0; i < count; i++)
            {
                Instantiate(entry.prefab, position, Quaternion.identity);
            }
        }
    }
}
