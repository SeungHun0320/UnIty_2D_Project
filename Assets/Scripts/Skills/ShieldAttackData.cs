using System.Collections;
using UnityEngine;

// 방패 공격 스킬입니다. shield_attack 애니메이션을 재생하고 투사체를 발사합니다.
[CreateAssetMenu(fileName = "ShieldAttack", menuName = "Game/Skills/ShieldAttack")]
public class ShieldAttackData : SkillData
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float      damageMultiplier = 1.5f;
    public Vector2    spawnOffset      = new Vector2(0.5f, 0f);

    [Header("Animation")]
    public string animationKey = "shield_attack";

    public override IEnumerator Execute(SkillContext ctx)
    {
        // 1. 애니메이션 + 이펙트 재생
        ctx.Anim.PlayAnimation(animationKey);
        PlayEffects(ctx);

        // 2. 투사체 즉시 발사 — yield 이전에 spawn해야 피격으로 스킬이 취소돼도 투사체가 보장됩니다.
        //    발사 직후 정지 구간은 speedCurve 초기값(0)이 담당합니다.
        SpawnProjectile(ctx);

        // 3. hitbox.delay 동안 스킬 상태 유지 (연속 사용 방지)
        yield return new WaitForSeconds(hitbox.delay);
    }

    private void SpawnProjectile(SkillContext ctx)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[ShieldAttackData] projectilePrefab이 할당되지 않았습니다.");
            return;
        }

        // localScale.x 부호로 바라보는 방향 판별
        float dirX = Mathf.Sign(ctx.Origin.localScale.x);
        // SpawnPoint가 지정된 경우 해당 위치 사용, 없으면 Origin + offset으로 폴백
        Vector2 spawnPos = ctx.SpawnPoint != null
            ? (Vector2)ctx.SpawnPoint.position
            : (Vector2)ctx.Origin.position + new Vector2(spawnOffset.x * dirX, spawnOffset.y);

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        // localScale.x로 방향을 잡으면 애니메이션 flipX가 방향과 무관하게 동작합니다.
        go.transform.localScale = new Vector3(dirX, 1f, 1f);
        Projectile proj = go.GetComponent<Projectile>();

        float damage = ctx.Stats != null
            ? ctx.Stats.TotalAttackPower * damageMultiplier
            : damageMultiplier;

        proj?.Init(new Vector2(dirX, 0f), damage);
    }
}
