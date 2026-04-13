using UnityEngine;

// Animator 기반 스프라이트 이펙트입니다.
// AnimatorController가 붙은 프리팹을 스폰하고 자동으로 재생합니다.
[CreateAssetMenu(fileName = "SpriteEffect", menuName = "Game/Skills/Effects/SpriteEffect")]
public class SpriteEffect : SkillEffect
{
    [Header("Sprite Effect")]
    public GameObject effectPrefab;         // SpriteRenderer + Animator가 붙은 프리팹
    public float      destroyAfter = 0f;    // 자동 제거 시간 (0이면 Animator 길이 자동 감지)

    public override void Play(SkillContext ctx)
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("[SpriteEffect] effectPrefab이 할당되지 않았습니다.");
            return;
        }

        var (pos, dirX) = GetSpawnInfo(ctx);

        // 방향 미러링 Wrapper: 부모의 scale.x를 반전해 Z 회전 방향도 자동으로 대칭 처리합니다.
        GameObject wrapper = new GameObject("EffectWrapper");
        wrapper.transform.position   = pos;
        wrapper.transform.localScale = new Vector3(dirX, 1f, 1f);

        GameObject go = Object.Instantiate(effectPrefab, pos, Quaternion.identity, wrapper.transform);
        go.transform.localScale = effectPrefab.transform.localScale; // 프리팹 원본 스케일 유지

        if (followPlayer)
            wrapper.transform.SetParent(ctx.Origin);

        if (destroyAfter > 0f)
            Object.Destroy(wrapper, destroyAfter);
        else
            go.AddComponent<EffectLifetimeHelper>(); // Animator 길이 자동 감지
    }
}
