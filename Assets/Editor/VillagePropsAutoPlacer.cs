// 한국어: Cainos Village Props를 Map_Deco에 자동 배치하는 에디터 툴입니다.
// 한국어: 씬 구조(Trigger/Map_Deco)를 기준으로 초안 레이아웃을 빠르게 만듭니다.

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class VillagePropsAutoPlacer
{
    private const string MenuPath = "Tools/Village Props/Auto Place (Map_Deco)";
    private const string ClearMenuPath = "Tools/Village Props/Clear Auto Layout (Map_Deco)";
    private const string PlaceGroundMenuPath = "Tools/Village Props/Place Ground Collision (GroundTilemap)";
    private const string ClearGroundMenuPath = "Tools/Village Props/Clear Ground Collision (GroundTilemap)";

    [MenuItem(MenuPath)]
    public static void AutoPlaceOnMapDeco()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[VillagePropsAutoPlacer] PlayMode 전환 중이라 배치를 중단했습니다.");
            return;
        }

        var mapDeco = FindGameObjectByPath("Map/Map_Deco");
        if (mapDeco == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'Map/Map_Deco'를 찾지 못했습니다. 하이어라키 구조를 확인해주세요.");
            return;
        }

        var start = GameObject.Find("Trigger_StartPoint");
        var goal = GameObject.Find("Trigger_Goal");

        var startPos = start != null ? start.transform.position : Vector3.zero;
        var goalPos = goal != null ? goal.transform.position : startPos + new Vector3(12f, 0f, 0f);
        if (Vector2.Distance(startPos, goalPos) < 1f) goalPos = startPos + new Vector3(12f, 0f, 0f);

        var container = EnsureChild(mapDeco.transform, "VillageProps_AutoLayout");

        var prefabs = LoadCandidatePrefabs();
        if (prefabs.Count == 0)
        {
            Debug.LogError("[VillagePropsAutoPlacer] Cainos Village Props 프리팹을 찾지 못했습니다. 임포트 경로를 확인해주세요.");
            return;
        }

        // 기존 배치물을 바로 삭제하진 않고, 새로 생성되는 오브젝트만 추가합니다.
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Auto place village props");
        var undoGroup = Undo.GetCurrentGroup();

        var layout = ComputeLayoutLine(startPos, goalPos);
        var groundBounds = TryGetGroundWorldBounds();
        var yBase = groundBounds.HasValue ? groundBounds.Value.max.y : Mathf.Max(startPos.y, goalPos.y);

        var rng = new System.Random(20260402);

        // 이전 버전(10~28개)보다 더 “맵이 채워지는 느낌”을 내도록 늘립니다.
        var count = Mathf.Clamp(Mathf.RoundToInt(layout.length / 1.1f), 24, 80);

        var created = 0;
        for (var i = 0; i < count; i++)
        {
            // 지면/중경/후경 3줄로 분산 배치
            var bandRoll = rng.NextDouble();
            var band = bandRoll < 0.72 ? PlacementBand.Ground : (bandRoll < 0.92 ? PlacementBand.Mid : PlacementBand.Back);

            var prefab = PickPrefab(prefabs, rng, band);

            var t = (i + 0.35f) / (count + 0.7f);
            var along = t * layout.length;

            var pos = layout.start + layout.dir * along + layout.perp * JitterSide(rng, band);
            pos.y = yBase + JitterUp(rng, band) + BandYOffset(band);
            pos.z = 0f;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) continue;

            Undo.RegisterCreatedObjectUndo(instance, "Place village prop");
            instance.transform.SetParent(container, worldPositionStays: true);
            instance.transform.position = pos;

            var local = instance.transform.localScale;
            local.x = (rng.NextDouble() < 0.35) ? -Mathf.Abs(local.x) : Mathf.Abs(local.x);
            instance.transform.localScale = local;

            // 깊이감: 후경은 살짝 축소
            if (band == PlacementBand.Back)
            {
                instance.transform.localScale = new Vector3(
                    instance.transform.localScale.x * 0.92f,
                    instance.transform.localScale.y * 0.92f,
                    instance.transform.localScale.z);
            }

            created++;
        }

        EditorUtility.SetDirty(container.gameObject);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[VillagePropsAutoPlacer] 생성 완료: {created}개 (컨테이너: {container.name})");
        Selection.activeObject = container.gameObject;
    }

    [MenuItem(ClearMenuPath)]
    public static void ClearAutoLayout()
    {
        var mapDeco = FindGameObjectByPath("Map/Map_Deco");
        if (mapDeco == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'Map/Map_Deco'를 찾지 못했습니다.");
            return;
        }

        var container = mapDeco.transform.Find("VillageProps_AutoLayout");
        if (container == null)
        {
            Debug.Log("[VillagePropsAutoPlacer] 삭제할 'VillageProps_AutoLayout'가 없습니다.");
            return;
        }

        Undo.DestroyObjectImmediate(container.gameObject);
        Debug.Log("[VillagePropsAutoPlacer] 'VillageProps_AutoLayout'를 삭제했습니다.");
    }

    [MenuItem(PlaceGroundMenuPath)]
    public static void PlaceGroundCollision()
    {
        // 한국어: GroundTilemap에 Ground Tile을 자동으로 칠합니다.
        // 한국어: 시작~골 구간에 간단한 충돌 플랫폼을 생성합니다.

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[VillagePropsAutoPlacer] PlayMode 전환 중이라 타일 배치를 중단했습니다.");
            return;
        }

        var groundGo = FindGameObjectByPath("Map/Map_Collision/Grid_Collision/GroundTilemap") ?? GameObject.Find("GroundTilemap");
        if (groundGo == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'GroundTilemap'을 찾지 못했습니다.");
            return;
        }

        var tilemap = groundGo.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'GroundTilemap'에 Tilemap 컴포넌트가 없습니다.");
            return;
        }

        var start = GameObject.Find("Trigger_StartPoint");
        var goal = GameObject.Find("Trigger_Goal");
        var startPos = start != null ? start.transform.position : Vector3.zero;
        var goalPos = goal != null ? goal.transform.position : startPos + new Vector3(16f, 0f, 0f);
        if (Vector2.Distance(startPos, goalPos) < 1f) goalPos = startPos + new Vector3(16f, 0f, 0f);

        var groundTile = FindGroundTileCandidate();
        if (groundTile == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'Ground' 타일(TileBase)을 찾지 못했습니다. 타일 에셋 이름에 'Ground'가 포함되어 있는지 확인해주세요.");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Place ground collision tiles");
        var undoGroup = Undo.GetCurrentGroup();

        Undo.RegisterCompleteObjectUndo(tilemap, "Paint ground tiles");

        // 월드 좌표 → 셀 좌표 (타일맵의 transform을 고려)
        var startCell = tilemap.WorldToCell(startPos);
        var goalCell = tilemap.WorldToCell(goalPos);

        // y는 “더 낮은 쪽”을 기준으로 바닥을 잡습니다(지형이 비어있는 경우에도 안전).
        var y = Mathf.Min(startCell.y, goalCell.y);
        var xMin = Mathf.Min(startCell.x, goalCell.x);
        var xMax = Mathf.Max(startCell.x, goalCell.x);

        // 너무 짧으면 기본 길이 확보
        if (xMax - xMin < 8) xMax = xMin + 12;

        // 간단한 구조: 메인 바닥 1줄 + (가끔) 2번째 줄로 두께
        var painted = 0;
        var rng = new System.Random(20260402);

        for (var x = xMin; x <= xMax; x++)
        {
            var pos = new Vector3Int(x, y, 0);
            tilemap.SetTile(pos, groundTile);
            painted++;

            if (rng.NextDouble() < 0.35)
            {
                tilemap.SetTile(new Vector3Int(x, y - 1, 0), groundTile);
                painted++;
            }
        }

        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[VillagePropsAutoPlacer] GroundTilemap에 충돌 타일 배치 완료: {painted}칸 (Tile='{groundTile.name}')");
        Selection.activeObject = groundGo;
    }

    [MenuItem(ClearGroundMenuPath)]
    public static void ClearGroundCollision()
    {
        // 한국어: GroundTilemap의 타일을 전부 지웁니다.
        // 한국어: 실수 방지를 위해 Undo 지원합니다.

        var groundGo = FindGameObjectByPath("Map/Map_Collision/Grid_Collision/GroundTilemap") ?? GameObject.Find("GroundTilemap");
        if (groundGo == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'GroundTilemap'을 찾지 못했습니다.");
            return;
        }

        var tilemap = groundGo.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            Debug.LogError("[VillagePropsAutoPlacer] 'GroundTilemap'에 Tilemap 컴포넌트가 없습니다.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(tilemap, "Clear ground tiles");
        tilemap.ClearAllTiles();
        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);

        Debug.Log("[VillagePropsAutoPlacer] GroundTilemap의 타일을 모두 삭제했습니다.");
        Selection.activeObject = groundGo;
    }

    private static Transform EnsureChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null) return child;

        var go = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(go, "Create auto layout container");
        go.transform.SetParent(parent, worldPositionStays: false);
        return go.transform;
    }

    private static GameObject FindGameObjectByPath(string path)
    {
        var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return null;

        var current = GameObject.Find(parts[0]);
        if (current == null) return null;

        for (var i = 1; i < parts.Length; i++)
        {
            var t = current.transform.Find(parts[i]);
            if (t == null) return null;
            current = t.gameObject;
        }

        return current;
    }

    private static List<GameObject> LoadCandidatePrefabs()
    {
        // Cainos 폴더의 Village Props 프리팹들에서 “기본 데코”만 골라 씁니다.
        var searchRoots = new[]
        {
            "Assets/Cainos/Pixel Art Platformer - Village Props/Prefab",
            "Assets/Cainos"
        };

        var keywords = new[]
        {
            "Grass", "Bush", "Fence", "Barrel", "Crate", "Billboard", "Banner", "Gravestone"
        };

        var results = new List<GameObject>(64);
        var seen = new HashSet<string>();

        foreach (var root in searchRoots)
        {
            var guids = AssetDatabase.FindAssets("t:prefab", new[] { root });
            foreach (var guid in guids)
            {
                if (!seen.Add(guid)) continue;
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (keywords.All(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) < 0)) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) results.Add(prefab);
            }
        }

        return results;
    }

    private static TileBase FindGroundTileCandidate()
    {
        // Cainos 타일셋이 많을 수 있어 이름 기반으로 “Ground”를 우선으로 찾습니다.
        // 1) Cainos 폴더 내 TileBase 검색
        var roots = new[] { "Assets/Cainos" };
        var guids = AssetDatabase.FindAssets("t:TileBase Ground", roots);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null) return tile;
        }

        // 2) 좀 더 넓게(전체 Assets)에서 'Ground' 타일 검색
        guids = AssetDatabase.FindAssets("t:TileBase Ground", new[] { "Assets" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile != null) return tile;
        }

        // 3) 마지막 fallback: TileBase 전체 중 이름에 Ground가 포함된 것
        guids = AssetDatabase.FindAssets("t:TileBase", new[] { "Assets/Cainos" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var tile = AssetDatabase.LoadAssetAtPath<TileBase>(path);
            if (tile == null) continue;
            if (tile.name.IndexOf("Ground", StringComparison.OrdinalIgnoreCase) >= 0) return tile;
        }

        return null;
    }

    private enum PlacementBand
    {
        Ground,
        Mid,
        Back
    }

    private static float BandYOffset(PlacementBand band)
    {
        switch (band)
        {
            case PlacementBand.Ground: return 0f;
            case PlacementBand.Mid: return 0.9f;
            case PlacementBand.Back: return 1.6f;
            default: return 0f;
        }
    }

    private static float JitterSide(System.Random rng, PlacementBand band)
    {
        // 후경으로 갈수록 퍼짐을 줄입니다.
        var range = band == PlacementBand.Ground ? 0.9f : (band == PlacementBand.Mid ? 0.6f : 0.35f);
        return (float)(rng.NextDouble() * (range * 2) - range);
    }

    private static float JitterUp(System.Random rng, PlacementBand band)
    {
        var range = band == PlacementBand.Ground ? 0.35f : (band == PlacementBand.Mid ? 0.25f : 0.2f);
        var baseUp = band == PlacementBand.Ground ? -0.06f : -0.03f;
        return baseUp + (float)(rng.NextDouble() * range);
    }

    private static GameObject PickPrefab(IReadOnlyList<GameObject> prefabs, System.Random rng, PlacementBand band)
    {
        // “풀/덤불” 가중치가 높고, “울타리/통/표지판/배너/묘비”는 밴드에 따라 섞습니다.
        int Weight(string n)
        {
            // 공통
            if (n.IndexOf("Grass", StringComparison.OrdinalIgnoreCase) >= 0) return 10;
            if (n.IndexOf("Bush", StringComparison.OrdinalIgnoreCase) >= 0) return 8;

            var isFence = n.IndexOf("Fence", StringComparison.OrdinalIgnoreCase) >= 0;
            var isBarrel = n.IndexOf("Barrel", StringComparison.OrdinalIgnoreCase) >= 0;
            var isCrate = n.IndexOf("Crate", StringComparison.OrdinalIgnoreCase) >= 0;
            var isBillboard = n.IndexOf("Billboard", StringComparison.OrdinalIgnoreCase) >= 0;
            var isBanner = n.IndexOf("Banner", StringComparison.OrdinalIgnoreCase) >= 0;
            var isGrave = n.IndexOf("Gravestone", StringComparison.OrdinalIgnoreCase) >= 0;

            if (band == PlacementBand.Ground)
            {
                if (isFence) return 7;
                if (isBarrel) return 5;
                if (isCrate) return 5;
                if (isGrave) return 3;
                if (isBillboard) return 2;
                if (isBanner) return 1;
            }
            else if (band == PlacementBand.Mid)
            {
                if (isFence) return 3;
                if (isBarrel) return 2;
                if (isCrate) return 2;
                if (isGrave) return 3;
                if (isBillboard) return 4;
                if (isBanner) return 3;
            }
            else // Back
            {
                // 후경에는 표지판/배너 비중을 올립니다.
                if (isBillboard) return 9;
                if (isBanner) return 7;
                if (isFence) return 2;
                if (isGrave) return 2;
                if (isBarrel) return 1;
                if (isCrate) return 1;
            }

            return 1;
        }

        var total = 0;
        for (var i = 0; i < prefabs.Count; i++) total += Weight(prefabs[i].name);

        var roll = rng.Next(0, Math.Max(1, total));
        for (var i = 0; i < prefabs.Count; i++)
        {
            roll -= Weight(prefabs[i].name);
            if (roll < 0) return prefabs[i];
        }

        return prefabs[prefabs.Count - 1];
    }

    private static (Vector3 start, Vector3 dir, Vector3 perp, float length) ComputeLayoutLine(Vector3 start, Vector3 goal)
    {
        var d = goal - start;
        d.z = 0f;
        var len = Mathf.Max(0.001f, d.magnitude);
        var dir = d / len;
        var perp = new Vector3(-dir.y, dir.x, 0f);
        return (start, dir, perp, len);
    }

    private static Bounds? TryGetGroundWorldBounds()
    {
        // 충돌 타일맵이 있으면 거기 bounds로 대략적인 기준 높이를 잡습니다.
        // 타일이 비어있는 경우(바운드 0)도 있어서 Renderer bounds를 우선 사용합니다.
        var groundTilemap = GameObject.Find("GroundTilemap");
        if (groundTilemap == null) return null;

        var tm = groundTilemap.GetComponent<Tilemap>();
        var tr = groundTilemap.GetComponent<TilemapRenderer>();

        if (tr != null)
        {
            var rb = tr.bounds;
            if (rb.size.x > 0.01f && rb.size.y > 0.01f) return rb;
        }

        if (tm == null) return null;

        var cellBounds = tm.cellBounds;
        if (cellBounds.size.x <= 0 || cellBounds.size.y <= 0) return null;

        var worldMin = tm.CellToWorld(cellBounds.min);
        var worldMax = tm.CellToWorld(cellBounds.max);

        var b = new Bounds();
        b.SetMinMax(Vector3.Min(worldMin, worldMax), Vector3.Max(worldMin, worldMax));
        return b;
    }
}

