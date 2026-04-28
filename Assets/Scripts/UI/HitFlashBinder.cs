using UnityEngine;

// 플레이어 체력 감소를 감지해 HitFlashUI.Flash()를 호출하는 ViewModel입니다.
// PlayerStatsBinder를 통해 씬 로드 후 PlayerStats 재연결을 자동 처리합니다.
public class HitFlashBinder : PlayerStatsBinder
{
    [Header("View")]
    [SerializeField] private HitFlashUI hitFlashUI;

    private float _lastHealth = float.MaxValue;

    protected override void Awake()
    {
        base.Awake();
        if (hitFlashUI == null)
            hitFlashUI = FindAnyObjectByType<HitFlashUI>();
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
            _lastHealth = playerStats.CurrentHealth;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        if (currentHealth < _lastHealth && hitFlashUI != null)
            hitFlashUI.Flash();
        _lastHealth = currentHealth;
    }
}
