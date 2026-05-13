float4x4 WorldViewProj;
texture2D DiffuseMap;
float4x4 WorldIT;
float3 SunDir;
float4x4 World;
float TileCount; // hányszor ismétlődjön
float BlendWidth; // átmenet szélessége UV-ban (10px / 1024 ≈ 0.0098)

sampler DiffuseMapSampler = sampler_state
{
    Texture = <DiffuseMap>;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSO
{
    float4 pos : POSITION;
    float4 worldPos : TEXCOORD2;
    float2 tex : TEXCOORD;
    float3 normal : NORMAL;
};

VSO VS(float4 inPos : POSITION, float3 normal : NORMAL, float2 tex : TEXCOORD)
{
    VSO ret;
    ret.tex = inPos.xz;
    ret.pos = mul(inPos, WorldViewProj);
    ret.normal = normalize(mul(normal, (float3x3) WorldIT));
    ret.worldPos = mul(inPos, World);
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
    clip(vso.worldPos.y);
    float Ld = saturate(dot(vso.normal, SunDir) + dot(vso.normal, float3(0, 1, 0)));
    Ld = Ld / (1 + Ld) * (1 + Ld / 1.5);
    float4 diffuse = SampleTiled(vso.tex * TileCount) * Ld;
    return float4(diffuse.rgb, 1);
}

float4 PS_Height(VSO vso) : COLOR
{
    return float4(vso.worldPos.y, 0, 0, 0);
}

float4 PS_Refraction(VSO vso) : COLOR
{
    float Ld = saturate(dot(vso.normal, SunDir) + dot(vso.normal, float3(0, 1, 0)));
    float4 diffuse = SampleTiled(vso.tex * TileCount) * Ld;
    return float4(diffuse.rgb, 1);
}

technique Island
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
    pass P1
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS_Height();
    }
    pass P2
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS_Refraction();
    }
}