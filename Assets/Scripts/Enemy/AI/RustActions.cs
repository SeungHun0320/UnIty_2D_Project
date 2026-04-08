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
        if (Blackboard.playerTransform == null || Blackboard.selfTransform == null || Blackboard.rb == null)
            return BTNodeState.Failure;

        float selfX = Blackboard.selfTransform.position.x;
        float targetX = Blackboard.playerTransform.position.x;
        float dirX = Mathf.Sign(targetX - selfX);

        // 공격 사거리 안이면 멈춥니다.
        if (Blackboard.DistanceToPlayer <= Blackboard.attackRange)
        {
            Blackboard.rb.linearVelocity = new Vector2(0f, Blackboard.rb.linearVelocity.y);
            Blackboard.animationDriver?.SetMoving(false);
            return BTNodeState.Success;
        }

        // 벽 감지: 진행 방향 앞에 벽이 있으면 멈춥니다.
        if (IsWallAhead(dirX))
        {
            Blackboard.rb.linearVelocity = new Vector2(0f, Blackboard.rb.linearVelocity.y);
            Blackboard.animationDriver?.SetMoving(false);
            return BTNodeState.Running;
        }

        // 낙하 감지: 발 앞에 지면이 없으면 멈춥니다.
        if (IsLedgeAhead(dirX))
        {
            Blackboard.rb.linearVelocity = new Vector2(0f, Blackboard.rb.linearVelocity.y);
            Blackboard.animationDriver?.SetMoving(false);
            return BTNodeState.Running;
        }

        // X축만 이동, Y는 Rigidbody2D Physics(중력)에 맡깁니다.
        Blackboard.rb.linearVelocity = new Vector2(dirX * Blackboard.moveSpeed, Blackboard.rb.linearVelocity.y);

        // 좌우 방향에 따라 스케일 반전
        Vector3 scale = Blackboard.selfTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * dirX;
        Blackboard.selfTransform.localScale = scale;

        Blackboard.animationDriver?.SetMoving(true);
        return BTNodeState.Running;
    }

    private static readonly RaycastHit2D[] _rayBuffer = new RaycastHit2D[8];

    // 진행 방향 앞에 벽이 있는지 감지합니다. (자기 자신 제외)
    private bool IsWallAhead(float dirX)
    {
        if (Blackboard.col == null) return false;
        Bounds b = Blackboard.col.bounds;
        Vector2 origin = new Vector2(dirX > 0f ? b.max.x : b.min.x, b.center.y);
        int count = Physics2D.RaycastNonAlloc(origin, Vector2.right * dirX,
            _rayBuffer, Blackboard.wallCheckDistance, Blackboard.groundLayers);
        for (int i = 0; i < count; i++)
            if (_rayBuffer[i].collider != null && _rayBuffer[i].collider != Blackboard.col)
                return true;
        return false;
    }

    // 발 앞쪽 아래에 지면이 없는지(낙하 위험) 감지합니다. (자기 자신 제외)
    private bool IsLedgeAhead(float dirX)
    {
        if (Blackboard.col == null) return false;
        Bounds b = Blackboard.col.bounds;
        Vector2 origin = new Vector2(dirX > 0f ? b.max.x : b.min.x, b.min.y);
        int count = Physics2D.RaycastNonAlloc(origin, Vector2.down,
            _rayBuffer, Blackboard.ledgeCheckDistance, Blackboard.groundLayers);
        for (int i = 0; i < count; i++)
            if (_rayBuffer[i].collider != null && _rayBuffer[i].collider != Blackboard.col)
                return false; // 지면 있음
        return true; // 지면 없음 = 낙하 위험
    }
}

public class RustAttackActionNode : RustActionNode
{
    private bool _attackStarted;
    private float _attackStartTime;

    public RustAttackActionNode(RustBlackboard blackboard) : base(blackboard) { }

    protected override void OnStart()
    {
        _attackStarted = false;
        _attackStartTime = 0f;
        // 이전 공격이 중단된 채로 남아있을 경우를 대비해 히트박스를 초기화합니다.
        Blackboard.AttackHitbox?.Deactivate();
    }

    protected override BTNodeState OnUpdate()
    {
        if (Blackboard.animationDriver == null)
            return BTNodeState.Failure;

        if (!_attackStarted)
        {
            Blackboard.animationDriver.SetMoving(false);
            Blackboard.animationDriver.PlayAttack();

            // 히트박스 활성화 → 데미지는 PlayerHitReceiver가 처리합니다.
            Blackboard.AttackHitbox?.Activate();

            _attackStarted = true;
            _attackStartTime = Time.time;
            return BTNodeState.Running;
        }

        // 공격 애니메이션이 충분히 재생될 때까지 Running 상태를 유지합니다.
        if (Time.time - _attackStartTime < Blackboard.attackDuration)
            return BTNodeState.Running;

        // 공격 종료 → 히트박스 비활성화
        Blackboard.AttackHitbox?.Deactivate();
        return BTNodeState.Success;
    }
}

// 테스트 체크리스트:
// - 추격 노드가 플레이어 방향으로 연속 이동하는지 확인
// - 공격 노드가 첫 Tick에서만 PlayAttack을 호출하는지 확인
