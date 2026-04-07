using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

/// <summary>
/// Player 설정 자동 수정 도구
/// 메뉴: Tools > Fix Player Setup
/// </summary>
public static class PlayerSetupFixer
{
    [MenuItem("Tools/Fix Player Setup")]
    public static void FixAll()
    {
        bool anyFixed = false;

        anyFixed |= FixPlayer();
        anyFixed |= FixTilemap();

        if (anyFixed)
        {
            EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[PlayerSetupFixer] 완료. Ctrl+S로 씬을 저장하세요.");
        }
        else
        {
            Debug.Log("[PlayerSetupFixer] 수정할 항목 없음 (이미 올바르게 설정됨).");
        }
    }

    static bool FixPlayer()
    {
        // Player GameObject 찾기 (이름 또는 태그)
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            // 태그로 재시도
            player = GameObject.FindWithTag("Player");
        }
        if (player == null)
        {
            Debug.LogWarning("[PlayerSetupFixer] 'Player' GameObject를 찾을 수 없습니다.");
            return false;
        }

        bool changed = false;

        // 1. Rigidbody2D → Kinematic 설정
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (rb.bodyType != RigidbodyType2D.Kinematic)
            {
                Undo.RecordObject(rb, "Set Rigidbody2D Kinematic");
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.gravityScale = 0f;
                rb.useFullKinematicContacts = true;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
                EditorUtility.SetDirty(rb);
                Debug.Log("[PlayerSetupFixer] Rigidbody2D → Kinematic 설정 완료.");
                changed = true;
            }
            else
            {
                Debug.Log("[PlayerSetupFixer] Rigidbody2D 이미 Kinematic.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSetupFixer] Rigidbody2D 컴포넌트를 찾을 수 없습니다.");
        }

        // 2. PlayerMover 컴포넌트 추가
        var playerMover = player.GetComponent<PlayerMover>();
        if (playerMover == null)
        {
            Undo.AddComponent<PlayerMover>(player);
            playerMover = player.GetComponent<PlayerMover>();
            Debug.Log("[PlayerSetupFixer] PlayerMover 컴포넌트 추가 완료.");
            changed = true;
        }
        else
        {
            Debug.Log("[PlayerSetupFixer] PlayerMover 이미 존재.");
        }

        // 3. SpineInputController 참조 연결
        var inputController = player.GetComponent<SpineInputController>();
        if (inputController != null && playerMover != null)
        {
            // SerializedObject로 private 직렬화 필드에 접근
            var so = new SerializedObject(inputController);

            // animationDriverComponent 연결
            var animDriverProp = so.FindProperty("animationDriverComponent");
            if (animDriverProp != null && animDriverProp.objectReferenceValue == null)
            {
                var driver = player.GetComponent<SpineAnimationDriver>();
                if (driver != null)
                {
                    animDriverProp.objectReferenceValue = driver;
                    Debug.Log("[PlayerSetupFixer] animationDriverComponent 연결 완료.");
                    changed = true;
                }
            }

            // playerMover 연결
            var moverProp = so.FindProperty("playerMover");
            if (moverProp != null && moverProp.objectReferenceValue == null)
            {
                moverProp.objectReferenceValue = playerMover;
                Debug.Log("[PlayerSetupFixer] playerMover 연결 완료.");
                changed = true;
            }

            // playerStateMachine 연결
            var smProp = so.FindProperty("playerStateMachine");
            if (smProp != null && smProp.objectReferenceValue == null)
            {
                var sm = player.GetComponent<PlayerStateMachine>();
                if (sm != null)
                {
                    smProp.objectReferenceValue = sm;
                    Debug.Log("[PlayerSetupFixer] playerStateMachine 연결 완료.");
                    changed = true;
                }
            }

            so.ApplyModifiedProperties();
        }
        else if (inputController == null)
        {
            Debug.LogWarning("[PlayerSetupFixer] SpineInputController를 찾을 수 없습니다.");
        }

        return changed;
    }

    static bool FixTilemap()
    {
        // TilemapCollider2D가 있는 오브젝트 찾기
        var tilemapColliders = Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        if (tilemapColliders.Length == 0)
        {
            Debug.Log("[PlayerSetupFixer] TilemapCollider2D 없음, 타일맵 수정 건너뜀.");
            return false;
        }

        bool changed = false;

        foreach (var tc in tilemapColliders)
        {
            var go = tc.gameObject;

            // CompositeCollider2D가 없으면 추가
            var composite = go.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                // CompositeCollider2D는 Rigidbody2D가 필요함
                var rbTile = go.GetComponent<Rigidbody2D>();
                if (rbTile == null)
                {
                    rbTile = Undo.AddComponent<Rigidbody2D>(go);
                    rbTile.bodyType = RigidbodyType2D.Static;
                    EditorUtility.SetDirty(rbTile);
                    Debug.Log($"[PlayerSetupFixer] {go.name}: Static Rigidbody2D 추가.");
                }
                else if (rbTile.bodyType != RigidbodyType2D.Static)
                {
                    Undo.RecordObject(rbTile, "Set Tilemap Rigidbody Static");
                    rbTile.bodyType = RigidbodyType2D.Static;
                    EditorUtility.SetDirty(rbTile);
                }

                composite = Undo.AddComponent<CompositeCollider2D>(go);
                EditorUtility.SetDirty(composite);
                Debug.Log($"[PlayerSetupFixer] {go.name}: CompositeCollider2D 추가.");
                changed = true;
            }
            else
            {
                Debug.Log($"[PlayerSetupFixer] {go.name}: CompositeCollider2D 이미 존재.");
            }

            // TilemapCollider2D → Used By Composite 활성화
            var so = new SerializedObject(tc);
            var compositeProp = so.FindProperty("m_CompositeOperation");
            if (compositeProp != null && compositeProp.intValue != 1)
            {
                Undo.RecordObject(tc, "Enable Used By Composite");
                compositeProp.intValue = 1; // Merge
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(tc);
                Debug.Log($"[PlayerSetupFixer] {go.name}: TilemapCollider2D Used By Composite 활성화.");
                changed = true;
            }
            else
            {
                Debug.Log($"[PlayerSetupFixer] {go.name}: Used By Composite 이미 활성화됨.");
            }
        }

        return changed;
    }

    // 충돌 레이어 상태 확인 (수정 없이 로그만 출력)
    [MenuItem("Tools/Check Collision Layers")]
    public static void CheckCollisionLayers()
    {
        Debug.Log("=== 충돌 레이어 상태 ===");
        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (!string.IsNullOrEmpty(layerName))
                Debug.Log($"  Layer {i}: {layerName}");
        }

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var mover = player.GetComponent<PlayerMover>();
            if (mover != null)
            {
                var so = new SerializedObject(mover);
                var layerProp = so.FindProperty("groundLayers");
                if (layerProp != null)
                    Debug.Log($"  PlayerMover.groundLayers: {layerProp.intValue} (비트마스크)");
            }
            Debug.Log($"  Player Layer: {player.layer} ({LayerMask.LayerToName(player.layer)})");
        }

        var tilemapColliders = Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None);
        foreach (var tc in tilemapColliders)
            Debug.Log($"  TilemapCollider2D '{tc.gameObject.name}' Layer: {tc.gameObject.layer} ({LayerMask.LayerToName(tc.gameObject.layer)})");
    }
}
