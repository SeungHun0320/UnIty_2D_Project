using UnityEngine;

// 실행 플랫폼에 따라 입력 컨트롤러를 선택합니다. (SRP)
// Android / iOS → MobileInputController + 모바일 UI 활성화
// 그 외         → SpineInputController 활성화
public class InputRouter : MonoBehaviour
{
    [Header("Controllers")]
    [SerializeField] private SpineInputController  keyboardController;
    [SerializeField] private MobileInputController mobileController;

    [Header("Mobile UI")]
    [Tooltip("모바일 전용 버튼 UI 루트 오브젝트입니다.")]
    [SerializeField] private GameObject mobileUIRoot;

    [Header("Debug")]
    [Tooltip("에디터에서 모바일 UI를 강제 활성화해 테스트합니다.")]
    [SerializeField] private bool forceMobile = false;

    private void Awake()
    {
        bool isMobile = forceMobile
            || Application.platform == RuntimePlatform.Android
            || Application.platform == RuntimePlatform.IPhonePlayer;

        if (keyboardController != null) keyboardController.enabled = !isMobile;
        if (mobileController   != null) mobileController.enabled   = isMobile;
        if (mobileUIRoot       != null) mobileUIRoot.SetActive(isMobile);
    }
}
