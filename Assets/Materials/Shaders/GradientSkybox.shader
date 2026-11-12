Shader "Stylized/GradientSkybox"
{
    Properties
    {
        _TopColor ("Top", Color) = (0.33, 0.62, 0.98, 1)
        _HorizonColor ("Horizon", Color) = (0.82, 0.93, 1, 1)
        _BottomColor ("Bottom", Color) = (0.60, 0.77, 0.96, 1)
        _TopExponent ("Top Exponent", Range(0.1, 4)) = 0.7
        _BottomExponent ("Bottom Exponent", Range(0.1, 4)) = 0.5
        _SunColor ("Sun Glow Color", Color) = (1, 0.93, 0.75, 1)
        _SunDirection ("Sun Direction (towards sun)", Vector) = (-0.35, 0.7, -0.6, 0)
        _SunGlow ("Sun Glow", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            float4 _SunDirection;
            float _TopExponent, _BottomExponent, _SunGlow;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };
            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }
            half4 frag (Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half3 col = d.y >= 0
                    ? lerp(_HorizonColor.rgb, _TopColor.rgb, pow(saturate(d.y), _TopExponent))
                    : lerp(_HorizonColor.rgb, _BottomColor.rgb, pow(saturate(-d.y), _BottomExponent));
                float s = saturate(dot(d, normalize(_SunDirection.xyz)));
                col += _SunColor.rgb * (pow(s, 6) * 0.25 + pow(s, 64) * 0.6) * _SunGlow;
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}