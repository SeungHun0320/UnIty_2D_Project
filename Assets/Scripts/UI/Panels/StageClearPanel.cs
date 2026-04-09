using TMPro;
using UnityEngine;

// 스테이지 클리어 시 표시되는 패널입니다.
// 이벤트 구독은 UIManager가 담당합니다.
public class StageClearPanel : BasePanel
{
    [Header("텍스트")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI guideText;

    [Header("텍스트 내용")]
    [SerializeField] private string titleString = "STAGE CLEAR";
    [SerializeField] private string guideString = "";

    protected override void Awake()
    {
        base.Awake();
        if (titleText != null) titleText.text = titleString;
        if (guideText  != null) guideText.text  = guideString;
    }
}
