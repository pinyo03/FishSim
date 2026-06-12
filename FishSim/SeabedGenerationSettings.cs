namespace FishSim
{
    // A procedurálisan generált seabed-rács paraméterei (fBm Perlin zaj alapján).
    public class SeabedGenerationSettings
    {
        // A rács szélessége/magassága vertexben (Resolution x Resolution).
        public int Resolution = 1024;

        public int Seed = 12345;

        // Az fBm octave-jainak száma (rétegek, ahol mindegyik finomabb részletet ad).
        public int Octaves = 4;

        // A zaj alapfrekvenciája a [0,1] domainen - kisebb érték = nagyobb, lankásabb dűnék.
        public float Frequency = 0.01f;

        // Amplitúdó-szorzó octave-onként (0-1 között) - kisebb érték = simább felszín.
        public float Persistence = 0.5f;

        // Frekvencia-szorzó octave-onként.
        public float Lacunarity = 2f;

        // A végső, [0,1]-re normalizált magasság szorzója.
        public float HeightScale = 1f;

        // A diffúz textúra (homok) fényessége mennyire mozdítsa el a magasságot - nagyobb érték = jól látható domborzat a színből.
        public float ColorHeightInfluence = 0.5f;
    }
}
