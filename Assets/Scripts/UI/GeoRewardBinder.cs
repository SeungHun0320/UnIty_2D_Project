using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// GeoWallet(Model)과 GeoUI(View)를 연결하는 ViewModel입니다.
// Geo 획득 대기 누적·커밋 로직을 담당합니다.
public class GeoRewardBinder : MonoBehaviour
{
    [Header("Model")]
    [SerializeField] private GeoWallet geoWallet;

    [Header("View")]
    [SerializeField] private GeoUI geoUI;

    [Header("대기 반영")]
    [Tooltip("지오 획득 후 이 시간(초) 동안 추가 획득이 없으면 메인 지오에 반영합니다.")]
    [SerializeField] [Min(0.1f)] private float commitDelay = 1.5f;

    [Header("디버그")]
    [Tooltip("체크 시 F3/F4로 지오 획득·차감 테스트. 빌드에서는 끄는 것을 권장합니다.")]
    [SerializeField] private bool enableDebugKeys = true;
    [SerializeField] private int debugStep = 10;

    private int _pendingGeo;
    private Coroutine _commitRoutine;

    private void Awake()
    {
        if (geoWallet == null)
            geoWallet = FindAnyObjectByType<GeoWallet>();
        if (geoWallet == null)
            Debug.LogWarning("[GeoRewardBinder] GeoWallet을 찾을 수 없습니다.");

        if (geoUI == null)
            geoUI = GetComponent<GeoUI>();
        if (geoUI == null)
            Debug.LogWarning("[GeoRewardBinder] GeoUI를 찾을 수 없습니다.");
    }

    private void OnEnable()
    {
        Subscribe();
        SyncInitial();
    }

    private void OnDisable() => Unsubscribe();

    private void Update()
    {
        if (!enableDebugKeys || geoWallet == null) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.f3Key.wasPressedThisFrame)
            geoWallet.AddGeo(debugStep);
        if (keyboard.f4Key.wasPressedThisFrame)
            geoWallet.SetGeo(Mathf.Max(0, geoWallet.GetGeo() - debugStep));
    }

    private void Subscribe()
    {
        if (geoWallet == null) return;
        geoWallet.OnGeoAdded += HandleGeoAdded;
        geoWallet.OnGeoSet   += HandleGeoSet;
    }

    private void Unsubscribe()
    {
        if (geoWallet == null) return;
        geoWallet.OnGeoAdded -= HandleGeoAdded;
        geoWallet.OnGeoSet   -= HandleGeoSet;
    }

    private void SyncInitial()
    {
        if (geoWallet == null || geoUI == null) return;
        geoUI.SyncDisplay(geoWallet.CurrentGeo, 0);
    }

    // GeoWallet.OnGeoAdded → 대기 누적 후 commitDelay 뒤 메인 텍스트에 반영
    private void HandleGeoAdded(int amount)
    {
        if (amount <= 0) return;
        _pendingGeo += amount;
        geoUI?.UpdatePendingGeo(_pendingGeo);

        if (_commitRoutine != null)
            StopCoroutine(_commitRoutine);
        _commitRoutine = StartCoroutine(CommitAfterDelay());
    }

    // GeoWallet.OnGeoSet → 대기 초기화 후 즉시 갱신 (세이브 로드 등)
    private void HandleGeoSet(int total)
    {
        if (_commitRoutine != null)
        {
            StopCoroutine(_commitRoutine);
            _commitRoutine = null;
        }
        _pendingGeo = 0;
        geoUI?.SyncDisplay(total, 0);
    }

    private IEnumerator CommitAfterDelay()
    {
        yield return new WaitForSeconds(commitDelay);
        _commitRoutine = null;
        _pendingGeo = 0;
        if (geoWallet != null)
            geoUI?.SyncDisplay(geoWallet.CurrentGeo, 0);
    }
}
