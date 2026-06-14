float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldIT;
texture2D Texture;
texture2D NormalTexture;

float3 AmbientColor;
float3 Light0Dir;
float3 Light0Color;
float3 Light1Dir;
float3 Light1Color;

#include "Caustics.fxh"
#include "WaterColor.fxh"

sampler TextureSampler = sampler_state
{
    Texture = <Texture>;
};

sampler NormalSampler = sampler_state
{
    Texture = <NormalTexture>;
};

struct VSO
{
    float4 pos : POSITION;
    float3 worldPos : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float2 tex : TEXCOORD2;
    float3 tangent : TEXCOORD3;
};

VSO VS(float4 inPos : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD0, float3 tangent : TANGENT0)
{
    VSO ret;
    float4 worldPos = mul(inPos, World);
    ret.worldPos = worldPos.xyz;
    ret.pos = mul(mul(worldPos, View), Projection);
    ret.normal = normalize(mul(normal, (float3x3) WorldIT));
    ret.tangent = mul(tangent, (float3x3) World);
    ret.tex = tex;
    return ret;
}

float4 PS(VSO vso) : COLOR
{
    float3 n = normalize(vso.normal);

    // Ha a modell nem tartalmaz ervenyes tangenst (pl. degeneralt UV), valasszunk
    // tetszoleges, a normalra merleges iranyt, hogy elkeruljuk a normalize(0)-bol
    // adodo NaN-t, ami egyebkent az egesz halat sik (csak ambient) megvilagitasura valtana.
    float3 rawTangent = vso.tangent;
    if (dot(rawTangent, rawTangent) < 1e-8)
        rawTangent = abs(n.y) < 0.99 ? cross(float3(0, 1, 0), n) : cross(float3(1, 0, 0), n);

    float3 t = normalize(rawTangent - n * dot(n, rawTangent));
    float3 b = cross(n, t);

    float3 mapNormal = tex2D(NormalSampler, vso.tex).rgb * 2.0 - 1.0;
    n = normalize(mapNormal.x * t + mapNormal.y * b + mapNormal.z * n);

    float3 lighting = AmbientColor
        + Light0Color * saturate(dot(n, -Light0Dir))
        + Light1Color * saturate(dot(n, -Light1Dir));

    float4 tex = tex2D(TextureSampler, vso.tex);
    float3 color = tex.rgb * lighting;
    color = ApplyCaustics(color, vso.worldPos, n);
    color = ApplyDepthFog(color, vso.worldPos, CameraPosition, WaterFogDensity);
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
