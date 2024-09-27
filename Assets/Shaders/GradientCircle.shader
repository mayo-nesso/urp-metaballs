Shader "Custom/GradientCircle"
{
    Properties
    {
        _Color ("Color", Color) = (1.0, 1.0, 1.0, 1)
        _Radius ("Radius", Range(0, 1)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
    }
    
    SubShader
    {
        Tags {
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent"
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };


            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Radius;
                float _Smoothness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize UV coordinates to range from -1 to 1
                const float2 uv = IN.uv * 2.0 - 1.0;

                // Circle center (in normalized coordinates)
                const float2 center = float2(0.0, 0.0);

                // Distance from current point (uv) to circle center
                const float dist = length(uv - center);

                // Smooth gradient: 1.0 at center, 0.0 at edge
                // The smoothstep function produces a smooth transition between 0.0 and 1.0
                // based on the distance from the circle center and the defined radius and smoothness
                const float smooth_circle = smoothstep(_Radius, _Radius - _Smoothness, dist);

                // The final alpha value is the smooth gradient value
                half4 color = _Color;
                color.a *= smooth_circle;
                
                return color;
            }
            ENDHLSL
        }
    }
}