using System.Collections.Generic;
using UnityEngine;

// 컴포지트 노드는 여러 자식 노드를 순차/선택적으로 실행합니다.
// 시퀀스/셀렉터로 기본적인 트리 구조를 만듭니다.

public abstract class BTComposite : BTNode
{
    protected readonly List<BTNode> children = new List<BTNode>();

    protected BTComposite(IEnumerable<BTNode> children)
    {
        this.children.AddRange(children);
    }
}

public class BTSequence : BTComposite
{
    private int _currentIndex;

    public BTSequence(IEnumerable<BTNode> children) : base(children) { }

    protected override void OnStart()
    {
        _currentIndex = 0;
    }

    protected override BTNodeState OnUpdate()
    {
        while (_currentIndex < children.Count)
        {
            BTNode child = children[_currentIndex];
            BTNodeState result = child.Tick();

            if (result == BTNodeState.Running)
                return BTNodeState.Running;

            if (result == BTNodeState.Failure)
                return BTNodeState.Failure;

            _currentIndex++;
        }

        return BTNodeState.Success;
    }
}

public class BTSelector : BTComposite
{
    public BTSelector(IEnumerable<BTNode> children) : base(children) { }

    // 매 Tick마다 처음부터 재평가합니다. (Reactive Priority Selector)
    // 높은 우선순위 브랜치가 언제든 끼어들 수 있도록 index를 유지하지 않습니다.
    protected override BTNodeState OnUpdate()
    {
        foreach (var child in children)
        {
            BTNodeState result = child.Tick();

            if (result == BTNodeState.Running)
                return BTNodeState.Running;

            if (result == BTNodeState.Success)
                return BTNodeState.Success;
        }

        return BTNodeState.Failure;
    }
}

// 테스트 체크리스트:
// - 시퀀스: 중간 자식이 Failure일 때 전체 Failure 되는지 확인
// - 셀렉터: 첫 Success 자식에서 멈추고 Success 반환하는지 확인
