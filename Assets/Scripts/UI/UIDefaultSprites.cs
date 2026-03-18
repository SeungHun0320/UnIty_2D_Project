using UnityEngine;

/// <summary>
/// UI용 기본 스프라이트 생성. 스프라이트를 할당하지 않았을 때 채움/마스크가 보이도록 사용.
/// - 흰색 사각형 (기본 채움)
/// - 물결 모양 상단이 있는 채움 (소울 게이지용)
/// </summary>
public static class UIDefaultSprites
{
    private static Sprite _whiteSprite;
    private static Sprite _waveFillSprite;

    /// <summary> 흰색 1x1 스프라이트. FillImage에 할당하면 단색 채움으로 보임. </summary>
    public static Sprite White
    {
        get
        {
            if (_whiteSprite != null) return _whiteSprite;
            _whiteSprite = CreateWhiteSprite();
            return _whiteSprite;
        }
    }

    /// <summary> 물결 모양 상단이 있는 채움 스프라이트. 상단이 부드러운 물결로 보임. </summary>
    public static Sprite WaveFill
    {
        get
        {
            if (_waveFillSprite != null) return _waveFillSprite;
            _waveFillSprite = CreateWaveFillSprite();
            return _waveFillSprite;
        }
    }

    private static Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// 아래는 불투명, 위쪽은 물결 형태 알파로 페이드되는 텍스처 생성.
    /// Filled Vertical Bottom으로 쓰면 채움 상단이 물결처럼 보임.
    /// </summary>
    private static Sprite CreateWaveFillSprite()
    {
        const int w = 64;
        const int h = 64;
        const float waveFreq = 5f;   // 물결 개수
        const float waveAmp = 6f;     // 물결 진폭(픽셀)
        const float fadePixels = 4f;   // 상단 페이드 두께

        Texture2D tex = new Texture2D(w, h);
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // 열마다 물결 경계 y (상단이 굴곡)
                float nx = (float)x / w * Mathf.PI * 2f * waveFreq;
                float waveY = h * 0.88f + Mathf.Sin(nx) * waveAmp;
                float alpha = 1f;
                if (y > waveY)
                    alpha = 0f;
                else if (y > waveY - fadePixels)
                    alpha = 1f - (y - (waveY - fadePixels)) / fadePixels;

                pixels[y * w + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f));
    }
}
