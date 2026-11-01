// Vertex-colour voxel shader with soft key light, sky/ground ambient and a glossy rim.
// Pass has no LightMode tag, so it renders in URP (SRPDefaultUnlit) and in the Built-in pipeline.
Shader "VoxelBloom/VoxelLit"
{
    Properties
    {
        _LightDir ("Key Light Direction", Vector) = (0.45, 0.8, -0.4, 0)
        _SkyColor ("Sky Ambient", Color) = (0.82, 0.9, 1, 1)
        _GroundColor ("Ground Ambient", Color) = (0.62, 0.58, 0.66, 1)
        _Rim ("Rim Strength", Range(0, 1)) = 0.25
        _Tint ("Tint", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _LightDir;
            fixed4 _SkyColor, _GroundColor, _Tint;
            half _Rim;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float3 n : TEXCOORD0; float3 v : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, i.vertex).xyz);
                fixed4 inst = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                // instanced fragments use _InstanceColor (alpha > 0), meshes use vertex colours
                o.color = inst.a > 0 ? inst : i.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(o.color.rgb); // palette colours are authored in sRGB
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 l = normalize(_LightDir.xyz);
                half diff = saturate(dot(n, l));
                fixed3 amb = lerp(_GroundColor.rgb, _SkyColor.rgb, n.y * 0.5 + 0.5);
                fixed3 c = i.color.rgb * (amb * 0.62 + diff * 0.55);
                half rim = pow(1 - saturate(dot(n, normalize(i.v))), 3) * _Rim;
                half spec = pow(saturate(dot(reflect(-l, n), normalize(i.v))), 24) * 0.18;
                return fixed4((c + rim + spec) * _Tint.rgb, 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
