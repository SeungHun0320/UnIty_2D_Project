using UnityEngine;

// 간단한 비헤이비어 트리 노드 베이스 클래스입니다.
// 각 노드는 Tick으로 한 스텝씩 평가되고 상태를 반환합니다.

public enum BTNodeState
{
    Running,
    Success,
    Failure
}

public abstract class BTNode
{
    public BTNodeState State { get; private set; } = BTNodeState.Running;

    public BTNodeState Tick()
    {
        if (State != BTNodeState.Running)
        {
            OnStart();
        }

        State = OnUpdate();

        if (State != BTNodeState.Running)
        {
            OnStop();
        }

        return State;
    }

    // 노드가 처음 실행될 때 한 번 호출됩니다.
    protected virtual void OnStart() { }

    // 노드의 메인 로직을 수행하고 상태를 반환합니다.
    protected abstract BTNodeState OnUpdate();

    // 노드가 종료될 때(성공/실패) 한 번 호출됩니다.
    protected virtual void OnStop() { }

    public void Reset()
    {
        State = BTNodeState.Running;
    }
}

// 테스트 체크리스트:
// - 노드가 Success/Failure 이후 다시 Tick 시 OnStart가 한 번만 호출되는지 확인
// - Reset 호출 후 상태가 Running으로 초기화되는지 확인
