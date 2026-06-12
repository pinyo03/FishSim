using System;

namespace FishSim
{
    // Seedelt klasszikus (Ken Perlin-féle) 2D gradiens zaj, fBm összegzéssel.
    public class PerlinNoise
    {
        private readonly int[] permutation;

        public PerlinNoise(int seed)
        {
            var rand = new Random(seed);
            var p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;
            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                (p[i], p[j]) = (p[j], p[i]);
            }

            permutation = new int[512];
            for (int i = 0; i < 512; i++) permutation[i] = p[i & 255];
        }

        public float Noise(float x, float y)
        {
            int xi = (int)MathF.Floor(x) & 255;
            int yi = (int)MathF.Floor(y) & 255;
            float xf = x - MathF.Floor(x);
            float yf = y - MathF.Floor(y);

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = permutation[permutation[xi] + yi];
            int ab = permutation[permutation[xi] + yi + 1];
            int ba = permutation[permutation[xi + 1] + yi];
            int bb = permutation[permutation[xi + 1] + yi + 1];

            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        // Fractal Brownian Motion: több octave-nyi zaj összegzése csökkenő amplitúdóval, növekvő frekvenciával.
        public float Fbm(float x, float y, int octaves, float persistence, float lacunarity)
        {
            float total = 0f;
            float amplitude = 1f;
            float maxAmplitude = 0f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency) * amplitude;
                maxAmplitude += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxAmplitude;
        }

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
