float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldIT;
texture2D Texture;

float3 AmbientColor;
float3 Light0Dir;
float3 Light0Color;
float3 Light1Dir;
float3 Light1Color;

#include "Caustics.fxh"

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
};

struct VSO
{
    float4 pos : POSITION;
    float3 worldPos : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float2 tex : TEXCOORD2;
};

VSO VS(float4 inPos : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD0)
{
    VSO ret;
    float4 worldPos = mul(inPos, World);
    ret.worldPos = worldPos.xyz;
    ret.pos = mul(mul(worldPos, View), Projection);
    ret.normal = normalize(mul(normal, (float3x3) WorldIT));
    ret.tex = tex;
    return ret;
}

float4 PS(VSO vso) : COLOR
{
    float3 n = normalize(vso.normal);
    float3 lighting = AmbientColor
        + Light0Color * saturate(dot(n, -Light0Dir))
        + Light1Color * saturate(dot(n, -Light1Dir));

    float4 tex = tex2D(TextureSampler, vso.tex);
    float3 color = tex.rgb * lighting;
    color = ApplyCaustics(color, vso.worldPos, n);
    return float4(color, tex.a);
}

technique FishLit
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}
