using UnityEngine;
using UnityEngine.UI;

// Title 씬의 UI 흐름을 담당합니다.
// 메인 패널(새 게임/불러오기) ↔ 슬롯 선택 패널 전환을 처리합니다.
public class TitleManager : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject loadPanel;

    [Header("슬롯 버튼 텍스트 (0=AUTO, 1~3=수동)")]
    [SerializeField] private Text[] slotTexts;

    private void Start()
    {
        // slotTexts가 Inspector에서 연결되지 않은 경우 SaveManager.Instance 유무만 확인합니다.
        if (GameInstance.Instance == null)
        {
            Debug.LogWarning("[TitleManager] GameInstance가 null입니다. Bootstrap 씬을 통해 실행하세요.");
        }

        ShowMain();

    }

    // ── 메인 패널 버튼 ────────────────────────────────────────────────────────

    public void OnNewGameClicked()
    {
        GameInstance.Instance?.StartNewGame();
    }

    public void OnLoadGameClicked()
    {
        RefreshSlotTexts();
        ShowLoad();
    }

    public void OnQuitClicked()
    {
        GameInstance.Instance?.QuitGame();
    }

    // ── 슬롯 선택 패널 버튼 ──────────────────────────────────────────────────

    // Button.onClick은 int 직접 바인딩 불가 → Inspector 연결용 래퍼
    public void OnSlot0Clicked() => OnSlotClicked(0);
    public void OnSlot1Clicked() => OnSlotClicked(1);
    public void OnSlot2Clicked() => OnSlotClicked(2);
    public void OnSlot3Clicked() => OnSlotClicked(3);

    public void OnSlotClicked(int slot)
    {
        if (GameInstance.Instance == null) return;
        var info = GameInstance.Instance.GetSlotInfo(slot);
        if (info.isEmpty)
        {
            Debug.Log($"[TitleManager] Slot {slot} 비어있음");
            return;
        }
        GameInstance.Instance.LoadGame(slot);
    }

    public void OnBackClicked()
    {
        ShowMain();
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private void ShowMain()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (loadPanel != null) loadPanel.SetActive(false);
    }

    private void ShowLoad()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (loadPanel != null) loadPanel.SetActive(true);
    }

    private void RefreshSlotTexts()
    {
        if (slotTexts == null || GameInstance.Instance == null) return;
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;
            var info = GameInstance.Instance.GetSlotInfo(i);
            slotTexts[i].text = info.isEmpty
                ? (i == GameInstance.AutoSlot ? "AUTO — 비어있음" : $"Slot {i} — 비어있음")
                : $"{(i == GameInstance.AutoSlot ? "AUTO" : $"Slot {i}")}  {info.sceneName}  {FormatPlaytime(info.playtime)}\n{info.savedAt}";
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
