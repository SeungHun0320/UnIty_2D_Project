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
        if (_frames == null || _frames.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(PlayOnce(onComplete));
    }

    private IEnumerator PlayOnce(Action onComplete)
    {
        float interval = 1f / Mathf.Max(fps, 0.1f);
        foreach (var frame in _frames)
        {
            if (_sr != null) _sr.sprite = frame;
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
