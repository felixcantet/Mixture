Shader "Unlit/TexturePainter"
{
    Properties
    {
        _PainterColor ("Painter Color", Color) = (0, 0, 0, 0)
        
        _BrushTexture ("Brush", 2D) = "white"
        _BrushScale ("BrushScale", float) = 0.1
        _BrushRotate("BrushRotate", float) = 0.0
    }

    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			sampler2D _MainTex;
            float4 _MainTex_ST;
            
            // BRUSH
            
            sampler2D _BrushTexture;
            float _BrushScale;
            float _BrushRotate;
            
            // -----
            
            float3 _PainterPosition;
            float2 _PainterUV;
            float _Radius;
            float _Hardness;
            float _Strength;
            float4 _PainterColor;
            float _PrepareUV;

            struct appdata
            {
                float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            float mask(float3 position, float3 center, float radius, float hardness)
            {
                float m = distance(center, position);
                return 1 - smoothstep(radius * hardness, radius, m);  
            }
            
            float maskBrush(float3 position, float3 center, float hardness, float value)
            {
                float m = distance(center, position);
                
                float step = smoothstep(value * hardness, value, m);
                
                return 1.0 - step;
            } 
                      
            float Deg2Rad(float degrees)
            {
                const float deg2Rad = (UNITY_PI * 2.0) / 360.0;
                return degrees * deg2Rad;
            }
            
            float2 RotateBrush(float2 p, float degrees)
            {
                float rad = Deg2Rad(degrees);
                float newX = p.x * cos(rad) - p.y * sin(rad);
                float newY = p.x * sin(rad) + p.y * cos(rad);
                return float2(newX, newY);
            }
            
            float2 CalculateBrushUV(float2 uv, float2 paintUV, float brushScale, float brushRotate)
            {
                return RotateBrush((uv - paintUV) / brushScale, -brushRotate) * 0.5 + 0.5;
            }

            v2f vert (appdata v)
            {
                v2f o;
				
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                
                o.uv = v.uv;
                
				float4 uv = float4(0, 0, 0, 1);
                uv.xy = float2(1, _ProjectionParams.x) * (v.uv.xy * float2( 2, 2) - float2(1, 1));
				o.vertex = uv; 
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {   
                if(_PrepareUV > 0 )
                {
                    return float4(0, 0, 1, 1);
                }
                
                float2 uv = CalculateBrushUV(i.uv, _PainterUV, _Radius, _BrushRotate);
                float alphaBrush = tex2D(_BrushTexture, uv).a;
                
                float4 col = tex2D(_MainTex, i.uv);
                
                //float f = mask(i.worldPos, _PainterPosition, _Radius, _Hardness);
                float f = maskBrush(i.worldPos, _PainterPosition, _Hardness, alphaBrush);
                
                
                float edge = f * _Strength;
                
                return lerp(col, _PainterColor, edge);//edge);
            }
            ENDCG
        }
    }
}
