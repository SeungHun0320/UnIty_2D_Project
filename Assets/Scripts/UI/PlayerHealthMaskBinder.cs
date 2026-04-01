using UnityEngine;

// 플레이어 체력을 할로우 나이트 스타일 마스크 UI(MaskUI)에 연동하는 ViewModel입니다.
// 규칙: MaskUI의 최대/현재 마스크 개수를 PlayerStats의 MaxHealth/CurrentHealth와 1:1로 매핑합니다.
public class PlayerHealthMaskBinder : MonoBehaviour
{
    [Header("References")]
    [Tooltip("체력을 가지고 있는 플레이어 스탯 컴포넌트입니다.")]
    [SerializeField] private PlayerStats playerStats;

    [Tooltip("체력(마스크) UI 컴포넌트입니다.")]
    [SerializeField] private MaskUI maskUI;

    private int _lastMaxHealth;
    private int _lastHealth;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();
        Subscribe();

        // 초기 상태를 한 번 동기화합니다.
        if (playerStats != null)
        {
            HandleHealthChanged(playerStats.CurrentHealth, playerStats.MaxHealth);
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    // 참조가 비어 있으면 자동으로 찾아주는 헬퍼입니다.
    private void EnsureReferences()
    {
        if (playerStats == null)
            playerStats = FindObjectOfType<PlayerStats>();

        if (maskUI == null)
            maskUI = FindObjectOfType<MaskUI>();
    }

    // Model(PlayerStats)의 체력 변경 이벤트에 구독합니다.
    private void Subscribe()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged += HandleHealthChanged;
    }

    private void Unsubscribe()
    {
        if (playerStats != null)
            playerStats.OnHealthChanged -= HandleHealthChanged;
    }

    // 체력이 변경될 때마다 호출되어 MaskUI 값을 갱신합니다.
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (maskUI == null)
            return;

        int maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
        int hp = Mathf.Clamp(Mathf.RoundToInt(currentHealth), 0, maxHp);

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

