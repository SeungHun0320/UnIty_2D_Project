using UnityEngine;

// 러스트 전용 행동 노드들입니다.
// 이동, 공격, 대기 등 구체적인 동작을 수행합니다.

public abstract class RustActionNode : BTNode
{
    protected readonly RustBlackboard Blackboard;

    protected RustActionNode(RustBlackboard blackboard)
    {
        Blackboard = blackboard;
    }
}

public class RustIdleActionNode : RustActionNode
{
    public RustIdleActionNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        if (Blackboard.animationDriver != null)
            Blackboard.animationDriver.PlayIdle();

        return BTNodeState.Success;
    }
}

public class RustChasePlayerActionNode : RustActionNode
{
    public RustChasePlayerActionNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        // 플레이어 / 자기 자신 참조가 없으면 추격 실패로 처리합니다.
        if (Blackboard.playerTransform == null || Blackboard.selfTransform == null)
            return BTNodeState.Failure;

        Vector3 selfPos = Blackboard.selfTransform.position;
        Vector3 targetPos = Blackboard.playerTransform.position;

        Vector3 dir = (targetPos - selfPos);
        dir.z = 0f;

        // 공격 사거리(또는 그 근처) 안으로 들어오면 더 이상 다가가지 않습니다.
        float distance = dir.magnitude;
        if (distance <= Blackboard.attackRange)
        {
            if (Blackboard.animationDriver != null)
                Blackboard.animationDriver.SetMoving(false);

            return BTNodeState.Success;
        }

        dir.Normalize();
        Vector3 delta = dir * (Blackboard.moveSpeed * Time.deltaTime);
        Blackboard.selfTransform.position += delta;

        // 좌우 방향에 따라 스케일 반전
        if (Mathf.Abs(dir.x) > 0.001f)
        {
            Vector3 scale = Blackboard.selfTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * Mathf.Sign(dir.x);
            Blackboard.selfTransform.localScale = scale;
        }

        if (Blackboard.animationDriver != null)
            Blackboard.animationDriver.SetMoving(true);

        return BTNodeState.Running;
    }
}

public class RustAttackActionNode : RustActionNode
{
    private bool _attackStarted;
    private float _attackStartTime;

    public RustAttackActionNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override void OnStart()
    {
        // 공격 시작 시 상태를 초기화합니다.
        _attackStarted = false;
        _attackStartTime = 0f;
    }

    protected override BTNodeState OnUpdate()
    {
        if (Blackboard.animationDriver == null)
            return BTNodeState.Failure;

        if (!_attackStarted)
        {
            // 공격을 시작할 때는 이동 플래그를 내려서 다른 이동 애니메이션이 끼어들지 않도록 합니다.
            if (Blackboard.animationDriver != null)
                Blackboard.animationDriver.SetMoving(false);

            Blackboard.animationDriver.PlayAttack();
            _attackStarted = true;
            _attackStartTime = Time.time;
            return BTNodeState.Running;
        }

        // 공격 애니메이션이 충분히 재생될 때까지 Running 상태를 유지합니다.
        if (Time.time - _attackStartTime < Blackboard.attackDuration)
        {
            return BTNodeState.Running;
        }

        // 충분한 시간이 지난 뒤에야 Success 로 처리하여
        // 비헤이비어 트리가 다시 다음 공격 여부를 판단하도록 합니다.
        return BTNodeState.Success;
    }
}

// 테스트 체크리스트:
// - 추격 노드가 플레이어 방향으로 연속 이동하는지 확인
// - 공격 노드가 첫 Tick에서만 PlayAttack을 호출하는지 확인
