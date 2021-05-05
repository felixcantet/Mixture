Texture2D<float4> _HeightMap;
RWTexture2D<float4> _SmoothedHeightMap;
Texture2D<float4> _Gradient;
Texture2D<float4> _PosGradient;
Texture2D<float4> _NegGradient;
float terrainHeight;
float cellLength;
SamplerState linear_repeat_sampler;

float GetNormalizedHeight(int2 uv)
{
    uv.x = clamp(uv.x, 0, _HeightMap.Length.x);
    uv.y = clamp(uv.y, 0, _HeightMap.Length.y);
    return _HeightMap[uv];
}

float GetHeight(int2 uv)
{
    return GetNormalizedHeight(uv) * terrainHeight;
}

float2 GetFirstDerivative(int2 uv)
{
    float w = cellLength;
    float z1 = GetHeight(uv + int2(-1, 1));
    float z2 = GetHeight(uv + int2(0, 1));
    float z3 = GetHeight(uv + int2(1, 1));
    float z4 = GetHeight(uv + int2(-1, 0));
    float z6 = GetHeight(uv + int2(1, 0));
    float z7 = GetHeight(uv + int2(-1, -1));
    float z8 = GetHeight(uv + int2(0, -1));
    float z9 = GetHeight(uv + int2(1, -1));
    
    float zx = (z3 + z6 + z9 - z1 - z4 - z7) / (6.0 * w);
    float zy = (z1 + z2 + z3 - z7 - z8 - z9) / (6.0 * w);
    
    return float2(-zx, -zy);
}

void GetDerivatives(int2 uv, inout float2 d1, inout float3 d2)
{
    float w = cellLength;
    float w2 = w * w;
    float z1 = GetHeight(uv + int2(-1, 1));
    float z2 = GetHeight(uv + int2(0, 1));
    float z3 = GetHeight(uv + int2(1, 1));
    float z4 = GetHeight(uv + int2(-1, 0));
    float z5 = GetHeight(uv);
    float z6 = GetHeight(uv + int2(1, 0));
    float z7 = GetHeight(uv + int2(-1, -1));
    float z8 = GetHeight(uv + int2(0, -1));
    float z9 = GetHeight(uv + int2(1, -1));
    
    float zx = (z3 + z6 + z9 - z1 - z4 - z7) / (6.0 * w);
    float zy = (z1 + z2 + z3 - z7 - z8 - z9) / (6.0 * w);
    
    float zxx = (z1 + z3 + z4 + z6 + z7 + z9 - 2.0f * (z2 + z5 + z8)) / (3.0 * w2);
    float zyy = (z1 + z2 + z3 + z7 + z8 + z9 - 2.0f * (z4 + z5 + z6)) / (3.0 * w2);
    float zxy = (z3 + z7 - z1 - z9) / (4.0 * w2);
    
    d1 = float2(-zx, -zy);
    d2 = float3(-zxx, -zyy, -zxy);
}

void SmoothHeightMap(int2 uv)
{
    float gaussian[5][5] =
    {
        { 1, 4, 6, 4, 1 },
        { 4, 16, 24, 16, 4 },
        { 6, 24, 36, 24, 6 },
        { 4, 16, 24, 16, 4 },
        { 1, 4, 6, 4, 1 }
    };
    
    float gaussScale = 1.0 / 256.0;
    float sum = 0;
            
    for (int i = 0; i < 5; i++)
    {
        for (int j = 0; j < 5; j++)
        {
            int xi = uv.x - 2 + i;
            int yi = uv.y - 2 + j;
                    
            sum += GetNormalizedHeight(int2(xi, yi)) * gaussian[i][j] * gaussScale;
        }

    }
            
    _SmoothedHeightMap[uv] = float4(sum, sum, sum, 1);
    //_SmoothedHeightMap[uv] = float4(gaussian[1][1] / 8, gaussian[0][0] / 8, gaussian[0][0] / 8, 1);
    //_SmoothedHeightMap[uv] = _HeightMap[uv];

}


float3 bilinear(float2 texcoord, float tex_dimension, Texture2D<float4> gradient)
{
    float3 result;

    // red channel
    float4 reds = gradient.GatherRed(linear_repeat_sampler, texcoord);
    float r1 = reds.x;
    float r2 = reds.y;
    float r3 = reds.z;
    float r4 = reds.w;

    float2 pixel = texcoord * tex_dimension + 0.5;
    float2 fract = frac(pixel);
      
    float top_row_red = lerp(r4, r3, fract.x);
    float bottom_row_red = lerp(r1, r2, fract.x);

    float final_red = lerp(top_row_red, bottom_row_red, fract.y);
    result.x = final_red;
            
    // green channel
    float4 greens = gradient.GatherGreen(linear_repeat_sampler, texcoord);
    float g1 = greens.x;
    float g2 = greens.y;
    float g3 = greens.z;
    float g4 = greens.w;

    float top_row_green = lerp(g4, g3, fract.x);
    float bottom_row_green = lerp(g1, g2, fract.x);

    float final_green = lerp(top_row_green, bottom_row_green, fract.y);
    result.y = final_green;
            
    // blue channel
    float4 blues = gradient.GatherBlue(linear_repeat_sampler, texcoord);
    float b1 = blues.x;
    float b2 = blues.y;
    float b3 = blues.z;
    float b4 = blues.w;

    float top_row_blue = lerp(b4, b3, fract.x);
    float bottom_row_blue = lerp(b1, b2, fract.x);

    float final_blue = lerp(top_row_blue, bottom_row_blue, fract.y);
    result.z = final_blue;

    return result;
}


float4 Colorize(float v, float exponent, bool nonNegative)
{
    if (exponent > 0.0)
    {
        float sign = v == 0.0 ? 0 : v < 0.0 ? -1.0 : 1.0;
        float p = pow(10, exponent);
        float l = log(1.0 + p * abs(v));

        v = sign * l;
    }

    if (nonNegative)
    {
        float3 col = bilinear(float2(v, 0), _Gradient.Length.x, _Gradient);
        return float4(col.xyz, 1);
        //return _Gradiant.SampleLevel(linear_clamp_sampler, float2(v / _Gradiant.Length.x, 0), 0);
        return _Gradient.SampleLevel(linear_repeat_sampler, float2(v, 0), 0);
    }
    else
    {
        if (v > 0)
        {
            float3 col = bilinear(float2(v, 0), _PosGradient.Length.x, _PosGradient);
            return float4(col.xyz, 1);
            return _PosGradient.SampleLevel(linear_repeat_sampler, float2(v, 0), 0);
        }
            //return float4(bilinear(float2(v / 5, 0), _Gradiant.Length), 1);
            //return _Gradiant.SampleLevel(linear_clamp_sampler, float2(v / 5/*_Gradiant.Length.x*/, 0), 0);
        else
        {
            float3 col = bilinear(float2(v, 0), _NegGradient.Length.x, _NegGradient);
            return float4(col.xyz, 1);
            //return float4(1, 0, 0, 1);
            return _NegGradient.SampleLevel(linear_repeat_sampler, float2(v, 0), 0);
        }
            //return float4(bilinear(float2(v / 5, 0), _Gradiant.Length), 1);
            //return _Gradiant.SampleLevel(linear_clamp_sampler, float2(-v / 5/*_Gradiant.Length.x*/, 0), 0);
    }
}
    

