using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 플레이어 피격 시 화면 플래시 이펙트를 재생합니다.
// hitSprites 배열을 1프레임씩 순서대로 표시해 히트감을 강조합니다.
public class HitFlashUI : MonoBehaviour
{
    [SerializeField] private Sprite[] hitSprites;

    private Image _image;
    private Coroutine _flashCoroutine;
    private float _lastHealth = float.MaxValue;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null)
            _image.enabled = false;
    }

    private void OnEnable()
    {
        EventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedEvent e)
    {
        if (!e.IsPlayer) return;
        if (e.CurrentHealth < _lastHealth)
            Flash();
        _lastHealth = e.CurrentHealth;
    }

    public void Flash()
    {
        if (_flashCoroutine != null)
            StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(PlayFlash());
    }

    // hitSprites를 1프레임씩 순서대로 표시 후 숨깁니다.
    // 히트렉(timeScale=0) 중에는 현재 스프라이트를 유지하고 복구 후 진행합니다.
    private IEnumerator PlayFlash()
    {
        if (_image == null || hitSprites == null || hitSprites.Length == 0) yield break;

        _image.enabled = true;
        foreach (var sprite in hitSprites)
        {
            _image.sprite = sprite;
            while (Time.timeScale <= 0f) yield return null; // 히트렉 대기
            yield return null;
        }
        _image.enabled = false;
        _flashCoroutine = null;
    }
}
