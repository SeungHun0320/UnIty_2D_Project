using UnityEngine;

// 모든 아이템 데이터의 기반 ScriptableObject. 공통 메타데이터를 정의합니다.
public abstract class ItemDataSO : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite icon;
}
