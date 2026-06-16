#ifndef WATERCOLOR_FXH
#define WATERCOLOR_FXH

// --- Shared water color / depth fog parameters (set from C# via WaterColorSettings.Apply) ---
#define WATER_ZONE_COUNT 8

float3 WaterZoneColors[WATER_ZONE_COUNT];
float  WaterZoneBaseY;
float  WaterZoneStep = 25.0;
// Tavolsag-alapu exponencialis kod surusege: fogFactor = 1 - exp(-WaterFogDensity * dist).
// dist a kamera -> fragment vilagkoordinata-tavolsaga (minden iranyban, nem csak Y menten).
// Tipikus ertek 0.012-0.018 kozott: ~55-80 egysegnel mar kb. felig elkodosodik a tavoli geometria.
float  WaterFogDensity = 0.015;
// A kod soha nem nyeli el teljesen a feluletet - max ennyire (0..1) keveredhet a kodszinbe,
// hogy pl. az eg textura nagy tavolsagban is athalljon a kodon.
float  WaterFogMaxBlend = 1;

// Csak a sky shaderhez: fuggolegesen felfele nezve egyre kevesebb viz van a kamera felett,
// ezert a kod max-keveredese a vizszinteshez tartozo WaterFogMaxBlend-rol negyzetes
// gorbe szerint ennyire (0..1) csokken, ha pontosan felfele nezunk (viewDir.y = 1).
float  SkyFogMinBlend = 0.775;

// Magassag alapjan interpolalt "viz alapszin" - sotet kek a melyben, egyre
// vilagosabb/kekebb a felszin felé. A also/felso hataron tul fix szin (nincs extrapolacio).
float3 GetWaterColorAtHeight(float y)
{
    float t = (y - WaterZoneBaseY) / WaterZoneStep;
    t = clamp(t, 0.0, (float)(WATER_ZONE_COUNT - 1));
    int i0 = (int)floor(t);
    int i1 = min(i0 + 1, WATER_ZONE_COUNT - 1);
    float frac = t - (float)i0;
    return lerp(WaterZoneColors[i0], WaterZoneColors[i1], frac);
}

// Valodi tavolsag-alapu (volumetrikus jellegu) melysegi kod: a baseColor a kamera ->
// fragment 3D tavolsaga alapjan olvad bele a kodszinbe - minden iranyban egyenletesen
// (elore, oldalra, fel, le), nem csak a magassag fuggvenyeben.
// A kodszin magassag szerint valtozik (sotetebb/kekebb lent, vilagosabb fent),
// de a keveredes merteket (fogFactor) kizarolag a kameratavolsag hatarozza meg.
float3 ApplyDepthFog(float3 baseColor, float3 worldPos, float3 cameraPos, float density, float maxBlend)
{
    float dist = length(worldPos - cameraPos);
    float fogFactor = saturate(1.0 - exp(-density * dist));
    fogFactor = min(fogFactor, maxBlend);
    float3 fogColor = GetWaterColorAtHeight(worldPos.y);
    return lerp(baseColor, fogColor, fogFactor);
}

float3 ApplyDepthFog(float3 baseColor, float3 worldPos, float3 cameraPos, float density)
{
    return ApplyDepthFog(baseColor, worldPos, cameraPos, density, WaterFogMaxBlend);
}

#endif
