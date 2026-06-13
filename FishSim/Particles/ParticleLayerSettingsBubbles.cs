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
            Count = 50000;
            Seed = 3003;

            VolumeHalfExtents = new Vector3(20, 20, 20);

            MinSize = 0.0095f;
            MaxSize = 0.028f;

            ColorA = new Color(10, 90, 100, 100);
            ColorB = new Color(25, 120, 90, 100);

            MinDriftSpeed = 0.03f;
            MaxDriftSpeed = 1.0f;

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
