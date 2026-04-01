using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어 전용 스탯 컴포넌트입니다.
// 공통 스탯(CharacterStats)을 상속받아 플레이어만의 확장 포인트를 제공합니다.
public class PlayerStats : CharacterStats
{
    [Header("Player Only")]
    [Tooltip("플레이어 전용 추가 공격력(버프 등)에 사용합니다.")]
    [SerializeField] private float bonusAttackPower = 0f;

    [Header("Debug (Play Mode Only)")]
    [Tooltip("플레이 중 F3/F4/F5로 체력/최대체력을 디버그 조정할지 여부입니다.")]
    [SerializeField] private bool enableDebugKeys = true;
    [Tooltip("F3/F4 한 번에 증감할 체력 양입니다.")]
    [SerializeField] private float debugHealthStep = 10f;
    [Tooltip("F5 한 번에 증감할 최대 체력 양입니다.")]
    [SerializeField] private float debugMaxHealthStep = 10f;

    // 총 공격력을 계산할 때 사용할 수 있는 프로퍼티입니다.
    public float TotalAttackPower => AttackPower + bonusAttackPower;

    private void Update()
    {
        if (!enableDebugKeys) return;
        if (!Application.isPlaying) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // F3: 체력 감소 (데미지)
        if (keyboard.f3Key.wasPressedThisFrame)
        {
            TakeDamage(debugHealthStep);
        }

        // F4: 체력 증가 (회복)
        if (keyboard.f4Key.wasPressedThisFrame)
        {
            Heal(debugHealthStep);
        }

        // F5: 최대 체력 증가
        if (keyboard.f5Key.wasPressedThisFrame)
        {
            AddMaxHealth(debugMaxHealthStep);
        }
    }
}

