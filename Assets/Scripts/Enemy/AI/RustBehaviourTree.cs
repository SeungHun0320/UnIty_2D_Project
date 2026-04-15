using System.Collections.Generic;
using UnityEngine;

// 러스트 몬스터 전용 비헤이비어 트리를 구성하고 실행하는 컴포넌트입니다.
// 매 프레임 루트 노드를 Tick 하여 AI 의사결정을 수행합니다.

[RequireComponent(typeof(RustBlackboard))]
public class RustBehaviourTree : MonoBehaviour
{
    [SerializeField] private RustBlackboard blackboard;

    private BTNode _root;

    private void Awake()
    {
        if (blackboard == null)
            blackboard = GetComponent<RustBlackboard>();

        BuildTree();
    }

    private void FixedUpdate()
    {
        if (_root == null)
            return;

        _root.Tick();
    }

    private void BuildTree()
    {
        // Dead 브랜치: 죽었으면 이후 로직을 모두 막습니다.
        var deadBranch = new BTSequence(new BTNode[]
        {
            new IsRustDeadNode(blackboard),
            new RustIdleActionNode(blackboard) // 추후 사망 전용 애니메이션 노드로 교체 가능
        });

        // 피격 경직 브랜치: 피격 상태 동안 Running을 반환해 이후 브랜치를 차단합니다.
        var hitStateBranch = new BTSequence(new BTNode[]
        {
            new IsEnemyInHitStateNode(blackboard),
            new RustHitStateActionNode(blackboard)
        });

        // 공격 브랜치: 시야 안 + 공격 거리 안일 때 공격 시도.
        var attackBranch = new BTSequence(new BTNode[]
        {
            new IsPlayerInSightNode(blackboard),
            new IsPlayerInAttackRangeNode(blackboard),
            new RustAttackActionNode(blackboard)
        });

        // 추격 브랜치: 시야 안이지만 아직 공격 거리가 아닐 때.
        var chaseBranch = new BTSequence(new BTNode[]
        {
            new IsPlayerInSightNode(blackboard),
            new RustChasePlayerActionNode(blackboard)
        });

        // 전투 셀렉터: 공격 우선, 그 다음 추격.
        var combatSelector = new BTSelector(new BTNode[]
        {
            attackBranch,
            chaseBranch
        });

        // 평상시 브랜치: 아무 조건도 맞지 않으면 Idle.
        var idleBranch = new RustIdleActionNode(blackboard);

        // 루트 셀렉터: Dead > HitState > Combat > Idle 순으로 평가.
        _root = new BTSelector(new List<BTNode>
        {
            deadBranch,
            hitStateBranch,
            combatSelector,
            idleBranch
        });
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (blackboard == null)
            blackboard = GetComponent<RustBlackboard>();
    }
#endif
}

// 테스트 체크리스트:
// - 플레이어가 없을 때 러스트가 Idle 상태를 유지하는지 확인
// - 플레이어가 시야/공격 범위에 따라 추격/공격으로 자연스럽게 전환되는지 확인
