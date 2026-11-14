Shader "Demo/Training Particle"
{
    Properties
    {
        _BaseMap("Original texture",2D)="white"{}
        _MaskMap("Original mask",2D)="white"{}
        [HDR]_BaseColor("Original tint",Color)=(1,1,1,1)
        _UseMask("Use source mask",Float)=0
        _MaskUSpeed("Source mask scroll",Float)=0
        [Enum(UnityEngine.Rendering.BlendMode)]_SrcBlend("Source blend",Float)=1
        [Enum(UnityEngine.Rendering.BlendMode)]_DstBlend("Destination blend",Float)=10
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MaskMap);SAMPLER(sampler_MaskMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor;float _UseMask,_MaskUSpeed;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct Varyings{float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.color=v.color*_BaseColor;o.uv=TRANSFORM_TEX(v.uv,_BaseMap);return o;}
            half4 frag(Varyings i):SV_Target{
                half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);
                // These exported atlases have opaque alpha; their luminous shape is in RGB.
                half4 c=half4(i.color.rgb,tex.r*tex.a*i.color.a);
                half mask=SAMPLE_TEXTURE2D(_MaskMap,sampler_MaskMap,i.uv+float2(_Time.y*_MaskUSpeed,0)).r;
                c.a*=lerp(1,mask,_UseMask);c.rgb*=c.a;return c;
            }
            ENDHLSL
        }
    }
}
