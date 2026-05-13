// 씬 왼쪽 끝(시작 지점 부근)에 배치합니다. 플레이어가 진입하면 이전 씬으로 이동합니다.
public class BackTrigger : StageTriggerBase
{
    protected override void OnTriggered() => GameInstance.Instance?.LoadPreviousStage();
}
