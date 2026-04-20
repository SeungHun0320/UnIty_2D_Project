using System;
using System.Collections.Generic;

// 전역 이벤트 버스입니다. (DIP / 느슨한 결합)
// 발행자(Publisher)와 구독자(Subscriber)가 서로를 직접 참조하지 않고 이벤트를 주고받습니다.
//
// 사용 예시:
//   EventBus.Subscribe<PlayerDeadEvent>(OnPlayerDead);
//   EventBus.Publish(new PlayerDeadEvent());
//   EventBus.Unsubscribe<PlayerDeadEvent>(OnPlayerDead);
public static class EventBus
{
    private static readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public static void Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
            list.Remove(handler);
    }

    public static void Publish<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list)) return;
        // 순회 중 컬렉션 수정 방지를 위해 복사본 사용
        foreach (var handler in list.ToArray())
            ((Action<T>)handler).Invoke(evt);
    }

    // 씬 전환 등 전체 초기화가 필요한 경우 사용합니다.
    public static void Clear() => _handlers.Clear();
}

// ---------- 게임 이벤트 정의 ----------

public readonly struct PlayerDeadEvent { }

public readonly struct EnemyDeadEvent
{
    public readonly UnityEngine.GameObject Enemy;
    public EnemyDeadEvent(UnityEngine.GameObject enemy) { Enemy = enemy; }
}

public readonly struct HealthChangedEvent
{
    public readonly float CurrentHealth;
    public readonly float MaxHealth;
    public readonly bool IsPlayer;
    public HealthChangedEvent(float current, float max, bool isPlayer)
    {
        CurrentHealth = current;
        MaxHealth = max;
        IsPlayer = isPlayer;
    }
}

public readonly struct GameStateChangedEvent
{
    public readonly GameState NewState;
    public GameStateChangedEvent(GameState state) { NewState = state; }
}

public readonly struct StageClearEvent { }

public readonly struct PlayerRespawnEvent
{
    public readonly UnityEngine.Vector3 Position;
    public PlayerRespawnEvent(UnityEngine.Vector3 pos) { Position = pos; }
}

// 플레이어 공격이 적에게 적중할 때 발행됩니다. IsMelee=true면 근접 공격 (소울 충전 대상)
public readonly struct EnemyHitByPlayerEvent
{
    public readonly bool IsMelee;
    public EnemyHitByPlayerEvent(bool isMelee) { IsMelee = isMelee; }
}

// 적 공격이 플레이어에게 적중할 때 발행됩니다. 히트렉·카메라 셰이크 트리거용
public readonly struct PlayerHitByEnemyEvent
{
    public readonly UnityEngine.Vector2 HitDirection; // 피격 방향 (적→플레이어)
    public PlayerHitByEnemyEvent(UnityEngine.Vector2 hitDirection) { HitDirection = hitDirection; }
}

// 스테이지 재시작 시 발행됩니다. EnemySpawner가 구독해 적을 재생성합니다.
public readonly struct StageRestartEvent { }

// 일시정지 / 재개 시 발행됩니다. UIManager가 PausePanel 표시/숨김에 사용합니다.
public readonly struct GamePausedEvent { }
public readonly struct GameResumedEvent { }
