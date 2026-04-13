// 특정 색상(기본: 검정) 이하의 픽셀을 투명하게 처리하는 스프라이트 셰이더입니다.
// 원본 텍스처는 수정하지 않고 GPU에서 렌더링 시 버립니다.
Shader "Custom/Sprite_ColorKey"
{
    Properties
    {
        _MainTex        ("Sprite Texture", 2D)     = "white" {}
        _Color          ("Tint",           Color)  = (1,1,1,1)
        _KeyThreshold   ("Black Threshold", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _KeyThreshold;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                // 밝기 계산 (Luminance)
                float brightness = dot(col.rgb, float3(0.299, 0.587, 0.114));

                // 임계값 이하 픽셀 버리기
                if (brightness < _KeyThreshold)
                    discard;

                return col;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
