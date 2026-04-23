using UnityEngine;
using UnityEngine.UI;

// 인게임 수동 저장 슬롯 선택 패널입니다. (Slot 1~3, AUTO 슬롯은 스테이지 이동 시 자동 저장)
public class SavePanel : BasePanel
{
    [Header("슬롯 버튼 텍스트 (index 0=Slot1, 1=Slot2, 2=Slot3)")]
    [SerializeField] private Text[] slotTexts;

    [Header("버튼")]
    [SerializeField] private Button slot1Button;
    [SerializeField] private Button slot2Button;
    [SerializeField] private Button slot3Button;
    [SerializeField] private Button backButton;

    protected override void Awake()
    {
        base.Awake();

        // slotTexts가 Inspector에서 연결되지 않은 경우 슬롯 버튼 자식 Text에서 자동 탐색합니다.
        if ((slotTexts == null || slotTexts.Length == 0)
            && slot1Button != null && slot2Button != null && slot3Button != null)
        {
            slotTexts = new[]
            {
                slot1Button.GetComponentInChildren<Text>(true),
                slot2Button.GetComponentInChildren<Text>(true),
                slot3Button.GetComponentInChildren<Text>(true),
            };
        }

        if (slot1Button != null) slot1Button.onClick.AddListener(OnSlot1Clicked);
        if (slot2Button != null) slot2Button.onClick.AddListener(OnSlot2Clicked);
        if (slot3Button != null) slot3Button.onClick.AddListener(OnSlot3Clicked);
        if (backButton  != null) backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        if (slot1Button != null) slot1Button.onClick.RemoveListener(OnSlot1Clicked);
        if (slot2Button != null) slot2Button.onClick.RemoveListener(OnSlot2Clicked);
        if (slot3Button != null) slot3Button.onClick.RemoveListener(OnSlot3Clicked);
        if (backButton  != null) backButton.onClick.RemoveListener(OnBackClicked);
    }

    // 패널이 열릴 때 슬롯 정보를 갱신합니다.
    protected override void OnShow() => RefreshSlotTexts();

    private void OnSlot1Clicked() => SaveAndBack(1);
    private void OnSlot2Clicked() => SaveAndBack(2);
    private void OnSlot3Clicked() => SaveAndBack(3);

    private void SaveAndBack(int slot)
    {
        SaveManager.Instance?.Save(slot);
        OnBackClicked();
    }

    private void OnBackClicked()
    {
        Hide();
        UIManager.Instance?.Show<PausePanel>();
    }

    private void RefreshSlotTexts()
    {
        if (slotTexts == null || SaveManager.Instance == null) return;
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;
            int slot = i + 1;
            var info = SaveManager.Instance.GetSlotInfo(slot);
            slotTexts[i].text = info.isEmpty
                ? $"Slot {slot} — 비어있음"
                : $"Slot {slot}  {info.sceneName}  {FormatPlaytime(info.playtime)}\n{info.savedAt}";
        }
    }

    private static string FormatPlaytime(float seconds)
    {
        int h = (int)(seconds / 3600);
        int m = (int)(seconds % 3600 / 60);
        int s = (int)(seconds % 60);
        return h > 0 ? $"{h:D2}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
    }
}
