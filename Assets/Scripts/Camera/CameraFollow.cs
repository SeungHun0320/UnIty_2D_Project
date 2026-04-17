using UnityEngine;

// 셰이크 연출 방식 선택 옵션
public enum CameraShakeType
{
    Perlin,      // 부드러운 유기적 흔들림
    Random,      // 거친 백색 소음 흔들림
    Sine,        // 규칙적인 사인파 진동
    Directional  // 피격 방향 반대로 튕기는 방향성 셰이크
}

// 플레이어를 부드럽게 추적하고, 피격 시 카메라 셰이크를 연출합니다.
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothSpeed = 5f;
    public Vector2 offset = new Vector2(0f, 1f);

    [Header("Bounds (optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    [Header("Camera Shake")]
    [SerializeField] private CameraShakeType shakeType          = CameraShakeType.Perlin;
    [SerializeField] private float playerHitShakeIntensity      = 0.25f; // 플레이어 피격 셰이크 강도
    [SerializeField] private float playerHitShakeDuration       = 0.2f;  // 플레이어 피격 셰이크 시간(초)
    [SerializeField] private float enemyHitShakeIntensity       = 0.12f; // 적 피격 셰이크 강도
    [SerializeField] private float enemyHitShakeDuration        = 0.1f;  // 적 피격 셰이크 시간(초)
    [SerializeField] private float shakeFrequency               = 30f;   // Perlin/Sine 샘플링 속도

    private Camera _cam;
    private float  _shakeIntensity;
    private float  _shakeDuration;
    private float  _shakeTimer;
    private Vector2 _hitDirection = Vector2.right;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Subscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHitByEnemyEvent>(OnPlayerHit);
        EventBus.Unsubscribe<EnemyHitByPlayerEvent>(OnEnemyHit);
    }

    private void OnPlayerHit(PlayerHitByEnemyEvent evt) =>
        TriggerShake(playerHitShakeIntensity, playerHitShakeDuration, evt.HitDirection);

    private void OnEnemyHit(EnemyHitByPlayerEvent _) =>
        TriggerShake(enemyHitShakeIntensity, enemyHitShakeDuration);

    // 외부에서 셰이크를 직접 발동할 수 있습니다.
    public void TriggerShake(float intensity, float duration, Vector2 hitDirection = default)
    {
        _shakeIntensity = intensity;
        _shakeDuration  = duration;
        _shakeTimer     = duration;
        _hitDirection   = hitDirection == default ? Vector2.right : hitDirection;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );

        if (useBounds)
        {
            float camHalfHeight = _cam.orthographicSize;
            float camHalfWidth  = _cam.orthographicSize * _cam.aspect;

            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x + camHalfWidth,  maxBounds.x - camHalfWidth);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y + camHalfHeight, maxBounds.y - camHalfHeight);
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 셰이크 오프셋 — 히트렉(timeScale=0) 중에도 동작하도록 unscaledDeltaTime 사용
        if (_shakeTimer > 0f)
        {
            float progress = _shakeTimer / _shakeDuration; // 1→0 선형 감쇠
            float offsetX = 0f, offsetY = 0f;

            switch (shakeType)
            {
                case CameraShakeType.Perlin:
                    float t = Time.realtimeSinceStartup * shakeFrequency;
                    offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * _shakeIntensity * progress;
                    offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * _shakeIntensity * progress;
                    break;

                case CameraShakeType.Random:
                    // 매 프레임 완전 무작위 — 거친 백색 소음
                    offsetX = Random.Range(-1f, 1f) * _shakeIntensity * progress;
                    offsetY = Random.Range(-1f, 1f) * _shakeIntensity * progress;
                    break;

                case CameraShakeType.Sine:
                    // X 위주 사인파 진동, Y는 0.5배 보조
                    float phase = (1f - progress) * shakeFrequency * Mathf.PI * 2f;
                    offsetX = Mathf.Sin(phase) * _shakeIntensity * progress;
                    offsetY = Mathf.Sin(phase * 0.5f) * _shakeIntensity * progress * 0.5f;
                    break;

                case CameraShakeType.Directional:
                    // 피격 방향 반대로 한 번 튕겼다 복귀 (사인파 감쇠)
                    float dirPhase  = (1f - progress) * Mathf.PI;
                    float dirMag    = Mathf.Sin(dirPhase) * _shakeIntensity;
                    Vector2 recoil  = -_hitDirection.normalized * dirMag;
                    offsetX = recoil.x;
                    offsetY = recoil.y;
                    break;
            }

            transform.position += new Vector3(offsetX, offsetY, 0f);
            _shakeTimer -= Time.unscaledDeltaTime;
        }
    }
}
