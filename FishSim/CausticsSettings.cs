using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim
{
    // Central tuning values for the shared caustics shader module (Caustics.fxh).
    static class CausticsSettings
    {
        public static Vector3 Color = new Vector3(0.6f, 0.85f, 1.0f);
        public static float Intensity = 0.5f;
        public static float Scale = 0.03f;
        public static float Speed = 0.05f;
        public static float Scale2 = 0.0098f;
        public static float Speed2 = 0.152f;
        public static float Layer2Weight = 0.7f;
        public static float Scale3 = 0.043f;
        public static float Speed3 = 0.094f;
        public static float Layer3Weight = 0.5f;
        public static float DepthFade = 0.05f;
        public static float WaterSurfaceY = 0.0f;
        public static float SkyIntensity = 0.05f;
        public static float SkyCeilingHeight = 40.0f;
        public static float SkyAngleStart = 0.25f;
        public static float SkyAngleFull = 0.7f;

        public static void Apply(Effect effect, float time)
        {
            effect.Parameters["Time"]?.SetValue(time);
            effect.Parameters["CausticsColor"]?.SetValue(Color);
            effect.Parameters["CausticsIntensity"]?.SetValue(Intensity);
            effect.Parameters["CausticsScale"]?.SetValue(Scale);
            effect.Parameters["CausticsSpeed"]?.SetValue(Speed);
            effect.Parameters["CausticsScale2"]?.SetValue(Scale2);
            effect.Parameters["CausticsSpeed2"]?.SetValue(Speed2);
            effect.Parameters["CausticsLayer2Weight"]?.SetValue(Layer2Weight);
            effect.Parameters["CausticsScale3"]?.SetValue(Scale3);
            effect.Parameters["CausticsSpeed3"]?.SetValue(Speed3);
            effect.Parameters["CausticsLayer3Weight"]?.SetValue(Layer3Weight);
            effect.Parameters["CausticsDepthFade"]?.SetValue(DepthFade);
            effect.Parameters["WaterSurfaceY"]?.SetValue(WaterSurfaceY);
            effect.Parameters["CausticsSkyIntensity"]?.SetValue(SkyIntensity);
            effect.Parameters["CausticsCeilingHeight"]?.SetValue(SkyCeilingHeight);
            effect.Parameters["CausticsSkyAngleStart"]?.SetValue(SkyAngleStart);
            effect.Parameters["CausticsSkyAngleFull"]?.SetValue(SkyAngleFull);
        }
    }
}
