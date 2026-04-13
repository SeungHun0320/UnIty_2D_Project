using UnityEngine;

// 스킬 이펙트의 추상 베이스 ScriptableObject입니다. (OCP)
// 새 이펙트 타입은 이 클래스를 상속해 Play()만 구현하면 됩니다.
public abstract class SkillEffect : ScriptableObject
{
    [Header("Transform")]
    public Vector2 offset       = Vector2.zero;
    public bool    followPlayer = false;

    public abstract void Play(SkillContext ctx);

    // 스폰 위치와 방향을 계산하는 공통 헬퍼입니다.
    protected (Vector2 pos, float dirX) GetSpawnInfo(SkillContext ctx)
    {
        float   dirX = Mathf.Sign(ctx.Origin.localScale.x);
        Vector2 pos  = (Vector2)ctx.Origin.position
                     + new Vector2(offset.x * dirX, offset.y);
        return (pos, dirX);
    }

    // 스폰 후 방향 및 부모 설정을 적용하는 공통 헬퍼입니다.
    // flipX로 방향을 처리하여 Z 회전 애니메이션이 양쪽에서 동일하게 재생됩니다.
    protected void ApplyTransform(GameObject go, SkillContext ctx, float dirX)
    {
        SpriteRenderer sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.flipX = dirX < 0f;

        if (followPlayer)
            go.transform.SetParent(ctx.Origin);
    }
}
