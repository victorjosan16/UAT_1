// Premium voxel shader: vertex colours, soft key light + sky/ground ambient, glossy rim and specular,
// bevelled cube edges with a "small cubes" sub-grid (per-face UVs), animated water (vertex alpha < 1),
// and a pulsing highlight used to show which voxels an attack is about to remove.
// No LightMode tag -> renders in URP (SRPDefaultUnlit) and in the Built-in pipeline.
Shader "VoxelBloom/VoxelLit"
{
    Properties
    {
        _LightDir ("Key Light Direction", Vector) = (0.45, 0.8, -0.4, 0)
        _SkyColor ("Sky Ambient", Color) = (0.84, 0.92, 1, 1)
        _GroundColor ("Ground Ambient", Color) = (0.64, 0.6, 0.7, 1)
        _Rim ("Rim Strength", Range(0, 1)) = 0.28
        _RimColor ("Rim Colour", Color) = (1, 0.92, 0.8, 1)
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Bevel ("Bevel Width", Range(0, 0.3)) = 0.09
        _Subdiv ("Small Cube Subdivision", Range(1, 4)) = 2
        _SubLine ("Sub-grid Line Strength", Range(0, 1)) = 0.35
        _Highlight ("Highlight Colour", Color) = (1, 1, 1, 0)
        _OffsetFactor ("Depth Offset Factor", Float) = 0
        _OffsetUnits ("Depth Offset Units", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Offset [_OffsetFactor], [_OffsetUnits]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            float4 _LightDir;
            fixed4 _SkyColor, _GroundColor, _Tint, _Highlight, _RimColor;
            half _Rim, _Bevel, _Subdiv, _SubLine;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _InstanceColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float3 n : TEXCOORD0; float3 v : TEXCOORD1; float2 uv : TEXCOORD2; float3 wp : TEXCOORD3; UNITY_VERTEX_INPUT_INSTANCE_ID };

            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.wp = mul(unity_ObjectToWorld, i.vertex).xyz;
                o.v = normalize(_WorldSpaceCameraPos - o.wp);
                fixed4 inst = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                o.color = inst.a > 0 ? fixed4(inst.rgb, 1) : i.color;
                o.uv = inst.a > 0 ? float2(0.5, 0.5) : i.uv;
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(o.color.rgb); // palette colours are authored in sRGB
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 l = normalize(_LightDir.xyz);
                float3 v = normalize(i.v);
                half diff = saturate(dot(n, l));
                fixed3 amb = lerp(_GroundColor.rgb, _SkyColor.rgb, n.y * 0.5 + 0.5);
                fixed3 base = i.color.rgb;

                // animated water: vertex alpha < 1 marks water voxels
                if (i.color.a < 0.99)
                {
                    half w = sin(_Time.y * 5.0 + i.wp.y * 9.0 + i.wp.x * 2.0) * 0.5 + 0.5;
                    base = lerp(base, base * 1.35 + 0.08, w * 0.45);
                }

                // bevelled cube edges + small-cube sub-grid
                float2 e = min(i.uv, 1 - i.uv);
                half edge = saturate(min(e.x, e.y) / max(_Bevel, 1e-3));
                float2 s = frac(i.uv * _Subdiv);
                float2 se = min(s, 1 - s);
                half sub = saturate(min(se.x, se.y) / max(_Bevel * 0.6 * _Subdiv, 1e-3));
                half shade = lerp(0.72, 1.0, edge) * lerp(1.0 - _SubLine * 0.35, 1.0, sub);
                half lip = (1 - edge) * saturate(dot(n, float3(0, 1, 0))) * 0.12; // light catching the top bevel

                fixed3 c = base * (amb * 0.6 + diff * 0.55) * shade + lip;
                half rim = pow(1 - saturate(dot(n, v)), 3) * _Rim;
                half spec = pow(saturate(dot(reflect(-l, n), v)), 28) * 0.22 * edge;
                half bounce = saturate(-n.y) * 0.08; // warm light bouncing off the ground/pedestal
                c = (c + rim * _RimColor.rgb + spec + bounce * fixed3(1, 0.9, 0.75)) * _Tint.rgb;
                c = lerp(c, _Highlight.rgb, _Highlight.a);
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Color"
}
