using UnityEngine;

// 플레이어 스파인 캐릭터의 단순 상태머신입니다.
// 이동/공격 입력을 상태로 묶어 SpineAnimationDriver에 전달합니다.

public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
}

[RequireComponent(typeof(SpineAnimationDriver))]
public class PlayerStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpineAnimationDriver animationDriver;

    [Header("State")]
    [SerializeField] private PlayerState currentState = PlayerState.Idle;

    public PlayerState CurrentState => currentState;

    private void Awake()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();

        // 시작 시 상태를 Idle로 맞추고 애니메이션도 Idle로 설정합니다.
        ChangeState(PlayerState.Idle, true);
    }

    public void OnMoveInput(Vector2 move, float movingThreshold)
    {
        bool isMoving = move.sqrMagnitude > movingThreshold * movingThreshold;

        if (!isMoving)
        {
            ChangeState(PlayerState.Idle);
            return;
        }

        ChangeState(PlayerState.Moving);
    }

    public void OnAttackInput()
    {
        // 공격은 일시적인 상태로 취급하고 SpineAnimationDriver에 위임합니다.
        ChangeState(PlayerState.Attacking, true);
        animationDriver.PlayAttack();
    }

    private void ChangeState(PlayerState newState, bool force = false)
    {
        if (!force && newState == currentState)
            return;

        currentState = newState;

        switch (currentState)
        {
            case PlayerState.Idle:
                animationDriver.SetMoving(false);
                break;
            case PlayerState.Moving:
                animationDriver.SetMoving(true);
                break;
            case PlayerState.Attacking:
                // 공격 애니메이션 전환은 OnAttackInput에서 처리합니다.
                break;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (animationDriver == null)
            animationDriver = GetComponent<SpineAnimationDriver>();
    }
#endif
}

// 테스트 체크리스트:
// - 이동 입력 시 Idle -> Moving 애니메이션 전환 확인
// - 입력이 0이 되면 Moving -> Idle 전환 확인
// - 공격 입력 시 Attack 애니메이션 재생 및 이후 Idle 복귀 확인
