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
        // 1. 애니메이션 재생
        ctx.Anim.PlayAnimation(animationKey);

        // 2. hitbox.delay 후 투사체 발사
        yield return new WaitForSeconds(hitbox.delay);
        SpawnProjectile(ctx);
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
        Vector2 spawnPos = (Vector2)ctx.Origin.position
            + new Vector2(spawnOffset.x * dirX, spawnOffset.y);

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile proj = go.GetComponent<Projectile>();

        float damage = ctx.Stats != null
            ? ctx.Stats.TotalAttackPower * damageMultiplier
            : damageMultiplier;

        proj?.Init(new Vector2(dirX, 0f), damage);
    }
}
