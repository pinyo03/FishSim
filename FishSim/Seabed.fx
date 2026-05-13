float4x4 WorldViewProj;
texture2D DiffuseMap;
float4x4 WorldIT;
float3 SunDir;
float4x4 World;
sampler DiffuseMapSampler = sampler_state
{
    Texture = <DiffuseMap>;
};
struct VSO // Vertex shader output = pixel shader input 
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
    ret.normal = normalize(mul(normal, (float3x3)WorldIT));
    ret.worldPos = mul(inPos, World);
    return ret;
}
float4 PS(VSO vso) : COLOR
{
    clip(vso.worldPos.y);
    float Ld = saturate(dot(vso.normal, SunDir) + dot(vso.normal, float3(0, 1, 0)));
    Ld = Ld / (1 + Ld) * (1 + Ld / 1.5);
    float4 diffuse = tex2D(DiffuseMapSampler, vso.tex) * Ld;
    return float4(diffuse.rgb, 1);
}
float4 PS_Height(VSO vso) : COLOR
{
    return float4(vso.worldPos.y, 0, 0, 0);
}
float4 PS_Refraction(VSO vso) : COLOR
{
    float Ld = saturate(dot(vso.normal, SunDir) + dot(vso.normal, float3(0, 1, 0)));
    float4 diffuse = tex2D(DiffuseMapSampler, vso.tex) * Ld;
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