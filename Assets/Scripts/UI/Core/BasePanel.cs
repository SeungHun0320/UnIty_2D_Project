using System.Collections;
using UnityEngine;

// 모든 UI 패널의 베이스 클래스입니다.
// SetActive로 렌더링을 완전히 차단하고, CanvasGroup으로 페이드 연출합니다.
[RequireComponent(typeof(CanvasGroup))]
public abstract class BasePanel : MonoBehaviour
{
    [Header("Fade")]
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
        // 부모 체인이 비활성인 경우 활성화합니다.
        for (var t = transform.parent; t != null; t = t.parent)
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }

        // 씬에서 비활성으로 저장된 패널은 Awake가 아직 실행되지 않았을 수 있습니다.
        // SetActive(true) 시 Awake가 최초 실행되고 내부에서 SetActive(false)를 호출하므로
        // _canvasGroup을 먼저 확보하고, 비활성화된 경우 한 번 더 활성화합니다.
        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        _canvasGroup.alpha          = 1f;
        _canvasGroup.interactable   = true;
        _canvasGroup.blocksRaycasts = true;
        OnShow();
    }

    public void Hide()
    {
        // 이미 비활성 상태라면 코루틴을 시작할 수 없으므로 즉시 반환합니다.
        if (!gameObject.activeInHierarchy) return;

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
