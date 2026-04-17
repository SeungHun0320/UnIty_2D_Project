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
        float animDur  = frameDur * count;
        float cycleDur = animDur + Mathf.Max(0f, _fullConfig.intervalSeconds);

        while (true)
        {
            // Time.time 기반 글로벌 위상 계산 — 슬롯이 언제 시작해도 나머지 슬롯들과 동기화됩니다.
            float phase = Time.time % cycleDur;

            if (phase < animDur)
            {
                int frameIdx         = Mathf.Clamp(Mathf.FloorToInt(phase / frameDur), 0, count - 1);
                _icon.sprite         = _fullConfig.sprites[frameIdx];
                _rt.localEulerAngles = GetFrameRotation(frameIdx);
            }
            else
            {
                // interval 구간 — 첫 프레임 고정
                _icon.sprite         = _fullConfig.sprites[0];
                _rt.localEulerAngles = GetFrameRotation(0);
            }

            yield return null;
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

        float frameDur = 1f / Mathf.Max(_hitConfig.frameRate * _hitConfig.speed * speed, 0.01f);

        for (int i = 0; i < _hitConfig.sprites.Length; i++)
        {
            _icon.sprite   = _hitConfig.sprites[i];

            float scale    = (_hitConfig.scalePerFrame != null && i < _hitConfig.scalePerFrame.Length)
                             ? _hitConfig.scalePerFrame[i] : 1f;
            _rt.localScale = new Vector3(scale, scale, 1f);

            // 피벗 기반 위치 보정 — 스프라이트별 피벗이 슬롯 중앙에 오도록 offsetMin/Max 이동
            Vector2 pos = CalcPivotOffset(_hitConfig.sprites[i]);
            _icon.rectTransform.offsetMin = pos;
            _icon.rectTransform.offsetMax = pos;

            yield return new WaitForSeconds(frameDur);
        }

        // 스케일·회전·위치 복원
        _rt.localScale                   = Vector3.one;
        _rt.localEulerAngles             = Vector3.zero;
        _icon.rectTransform.offsetMin    = Vector2.zero;
        _icon.rectTransform.offsetMax    = Vector2.zero;

        _hitRoutine = null;
        onDone?.Invoke();
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────

    // 피벗이 슬롯 중앙에 정렬되도록 오프셋 계산 (preserveAspect 기준 표시 크기 사용)
    private Vector2 CalcPivotOffset(Sprite sprite)
    {
        if (sprite == null) return Vector2.zero;
        float slotW = _rt.rect.width;
        float slotH = _rt.rect.height;
        if (slotW <= 0f || slotH <= 0f) return Vector2.zero;   // 레이아웃 미계산 시 보정 생략
        float sprW  = sprite.rect.width;
        float sprH  = sprite.rect.height;
        if (sprW <= 0f || sprH <= 0f) return Vector2.zero;

        float scale            = Mathf.Min(slotW / sprW, slotH / sprH);
        Vector2 displayedSize  = new Vector2(sprW * scale, sprH * scale);
        Vector2 normalizedPivot = sprite.pivot / new Vector2(sprW, sprH);
        Vector2 offset = (new Vector2(0.5f, 0.5f) - normalizedPivot) * displayedSize;

        return offset;
    }

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
        // 피벗 보정 offset도 복원 — 코루틴 중단 시 틀어진 위치 방지
        if (_icon != null)
        {
            _icon.rectTransform.offsetMin = Vector2.zero;
            _icon.rectTransform.offsetMax = Vector2.zero;
        }
    }
}
