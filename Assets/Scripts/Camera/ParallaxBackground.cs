using UnityEngine;

// 시차 스크롤 배경을 제어합니다.
// 카메라가 움직일 때 각 레이어가 다른 속도로 스크롤되어 원근감을 만듭니다.
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public SpriteRenderer spriteRenderer;
        [Range(0f, 1f), Tooltip("0 = 완전 고정 (먼 배경/하늘), 1 = 카메라 완전 추종 (가까운 전경)")]
        public float parallaxFactor = 0.5f;

        // 런타임: 3장 타일 배열 (좌·중·우)
        [HideInInspector] public SpriteRenderer[] tiles;
        [HideInInspector] public float startY;
        [HideInInspector] public float textureWidth;
        [HideInInspector] public float idealX;         // X SmoothDamp 목표값
        [HideInInspector] public float currentX;       // X SmoothDamp 현재값
        [HideInInspector] public float velocityX;      // X SmoothDamp 내부 속도
        [HideInInspector] public float idealY;         // Y SmoothDamp 목표값
        [HideInInspector] public float currentY;       // Y SmoothDamp 현재값
        [HideInInspector] public float velocityY;      // Y SmoothDamp 내부 속도
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Layers")]
    [SerializeField] private ParallaxLayer[] layers;

    [Header("Smoothing")]
    [SerializeField, Min(0.01f), Tooltip("X축 SmoothDamp 시간. 낮을수록 빠르게 따라옴.")]
    private float smoothTimeX = 0.15f;
    [SerializeField, Min(0.01f), Tooltip("Y축 SmoothDamp 시간. 낮을수록 빠르게 따라옴.")]
    private float smoothTimeY = 0.15f;

    private float _previousCameraX;
    private float _previousCameraY;
    private bool _frozen;

    private void OnEnable()  => EventBus.Subscribe<StageClearEvent>(OnStageClear);
    private void OnDisable() => EventBus.Unsubscribe<StageClearEvent>(OnStageClear);
    private void OnStageClear(StageClearEvent _) => _frozen = true;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        _previousCameraX = targetCamera.transform.position.x;
        _previousCameraY = targetCamera.transform.position.y;

        foreach (var layer in layers)
        {
            if (layer.spriteRenderer == null) continue;

            layer.startY   = layer.spriteRenderer.transform.position.y;
            layer.idealX   = layer.spriteRenderer.transform.position.x;
            layer.currentX = layer.spriteRenderer.transform.position.x;
            layer.idealY   = layer.spriteRenderer.transform.position.y;
            layer.currentY = layer.spriteRenderer.transform.position.y;
            layer.textureWidth = layer.spriteRenderer.bounds.size.x;

            // 중앙·좌·우 3장 생성
            layer.tiles = new SpriteRenderer[3];
            layer.tiles[0] = layer.spriteRenderer;
            layer.tiles[1] = CreateCopy(layer.spriteRenderer, -layer.textureWidth);
            layer.tiles[2] = CreateCopy(layer.spriteRenderer, +layer.textureWidth);
        }
    }

    private SpriteRenderer CreateCopy(SpriteRenderer original, float offsetX)
    {
        GameObject go = new GameObject(original.gameObject.name + (offsetX < 0 ? "_L" : "_R"));
        go.transform.SetParent(original.transform.parent, false);
        // Z를 0.001f 오프셋해 같은 Z에서 렌더 순서가 프레임마다 바뀌는 깜빡임을 방지합니다.
        float zOffset = offsetX < 0 ? -0.001f : 0.001f;
        go.transform.position = original.transform.position + new Vector3(offsetX, 0f, zOffset);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite           = original.sprite;
        sr.sortingOrder     = original.sortingOrder;
        sr.sortingLayerID   = original.sortingLayerID;
        sr.sortingLayerName = original.sortingLayerName;
        return sr;
    }

    private void LateUpdate()
    {
        if (_frozen) return;

        float cameraDeltaX = targetCamera.transform.position.x - _previousCameraX;
        float cameraDeltaY = targetCamera.transform.position.y - _previousCameraY;
        float camX         = targetCamera.transform.position.x;
        float camY         = targetCamera.transform.position.y;

        foreach (var layer in layers)
        {
            if (layer.tiles == null) continue;

            // X: parallaxFactor만큼 카메라를 추종 (0 = 고정, 1 = 완전 추종)
            float targetDeltaX = cameraDeltaX * layer.parallaxFactor;
            layer.idealX  += targetDeltaX;
            float smoothedDeltaX = Mathf.SmoothDamp(
                layer.currentX, layer.idealX, ref layer.velocityX, smoothTimeX) - layer.currentX;
            layer.currentX += smoothedDeltaX;

            // Y: 카메라를 완전 추종 (SmoothDamp로 부드럽게)
            layer.idealY += cameraDeltaY;
            layer.currentY = Mathf.SmoothDamp(
                layer.currentY, layer.idealY, ref layer.velocityY, smoothTimeY);

            foreach (var tile in layer.tiles)
            {
                if (tile == null) continue;
                Vector3 pos = tile.transform.position;
                pos.x += smoothedDeltaX;
                pos.y  = layer.currentY;
                tile.transform.position = pos;
            }

            // 순환 재배치: 1.5W 기준으로 체크해 양쪽 조건이 오실레이션하지 않도록 합니다.
            // 1W 기준이면 점프 후 즉시 반대 조건이 충족되어 무한 왕복이 발생합니다.
            if (layer.textureWidth > 0f)
            {
                float threshold = layer.textureWidth * 1.5f;
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

        _previousCameraX = camX;
        _previousCameraY = camY;
    }
}
