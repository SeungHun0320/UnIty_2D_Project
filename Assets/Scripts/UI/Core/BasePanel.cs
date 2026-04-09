using System.Collections;
using UnityEngine;

// 모든 UI 패널의 베이스 클래스입니다.
// SetActive로 렌더링을 완전히 차단하고, CanvasGroup으로 페이드 연출합니다.
[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePanel : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField, Min(0f)] private float fadeInDuration  = 0.3f;
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.3f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha          = 0f;
        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }


    public void Show()
    {
        gameObject.SetActive(true);
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(0f, 1f, fadeInDuration, () =>
        {
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
        }));
        OnShow();
    }

    public void Hide()
    {
        _canvasGroup.interactable   = false;
        _canvasGroup.blocksRaycasts = false;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(Fade(1f, 0f, fadeOutDuration, () =>
        {
            gameObject.SetActive(false);
            OnHide();
        }));
    }

    protected virtual void OnShow() { }
    protected virtual void OnHide() { }

    private IEnumerator Fade(float from, float to, float duration, System.Action onComplete = null)
    {
        float elapsed = 0f;
        _canvasGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _canvasGroup.alpha = to;
        onComplete?.Invoke();
    }
}
