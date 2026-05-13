using Microsoft.Xna.Framework;

namespace FishSim
{
    class Body
    {
        public Verlet[] verlets;
        protected int[] cPairs;
        protected float[] cLengths;
        protected bool cPrevDir = false;
        protected int cCycles = 10;

        public Body() { }
        public Body(Body rb)
        {
            verlets = (Verlet[])rb.verlets.Clone();
            cPairs = (int[])rb.cPairs.Clone();
            cLengths = (float[])rb.cLengths.Clone();
            cCycles = rb.cCycles;
        }

        public static void ApplyLengthConstraint(ref Verlet v1, ref Verlet v2, float length)
        {
            Vector3 dPos = v2.Pos - v1.Pos;
            float dLength = dPos.Length();
            Vector3 correction = (dLength - length) / dLength * 0.5f * dPos;
            v1.Pos += correction; v2.Pos -= correction;
        }
        public void GenerateFullyConnectedBody()
        {
            cLengths = new float[(verlets.Length - 1) * verlets.Length / 2];
            cPairs = new int[cLengths.Length * 2];
            int idx = 0;
            for (int i = 0; i < verlets.Length - 1; i++)
                for (int j = i + 1; j < verlets.Length; j++, idx++)
                {
                    cPairs[2 * idx] = i;
                    cPairs[2 * idx + 1] = j;
                    cLengths[idx] = (verlets[i].Pos - verlets[j].Pos).Length();
                }
        }
        public void ApplyConstraints()
        {
            int i = cPrevDir ? cLengths.Length - 1 : 0;
            int dir = cPrevDir ? -1 : 1;
            for (int cycle = 0; cycle < cCycles; cycle++)
            {
                for (; i < cLengths.Length && i >= 0; i += dir)
                    ApplyLengthConstraint(ref verlets[cPairs[2 * i]], ref verlets[cPairs[2 * i + 1]], cLengths[i]);
                dir = -dir; i += dir;
            }
            cPrevDir = !cPrevDir;
        }
    }
}
