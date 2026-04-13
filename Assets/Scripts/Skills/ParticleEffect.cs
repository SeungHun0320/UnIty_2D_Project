using UnityEngine;

// ParticleSystem 기반 이펙트입니다.
// 파티클 프리팹을 스폰하고 재생 완료 후 자동으로 제거합니다.
[CreateAssetMenu(fileName = "ParticleEffect", menuName = "Game/Skills/Effects/ParticleEffect")]
public class ParticleEffect : SkillEffect
{
    [Header("Particle Effect")]
    public GameObject particlePrefab;

    public override void Play(SkillContext ctx)
    {
        if (particlePrefab == null)
        {
            Debug.LogWarning("[ParticleEffect] particlePrefab이 할당되지 않았습니다.");
            return;
        }

        var (pos, dirX) = GetSpawnInfo(ctx);
        GameObject go   = Object.Instantiate(particlePrefab, pos, Quaternion.identity);
        ApplyTransform(go, ctx, dirX);

        // 파티클 재생 완료 후 자동 제거합니다.
        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps != null)
            Object.Destroy(go, ps.main.duration + ps.main.startLifetime.constantMax);
        else
            Object.Destroy(go, 3f);
    }
}
