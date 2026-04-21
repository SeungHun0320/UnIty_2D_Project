using UnityEngine;

// 시차 스크롤 배경을 제어합니다. 씬별로 독립적으로 동작합니다.
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public SpriteRenderer spriteRenderer;
        [Range(0f, 1f), Tooltip("0 = 완전 고정 (먼 배경/하늘), 1 = 카메라 완전 추종 (가까운 전경)")]
        public float parallaxFactor = 0.5f;

        [HideInInspector] public SpriteRenderer[] tiles;
        [HideInInspector] public float textureWidth;
        [HideInInspector] public float idealX;
        [HideInInspector] public float currentX;
        [HideInInspector] public float velocityX;
        [HideInInspector] public float idealY;
        [HideInInspector] public float currentY;
        [HideInInspector] public float velocityY;
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Layers")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f)] private float smoothTimeX = 0.15f;
    [SerializeField, Min(0.01f)] private float smoothTimeY = 0.15f;
    [SerializeField, Min(1)] private int instantYSnapFrameCount = 8;
    [SerializeField, Min(1f)] private float cycleThresholdMultiplier = 1.5f;

    private float _previousCameraX;
    private float _previousCameraY;
    private int _instantYSnapFrames;

    private void OnEnable()
    {
        EventBus.Subscribe<StageRestartEvent>(OnStageRestart);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<StageRestartEvent>(OnStageRestart);
    }

    private void OnStageRestart(StageRestartEvent _)
    {
        if (targetCamera == null) return;

        _previousCameraX = targetCamera.transform.position.x;
        _previousCameraY = targetCamera.transform.position.y;
        _instantYSnapFrames = instantYSnapFrameCount;

        foreach (var layer in layers)
        {
            if (layer.tiles == null || layer.tiles[0] == null) continue;
            float snapY = layer.tiles[0].transform.position.y;
            layer.idealY    = snapY;
            layer.currentY  = snapY;
            layer.velocityY = 0f;
        }
    }

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null) return;

        _previousCameraX = targetCamera.transform.position.x;
        _previousCameraY = targetCamera.transform.position.y;

        foreach (var layer in layers)
        {
            if (layer.spriteRenderer == null) continue;

            layer.idealX       = layer.spriteRenderer.transform.position.x;
            layer.currentX     = layer.spriteRenderer.transform.position.x;
            layer.idealY       = layer.spriteRenderer.transform.position.y;
            layer.currentY     = layer.spriteRenderer.transform.position.y;
            layer.textureWidth = layer.spriteRenderer.bounds.size.x;

            layer.tiles    = new SpriteRenderer[3];
            layer.tiles[0] = layer.spriteRenderer;
            layer.tiles[1] = CreateCopy(layer.spriteRenderer, -layer.textureWidth);
            layer.tiles[2] = CreateCopy(layer.spriteRenderer, +layer.textureWidth);
        }
    }

    private SpriteRenderer CreateCopy(SpriteRenderer original, float offsetX)
    {
        GameObject go = new GameObject(original.gameObject.name + (offsetX < 0 ? "_L" : "_R"));
        go.transform.SetParent(original.transform.parent, false);
        // 같은 Z에서 렌더 순서 깜빡임 방지용 미세 Z 오프셋
        float zOffset = offsetX < 0 ? -0.001f : 0.001f;
        go.transform.position = original.transform.position + new Vector3(offsetX, 0f, zOffset);

        SpriteRenderer sr   = go.AddComponent<SpriteRenderer>();
        sr.sprite           = original.sprite;
        sr.sortingOrder     = original.sortingOrder;
        sr.sortingLayerID   = original.sortingLayerID;
        sr.sortingLayerName = original.sortingLayerName;
        return sr;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        float cameraDeltaX = targetCamera.transform.position.x - _previousCameraX;
        float cameraDeltaY = targetCamera.transform.position.y - _previousCameraY;
        float camX         = targetCamera.transform.position.x;
        float camY         = targetCamera.transform.position.y;

        foreach (var layer in layers)
        {
            if (layer.tiles == null) continue;

            float targetDeltaX = cameraDeltaX * layer.parallaxFactor;
            layer.idealX      += targetDeltaX;
            float smoothedDeltaX = Mathf.SmoothDamp(
                layer.currentX, layer.idealX, ref layer.velocityX, smoothTimeX) - layer.currentX;
            layer.currentX += smoothedDeltaX;

            layer.idealY += cameraDeltaY;
            if (_instantYSnapFrames > 0)
            {
                layer.currentY  = layer.idealY;
                layer.velocityY = 0f;
            }
            else
            {
                layer.currentY = Mathf.SmoothDamp(
                    layer.currentY, layer.idealY, ref layer.velocityY, smoothTimeY);
            }

            foreach (var tile in layer.tiles)
            {
                if (tile == null) continue;
                Vector3 pos = tile.transform.position;
                pos.x += smoothedDeltaX;
                pos.y  = layer.currentY;
                tile.transform.position = pos;
            }

            // 1.5W 기준 순환 재배치 (1W면 즉시 역방향 조건 충족으로 오실레이션 발생)
            if (layer.textureWidth > 0f)
            {
                float threshold = layer.textureWidth * cycleThresholdMultiplier;
                foreach (var tile in layer.tiles)
                {
                    if (tile == null) continue;
                    float tileX = tile.transform.position.x;
                    if (tileX + threshold < camX)
                    {
                        Vector3 p = tile.transform.position;
                        p.x += layer.textureWidth * 3f;
                        tile.transform.position = p;
                    }
                    else if (tileX - threshold > camX)
                    {
                        Vector3 p = tile.transform.position;
                        p.x -= layer.textureWidth * 3f;
                        tile.transform.position = p;
                    }
                }
            }
        }

        if (_instantYSnapFrames > 0)
            _instantYSnapFrames--;

        _previousCameraX = camX;
        _previousCameraY = camY;
    }
}
