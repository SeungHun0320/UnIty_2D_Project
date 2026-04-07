using UnityEngine;
using UnityEngine.InputSystem;

// 플레이 모드 디버그 키 전용 컴포넌트입니다. (SRP)
// PlayerStats에서 분리되어 게임 로직과 디버그 키를 독립적으로 관리합니다.
// 빌드 시 제거하거나 비활성화할 수 있습니다.
public class PlayerDebugController : MonoBehaviour
{
    [Header("Target Stats")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Debug Steps")]
    [Tooltip("F3/F4 한 번에 증감할 체력 양입니다.")]
    [SerializeField] private float healthStep = 10f;
    [Tooltip("F5 한 번에 증감할 최대 체력 양입니다.")]
    [SerializeField] private float maxHealthStep = 10f;

    private void Awake()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || playerStats == null) return;

        // F3: 체력 감소 (데미지 테스트)
        if (keyboard.f3Key.wasPressedThisFrame)
            playerStats.TakeDamage(healthStep);

        // F4: 체력 증가 (회복 테스트)
        if (keyboard.f4Key.wasPressedThisFrame)
            playerStats.Heal(healthStep);

        // F5: 최대 체력 증가
        if (keyboard.f5Key.wasPressedThisFrame)
            playerStats.AddMaxHealth(maxHealthStep);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerStats == null)
            playerStats = GetComponent<PlayerStats>();
    }
#endif
}
