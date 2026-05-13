using Microsoft.Xna.Framework;
using System;

namespace FishSim
{
    struct Verlet
    {
        public const float dT = 1f / 60;
        public Vector3 Pos, pPos, Acc, Fric;
        public Verlet(Vector3 pos) { Pos = pPos = pos; Acc = Fric = Vector3.Zero; }
        public Verlet(float x, float y, float z) : this(new Vector3(x, y, z)) { }
        public Vector3 Velocity => (Pos - pPos) * (1 / dT);
        public void Step()
        {
            Vector3 dPos = Pos - pPos + dT * dT * Acc;
            Vector3 f = dT * dT * Fric;
            float C = 1;
            if (Vector3.Dot(dPos, dPos + f) < 0)
                C = -dPos.LengthSquared() / Vector3.Dot(dPos, f);
            Vector3 newPos = Pos + dPos + C * f;
            pPos = Pos;
            Pos = newPos;
            Fric = new Vector3();
        }
        public void AddSqFriction(Vector3 fDir, float fC)
        {
            float length = Vector3.Dot(Velocity, fDir);
            Fric -= fDir * length * Math.Abs(length) * fC;
        }
    }
}
