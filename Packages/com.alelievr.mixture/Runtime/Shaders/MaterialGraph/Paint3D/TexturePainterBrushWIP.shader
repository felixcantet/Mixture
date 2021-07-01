// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/TexturePainterWIP"
{
    Properties
    {
        _PainterColor ("Painter Color", Color) = (0, 0, 0, 0)
        
        _BrushTexture ("Brush", 2D) = "white"
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


            float maskCircle(float3 position, float3 center, float radius, float hardness)
            {
                float m = distance(center, position);
                return 1 - smoothstep(radius * hardness, radius, m);  
            }
            
            float maskSquare(float3 position, float3 center, float radius, float hardness)
            {
                float mX = distance(center.x, position.x);
                float mY = distance(center.y, position.y);
                
                if(mX <= radius && mY <= radius)
                    return 1 - smoothstep(radius * hardness, radius, (mX+mY) * 0.5);
                
                return 0;
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
                
                
                float4 col = tex2D(_MainTex, i.uv);
                
                //float2 uv = CalculateBrushUV(i.uv, _PainterUV, _Radius, _BrushRotate);
                float2 uv = CalculateBrushUV(i.worldPos, _PainterPosition, _Radius, _BrushRotate);
                //float2 uv = CalculateBrushUV(i.vertex, _PainterUV, _Radius, _BrushRotate);
                
                float alphaBrush = tex2D(_BrushTexture, uv).a;
                
                //float f = maskCircle(i.worldPos, _PainterPosition, _Radius, _Hardness);
                float f = maskSquare(i.worldPos, _PainterPosition, _Radius, _Hardness);
                float edge = f * _Strength * alphaBrush;
                
                return lerp(col, _PainterColor, edge);
                
            }
            ENDCG
        }
    }
}

/*
bool ExistPointInTriangle(float3 p, float3 t1, float3 t2, float3 t3)
            {
                const float TOLERANCE = 1 - 0.1;
            
                float3 a = normalize(cross(t1 - t3, p - t1));
                float3 b = normalize(cross(t2 - t1, p - t2));
                float3 c = normalize(cross(t3 - t2, p - t3));
            
                float d_ab =dot(a, b);
                float d_bc =dot(b, c);
            
                if (TOLERANCE < d_ab && TOLERANCE < d_bc) {
                    return true;
                }
                return false;
            }
            
            bool IsPaintRange(float2 mainUV, float2 paintUV, float brushScale, float deg) 
            {
                float3 p = float3(mainUV, 0);
                float3 v1 = float3(RotateBrush(float2(-brushScale, brushScale), deg) + paintUV, 0);
                float3 v2 = float3(RotateBrush(float2(-brushScale, -brushScale), deg) + paintUV, 0);
                float3 v3 = float3(RotateBrush(float2(brushScale, -brushScale), deg) + paintUV, 0);
                float3 v4 = float3(RotateBrush(float2(brushScale, brushScale), deg) + paintUV, 0);
                return ExistPointInTriangle(p, v1, v2, v3) || ExistPointInTriangle(p, v1, v3, v4);
            }*/