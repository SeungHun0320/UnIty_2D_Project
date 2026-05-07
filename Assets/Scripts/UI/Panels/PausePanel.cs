using UnityEngine;
using UnityEngine.UI;

// 일시정지 시 표시되는 패널입니다.
// 이벤트 구독은 UIManager가 담당하고, 버튼 콜백은 패널이 직접 처리합니다.
public class PausePanel : BasePanel
{
    [Header("버튼")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button titleButton;
    [SerializeField] private Button quitButton;

    protected override void Awake()
    {
        base.Awake();
        if (resumeButton != null) resumeButton.onClick.AddListener(OnResumeClicked);
        if (saveButton   != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (titleButton  != null) titleButton.onClick.AddListener(OnTitleClicked);
        if (quitButton   != null) quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnDestroy()
    {
        if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResumeClicked);
        if (saveButton   != null) saveButton.onClick.RemoveListener(OnSaveClicked);
        if (titleButton  != null) titleButton.onClick.RemoveListener(OnTitleClicked);
        if (quitButton   != null) quitButton.onClick.RemoveListener(OnQuitClicked);
    }

    private void OnResumeClicked() => GameInstance.Instance?.Resume();

    private void OnSaveClicked()
    {
        Hide();
        GameInstance.Instance?.ShowPanel<SavePanel>();
    }

    private void OnTitleClicked() => GameInstance.Instance?.GoToTitle();

    private void OnQuitClicked() => GameInstance.Instance?.QuitGame();
}
