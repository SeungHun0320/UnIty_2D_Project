using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// Mask_N 슬롯 하나의 시각 상태를 담당합니다 (Full / Hit / Empty).
/// Animator 없이 코루틴으로 스프라이트·회전·스케일을 직접 제어합니다.
/// </summary>
public class MaskSlot : MonoBehaviour
{
    // ── 내부 참조 ─────────────────────────────────────────────────────────────
    private Image           _icon;
    private RectTransform   _rt;        // 슬롯 루트 — 회전·스케일 적용 대상
    private Sprite          _emptySprite;
    private MaskFullConfig  _fullConfig;
    private MaskHitConfig   _hitConfig;

    // ── 상태 ──────────────────────────────────────────────────────────────────
    private Coroutine _fullRoutine;
    private Coroutine _hitRoutine;
    private float     _fullSpeed = 1f;

    public bool IsPlayingHit => _hitRoutine != null;

    // ── 초기화 ────────────────────────────────────────────────────────────────

    /// <summary> MaskUI가 슬롯 생성 직후 호출합니다. </summary>
    public void Setup(Image icon, Sprite emptySprite, MaskFullConfig fullConfig, MaskHitConfig hitConfig)
    {
        _icon        = icon;
        _emptySprite = emptySprite;
        _fullConfig  = fullConfig;
        _hitConfig   = hitConfig;
        _rt          = GetComponent<RectTransform>();
    }

    // ── 공개 API ──────────────────────────────────────────────────────────────

    /// <summary> Full 루프 재생. 이미 같은 속도로 재생 중이면 재시작하지 않습니다. </summary>
    public void PlayFull(float speed = 1f)
    {
        if (_fullRoutine != null && Mathf.Approximately(_fullSpeed, speed)) return;
        StopHit();
        StopFull();
        _fullSpeed  = speed;
        _fullRoutine = StartCoroutine(FullCoroutine(speed));
    }

    /// <summary> Hit 시퀀스 1회 재생. 완료 시 onDone 콜백 호출. </summary>
    public void PlayHit(float speed = 1f, Action onDone = null)
    {
        StopHit();
        _hitRoutine = StartCoroutine(HitCoroutine(speed, onDone));
    }

    /// <summary> 빈 마스크 정적 표시. </summary>
    public void PlayEmpty()
    {
        StopAll();
        if (_icon != null) _icon.sprite = _emptySprite;
        _rt.localEulerAngles = Vector3.zero;
        _rt.localScale       = Vector3.one;
    }

    public void StopAll()
    {
        StopFull();
        StopHit();
    }

    // ── 코루틴 ────────────────────────────────────────────────────────────────

    private IEnumerator FullCoroutine(float speed)
    {
        if (_fullConfig == null || _fullConfig.sprites == null || _fullConfig.sprites.Length == 0)
            yield break;

        int   count    = _fullConfig.sprites.Length;
        float frameDur = 1f / Mathf.Max(_fullConfig.frameRate * speed, 0.01f);

        while (true)
        {
            // 한 사이클 재생
            for (int i = 0; i < count; i++)
            {
                _icon.sprite         = _fullConfig.sprites[i];
                _rt.localEulerAngles = GetFrameRotation(i);
                yield return new WaitForSecondsRealtime(frameDur);
            }

            if (!_fullConfig.animatePeriodically) continue;

            // 첫 프레임에서 정지 후 대기
            _icon.sprite         = _fullConfig.sprites[0];
            _rt.localEulerAngles = GetFrameRotation(0);
            yield return new WaitForSecondsRealtime(_fullConfig.intervalSeconds);
        }
    }

    private IEnumerator HitCoroutine(float speed, Action onDone)
    {
        StopFull();
        _rt.localEulerAngles = Vector3.zero;    // Full 회전 보정 초기화

        if (_hitConfig == null || _hitConfig.sprites == null || _hitConfig.sprites.Length == 0)
        {
            onDone?.Invoke();
            yield break;
        }

        float frameDur = 1f / Mathf.Max(_hitConfig.frameRate * speed, 0.01f);

        for (int i = 0; i < _hitConfig.sprites.Length; i++)
        {
            _icon.sprite   = _hitConfig.sprites[i];
            float scale    = (_hitConfig.scalePerFrame != null && i < _hitConfig.scalePerFrame.Length)
                             ? _hitConfig.scalePerFrame[i] : 1f;
            _rt.localScale = new Vector3(scale, scale, 1f);
            yield return new WaitForSecondsRealtime(frameDur);
        }

        // 스케일·회전 복원
        _rt.localScale       = Vector3.one;
        _rt.localEulerAngles = Vector3.zero;

        _hitRoutine = null;
        onDone?.Invoke();
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    private Vector3 GetFrameRotation(int frameIndex)
    {
        if (_fullConfig.rotations == null || frameIndex >= _fullConfig.rotations.Length)
            return Vector3.zero;
        return _fullConfig.rotations[frameIndex];
    }

    private void StopFull()
    {
        if (_fullRoutine == null) return;
        StopCoroutine(_fullRoutine);
        _fullRoutine = null;
    }

    private void StopHit()
    {
        if (_hitRoutine == null) return;
        StopCoroutine(_hitRoutine);
        _hitRoutine          = null;
        _rt.localScale       = Vector3.one;     // 스케일 항상 복원
        _rt.localEulerAngles = Vector3.zero;
    }
}
