using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 할로우 나이트 스타일 지오(골드) UI. 코너에 숫자로 표시.
/// </summary>
public class GeoUI : MonoBehaviour
{
    [SerializeField] private Text geoText;
    [SerializeField] private string prefix = "";
    [SerializeField] private int currentGeo;

    private void Awake()
    {
        if (geoText == null)
            geoText = GetComponent<Text>();
    }

    public void SetGeo(int amount)
    {
        currentGeo = Mathf.Max(0, amount);
        if (geoText != null)
            geoText.text = prefix + currentGeo.ToString();
    }

    public int GetGeo() => currentGeo;
}
