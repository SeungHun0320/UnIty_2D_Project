using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// 모바일 가상 버튼 컴포넌트입니다. (SRP)
// OnPointerDown/Up을 UnityEvent로 노출해 Inspector에서 메서드를 자유롭게 연결합니다.
[RequireComponent(typeof(UnityEngine.UI.Image))]
public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Events")]
    public UnityEvent onPress;
    public UnityEvent onRelease;

    public void OnPointerDown(PointerEventData _) => onPress?.Invoke();
    public void OnPointerUp(PointerEventData _)   => onRelease?.Invoke();
}
