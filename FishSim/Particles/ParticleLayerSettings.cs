using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FishSim.Particles
{
    public class ParticleLayerSettings
    {
        public int Count = 20000;
        public int Seed = 1234;

        // Spawn-doboz: a kamera köré relatív, AABB félméretek
        public Vector3 VolumeHalfExtents = new Vector3(200, 100, 200);

        // Méret-tartomány (a billboard fél-szélessége világegységben)
        public float MinSize = 0.05f;
        public float MaxSize = 0.195f;

        // Két szín, amik között a particle-ek színe random interpolál
        public Color ColorA = new Color(10, 90, 100, 90);
        public Color ColorB = new Color(255, 255, 255, 50);

        // Felfelé driftelés sebesség-tartománya (egység/mp)
        public float MinDriftSpeed = 0.01f;
        public float MaxDriftSpeed = 0.95f;

        // Oldalirányú "lebegés" (szinusz-hullám alapú wobble)
        public float MinWobbleAmplitude = 0.02f;
        public float MaxWobbleAmplitude = 0.2f;
        public float MinWobbleFrequency = 0.2f;
        public float MaxWobbleFrequency = 2.2f;

        // Non-premultiplied alpha blend, mert a vertex-színek nincsenek premultiplikálva
        public BlendState Blend = BlendState.NonPremultiplied;
    }
}
