Shader "Unlit/TexturePainting"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        ZTest  Off
		ZWrite Off
		Cull   Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float2 uv : TEXCOORD1;
            };
            
            
            float4 _Mouse;
            float4x4 mesh_Object2World;
            sampler2D _MainTex;
            float4 _BrushColor;
            float _BrushOpacity;
            float _BrushHardness;
            float _BrushSize;
            

            v2f vert (appdata v)
            {
                v2f o;
                
                float2 uvRemapped = v.uv.xy;
                uvRemapped.y = 1.0 - uvRemapped.y; // Flip y value
                
                o.vertex = float4(uvRemapped.xy, 0.0, 1.0);
                o.worldPos = mul(mesh_Object2World, v.vertex);
                o.uv = v.uv;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                
                float brushSize = _BrushSize;
                float brushHardness = _BrushHardness;
                
                float t = distance(_Mouse.xyz, i.worldPos);
                t = 1.0 - smoothstep(brushSize * brushHardness, brushSize, t);
                
                // W value of mouse stock if we are clicking or not
                col = lerp(col, _BrushColor, t * _Mouse.w * _BrushOpacity);
                col = saturate(col);
                                
                return col;
            }
            ENDCG
        }
    }
}
