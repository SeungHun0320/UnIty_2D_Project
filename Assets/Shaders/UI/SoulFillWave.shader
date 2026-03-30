Shader "UI/SoulFillWave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Fill ("Fill Amount", Range(0,1)) = 1
        _WaveSpeed ("Wave Speed", Float) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.001,0.05)) = 0.01
        _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)

        [Toggle] _UseShapeMask ("Use Shape Mask", Float) = 0
        _ShapeMaskTex ("Shape Mask (Alpha)", 2D) = "white" {}

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Pass
        {
            Name "UI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uvLocal  : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _ShapeMaskTex;
            fixed4 _Color;

            float _Fill;
            float _WaveSpeed;
            float _EdgeSoftness;
            float _UseShapeMask;
            float4 _SpriteUVRect;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                // UI quad local UV (0~1). Sprite rect remap은 frag에서 _SpriteUVRect로 처리합니다.
                output.uvLocal = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                // 단일 텍스처(_MainTex) + 단일 스프라이트(rect) 기반 샘플링:
                // scroll은 로컬(0~1)에서 frac 적용 후, _SpriteUVRect로 아틀라스 UV로 리맵합니다.
                float2 uvLocal = input.uvLocal;
                uvLocal.x = frac(uvLocal.x + _Time.y * _WaveSpeed);
                float2 uvAtlas = _SpriteUVRect.xy + uvLocal * _SpriteUVRect.zw;

                fixed4 col = tex2D(_MainTex, uvAtlas) * input.color;

                float surface = saturate(_Fill);
                // 세로 컷은 "로컬 uv.y" 기준(아틀라스 rect 영향 없이 0~1)
                float alphaMask = 1.0 - smoothstep(surface - _EdgeSoftness, surface, uvLocal.y);
                col.a *= alphaMask;

                if (_UseShapeMask > 0.5)
                {
                    fixed4 maskCol = tex2D(_ShapeMaskTex, input.uvLocal);
                    col.a *= maskCol.a;
                }

                return col;
            }
            ENDCG
        }
    }

    // CustomEditor 제거:
    // 프로젝트 설정/패키지 구성에 따라 UnityEditor.UI.DefaultShaderGUI를 찾지 못해 경고가 날 수 있습니다.
}
