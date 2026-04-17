using System;
using UnityEngine;

// 아이템 수집 시 재생되는 시각 효과 인터페이스.
// 구현체(GeoFlyEffect 등)를 ItemPickup에 추가하면 수집 전 애니메이션이 재생됩니다.
// 구현체가 없으면 ItemPickupBase가 즉시 ApplyEffect()를 호출합니다.
public interface IPickupEffect
{
    // fromWorldPos: 픽업 오브젝트의 월드 위치, onComplete: 효과 완료 후 호출할 콜백
    void Play(Vector3 fromWorldPos, Action onComplete);
}
