float4x4 World;
float4x4 View;
float4x4 Projection;
texture2D Texture;

#include "Caustics.fxh"

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VSO
{
    float4 pos : POSITION;
    float3 viewDir : TEXCOORD0;
    float2 tex : TEXCOORD1;
};

// The half-sphere dome is generated as unit direction vectors from its centre,
// so the local position itself is the view direction toward that sky point.
VSO VS(float4 inPos : POSITION, float2 tex : TEXCOORD0)
{
    VSO ret;
    float4 worldPos = mul(inPos, World);
    ret.viewDir = normalize(inPos.xyz);
    ret.pos = mul(mul(worldPos, View), Projection);
    ret.tex = tex;
    return ret;
}

float4 PS(VSO vso) : COLOR
{
    float4 tex = tex2D(TextureSampler, vso.tex);
    float3 color = ApplyCausticsSky(tex.rgb, normalize(vso.viewDir));
    return float4(color, tex.a);
}

technique Sky
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}
