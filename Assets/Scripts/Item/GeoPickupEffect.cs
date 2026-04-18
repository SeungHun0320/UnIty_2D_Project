using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// 코인 획득 시 Geo_Effect_0~3 스프라이트를 1회 재생하는 이펙트입니다.
[RequireComponent(typeof(SpriteRenderer))]
public class GeoPickupEffect : MonoBehaviour, IPickupEffect
{
    [SerializeField] private float fps = 16f;
    [Tooltip("true면 수집 시 랜덤 각도로 이펙트 출력.")]
    [SerializeField] private bool randomRotation = true;
    [Tooltip("randomRotation이 false일 때 프레임별 보정 각도.")]
    [SerializeField] private float[] frameRotations;

    private SpriteRenderer _sr;
    private Sprite[] _frames;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        _frames = Resources.LoadAll<Sprite>("Geo")
            .Where(s => Regex.IsMatch(s.name, @"^Geo_Effect_\d+$"))
            .Select(s => (sprite: s, index: ExtractNumber(s.name)))
            .OrderBy(t => t.index)
            .Select(t => t.sprite)
            .ToArray();

        if (_frames == null || _frames.Length == 0)
            Debug.LogWarning("[GeoPickupEffect] Geo_Effect 스프라이트를 로드할 수 없습니다.");
    }

    public void Play(Vector3 fromWorldPos, Action onComplete)
    {
        // 코인 애니메이션 즉시 중단
        var coinAnimator = GetComponent<GeoSpriteAnimator>();
        if (coinAnimator != null) coinAnimator.enabled = false;

        float baseAngle = randomRotation ? UnityEngine.Random.Range(0f, 360f) : 0f;
        transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle);

        if (_frames == null || _frames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(PlayOnce(baseAngle, onComplete));
    }

    private IEnumerator PlayOnce(float baseAngle, Action onComplete)
    {
        float interval = 1f / Mathf.Max(fps, 0.1f);
        for (int i = 0; i < _frames.Length; i++)
        {
            if (_sr != null) _sr.sprite = _frames[i];
            float correction = (frameRotations != null && i < frameRotations.Length) ? frameRotations[i] : 0f;
            transform.localRotation = Quaternion.Euler(0f, 0f, baseAngle + correction);
            yield return new WaitForSeconds(interval);
        }
        onComplete?.Invoke();
    }

    private static int ExtractNumber(string name)
    {
        var match = Regex.Match(name, @"(\d+)$");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}
