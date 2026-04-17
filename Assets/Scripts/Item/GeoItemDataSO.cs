using UnityEngine;

// 코인(지오) 아이템 데이터 SO. 드랍 수량 범위를 정의합니다.
[CreateAssetMenu(fileName = "GeoItemData", menuName = "Item/Geo Item Data")]
public class GeoItemDataSO : ItemDataSO
{
    [Header("지오 보상")]
    [Tooltip("드랍 시 지급될 지오 수량 범위 (최소, 최대)")]
    public Vector2Int geoRange = new Vector2Int(1, 5);

    // 스폰 시 1회 롤. 픽업 오브젝트가 Awake에서 호출해 수량을 고정합니다.
    public int RollAmount() => Random.Range(geoRange.x, geoRange.y + 1);
}
