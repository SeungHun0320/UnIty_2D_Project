using System.Collections;
using UnityEngine;

// 피격 시 스케일 사인파(squash & stretch)를 재생하는 공용 컴포넌트입니다. (SRP)
// 플레이어·몬스터 공통으로 사용합니다. Play(duration) 호출로 시작합니다.
public class HitScaleEffect : MonoBehaviour
{
    [Header("Squash & Stretch")]
    [SerializeField, Min(0f)] private float squashFrequency = 6f;
    [SerializeField, Range(0f, 1f)] private float squashAmplitude = 0.25f;

    // Awake 시점의 절댓값 — flip 부호와 분리해서 관리합니다.
    private Vector3 _baseScaleAbs;
    private Coroutine _routine;

    private void Awake()
    {
        Vector3 s = transform.localScale;
        _baseScaleAbs = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
    }

    // 피격 시 외부에서 호출합니다. duration 동안 사인파를 재생 후 원본 복원합니다.
    public void Play(float duration)
    {
        if (duration <= 0f) return;

        if (_routine != null)
            StopCoroutine(_routine);
        _routine = StartCoroutine(ScaleSineWave(duration));
    }

    // 감쇠 사인파로 Y를 늘렸다 줄였다 합니다. X는 역수로 면적을 보존합니다.
    private IEnumerator ScaleSineWave(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float envelope = 1f - (elapsed / duration);
            float sine     = Mathf.Sin(elapsed * squashFrequency * Mathf.PI * 2f);
            float scaleY   = 1f + sine * squashAmplitude * envelope;
            float scaleX   = scaleY > 0.001f ? 1f / scaleY : 1f;

            // flip 부호(localScale.x 음수 방향)를 유지합니다.
            float signX = Mathf.Sign(transform.localScale.x);
            if (signX == 0f) signX = 1f;

            transform.localScale = new Vector3(
                signX * _baseScaleAbs.x * scaleX,
                _baseScaleAbs.y * scaleY,
                _baseScaleAbs.z
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 원본 스케일 복원
        float finalSignX = Mathf.Sign(transform.localScale.x);
        if (finalSignX == 0f) finalSignX = 1f;
        transform.localScale = new Vector3(
            finalSignX * _baseScaleAbs.x,
            _baseScaleAbs.y,
            _baseScaleAbs.z
        );

        _routine = null;
    }
}
