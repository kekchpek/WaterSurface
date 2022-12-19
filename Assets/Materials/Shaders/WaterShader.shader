// Material/Surface shader: Hit shaders should be defined as a pass in a shader used for a
// material in the scene.
Shader "Custom/WaterShader"
{
    
    Properties {
        _Color("Main Color", Color) = (1, 1, 1, 1)
        _RaytraceColor("Raytrace Color", Color) = (1, 1, 1, 1)
        _SomeFactor("Factor", float) = 1.0
    }
    
    SubShader {
        Tags { "RenderType" = "Opaque" "DisableBatching" = "True"}
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            float4 _Color;
            float _SomeFactor;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                return _Color * _SomeFactor;
            }
            ENDCG
        }
    }
    
    SubShader { 
        Pass {
            Name "WaterRaytracing"
            // Add tags to identify the shaders to use for ray tracing.
            Tags{ "LightMode" = "RayTracing" }

            HLSLPROGRAM

            
            float4 _RaytraceColor;
            float _SomeFactor;
            #pragma multi_compile RAY_TRACING_PROCEDURAL_GEOMETRY

            // Specify this shader is a raytracing shader.
            #pragma raytracing IntersectionMain

            struct RayPayload
            {
                float4 color;
            };

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
            /*[shader("raygeneration")]
            void Raygen()
            {
                RayDesc rayDesc;
                rayDesc.Origin = origin;  // start at sample position position
                rayDesc.Direction = rayDir;
                rayDesc.TMin = 0;
                rayDesc.TMax = length(rayDirNotNormalized);
            }*/
            
            [shader("intersection")]
            void IntersectionMain()
            {
                float3 re = 1.0 / _SomeFactor;
                float3 or = ObjectRayOrigin();
                float3 dir = normalize(ObjectRayDirection());
                AttributeData attr;
                attr.barycentrics = float2(0, 0);
                float r = 0.35;
                float3 toCenter = float3(0., 0., 0.)-ObjectRayOrigin();
                float dirCos = dot(normalize(toCenter), dir);
                float minCos = cos(asin(r / length(toCenter)));
                if (dirCos > minCos) {
                    ReportHit(1, 0, attr);
                }
            }

            [shader("closesthit")]
            void ClosestHitMain(inout RayPayload payload : SV_RayPayload, in AttributeData attribs : SV_IntersectionAttributes) {
                payload.color = _RaytraceColor;
            }

        ENDHLSL
        }
    }
}
