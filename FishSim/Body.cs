using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FishSim
{
    class Body
    {
        public Verlet[] verlets;
        protected List<int> cPairs = new List<int>();
        protected List<float> cLengths = new List<float>();
        protected List<float> cStiffness = new List<float>();
        protected bool cPrevDir = false;
        protected int cCycles = 10;

        public Body() { }
        public Body(Body rb)
        {
            verlets = (Verlet[])rb.verlets.Clone();
            cPairs = new List<int>(rb.cPairs);
            cLengths = new List<float>(rb.cLengths);
            cStiffness = new List<float>(rb.cStiffness);
            cCycles = rb.cCycles;
        }

        public static void ApplyLengthConstraint(ref Verlet v1, ref Verlet v2, float length, float stiffness = 1f)
        {
            Vector3 dPos = v2.Pos - v1.Pos;
            float dLength = dPos.Length();
            Vector3 correction = (dLength - length) / dLength * 0.5f * stiffness * dPos;
            v1.Pos += correction; v2.Pos -= correction;
        }
        // Hozzaad egy constraint-et a ket index kozott. Ha length nincs megadva, a jelenlegi tavolsagot hasznalja.
        public void AddConstraint(int a, int b, float? length = null, float stiffness = 1f)
        {
            cPairs.Add(a);
            cPairs.Add(b);
            cLengths.Add(length ?? (verlets[a].Pos - verlets[b].Pos).Length());
            cStiffness.Add(stiffness);
        }
        public void GenerateFullyConnectedBody()
        {
            cPairs.Clear(); cLengths.Clear(); cStiffness.Clear();
            for (int i = 0; i < verlets.Length - 1; i++)
                for (int j = i + 1; j < verlets.Length; j++)
                    AddConstraint(i, j);
        }
        public void ApplyConstraints()
        {
            int i = cPrevDir ? cLengths.Count - 1 : 0;
            int dir = cPrevDir ? -1 : 1;
            for (int cycle = 0; cycle < cCycles; cycle++)
            {
                for (; i < cLengths.Count && i >= 0; i += dir)
                    ApplyLengthConstraint(ref verlets[cPairs[2 * i]], ref verlets[cPairs[2 * i + 1]], cLengths[i], cStiffness[i]);
                dir = -dir; i += dir;
            }
            cPrevDir = !cPrevDir;
        }
    }
}
