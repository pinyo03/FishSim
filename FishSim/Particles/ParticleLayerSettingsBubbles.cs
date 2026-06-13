using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim.Particles
{
    // Önálló, a ParticleLayerSettings alapértékeitől teljesen független konfiguráció egy
    // harmadik réteghez (pl. buborékok). Ugyanazokkal a tulajdonságokkal állítható, de a
    // két osztály értékei egymástól elkülönítve szerkeszthetők.
    public class ParticleLayerSettingsBubbles : ParticleLayerSettings
    {
        public ParticleLayerSettingsBubbles()
        {
            Count = 20000;
            Seed = 3003;

            VolumeHalfExtents = new Vector3(5, 5, 5);

            MinSize = 0.003f;
            MaxSize = 0.0095f;

            ColorA = new Color(10, 90, 100, 200);
            ColorB = new Color(25, 120, 90, 200);

            MinDriftSpeed = 0.005f;
            MaxDriftSpeed = 0.03f;

            MinWobbleAmplitude = 0.03f;
            MaxWobbleAmplitude = 0.12f;
            MinWobbleFrequency = 0.5f;
            MaxWobbleFrequency = 2.0f;

            // NonPremultiplied, mert ezek a sötét színek additív módban (color*alpha hozzáadás)
            // gyakorlatilag láthatatlanok lennének; normál alpha blendinggel viszont jól látszik a tónus
            Blend = BlendState.NonPremultiplied;
        }
    }
}
