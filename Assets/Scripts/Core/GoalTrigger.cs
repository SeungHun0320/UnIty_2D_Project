// Goal 트리거에 플레이어가 진입하면 스테이지 클리어를 알립니다.
public class GoalTrigger : StageTriggerBase
{
    protected override void OnTriggered() => GameInstance.Instance?.OnStageClear();
}
