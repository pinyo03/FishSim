using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FishSim
{
    class Flock
    {
        readonly Fish leader;
        readonly List<Fish> boids;
        readonly float[] noisePhasesX;
        readonly float[] noisePhasesY;
        readonly bool[] targetAhead;   // true = 10m előre, false = 10m hátra
        readonly float[] switchTimers;
        readonly float[] switchIntervals;  // 40..60 mp között random per boid
        readonly Random rng = new Random();
        float noiseTime;

        public const float SpawnRadius = 40.0f;
        public const float MinSpawnGap = 3.5f;

        const float SeparationRadius     = 4.0f;
        const float AlignmentRadius      = 20.0f;
        const float CohesionRadius       = 30.0f;
        const float LeaderRadius         = 40.0f;
        const float HardSepRadius        = 2.4f;
        public const float LeaderExclusionRadius = 4.0f;  // leader körüli "tiltott zóna" sugara
        const float LeaderRestitution    = 0.4f;

        const float SeparationWeight = 2.0f;
        const float AlignmentWeight  = 0.8f;
        const float CohesionWeight   = 1.0f;
        const float LeaderWeight     = 1.5f;
        const float RandomWeight     = 0.1f;

        public Flock(Fish leader, List<Fish> boids)
        {
            this.leader = leader;
            this.boids  = boids;
            var rng = this.rng;
            noisePhasesX    = new float[boids.Count];
            noisePhasesY    = new float[boids.Count];
            targetAhead     = new bool[boids.Count];
            switchTimers    = new float[boids.Count];
            switchIntervals = new float[boids.Count];
            for (int i = 0; i < boids.Count; i++)
            {
                noisePhasesX[i]    = (float)(rng.NextDouble() * MathHelper.TwoPi);
                noisePhasesY[i]    = (float)(rng.NextDouble() * MathHelper.TwoPi);
                targetAhead[i]     = rng.NextDouble() < 0.5;
                switchIntervals[i] = 40f + (float)(rng.NextDouble() * 20f);
                boids[i].SizeScale = 0.7f + (float)(rng.NextDouble() * 0.6f);
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            noiseTime += dt * 0.4f;
            for (int i = 0; i < boids.Count; i++)
            {
                switchTimers[i] += dt;
                if (switchTimers[i] >= switchIntervals[i])
                {
                    switchTimers[i]    = 0f;
                    switchIntervals[i] = 40f + (float)(rng.NextDouble() * 20f);
                    targetAhead[i]     = !targetAhead[i];
                }
            }

            for (int bi = 0; bi < boids.Count; bi++)
            {
                var b = boids[bi];
                b.ctrlW = b.ctrlS = b.ctrlA = b.ctrlD = b.ctrlQ = b.ctrlE = false;

                Vector3 sepVec   = Vector3.Zero;
                Vector3 alignSum = Vector3.Zero;
                Vector3 cohSum   = Vector3.Zero;
                int alignCount = 0, cohCount = 0;

                for (int j = 0; j < boids.Count; j++)
                {
                    if (j == bi) continue;
                    var other = boids[j];
                    Vector3 delta = b.Position - other.Position;
                    float dist = delta.Length();

                    if (dist < SeparationRadius && dist > 0.001f)
                        sepVec += Vector3.Normalize(delta) / Math.Max(dist * dist, 0.25f);

                    if (dist < AlignmentRadius)
                    {
                        alignSum += Vector3.Normalize(other.Direction);
                        alignCount++;
                    }

                    if (dist < CohesionRadius)
                    {
                        cohSum += other.Position;
                        cohCount++;
                    }
                }

                // Leader: szétválás, igazodás, kohézió
                {
                    Vector3 delta = b.Position - leader.Position;
                    float dist = delta.Length();
                    if (dist < SeparationRadius && dist > 0.001f)
                        sepVec += Vector3.Normalize(delta) / Math.Max(dist * dist, 0.25f);
                    if (dist < AlignmentRadius)
                    {
                        alignSum += Vector3.Normalize(leader.Direction);
                        alignCount++;
                    }
                }

                // Célpont: 10m előre vagy 10m hátra a leadertől (30mp-enként vált).
                Vector3 lFwd = Vector3.Normalize(leader.Direction);
                Vector3 slot = leader.Position + lFwd * (targetAhead[bi] ? 10f : -10f);

                Vector3 toSlot    = slot - b.Position;
                float   slotDist  = toSlot.Length();
                Vector3 leaderDir = slotDist > 0.001f ? toSlot / slotDist : Vector3.Zero;
                if (slotDist > LeaderRadius) leaderDir *= 2.5f;

                // Fizikai kizárási zóna: tényleges leader pozíció alapján.
                Vector3 toActualLeader = leader.Position - b.Position;
                float   leaderDist     = toActualLeader.Length();
                if (leaderDist < LeaderExclusionRadius)
                {
                    leaderDir = Vector3.Zero;
                    if (leaderDist > 0.001f)
                    {
                        float t2 = 1.0f - leaderDist / LeaderExclusionRadius;
                        sepVec += Vector3.Normalize(b.Position - leader.Position) * t2 * 20.0f;
                    }
                }

                Vector3 sepDir = sepVec.LengthSquared() > 0.0001f
                    ? Vector3.Normalize(sepVec) : Vector3.Zero;

                Vector3 alignDir = Vector3.Zero;
                if (alignCount > 0)
                {
                    var avg = alignSum / alignCount;
                    if (avg.LengthSquared() > 0.0001f) alignDir = Vector3.Normalize(avg);
                }

                Vector3 cohDir = Vector3.Zero;
                if (cohCount > 0)
                {
                    Vector3 toCentre = cohSum / cohCount - b.Position;
                    if (toCentre.LengthSquared() > 0.0001f) cohDir = Vector3.Normalize(toCentre);
                }

                // Per-boid jitter a hal saját right/up tengelyein
                float t = noiseTime;
                Vector3 bRight = Vector3.Normalize(b.Right);
                Vector3 bUp    = Vector3.Normalize(b.Up);
                Vector3 noise  = bRight * MathF.Sin(t * 1.3f + noisePhasesX[bi])
                               + bUp    * MathF.Cos(t * 0.7f + noisePhasesY[bi]);

                Vector3 desired =
                    sepDir    * SeparationWeight +
                    alignDir  * AlignmentWeight  +
                    cohDir    * CohesionWeight   +
                    leaderDir * LeaderWeight     +
                    noise     * RandomWeight;

                // BoidSteerDir: normalizált kívánt irány; ha nulla, maradjon az aktuális előre irány
                b.BoidSteerDir = desired.LengthSquared() > 0.0001f
                    ? Vector3.Normalize(desired)
                    : Vector3.Normalize(b.Direction);
            }
        }

        // Leader kizárási gömb: seabed-collision mintájára tolja ki és pattantja vissza a boidot.
        public void ApplyLeaderExclusion()
        {
            for (int bi = 0; bi < boids.Count; bi++)
            {
                var b = boids[bi];
                Vector3 delta = b.Position - leader.Position;
                float dist = delta.Length();
                if (dist >= LeaderExclusionRadius || dist < 0.001f) continue;

                float penetration = LeaderExclusionRadius - dist;
                Vector3 normal = Vector3.Normalize(delta);  // leader → boid irány

                for (int k = 0; k < b.verlets.Length; k++)
                {
                    // Pozíció korrekció (mindkettőt mozgatjuk, hogy ne injektáljon sebességet)
                    b.verlets[k].Pos  += normal * penetration;
                    b.verlets[k].pPos += normal * penetration;

                    // Visszapattanás: ha a sebességkomponens a leader felé mutat, tükrözzük
                    Vector3 vel = b.verlets[k].Pos - b.verlets[k].pPos;
                    float velTowardLeader = -Vector3.Dot(vel, normal);  // pozitív = leader felé
                    if (velTowardLeader > 0)
                        b.verlets[k].pPos -= normal * velTowardLeader * (1.0f + LeaderRestitution);
                }
            }
        }

        // Kemény pozíció-korrekció ha két hal collision-ellipszoidja átfed.
        public void ApplyHardSeparation()
        {
            int n = boids.Count + 1;
            Fish GetFish(int i) => i < boids.Count ? boids[i] : leader;

            for (int i = 0; i < n - 1; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var a = GetFish(i);
                    var b = GetFish(j);
                    Vector3 delta = a.Position - b.Position;
                    float dist = delta.Length();
                    if (dist >= HardSepRadius || dist < 0.001f) continue;

                    float penetration = (HardSepRadius - dist) * 0.5f;
                    Vector3 push = Vector3.Normalize(delta) * penetration;

                    for (int k = 0; k < a.verlets.Length; k++)
                    {
                        a.verlets[k].Pos  += push;
                        a.verlets[k].pPos += push;
                    }
                    for (int k = 0; k < b.verlets.Length; k++)
                    {
                        b.verlets[k].Pos  -= push;
                        b.verlets[k].pPos -= push;
                    }
                }
            }
        }
    }
}
