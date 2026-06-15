float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldIT;
texture2D Texture;
texture2D NormalTexture;
texture2D MetallicTexture;
texture2D RoughnessTexture;
texture2D AOTexture;
bool HasAO = false;

float3 AmbientColor;
float3 Light0Dir;
float3 Light0Color;
float3 Light1Dir;
float3 Light1Color;

// Csontvazas animacio (skinning) - csak a Body mesh-hez hasznalt FishLitSkinned technique-ben.
// A TunaFish csontvaza ~98 csontot tartalmaz, ezert vs_5_0 kell (a level_9_3 constant
// register limitje nem eleg ennyi 4x4-es matrixhoz).
#define MAX_BONES 100
float4x4 Bones[MAX_BONES];

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

sampler MetallicSampler = sampler_state
{
    Texture = <MetallicTexture>;
};

sampler RoughnessSampler = sampler_state
{
    Texture = <RoughnessTexture>;
};

sampler AOSampler = sampler_state
{
    Texture = <AOTexture>;
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

    float metallic = tex2D(MetallicSampler, vso.tex).r;
    float roughness = saturate(tex2D(RoughnessSampler, vso.tex).r);
    float ao = HasAO ? tex2D(AOSampler, vso.tex).r : 1.0;

    float3 lighting = AmbientColor * ao
        + Light0Color * saturate(dot(n, -Light0Dir))
        + Light1Color * saturate(dot(n, -Light1Dir));

    float4 tex = tex2D(TextureSampler, vso.tex);
    float3 albedo = tex.rgb * ao;
    float3 color = albedo * lighting;

    // Egyszeru Blinn-Phong specular: fenyesebb (kis roughness) es femes (nagy metallic)
    // feluleteken erosebb, a fem szinet az albedo, a nem-femet feher hatarozza meg.
    float3 viewDir = normalize(CameraPosition - vso.worldPos);
    float shininess = lerp(4.0, 512.0, 1.0 - roughness);
    float3 specColor = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);
    float3 halfDir0 = normalize(-Light0Dir + viewDir);
    float3 halfDir1 = normalize(-Light1Dir + viewDir);
    float3 spec = pow(saturate(dot(n, halfDir0)), shininess) * Light0Color
                + pow(saturate(dot(n, halfDir1)), shininess) * Light1Color;
    color += specColor * spec * (1.0 - roughness);

    color = ApplyCaustics(color, vso.worldPos, n);
    color = ApplyDepthFog(color, vso.worldPos, CameraPosition, WaterFogDensity);
    return float4(color, tex.a);
}

VSO VSSkinned(
    float4 inPos : POSITION,
    float3 normal : NORMAL,
    float2 tex : TEXCOORD0,
    float3 tangent : TANGENT0,
    float4 blendWeights : BLENDWEIGHT0,
    uint4 blendIndices : BLENDINDICES0) // <--- EZ A LÉNYEG
{
    VSO ret;

    float4x4 skinTransform =
        Bones[blendIndices.x] * blendWeights.x +
        Bones[blendIndices.y] * blendWeights.y +
        Bones[blendIndices.z] * blendWeights.z +
        Bones[blendIndices.w] * blendWeights.w;

    float4 skinnedPos = mul(inPos, skinTransform);
    float3 skinnedNormal = mul(normal, (float3x3) skinTransform);
    float3 skinnedTangent = mul(tangent, (float3x3) skinTransform);

    float4 worldPos = mul(skinnedPos, World);
    ret.worldPos = worldPos.xyz;
    ret.pos = mul(mul(worldPos, View), Projection);
    ret.normal = normalize(mul(skinnedNormal, (float3x3) WorldIT));
    ret.tangent = mul(skinnedTangent, (float3x3) World);
    ret.tex = tex;
    return ret;
}

technique FishLit
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}

technique FishLitSkinned
{
    pass P0
    {
        VertexShader = compile vs_5_0 VSSkinned();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}
