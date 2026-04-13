using System.Collections;
using UnityEngine;

// SpriteEffect의 destroyAfter가 0일 때 Animator 클립 길이를 자동으로 읽어 제거합니다.
// Speed 변경에도 자동 대응합니다.
public class EffectLifetimeHelper : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Animator 초기화 대기 (1프레임)
        yield return null;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            float duration = info.length / Mathf.Max(anim.speed, 0.01f);
            // Wrapper 구조일 경우 부모까지 함께 제거합니다.
            GameObject target = transform.parent != null ? transform.parent.gameObject : gameObject;
            Destroy(target, duration);
        }
        else
        {
            Destroy(gameObject, 3f); // 폴백
        }
    }
}
