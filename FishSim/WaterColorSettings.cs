using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim
{
    // Central tuning values for the shared water-color / depth-fog shader module (WaterColor.fxh).
    static class WaterColorSettings
    {
        // Index 0 = legmelyebb zona (seabed kornyeke), felfele egyre vilagosabb/kekebb.
        // A zonak WaterZoneBaseY-tol kezdve WaterZoneStep egysegenkent kovetik egymast.
        public static Vector3[] ZoneColors = new[]
        {
            new Vector3(0.01f, 0.03f, 0.08f), // y <= BaseY
            new Vector3(0.02f, 0.06f, 0.15f),
            new Vector3(0.03f, 0.10f, 0.22f),
            new Vector3(0.05f, 0.16f, 0.32f),
            new Vector3(0.08f, 0.24f, 0.42f),
            new Vector3(0.13f, 0.34f, 0.52f),
            new Vector3(0.22f, 0.46f, 0.62f),
            new Vector3(0.35f, 0.60f, 0.75f), // y >= BaseY + 7*ZoneStep
        };
        public static float ZoneBaseY = -60f;
        public static float ZoneStep = 40f;

        // Tavolsag-alapu exponencialis kod surusege (lasd WaterColor.fxh: ApplyDepthFog).
        // fogFactor = 1 - exp(-FogDensity * dist), ahol dist a kamera -> fragment 3D tavolsaga.
        // Nagyobb ertek = kozelebb mar elnyeli a kod a tavoli geometriat.
        // 0.015 korul: ~45 egysegnel ~50%, ~90 egysegnel ~75% kodfaktor.
        public static float FogDensity = 0.015f;

        // A kod soha nem nyeli el teljesen a feluletet - max ennyire (0..1) keveredhet a
        // kodszinbe, hogy pl. az eg textura nagy tavolsagban iís athalljon a kodon.
        public static float FogMaxBlend = 1.0f;

        // Egyetlen hivas, ami bármely WaterColor.fxh-t hasznalo effektet (hal, korall, seabed, ég, ...)
        // felkeszit a melysegi kod / magassag szerinti szinatmenet szamitasara.
        // Hivd meg minden draw-olt effekten, kameranykent egyszer, kozvetlenul a draw elott.
        public static void Apply(Effect effect, Vector3 cameraPosition)
        {
            effect.Parameters["WaterZoneColors"]?.SetValue(ZoneColors);
            effect.Parameters["WaterZoneBaseY"]?.SetValue(ZoneBaseY);
            effect.Parameters["WaterZoneStep"]?.SetValue(ZoneStep);
            effect.Parameters["WaterFogDensity"]?.SetValue(FogDensity);
            effect.Parameters["WaterFogMaxBlend"]?.SetValue(FogMaxBlend);
            effect.Parameters["CameraPosition"]?.SetValue(cameraPosition);
        }

        // CPU-oldali megfeleloje a shaderbeli GetWaterColorAtHeight-nak (pl. ambient szinekhez).
        public static Vector3 GetColorAtHeight(float y)
        {
            float t = (y - ZoneBaseY) / ZoneStep;
            t = MathHelper.Clamp(t, 0f, ZoneColors.Length - 1);
            int i0 = (int)System.MathF.Floor(t);
            int i1 = System.Math.Min(i0 + 1, ZoneColors.Length - 1);
            float frac = t - i0;
            return Vector3.Lerp(ZoneColors[i0], ZoneColors[i1], frac);
        }
    }
}
