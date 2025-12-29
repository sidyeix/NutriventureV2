Shader "Custom/URP_Forcefield"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0, 0.8, 1, 0.5)
        _EdgeColor ("Edge Glow Color", Color) = (0, 1, 1, 1)
        _EdgePower ("Edge Power", Range(1, 8)) = 4
        _PulseSpeed ("Pulse Speed", Float) = 2
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS    : TEXCOORD0;
                float3 viewDirWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EdgeColor;
                float _EdgePower;
                float _PulseSpeed;
                float _PulseStrength;
            CBUFFER_END

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.viewDirWS = normalize(GetWorldSpaceViewDir(worldPos));

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Fresnel (edge glow)
                float fresnel = pow(1.0 - saturate(dot(IN.normalWS, IN.viewDirWS)), _EdgePower);

                // Animated pulse
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseStrength;

                float alpha = saturate(_BaseColor.a + fresnel + pulse);

                half3 color =
                    _BaseColor.rgb +
                    fresnel * _EdgeColor.rgb;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
