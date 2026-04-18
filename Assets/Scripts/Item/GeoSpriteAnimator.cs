using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

// Geo.png 슬라이스 스프라이트를 순환 재생하는 코인 애니메이터 (프레임별 회전 지원)
[RequireComponent(typeof(SpriteRenderer))]
public class GeoSpriteAnimator : MonoBehaviour
{
    [SerializeField] private float fps = 12f;
    public float Fps
    {
        get => fps;
        set { fps = Mathf.Max(0.1f, value); RestartLoop(); }
    }

    [Tooltip("프레임 수와 동일하게 입력. 비워두면 회전 없음.")]
    [SerializeField] private float[] frameRotations;

    private SpriteRenderer _renderer;
    private Sprite[] _frames;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null)
        {
            Debug.LogWarning("[GeoSpriteAnimator] SpriteRenderer를 찾을 수 없습니다.");
            return;
        }

        // Geo_0~7 (8프레임) 사용
        _frames = Resources.LoadAll<Sprite>("Geo")
            .Where(s => System.Text.RegularExpressions.Regex.IsMatch(s.name, @"^Geo_\d+$"))
            .OrderBy(s => ExtractNumber(s.name))
            .Where(s => ExtractNumber(s.name) <= 7)
            .ToArray();

        if (_frames == null || _frames.Length == 0)
        {
            Debug.LogWarning("[GeoSpriteAnimator] Resources/Geo 스프라이트를 로드할 수 없습니다.");
            return;
        }
    }

    private void OnEnable()
    {
        if (_frames != null && _frames.Length > 0)
            StartCoroutine(PlayLoop());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private void RestartLoop()
    {
        if (!isActiveAndEnabled || _frames == null || _frames.Length == 0) return;
        StopAllCoroutines();
        StartCoroutine(PlayLoop());
    }

    private IEnumerator PlayLoop()
    {
        int index = 0;

        while (true)
        {
            float interval = 1f / Mathf.Max(fps, 0.1f);
            _renderer.sprite = _frames[index];

            if (frameRotations != null && index < frameRotations.Length)
                transform.localRotation = Quaternion.Euler(0f, 0f, frameRotations[index]);

            index = (index + 1) % _frames.Length;
            yield return new WaitForSeconds(interval);
        }
    }

    // 이름 끝 숫자 추출 (atlas0_324_10 → 10)
    private static int ExtractNumber(string name)
    {
        var match = Regex.Match(name, @"(\d+)$");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}
