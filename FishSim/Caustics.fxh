#ifndef CAUSTICS_FXH
#define CAUSTICS_FXH

// --- Shared caustics parameters (set from C# via CausticsSettings.Apply) ---
float Time;
float3 CausticsColor = float3(0.6, 0.85, 1.0);
float CausticsIntensity = 0.5;
float CausticsScale = 0.15;
float CausticsSpeed = 0.5;
float CausticsScale2 = 0.05;
float CausticsSpeed2 = 0.25;
float CausticsLayer2Weight = 0.7;
float CausticsScale3 = 0.4;
float CausticsSpeed3 = 0.9;
float CausticsLayer3Weight = 0.5;
float CausticsDepthFade = 0.05;
float WaterSurfaceY = 0.0;
float CausticsSkyIntensity = 0.05;
float CausticsCeilingHeight = 40.0;
float CausticsSkyAngleStart = 0.25;
float CausticsSkyAngleFull = 0.7;
float3 CameraPosition;

// One layer of two crossed, scrolling sine-wave fields approximating a caustics net.
float CausticsLayer(float2 worldXZ, float scale, float speed, float time, float phase)
{
    float2 p1 = worldXZ * scale;
    float2 p2 = worldXZ * (scale * 1.37);

    float2 m1 = p1 + float2(time, time * 0.7) * speed;
    float2 m2 = float2(p2.x - p2.y, p2.x + p2.y) * 0.7 - float2(time * 0.6, time * 1.1) * speed;

    float w1 = abs(sin(m1.x * 6.2832 + phase) * sin(m1.y * 6.2832 + 1.5 + phase));
    float w2 = abs(sin(m2.x * 6.2832 + 0.8 + phase) * sin(m2.y * 6.2832 + phase));

    float c = w1 * w2;
    return pow(saturate(c), 3.0);
}

// Combines three independently scaled/animated layers, sampled in world-space XZ
// so it lines up across different objects. The extra layers run at different
// scale/speed to break up the repetitiveness of a single layer.
float CausticsPattern(float2 worldXZ, float time)
{
    float c1 = CausticsLayer(worldXZ, CausticsScale, CausticsSpeed, time, 0.0);
    float c2 = CausticsLayer(worldXZ, CausticsScale2, CausticsSpeed2, time, 2.3);
    float c3 = CausticsLayer(worldXZ, CausticsScale3, CausticsSpeed3, time, 4.7);
    return saturate(max(c1, max(c2 * CausticsLayer2Weight, c3 * CausticsLayer3Weight)));
}

// Adds animated caustics to a lit surface colour, fading with depth and surface orientation.
float3 ApplyCaustics(float3 baseColor, float3 worldPos, float3 normal)
{
    float upFactor = saturate(dot(normalize(normal), float3(0, 1, 0)));
    float depth = max(WaterSurfaceY - worldPos.y, 0);
    float depthFade = exp(-depth * CausticsDepthFade);
    float pattern = CausticsPattern(worldPos.xz, Time);
    return baseColor + CausticsColor * pattern * (CausticsIntensity * upFactor * depthFade);
}

// Subtle shimmer for the sky dome, only visible when looking up toward the
// water surface (high dirY) — projects the view ray onto a horizontal plane
// near the surface so the pattern lines up with the seabed caustics.
float3 ApplyCausticsSky(float3 baseColor, float3 viewDir)
{
    float mask = smoothstep(CausticsSkyAngleStart, CausticsSkyAngleFull, viewDir.y);
    if (mask <= 0.0)
        return baseColor;

    float t = (CausticsCeilingHeight - CameraPosition.y) / max(viewDir.y, 0.0001);
    float2 projXZ = CameraPosition.xz + viewDir.xz * t;

    float pattern = CausticsPattern(projXZ, Time);
    return baseColor + CausticsColor * pattern * CausticsSkyIntensity * mask;
}

#endif
