float4x4 WorldViewProj;
float4x4 World;
texture2D ReflectionMap;
float Time;
float waveF1 = 0.01, waveF2 = 0.2;
float2 waveV1 = float2(0.006, 0.008), waveV2 = float2(-0.02, 0.042);
float3 CamPos;
float3 SunDir;
sampler ReflectionMapSampler = sampler_state
{
    Texture = <ReflectionMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};
texture2D NormalMap;
sampler NormalMapSampler = sampler_state
{
    Texture = <NormalMap>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};
struct VSO // Vertex shader output = pixel shader input 
{
    float4 pos : POSITION;
    float4 screen : TEXCOORD;
    float4 worldPos : TEXCOORD1;
};
VSO VS(float4 inPos : POSITION)
{
    VSO ret;
    ret.worldPos = mul(inPos, World);
    ret.screen = ret.pos = mul(inPos, WorldViewProj);
    return ret;
}
texture2D RefractionMap;
sampler RefractionMapSampler = sampler_state
{
    Texture = <RefractionMap>;
};
texture2D HeightMap;
sampler HeightMapSampler = sampler_state
{
    Texture = <HeightMap>;
};
float4 PS(VSO vso) : COLOR
{
    float2 reversedTexCoord = (vso.screen.xy / vso.screen.w + 1) * 0.5;
    float2 texCoord = float2(reversedTexCoord.x, 1 - reversedTexCoord.y); // wave normal
    float waveF1 = 0.01, waveF2 = 0.02;
    float2 waveV1 = float2(0.006, 0.008), waveV2 = float2(0, -0.01);
    float4 m1 = tex2D(NormalMapSampler, vso.worldPos.xz * waveF1 + waveV1 * Time);
    float4 m2 = tex2D(NormalMapSampler, vso.worldPos.xz * waveF2 + waveV2 * Time);
    float3 n = normalize((m1 + m2).xyz - 1); // height map  
    float height = tex2D(HeightMapSampler, texCoord).r; // reflection  
    float2 reflectionTexCoord = reversedTexCoord + n.xy * 0.05 * saturate(-height);
    float3 reflectionDiffuse = tex2D(ReflectionMapSampler, reflectionTexCoord).rgb; // refraction  
    
    float3 deepWaterColor = float3(0, 0.15, 0.25);
    float3 refractionDiffuse = lerp(tex2D(RefractionMapSampler, texCoord).rgb, deepWaterColor, saturate(-2 * height));
    
    float3 viewVector = normalize(CamPos - vso.worldPos);
    float fresnel = 0.02 + 0.98 * pow(1 - dot(float3(0, 1, 0), viewVector), 5);
    float3 result = lerp(refractionDiffuse, reflectionDiffuse, fresnel); // specular  
    float3 H = normalize(SunDir + viewVector);
    float specularStrength = pow(saturate(dot(H, n.xzy)), 25);
    result += float3(1, 0.8, 0.75) * specularStrength;
    return float4(result, 1);
}

technique Water
{
    pass P0
    {
        VertexShader = compile vs_4_0_level_9_3 VS();
        PixelShader = compile ps_4_0_level_9_3 PS();
    }
}