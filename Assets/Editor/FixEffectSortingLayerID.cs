using UnityEditor;
using UnityEngine;

public class FixEffectSortingLayerID
{
    public static void Execute()
    {
        int effectID = SortingLayer.NameToID("Effect");
        if (effectID == 0)
        {
            Debug.LogError("[FixEffectSortingLayerID] Effect 레이어를 찾을 수 없습니다.");
            return;
        }

        string[] prefabPaths = new[]
        {
            "Assets/Prefabs/Items/GeoPickup.prefab",
            "Assets/Prefabs/Skills/Projectile.prefab",
            "Assets/Prefabs/Skills/ShieldAuraEffect.prefab",
        };

        foreach (var path in prefabPaths)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(path);
            var renderers = scope.prefabContentsRoot.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                sr.sortingLayerID = effectID;
                sr.sortingLayerName = "Effect";
            }
            Debug.Log($"[FixEffectSortingLayerID] {path} → sortingLayerID={effectID} 적용 완료");
        }
    }
}
