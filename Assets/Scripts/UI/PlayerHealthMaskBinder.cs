using UnityEngine;

// 플레이어 체력을 할로우 나이트 스타일 마스크 UI(MaskUI)에 연동하는 ViewModel입니다.
// 규칙: MaskUI의 최대/현재 마스크 개수를 PlayerStats의 MaxHealth/CurrentHealth와 1:1로 매핑합니다.
public class PlayerHealthMaskBinder : PlayerStatsBinder
{
    [Header("View")]
    [SerializeField] private MaskUI maskUI;

    private int _lastMaxHealth = -1;
    private int _lastHealth    = -1;

    protected override void Awake()
    {
        base.Awake();
        if (maskUI == null)
            maskUI = FindAnyObjectByType<MaskUI>();
    }

    protected override void Subscribe()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged += HandleHealthChanged;
    }

    protected override void Unsubscribe()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= HandleHealthChanged;
    }

    protected override void SyncInitial()
    {
        if (playerStats != null)
            HandleHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
    }

    // 체력이 변경될 때마다 호출되어 MaskUI 값을 갱신합니다.
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (maskUI == null) return;

        int maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
        int hp    = Mathf.Clamp(Mathf.RoundToInt(currentHealth), 0, maxHp);

        // 최대 체력이 바뀌면 마스크 슬롯 수를 갱신합니다.
        if (maxHp != _lastMaxHealth)
        {
            maskUI.SetMaxHealth(maxHp);
            _lastMaxHealth = maxHp;
        }

        // 이전 체력을 넘겨줘서 cracked 연출이 제대로 나오도록 합니다.
        if (hp != _lastHealth)
        {
            int previous = _lastHealth < 0 ? hp : _lastHealth;
            maskUI.SetHealth(hp, previous);
            _lastHealth = hp;
        }
    }
}
