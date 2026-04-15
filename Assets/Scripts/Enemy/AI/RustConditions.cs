using UnityEngine;

// 러스트 전용 조건 노드들입니다.
// 블랙보드 값을 검사해 트리 분기를 제어합니다.

public abstract class RustConditionNode : BTNode
{
    protected readonly RustBlackboard Blackboard;

    protected RustConditionNode(RustBlackboard blackboard)
    {
        Blackboard = blackboard;
    }
}

public class IsRustDeadNode : RustConditionNode
{
    public IsRustDeadNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        return Blackboard.IsDead ? BTNodeState.Success : BTNodeState.Failure;
    }
}

public class IsPlayerInSightNode : RustConditionNode
{
    public IsPlayerInSightNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        if (Blackboard.playerTransform == null)
            return BTNodeState.Failure;

        return Blackboard.DistanceToPlayer <= Blackboard.sightRange
            ? BTNodeState.Success
            : BTNodeState.Failure;
    }
}

public class IsPlayerInAttackRangeNode : RustConditionNode
{
    public IsPlayerInAttackRangeNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        if (Blackboard.playerTransform == null)
            return BTNodeState.Failure;

        return Blackboard.DistanceToPlayer <= Blackboard.attackRange
            ? BTNodeState.Success
            : BTNodeState.Failure;
    }
}

// 피격 상태 여부를 검사합니다. hitStateBranch 최상위 조건 노드로 사용됩니다.
public class IsEnemyInHitStateNode : RustConditionNode
{
    public IsEnemyInHitStateNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override BTNodeState OnUpdate()
    {
        return Blackboard.IsInHitState ? BTNodeState.Success : BTNodeState.Failure;
    }
}

// 테스트 체크리스트:
// - 체력이 0 이하일 때 IsRustDeadNode가 Success를 반환하는지 확인
// - 플레이어 거리 변화에 따라 Sight/AttackRange 조건 결과가 바뀌는지 확인
