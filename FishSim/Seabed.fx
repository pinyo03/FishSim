float4x4 WorldViewProj;
float4x4 World;
texture2D DiffuseMap;
float4x4 WorldIT;
float3 SunDir;
float TileCount; // hányszor ismétlődjön
float BlendWidth; // átmenet szélessége UV-ban (10px / 1024 ≈ 0.0098)

#include "Caustics.fxh"
#include "WaterColor.fxh"

sampler DiffuseMapSampler = sampler_state
{
    Texture = <DiffuseMap>;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSO
{
    float4 pos : POSITION;
    float2 tex : TEXCOORD0;
    float3 normal : NORMAL;
    float3 worldPos : TEXCOORD1;
};

VSO VS(float4 inPos : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD)
{
    VSO ret;
    ret.tex = inPos.xz;
    ret.pos = mul(inPos, WorldViewProj);
    ret.normal = normalize(mul(normal, (float3x3) WorldIT));
    ret.worldPos = mul(inPos, World).xyz;
    return ret;
}

// Seamless tiling: két, félperiódussal eltolt mintavétel lineáris keverése
float4 SampleTiled(float2 uv)
{
    float2 uv1 = frac(uv); // alap tile
    float2 uv2 = frac(uv + 0.5); // félperiódussal eltolt tile

    // Súly: 0 a tile szélén, 1 a közepén
    float2 w = smoothstep(0, BlendWidth, uv1)
             * smoothstep(0, BlendWidth, 1.0 - uv1);
    float weight = w.x * w.y;

    float4 col1 = tex2D(DiffuseMapSampler, uv1);
    float4 col2 = tex2D(DiffuseMapSampler, uv2);
    return lerp(col2, col1, weight);
}

float4 PS(VSO vso) : COLOR
{
    float Ld = saturate(dot(vso.normal, SunDir) + dot(vso.normal, float3(0, 1, 0)));
    Ld = Ld / (1 + Ld) * (1 + Ld / 1.5);
    float4 diffuse = SampleTiled(vso.tex * TileCount) * Ld;
    float3 color = ApplyCaustics(diffuse.rgb, vso.worldPos, vso.normal);
    color = ApplyDepthFog(color, vso.worldPos, CameraPosition, WaterFogDensity);
    return float4(color, 1);
}

technique Seabed
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}

// --- Sand Plane ---
float3 SandColor;

struct VSO_Sand
{
    float4 pos : POSITION;
    float3 worldPos : TEXCOORD0;
};

VSO_Sand VS_Sand(float4 inPos : POSITION)
{
    VSO_Sand ret;
    ret.pos = mul(inPos, WorldViewProj);
    ret.worldPos = mul(inPos, World).xyz;
    return ret;
}

float4 PS_Sand(VSO_Sand vso) : COLOR
{
    float3 color = ApplyCaustics(SandColor, vso.worldPos, float3(0, 1, 0));
    color = ApplyDepthFog(color, vso.worldPos, CameraPosition, WaterFogDensity);
    return float4(color, 1.0);
}

technique SandPlane
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS_Sand();
        PixelShader = compile ps_4_0_level_9_3 PS_Sand();
    }
}
